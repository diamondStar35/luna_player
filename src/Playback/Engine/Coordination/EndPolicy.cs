using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
namespace LunaPlayer.Playback;
using LunaPlayer.Media;
internal sealed partial class PlaybackCoordinator
{

    // mpv owns both halves of this: keep-open leaves a finished file loaded so it can still be seeked,
    // and loop-file repeats it without the gap a reload would leave. The managed end-of-file handler still
    // runs for the advance case, and as a fallback if either property is unavailable.
    public void SetEndBehavior(EndBehavior behavior)
    {
        _endBehavior = behavior;
        if (behavior != EndBehavior.Advance)
            CancelEndFade();
        if (!_selectionRangeActive && !_endBehaviorRestorePending)
            ApplyEndBehavior();
    }

    public void SetRepeatFile(bool enabled)
    {
        _repeatFile = enabled;
        if (enabled)
            CancelEndFade();
    }

    private void CancelEndFade(bool restoreVolume = true)
    {
        if (!_endFadeActive)
            return;
        _endFadeActive = false;
        if (restoreVolume)
            FadeVolume(1, _fadeIntervalMilliseconds);
    }

    private void ApplyEndBehavior()
    {
        _session.SetPropertySafely("keep-open", _chapterStopEnd.HasValue || _holdAutomaticAdvance
            || _endBehavior == EndBehavior.None ? "yes" : "no");
        _session.SetPropertySafely("loop-file", !_holdAutomaticAdvance && _playbackRange is null && _chapterStopEnd is null && _endBehavior == EndBehavior.Loop ? "inf" : "no");
    }

}
