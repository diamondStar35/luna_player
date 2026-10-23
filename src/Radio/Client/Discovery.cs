using System.Net;
using System.Net.Sockets;

namespace LunaPlayer.Radio.Client;

internal sealed partial class Browser
{
    private sealed record Servers(Uri[] Addresses, DateTimeOffset Expires);
    private readonly SemaphoreSlim _discovery = new(1, 1);
    private Servers? _servers;

    private async Task<Uri[]> GetServersAsync(CancellationToken token)
    {
        if (_options.Server is Uri configured)
            return [new Uri(configured.AbsoluteUri.TrimEnd('/') + "/")];
        if (Volatile.Read(ref _servers) is { } cached && cached.Expires > DateTimeOffset.UtcNow)
            return cached.Addresses;
        await _discovery.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (_servers is { } current && current.Expires > DateTimeOffset.UtcNow)
                return current.Addresses;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(_options.RequestTimeout);
            var addresses = await Dns.GetHostAddressesAsync("all.api.radio-browser.info", timeout.Token).ConfigureAwait(false);
            var mirrors = new List<Uri>();
            // PTR names provide HTTPS certificate names; connecting to the bare addresses would not.
            // Limit reverse lookups in flight, rather than pinging every server from a constructor.
            foreach (var batch in addresses.Distinct().Chunk(8))
            {
                Uri?[] results;
                try
                {
                    results = await Task.WhenAll(batch.Select(address => ResolveAsync(address, timeout.Token)))
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested && mirrors.Count > 0)
                {
                    // Keep names from completed batches if later DNS lookups use the remaining budget.
                    break;
                }
                foreach (var uri in results)
                    if (uri is not null && !mirrors.Contains(uri))
                        mirrors.Add(uri);
            }
            if (mirrors.Count == 0)
                throw new RequestException(FailureKind.Discovery);
            var servers = mirrors.ToArray();
            Random.Shared.Shuffle(servers);
            Volatile.Write(ref _servers, new Servers(servers, DateTimeOffset.UtcNow + _options.DiscoveryCacheDuration));
            return servers;
        }
        catch (SocketException exception)
        {
            throw new RequestException(FailureKind.Discovery, innerException: exception);
        }
        catch (OperationCanceledException exception) when (!token.IsCancellationRequested)
        {
            throw new RequestException(FailureKind.Timeout, innerException: exception);
        }
        finally
        {
            _discovery.Release();
        }
    }

    private static async Task<Uri?> ResolveAsync(IPAddress address, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            var entry = await Dns.GetHostEntryAsync(address.ToString(), AddressFamily.Unspecified, timeout.Token)
                .ConfigureAwait(false);
            var host = entry.HostName.TrimEnd('.');
            return Uri.CheckHostName(host) == UriHostNameType.Dns ? new Uri($"https://{host}/") : null;
        }
        catch (SocketException)
        {
            // A single missing PTR record does not invalidate the other discovered mirrors.
            return null;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            // One unresponsive reverse lookup should not hide the mirrors that resolved successfully.
            return null;
        }
    }
}
