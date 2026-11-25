using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LunaPlayer.Tests.Infrastructure;

internal sealed class MediaServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentBag<Task> _requests = [];
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);
    private readonly IReadOnlyDictionary<string, byte[]> _files;
    private readonly Task _accept;
    internal Uri Address { get; }

    internal MediaServer(IReadOnlyDictionary<string, byte[]> files)
    {
        _files = files; _listener.Start();
        Address = new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");
        _accept = Accept();
    }
    internal string Url(string path) => new Uri(Address, path).AbsoluteUri;
    internal int Requests(string path) => _counts.GetValueOrDefault(path);

    private async Task Accept()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                _requests.Add(Serve(client));
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (SocketException) when (_stop.IsCancellationRequested) { }
    }

    private async Task Serve(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                var request = await reader.ReadLineAsync(_stop.Token);
                if (request is null) return;
                var parts = request.Split(' ');
                var path = new Uri(Address, parts[1]).AbsolutePath.TrimStart('/');
                var start = 0;
                while (await reader.ReadLineAsync(_stop.Token) is { Length: > 0 } header)
                    if (header.StartsWith("Range: bytes=", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(header[13..].Split('-')[0], out start);
                if (!_files.TryGetValue(path, out var bytes))
                {
                    await stream.WriteAsync("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(), _stop.Token);
                    return;
                }
                if (parts[0] == "GET") _counts.AddOrUpdate(path, 1, (_, count) => count + 1);
                start = Math.Clamp(start, 0, bytes.Length);
                var partial = start > 0;
                var headers = $"HTTP/1.1 {(partial ? "206 Partial Content" : "200 OK")}\r\nContent-Type: audio/wav\r\n"
                    + $"Content-Length: {bytes.Length - start}\r\nAccept-Ranges: bytes\r\nConnection: close\r\n"
                    + (partial ? $"Content-Range: bytes {start}-{bytes.Length - 1}/{bytes.Length}\r\n" : "") + "\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(headers), _stop.Token);
                if (parts[0] != "HEAD") await stream.WriteAsync(bytes.AsMemory(start), _stop.Token);
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
            catch (IOException)
            {
                // A decoder or metadata probe may close its connection before reading the body.
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel(); _listener.Stop(); await _accept;
        await Task.WhenAll(_requests); _stop.Dispose();
    }
}
