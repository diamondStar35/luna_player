namespace LunaPlayer.Spotify.Playback;

internal sealed partial class Sessions
{
    private int _loadAttempt;
    private ReportedFailure? _reportedFailure;
    private sealed record ReportedFailure(Exception Error, Track Track, int Generation, int Attempt, long Revision);

    private void ReportFailure(Exception error)
    {
        if (!_active || !_player.InSpotifySession || _current is not { } track) return;
        var generation = CurrentLoadGeneration;
        var revision = _player.PlaybackRevision;
        if (_reportedFailure is { } previous && previous.Generation == generation
            && previous.Attempt == _loadAttempt && previous.Revision == revision) return;
        _reportedFailure = new ReportedFailure(error, track, generation, _loadAttempt, revision);
        Diagnostics.Write("failure.reported", _current?.Uri,
            $"generation={generation} attempt={_loadAttempt} revision={revision}", error);
        CommandFailed?.Invoke(error);
    }

    // The dialog is posted separately from the failed operation. Only that exact operation
    // may interrupt playback; a subsequent successful load invalidates its pending dialog.
    internal bool TakeCurrentFailure(Exception error)
    {
        if (_reportedFailure is not { } failure || !ReferenceEquals(failure.Error, error)) return false;
        _reportedFailure = null;
        var current = _active && _player.InSpotifySession && failure.Generation == CurrentLoadGeneration
            && ReferenceEquals(failure.Track, _current) && failure.Attempt == _loadAttempt
            && failure.Revision == _player.PlaybackRevision;
        Diagnostics.Write(current ? "failure.present" : "failure.stale", _current?.Uri, error: error);
        return current;
    }

    private bool OwnsLoad(string uri, int generation, int attempt, long revision, CancellationToken token) =>
        !token.IsCancellationRequested && _active && _player.InSpotifySession && _current?.Uri == uri
        && generation == CurrentLoadGeneration && attempt == _loadAttempt && revision == _player.PlaybackRevision;
}
