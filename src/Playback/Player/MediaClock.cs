namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    private bool _requiresMediaTick;
    private int _mediaTickObservers;
    internal bool RequiresMediaTick { get => _requiresMediaTick || _mediaTickObservers > 0; set => _requiresMediaTick = value; }

    internal IDisposable ObserveMediaTick(Action action)
    {
        SamplingTick += action; _mediaTickObservers++; StateChanged?.Invoke();
        return new MediaSubscription(this, action);
    }
    private sealed class MediaSubscription(Player player, Action action) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            player.SamplingTick -= action; player._mediaTickObservers--;
            if (!player._disposed) player.StateChanged?.Invoke();
        }
    }
}
