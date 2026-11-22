namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    private readonly HashSet<string> _unavailableMedia = new(StringComparer.Ordinal);
    private bool _mediaForward = true;
    internal event Action<string>? MediaUnavailable;
    internal bool IsMediaUnavailable(string path) => _unavailableMedia.Contains(path);
    internal void RetryServerMedia(Guid server)
        => _unavailableMedia.RemoveWhere(path => Subsonic.Playback.Reference.Parse(path)?.ServerId == server);

    private static bool MissingMedia(Exception error) => error is Subsonic.RequestException request
        && (request.Code == 70 || request.StatusCode == System.Net.HttpStatusCode.NotFound);

    private void MarkUnavailable(string path)
    {
        if (_unavailableMedia.Add(path)) MediaUnavailable?.Invoke(path);
    }

    private bool MoveAvailable(bool forward, bool wrap)
    {
        for (var remaining = Count; remaining > 0; remaining--)
        {
            if (!(forward ? _playlist.MoveNext(wrap) : _playlist.MovePrevious(wrap))) return false;
            if (_playlist.CurrentPath is { } path && !IsMediaUnavailable(path)) return true;
        }
        return false;
    }

    private void MediaLoadFailed(string identity, Exception error, bool forward)
    {
        _resolvingMedia = false;
        if (MissingMedia(error))
        {
            MarkUnavailable(identity);
            if (MoveAvailable(forward, _wrapPlaylist)) { LoadCurrent(fadeTransition: true); return; }
        }
        _engine.Stop(); StateChanged?.Invoke(); MediaResolutionFailed?.Invoke(error);
    }
}
