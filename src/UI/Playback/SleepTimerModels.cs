using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer.UI.Playback;

/// <summary>What the sleep-timer dialog was asked to set.</summary>
internal readonly record struct SleepTimerRequest(
    SleepTimerMode Mode, int DurationMinutes, SleepTimerAction Action, bool Fade, int FadeSeconds);

/// <summary>The dialog's answer: either turn an existing timer off, or arm the carried request. A null result
/// (the window was cancelled) means nothing should change.</summary>
internal readonly record struct SleepTimerDialogResult(bool TurnOff, SleepTimerRequest Request);
