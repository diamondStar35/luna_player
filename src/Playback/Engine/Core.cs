using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
using LunaPlayer.Media;
using MpvNet;

namespace LunaPlayer.Playback;

internal sealed partial class Engine : IEngine
{
    private readonly MPV _mpv;
    private readonly IDisposable _endRegistration;
    private readonly IDisposable _playbackRestartRegistration;
    private readonly IDisposable _positionRegistration;
    private readonly IDisposable _sampleRateRegistration;
    private readonly IDisposable _videoParamsRegistration;
    private readonly IDisposable _trackListRegistration;
    private readonly IDisposable _subTextRegistration;

    // Cached from the video-params observer; mpv learns it asynchronously after a load, not on demand.
    private bool _hasVideo;

    private double _volume = 100;
    private double _pitch;
    private bool _pitchFilterActive;
    private double _pan;
    private bool _panFilterActive;
    private bool _normalizationEnabled;

    private EndBehavior _endBehavior;
    private GaplessPlaybackMode _gaplessPlayback;
    private int _fadeIntervalMilliseconds;
    private bool _fadeSeeking;
    private int _seekFadeIntervalMilliseconds = 100;
    private bool _fadeFileTransitions;
    private bool _fadePauseResume;
    private bool? _loadingPaused;
    private bool _fadeInOnRestart;
    private bool _endFadeActive;

    private bool _loaded;
    private bool _repeatFile;
    private bool _currentUsesAudioFile;
    private LoadRequest? _nextRequest;
    private bool _queuedNext;
    private long? _queuedSourceEntryId;
    private bool _awaitingLoadRestart;
    private string? _loadingPath;
    private readonly Lock _loadSync = new();
    private Probe? _probe;
    private long _loadGeneration;
    private long _queueGeneration;
    private long? _loadingSourceEntryId;
    private long? _activeEntryId;
    private string? _activePath;
    private double? _lastPosition;
    private LoadRequest? _pendingAdvance;
    private double? _pendingSourcePosition;
    private QueuedAdvance? _completedAdvance;
    private bool _crossfade;
    private bool _crossfadeManual;
    private int _crossfadeMilliseconds = 3000;
    private bool _crossfadeEqualPower = true;
    private bool _holdAutomaticAdvance;
    private LoadRequest? _manualRequest;

    private bool _selectionRangeActive;
    private bool _selectionRangeRepeats;
    private bool _selectionRangeCompleted;
    private double _selectionRangeEnd;
    private long? _selectionRangeEntryId;
    private bool _releasedSelectionEndPending;
    private double _releasedSelectionEnd;
    private long? _releasedSelectionEntryId;
    private bool _releasedSelectionWasPlaying;
    private bool _releasedSelectionWasCompleted;
    private bool _releasedSelectionPassed;
    private bool _endBehaviorRestorePending;
    private bool _selectionSeekPending;
    private bool _playAfterSelectionSeek;
    private bool _restoreEndBehaviorAfterSelectionSeek;
    private double _selectionSeekTarget;

    private Preset? _equalizerPreset;
    private double _equalizerSampleRate;
    private bool _equalizerFilterActive;

    // The last subtitle line handed out, so mpv re-reporting the same cue is not announced twice. Cleared
    // when the cue goes away, so a line that genuinely returns is read again.
    private string? _lastSubText;
    private bool _disposed;

    internal Engine(nint windowHandle, bool mediaControls = true)
    {
        var options = new Dictionary<string, object?>
        {
            ["vo"] = "gpu",
            ["osc"] = false,
            ["keep_open"] = "no",
            ["input_default_bindings"] = false,
            ["input_vo_keyboard"] = false,
            // Subtitles start off: nothing is read until the user picks a track. sub-auto keeps mpv from
            // pulling in sidecar .srt files, and sid=no leaves every embedded track unselected on load.
            ["sub_auto"] = "no",
            ["sid"] = "no",
            ["volume_max"] = Math.Ceiling(ToMpvVolume(AudioSettings.MaximumVolume)),
        };
        if (windowHandle != 0)
            options["wid"] = windowHandle.ToString(CultureInfo.InvariantCulture);

        _mpv = new MPV(options: options);
        SetPropertySafely("network-timeout", 10);
        ApplyMediaControls(mediaControls);
        _endRegistration = _mpv.OnEvent(HandleEndFile, MpvEventId.EndFile);
        _playbackRestartRegistration = _mpv.OnEvent(HandlePlaybackRestart, MpvEventId.PlaybackRestart);
        _positionRegistration = _mpv.ObserveProperty("time-pos", HandlePositionChange);
        // The equalizer is left out of the chain entirely until a preset asks for it, and which bands it
        // then carries depends on the file's sample rate: a band at the Nyquist frequency rings rather
        // than filters, so the graph is rebuilt whenever that rate changes under it. Off is nothing at
        // all, not a row of flat bands.
        _sampleRateRegistration = _mpv.ObserveProperty("audio-params/samplerate", HandleSampleRateChange);
        // Video parameters are not known when the load returns, so the width is watched rather than read.
        _videoParamsRegistration = _mpv.ObserveProperty("video-params/w", HandleVideoParamsChange);
        // Tracks are learned after the load returns as well, so the count is watched to tell when the audio
        // tracks a file offers have changed under the menu that gates on them.
        _trackListRegistration = _mpv.ObserveProperty("track-list/count", HandleTrackListChange);
        // The text of the subtitle currently on screen. Watching it turns each cue into a line to announce as
        // it comes up; mpv leaves it empty between cues and whenever no subtitle track is selected.
        _subTextRegistration = _mpv.ObserveProperty("sub-text", HandleSubTextChange);
    }

    public event Action<EndReason>? Ended;

    public event Action<double>? SelectionEnded;

    public event Action? SelectionSeekCompleted;

    public event Action? VideoAvailabilityChanged;

    public event Action? AudioTracksChanged;

    public event Action? SubtitleTracksChanged;

    public event Action<string>? SubtitleTextChanged;

    public bool HasVideo => _hasVideo;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Interlocked.Increment(ref _loadGeneration);
        Interlocked.Increment(ref _queueGeneration);
        if (_probe is Probe probe)
            _ = Task.Run(probe.Dispose);
        _endRegistration.Dispose();
        _playbackRestartRegistration.Dispose();
        _positionRegistration.Dispose();
        _sampleRateRegistration.Dispose();
        _videoParamsRegistration.Dispose();
        _trackListRegistration.Dispose();
        _subTextRegistration.Dispose();
        if (_spotifyAudio is { } streams)
            foreach (var pending in streams)
                if (streams.TryRemove(pending.Key, out var unused)) unused.Dispose();
        _mpv.Dispose();
    }

    /// <summary>What mpv reports as media-title. mpv substitutes the file name when the media declares no
    /// title, so deciding whether this is a real title is left to the caller, which knows the path.</summary>
    public string? MediaTitle => ReadString("media-title")?.Trim() is { Length: > 0 } title
        ? LegacyMetadataEncoding.RepairArabicMojibake(title)
        : null;

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
        SetPropertySafely("keep-open", _endBehavior == EndBehavior.None ? "yes" : "no");
        SetPropertySafely("loop-file", !_holdAutomaticAdvance && _endBehavior == EndBehavior.Loop ? "inf" : "no");
    }

    public void SetMediaControls(bool enabled) => ApplyMediaControls(enabled);

    // Both halves are set together: media-controls is the OS transport overlay mpv publishes, and
    // input-media-keys is whether it answers the play/pause keys on a keyboard or headset. Disabling the
    // controls while still capturing the keys is the arrangement that surprises people, so the setting
    // moves them as one.
    private void ApplyMediaControls(bool enabled)
    {
        var value = enabled ? "yes" : "no";
        SetPropertySafely("media-controls", value);
        SetPropertySafely("input-media-keys", value);
    }

    private void SetPropertySafely(string name, object value) => TrySetProperty(name, value);

    /// <summary>Converts Luna's linear percentage to mpv's cubic volume scale. For example, Luna's
    /// 2000% is a 20x amplitude gain and maps to approximately 271.44 on mpv's volume property.</summary>
    private static double ToMpvVolume(double volume)
        => volume <= 0 ? 0 : Precision.Normalize(100 * Math.Cbrt(volume / 100));

    private void ApplyVolume() => SetPropertySafely("volume", ToMpvVolume(_volume));

    private void SetFade(double level) => TryDo(mpv => mpv.FadeVolume(level, 0));

    private void FadeVolume(double target, int milliseconds)
        => TryDo(mpv => mpv.FadeVolume(target, milliseconds / 1000.0));

    /// <summary>The failures a call into libmpv can produce: mpv refusing the call, the player having been
    /// shut down under it, and a property whose value does not convert to the type the caller asked for.
    /// None of them is worth bringing the player down over - every caller here has something sensible to do
    /// with "that did not work", and a media file that makes mpv unhappy is an ordinary event.</summary>
    private static bool IsFailure(Exception exception)
        => exception is MpvException or InvalidOperationException
            or FormatException or InvalidCastException or OverflowException;

    private static bool TryConvertDouble(object? value, out double result)
    {
        try
        {
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return value is not null;
        }
        catch (Exception exception) when (IsFailure(exception))
        {
            result = 0;
            return false;
        }
    }

    /// <summary>Runs something against mpv, reporting whether it got through.</summary>
    private bool TryDo(Action<MPV> action)
    {
        try
        {
            action(_mpv);
            return true;
        }
        catch (Exception exception) when (IsFailure(exception))
        {
            return false;
        }
    }

    /// <summary>Reads a property and converts it, or null when mpv has no value for it, will not answer, or
    /// answers with something the conversion cannot use.</summary>
    private T? ReadValue<T>(string name, Func<object, T> convert) where T : struct
    {
        try
        {
            return _mpv.GetProperty(name) is { } value ? convert(value) : null;
        }
        catch (Exception exception) when (IsFailure(exception))
        {
            return null;
        }
    }

    /// <summary>A property read without converting it, for the ones that answer with a list rather than a
    /// value. Null means mpv had nothing to say, one way or another.</summary>
    private object? ReadObject(string name)
    {
        try
        {
            return _mpv.GetProperty(name);
        }
        catch (Exception exception) when (IsFailure(exception))
        {
            return null;
        }
    }

    private bool TrySetProperty(string name, object value) => TryDo(mpv => mpv.SetProperty(name, value));

    private double? ReadDouble(string name)
        => ReadValue(name, static value => Convert.ToDouble(value, CultureInfo.InvariantCulture));

    private long? ReadInt64(string name)
        => ReadValue(name, static value => Convert.ToInt64(value, CultureInfo.InvariantCulture));

    private long? PositiveEntryId()
    {
        // MPV exposes entry IDs through playlist records, not a playlist-entry-id property.
        // Read one snapshot: the current entry can already name the successor while the outgoing
        // entry is still playing, so a separate index lookup can identify the wrong file.
        if (ReadObject("playlist") is not IEnumerable<object?> entries)
            return null;
        foreach (var value in entries)
        {
            if (value is not IDictionary<string, object?> entry
                || !entry.TryGetValue("playing", out var playing) || playing is not true
                || !entry.TryGetValue("id", out var raw))
                continue;
            try
            {
                var id = Convert.ToInt64(raw, CultureInfo.InvariantCulture);
                return id > 0 ? id : null;
            }
            catch (Exception exception) when (IsFailure(exception))
            {
                return null;
            }
        }
        return null;
    }

    private bool? ReadBoolean(string name)
        => ReadValue(name, static value => Convert.ToBoolean(value, CultureInfo.InvariantCulture));

    private string? ReadString(string name)
        => ReadObject(name) is { } value ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private bool AddFilter(string filter) => TryDo(mpv => mpv.Command("af", "add", filter));

    private void RemoveFilter(string label) => TryDo(mpv => mpv.Command("af", "remove", label));
}
