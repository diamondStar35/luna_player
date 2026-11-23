using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Servers;

internal sealed record Inspection(Capabilities Capabilities, MusicFolder[] Folders, User? User, string Username);

internal sealed partial class Connections
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<Guid, (Profile Profile, DateTimeOffset Created, Task<Inspection> Request)> _inspections = [];
    private bool _disposed;

    internal Task<Inspection> InspectAsync(Guid server, bool refresh = false, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var profile = Profile(server);
        lock (_clients)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!refresh && _inspections.TryGetValue(server, out var previous) && previous.Profile == profile
                && !previous.Request.IsFaulted && !previous.Request.IsCanceled
                && DateTimeOffset.UtcNow - previous.Created < TimeSpan.FromMinutes(5))
                return previous.Request.WaitAsync(token);
            var client = Client(server);
            // A caller leaving a view cancels only its wait. Other views can reuse the same
            // inspection; disposal of the connection service cancels the actual requests.
            var lifetime = _lifetime.Token;
            var request = Task.Run(() => Inspect(profile, client, refresh, lifetime), lifetime);
            // The cache still owns this request after all callers cancel their waits.
            // Observe failures without changing what an active caller receives.
            _ = request.ContinueWith(completed => { _ = completed.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            _inspections[server] = (profile, DateTimeOffset.UtcNow, request);
            return request.WaitAsync(token);
        }
    }

    private static async Task<Inspection> Inspect(Profile profile, Client client, bool refresh, CancellationToken token)
    {
        var capabilities = client.DiscoverAsync(refresh, token);
        var folders = client.Library.GetMusicFoldersAsync(token);
        var account = ReadUser();
        await Task.WhenAll(capabilities, folders, account).ConfigureAwait(false);
        var (user, username) = await account.ConfigureAwait(false);
        return new(await capabilities.ConfigureAwait(false), await folders.ConfigureAwait(false), user, user?.Username ?? username);

        async Task<(User? User, string Username)> ReadUser()
        {
            var username = profile.Credentials.Username;
            try
            {
                if (profile.Credentials.Kind == AuthenticationKind.ApiKey)
                    username = (await client.GetTokenInfoAsync(token).ConfigureAwait(false)).Username;
                return (await client.GetUserAsync(username, token).ConfigureAwait(false), username);
            }
            catch (RequestException error) when (error.Kind is FailureKind.Unsupported or FailureKind.Forbidden)
            {
                // Role introspection is optional. Actual operations still use server authorization.
                return (null, username);
            }
        }
    }
}
