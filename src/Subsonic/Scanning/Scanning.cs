using System.Runtime.CompilerServices;
using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic.Scanning;

internal sealed class Scanning(Session session) : Feature(session)
{
    internal async Task<ScanStatus> GetStatusAsync(CancellationToken token = default)
        => Required((await Session.ReadAsync("getScanStatus", token: token).ConfigureAwait(false)).ScanStatus, "getScanStatus");
    internal async Task<ScanStatus> StartAsync(bool? fullScan = null, CancellationToken token = default)
        => Required((await Session.ReadAsync("startScan", new Parameters().With("fullScan", fullScan), token, mutation: true).ConfigureAwait(false)).ScanStatus, "startScan");
    /// <summary>Observes an existing scan. Cancelling enumeration does not cancel work on the server.</summary>
    internal async IAsyncEnumerable<ScanStatus> WatchAsync(TimeSpan? interval = null,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var delay = interval ?? TimeSpan.FromMilliseconds(1500);
        if (delay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
        while (true)
        {
            var status = await GetStatusAsync(token).ConfigureAwait(false);
            yield return status;
            if (!status.Scanning) yield break;
            await Task.Delay(delay, Session.Clock, token).ConfigureAwait(false);
        }
    }
}
