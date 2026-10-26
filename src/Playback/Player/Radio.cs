namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    private bool _radioSession;
    internal bool InRadioSession => _radioSession;

    /// <summary>Capture the old playlist before any radio preparation can stop its engine.</summary>
    internal void BeginRadioSession()
    {
        if (_spotifySession) LeaveSession();
        EnterSession();
        _radioSession = true;
        _engine.ClearQueuedNext();
    }

    /// <summary>Retains visited stations, while the radio session owns their browsing order.</summary>
    internal bool PlayRadioStation(string url, string title, string source, int? existing, out int entry)
    {
        BeginRadioSession();
        if (existing is int index && index >= 0 && index < _playlist.Count
            && _playlist.Items[index].Path == url)
            _playlist.GoToIndex(index);
        else if (!_playlist.Append(url, jump: true))
        {
            entry = -1;
            return false;
        }
        entry = _playlist.CurrentIndex;
        Describe(url, title, source, null, audioOnly: true);
        return LoadCurrent(fadeTransition: _loaded);
    }

    internal void DiscardRadioSession()
    {
        if (!_radioSession) return;
        _radioSession = false;
        _suspendedPlaylist = null;
    }
}
