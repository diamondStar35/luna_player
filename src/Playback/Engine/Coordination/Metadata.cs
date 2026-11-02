namespace LunaPlayer.Playback;
internal sealed partial class PlaybackCoordinator
{
    public string? MediaTitle
    {
        get
        {
            lock (_loadSync)
                return _disposed || !_loaded || _awaitingLoadRestart ? null : _metadata.Title;
        }
    }
    public Metadata GetMetadata()
    {
        lock (_loadSync)
            return _disposed || !_loaded || _awaitingLoadRestart ? Metadata.None : _metadata.Read();
    }
}
