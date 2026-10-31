using MpvNet;

namespace LunaPlayer.Playback;

internal sealed partial class MpvSession
{
    private readonly WorkQueue _work;
    internal event Action<AudioFilter>? FilterFailed { add => _work.Failed += value; remove => _work.Failed -= value; }

    /// <summary>Reports acceptance into the native queue; the caller never waits for mpv's core.</summary>
    internal bool TryDo(Action<MPV> action, Action<MPV>? fallback = null)
        => _work.Post(() => TryDoNow(action) || (fallback is not null && TryDoNow(fallback)));

    internal bool QueueFilter(AudioFilter filter, Func<bool> apply, bool replay = false) => _work.Post(apply, filter, replay);

    // Used only by the background queue. Filter success is learned here rather than guessed
    // from a temporary graph or a request accepted by the UI.
    internal bool TryDoNow(Action<MPV> action)
    {
        try { action(_mpv); return true; }
        catch (Exception exception) when (IsFailure(exception))
        {
            Console.Error.WriteLine(exception.Message);
            return false;
        }
    }

    internal void SetPropertySafely(string name, object value) => TrySetProperty(name, value);
    internal bool TrySetProperty(string name, object value)
    {
        var accepted = TryDo(mpv => mpv.SetProperty(name, value));
        if (accepted) CacheProperty(name, value);
        return accepted;
    }
    internal void SetFade(double level) => TryDo(mpv => mpv.FadeVolume(level, 0));

    internal bool AddFilter(string filter) => TryDoNow(mpv => mpv.Command("af", "add", filter));
    internal void RemoveFilter(string label) => TryDoNow(mpv => mpv.Command("af", "remove", label));
}
