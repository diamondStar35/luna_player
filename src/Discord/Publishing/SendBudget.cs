namespace LunaPlayer.Discord.Publishing;

/// <summary>Allows short bursts for state changes instead of imposing a delay on every update.</summary>
internal sealed class SendBudget(int limit = 5, TimeSpan? window = null)
{
    private readonly Queue<TimeSpan> _attempts = new();
    private readonly TimeSpan _window = window ?? TimeSpan.FromSeconds(20);

    internal TimeSpan Delay(TimeSpan now, bool stateChanged)
    {
        while (_attempts.TryPeek(out var first) && now - first >= _window) _attempts.Dequeue();
        // Reserve one slot for a pause, resume or new item when timing or artwork updates are busy.
        var available = stateChanged ? limit : Math.Max(1, limit - 1);
        return _attempts.Count < available ? TimeSpan.Zero : _attempts.Peek() + _window - now;
    }

    internal void Record(TimeSpan now) => _attempts.Enqueue(now);
}
