using System.Globalization;
using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer.UI.Audio;

internal sealed class PlaybackPreferences : LunaPlayer.UI.Preferences.PreferencesPage
{
    private readonly AudioSettings _settings;
    private readonly CheckBox _normalize;
    private readonly CheckBox _mono;
    private readonly SpinCtrl _fadeInterval;
    private readonly CheckBox _fadeSeeking;
    private readonly StaticText _seekFadeLabel;
    private readonly TextCtrl _seekFadeInterval;
    private readonly CheckBox _fadeFileTransitions;
    private readonly CheckBox _fadePauseResume;
    private readonly Choice _gapless;
    private readonly CheckBox _crossfade;
    private readonly StaticText _crossfadeLabel;
    private readonly SpinCtrl _crossfadeInterval;
    private readonly StaticText _curveLabel;
    private readonly Choice _crossfadeCurve;
    private readonly CheckBox _crossfadeManual;

    internal PlaybackPreferences(Window parent, AudioSettings settings)
        // Translators: Spoken description of the Playback page inside Audio settings.
        : base(new ScrolledWindow(parent), Tr("Move through this page with Tab or Shift+Tab. Press F1 while a control is focused to hear what it changes."))
    {
        _settings = settings;
        var panel = (ScrolledWindow)Window;
        panel.SetScrollRate(8, 8);
        // Translators: Tick box that evens out audio loudness and holds back the loudest peaks.
        _normalize = new CheckBox(panel, label: Tr("Enable dynamic normalize and limiter")) { Checked = settings.NormalizeAudio };
        // Translators: Tick box that plays the left and right audio channels mixed together.
        _mono = new CheckBox(panel, label: Tr("Play audio as Mono")) { Checked = settings.MonoAudio };
        // Translators: Label of the duration used to fade audio when playback starts, stops, pauses, or resumes.
        var fadeIntervalLabel = new StaticText(panel, label: Tr("Volume fading interval (milliseconds)"));
        _fadeInterval = new SpinCtrl(panel, settings.FadeIntervalMilliseconds, 0,
            AudioSettings.MaximumFadeIntervalMilliseconds);
        // Translators: Tick box that fades audio down and up around a seek operation.
        _fadeSeeking = new CheckBox(panel, label: Tr("Enable volume fading during seeking")) { Checked = settings.FadeSeeking };
        // Translators: Label of the duration used to fade audio around a seek operation.
        _seekFadeLabel = new StaticText(panel, label: Tr("Seek fading interval (milliseconds)"));
        _seekFadeInterval = new TextCtrl(panel,
            value: settings.SeekFadeIntervalMilliseconds.ToString(CultureInfo.InvariantCulture));
        // Translators: Tick box that fades audio when moving from one file to another.
        _fadeFileTransitions = new CheckBox(panel, label: Tr("Enable volume fading at the start or end of a file")) { Checked = settings.FadeFileTransitions };
        // Translators: Tick box that fades audio when playback is paused or resumed.
        _fadePauseResume = new CheckBox(panel, label: Tr("Enable volume fading when pausing or resuming playback")) { Checked = settings.FadePauseResume };
        // Translators: Label of the list that controls uninterrupted playback between adjacent files.
        var gaplessLabel = new StaticText(panel, label: Tr("Gapless playback mode"));
        _gapless = Choice(panel, [
            // Translators: Gapless playback is turned off.
            Tr("Disabled"),
            // Translators: Use gapless playback only when adjacent files have compatible audio formats.
            Tr("Compatible files"),
            // Translators: Ask the player to preserve gapless playback even when it must adapt differing audio formats.
            Tr("Always when possible")], (int)settings.GaplessPlayback);
        // Translators: Checkbox on Audio > Playback that overlaps adjacent audio files. Files with video are excluded.
        _crossfade = new CheckBox(panel, label: Tr("Enable cross-fading for audio files")) { Checked = settings.Crossfade };
        // Translators: Label on Audio > Playback for how long two audio files overlap, measured in milliseconds.
        _crossfadeLabel = new StaticText(panel, label: Tr("Cross-fading duration (milliseconds)"));
        _crossfadeInterval = new SpinCtrl(panel, settings.CrossfadeMilliseconds, 1,
            AudioSettings.MaximumCrossfadeMilliseconds);
        // Translators: Label on Audio > Playback for the shape of the volume changes during an audio overlap.
        _curveLabel = new StaticText(panel, label: Tr("Cross-fading curve"));
        _crossfadeCurve = Choice(panel, [
            // Translators: Cross-fading curve on Audio > Playback that maintains perceived loudness while unrelated sounds overlap.
            Tr("Equal-power"),
            // Translators: Cross-fading curve on Audio > Playback that changes the two files' gains at a constant rate.
            Tr("Linear")], (int)settings.CrossfadeCurve);
        // Translators: Checkbox on Audio > Playback that also overlaps audio when the user navigates to another file.
        _crossfadeManual = new CheckBox(panel, label: Tr("Enable cross-fading during manual navigation"))
            { Checked = settings.CrossfadeManualNavigation };

        var sizer = new BoxSizer(Orientation.Vertical);
        AddCheck(sizer, _normalize);
        AddCheck(sizer, _mono);
        AddField(sizer, fadeIntervalLabel, _fadeInterval);
        AddCheck(sizer, _fadeSeeking);
        AddField(sizer, _seekFadeLabel, _seekFadeInterval);
        AddCheck(sizer, _fadeFileTransitions);
        AddCheck(sizer, _fadePauseResume);
        AddField(sizer, gaplessLabel, _gapless);
        AddCheck(sizer, _crossfade);
        AddField(sizer, _crossfadeLabel, _crossfadeInterval);
        AddField(sizer, _curveLabel, _crossfadeCurve);
        AddCheck(sizer, _crossfadeManual);
        panel.SetSizer(sizer);
        _fadeSeeking.Toggled += (_, _) => ShowSeekInterval();
        ShowSeekInterval();
        _crossfade.Toggled += (_, _) => ShowCrossfadeSettings();
        ShowCrossfadeSettings();

        Help(_normalize,
            // Translators: Help text for the tick box that evens out loudness and holds back loud peaks.
            Tr("Evens out changing loudness and restrains the loudest peaks to reduce clipping. Clear this option to hear the source without those filters."));
        Help(_mono,
            // Translators: Help text for the tick box that mixes the left and right channels together.
            Tr("Combines the left and right channels into one mono output. Clear this option to preserve the source channel layout."));
        Help(_fadeInterval,
            // Translators: Help text for the normal playback fade duration.
            Tr("Sets how long playback fades take. Zero disables these fades even when a related option is selected. Seek fading uses its own interval."));
        Help(_fadeSeeking,
            // Translators: Help text for the option that fades sound around seeking.
            Tr("Fades the sound out before moving and back in after playback restarts at the new position."));
        Help(_seekFadeInterval,
            // Translators: Help text for the seek fade duration.
            Tr("Sets the duration of each fade around a seek, from 1 to 10000 milliseconds."));
        Help(_fadeFileTransitions,
            // Translators: Help text for fading between files without mixing the files together.
            Tr("Fades out as a file reaches its end and fades in when another file starts, including after manual navigation. This does not mix two files together."));
        Help(_fadePauseResume,
            // Translators: Help text for fading when playback is paused or resumed.
            Tr("Fades out before pausing and fades in after playback resumes."));
        Help(_gapless,
            // Translators: Help text for gapless playback modes.
            Tr("The Compatible files mode preserves the original audio format. Always when possible may adapt later files to the first file's output format. Online streams use gapless playback only when the next stream is ready."));
        Help(_crossfade,
            // Translators: Context help for the cross-fading checkbox on Audio > Playback.
            Tr("Overlaps the end of one audio file with the beginning of the next. Files containing video are excluded. Cross-fading takes precedence over ordinary file-transition fading and works independently of the gapless setting."));
        Help(_crossfadeInterval,
            // Translators: Context help for the cross-fading duration on Audio > Playback.
            Tr("Sets the overlap from 1 to 30000 milliseconds. The default is 3000. Short files or a stream that is not ready may produce a shorter overlap. Longer overlaps require more decoded audio buffering."));
        Help(_crossfadeCurve,
            // Translators: Context help for the cross-fading curve on Audio > Playback.
            Tr("Equal-power maintains perceived loudness while the files overlap. Linear changes their gains at a constant rate. Equal-power can produce higher peaks."));
        Help(_crossfadeManual,
            // Translators: Context help for cross-fading during manual navigation on Audio > Playback.
            Tr("Also cross-fades when you move to another audio file with navigation commands. This is off by default. Navigation while paused opens the destination without an overlap."));
    }

    public override void Apply()
    {
        _settings.NormalizeAudio = _normalize.Checked;
        _settings.MonoAudio = _mono.Checked;
        _settings.FadeIntervalMilliseconds = _fadeInterval.Value;
        _settings.FadeSeeking = _fadeSeeking.Checked;
        _settings.SeekFadeIntervalMilliseconds = int.Parse(
            _seekFadeInterval.Value.Trim(), CultureInfo.InvariantCulture);
        _settings.FadeFileTransitions = _fadeFileTransitions.Checked;
        _settings.FadePauseResume = _fadePauseResume.Checked;
        _settings.GaplessPlayback = (GaplessPlaybackMode)Math.Max(0, _gapless.SelectedIndex);
        _settings.Crossfade = _crossfade.Checked;
        _settings.CrossfadeMilliseconds = _crossfadeInterval.Value;
        _settings.CrossfadeCurve = (CrossfadeCurve)Math.Max(0, _crossfadeCurve.SelectedIndex);
        _settings.CrossfadeManualNavigation = _crossfadeManual.Checked;
    }

    public override void Refresh()
    {
        _normalize.Checked = _settings.NormalizeAudio;
        _mono.Checked = _settings.MonoAudio;
        _fadeInterval.Value = _settings.FadeIntervalMilliseconds;
        _fadeSeeking.Checked = _settings.FadeSeeking;
        _seekFadeInterval.Value = _settings.SeekFadeIntervalMilliseconds.ToString(CultureInfo.InvariantCulture);
        _fadeFileTransitions.Checked = _settings.FadeFileTransitions;
        _fadePauseResume.Checked = _settings.FadePauseResume;
        _gapless.SelectedIndex = (int)_settings.GaplessPlayback;
        _crossfade.Checked = _settings.Crossfade;
        _crossfadeInterval.Value = _settings.CrossfadeMilliseconds;
        _crossfadeCurve.SelectedIndex = (int)_settings.CrossfadeCurve;
        _crossfadeManual.Checked = _settings.CrossfadeManualNavigation;
        ShowSeekInterval();
        ShowCrossfadeSettings();
    }

    public override string? Validate()
    {
        if (!int.TryParse(_seekFadeInterval.Value.Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var interval)
            || interval < 1 || interval > AudioSettings.MaximumFadeIntervalMilliseconds)
            // Translators: Error shown when the seek fade duration is not a whole number from 1 through 10000.
            return Tr("Seek fading interval must be a whole number from 1 to 10000 milliseconds.");
        return null;
    }

    private void ShowSeekInterval()
    {
        var visible = _fadeSeeking.Checked;
        _seekFadeLabel.Show(visible);
        _seekFadeInterval.Show(visible);
        Window.Layout();
        Window.FitInside();
    }

    private static Choice Choice(Window parent, IEnumerable<string> values, int selected)
    { var choice = new Choice(parent); foreach (var value in values) choice.Add(value); choice.SelectedIndex = selected; return choice; }

    private void ShowCrossfadeSettings()
    {
        var visible = _crossfade.Checked;
        foreach (var control in new Window[] { _crossfadeLabel, _crossfadeInterval, _curveLabel, _crossfadeCurve, _crossfadeManual })
            control.Show(visible);
        Window.Layout();
        Window.FitInside();
    }

    private static void AddCheck(BoxSizer sizer, Window control)
        => sizer.Add(control, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);

    private static void AddField(BoxSizer sizer, StaticText label, Window control)
    {
        sizer.Add(label, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        sizer.Add(control, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
    }
}
