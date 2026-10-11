using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Actions;
using LunaPlayer.Equalizer;
using LunaPlayer.Playback;
using LunaPlayer.Recording;


namespace LunaPlayer.Configuration;

internal enum SpeechVerbosity { Beginner, Advanced }
internal enum OpenFilesMode { FileOnly, MainFolder, MainAndSubfolders }
/// <summary>Which interface appearance the player asks Windows for. <see cref="SystemDefault"/> follows the
/// Windows setting; the other two force light or dark regardless. A high-contrast scheme always wins over
/// this choice. Applied once at startup, so a change takes effect after the player restarts.</summary>
internal enum AppTheme { SystemDefault, Light, Dark }
internal enum EndBehavior { Advance, Loop, None }
internal enum SilenceDetection { Peak, Rms }
/// <summary>The picture heights the player offers. The backing number is the
/// height in pixels, so it goes straight to yt-dlp's height cap.</summary>
internal enum VideoQuality
{
    P144 = 144, P240 = 240, P360 = 360, P480 = 480,
    P720 = 720, P1080 = 1080, P1440 = 1440, P2160 = 2160,
}

/// <summary>The audio bitrates the player offers. The backing number is the
/// bitrate in kilobits per second.</summary>
internal enum AudioQuality { Kbps64 = 64, Kbps128 = 128, Kbps256 = 256 }
internal enum MixedLinkBehavior { Ask, Video, Playlist }
internal enum YtDlpChannel { Stable, Nightly, Master }
internal enum SleepTimerMode { Duration, EndOfTrack }
internal enum SleepTimerAction { Pause, Stop }

internal sealed class PlayerSettings
{
    public int Version { get; set; } = 4;
    public GeneralSettings General { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public SilenceSettings Silence { get; set; } = new();
    public EqualizerSettings Equalizer { get; set; } = new();
    public ShortcutSettings Shortcuts { get; set; } = new();
    public YouTubeSettings YouTube { get; set; } = new();
    public RecordingSettings Recording { get; set; } = new();
    public SleepTimerSettings SleepTimer { get; set; } = new();

    internal PlayerSettings Copy() => new()
    {
        Version = Version,
        General = General.Copy(),
        Audio = Audio.Copy(),
        Playback = Playback.Copy(),
        Silence = Silence.Copy(),
        Equalizer = Equalizer.Copy(),
        Shortcuts = Shortcuts.Copy(),
        YouTube = YouTube.Copy(),
        Recording = Recording.Copy(),
        SleepTimer = SleepTimer.Copy(),
    };

    internal void Apply(PlayerSettings source)
    {
        Version = Math.Max(4, source.Version);
        General.Apply(source.General);
        Audio.Apply(source.Audio);
        Playback.Apply(source.Playback);
        Silence.Apply(source.Silence);
        Equalizer.Apply(source.Equalizer);
        Shortcuts.Apply(source.Shortcuts);
        YouTube.Apply(source.YouTube);
        Recording.Apply(source.Recording);
        SleepTimer.Apply(source.SleepTimer);
        Validate();
    }

    /// <summary>Rejects malformed persisted settings before normalization can hide the problem.</summary>
    internal void ValidateStored()
    {
        if (General is null || Audio is null || Playback is null || Silence is null
            || Shortcuts is null || YouTube is null || Recording is null || Equalizer is null
            || SleepTimer is null)
            throw new JsonException("The settings file is missing a required section.");

        Require(Version >= 1, "version");
        Require(!string.IsNullOrWhiteSpace(General.Language), "general.language");
        Require(General.LastDirectory is not null, "general.lastDirectory");
        Require(Enum.IsDefined(General.Verbosity), "general.verbosity");
        Require(Enum.IsDefined(General.OpenFilesMode), "general.openFilesMode");
        Require(Enum.IsDefined(General.Theme), "general.theme");

        Require(FiniteBetween(Audio.Volume, 0, AudioSettings.MaximumVolume), "audio.volume");
        Require(FiniteBetween(Audio.Speed, AudioSettings.MinimumSpeed, AudioSettings.MaximumSpeed), "audio.speed");
        Require(FiniteBetween(Audio.Pitch, AudioSettings.MinimumPitch, AudioSettings.MaximumPitch), "audio.pitch");
        Require(FiniteBetween(Audio.PitchStep, AudioSettings.MinimumPitchStep, AudioSettings.MaximumPitchStep),
            "audio.pitchStep");
        Require(Audio.Device is not null, "audio.device");
        Require(Audio.VolumeStep is >= 1 and <= 20, "audio.volumeStep");
        Require(FiniteBetween(Audio.Pan, -100, 100), "audio.pan");
        Require(Audio.PanStep is >= 1 and <= 100, "audio.panStep");
        Require(double.IsFinite(Audio.SpeedStep) && Audio.SpeedStep > 0, "audio.speedStep");
        Require(double.IsFinite(Audio.CustomSeekStep) && Audio.CustomSeekStep > 0, "audio.customSeekStep");
        Require(Audio.SeekStepKey is { Length: 1 }
            && "1234567890-".Contains(Audio.SeekStepKey, StringComparison.Ordinal), "audio.seekStepKey");
        Require(Enum.IsDefined(Audio.EndBehavior), "audio.endBehavior");

        Require(Playback.LastFile is not null, "playback.lastFile");
        Require(double.IsFinite(Playback.LastPosition) && Playback.LastPosition >= 0,
            "playback.lastPosition");

        Require(Silence.StartPeriods >= 0, "silence.startPeriods");
        Require(double.IsFinite(Silence.StartDuration) && Silence.StartDuration >= 0,
            "silence.startDuration");
        Require(double.IsFinite(Silence.Threshold), "silence.threshold");
        Require(Silence.StopPeriods >= -1, "silence.stopPeriods");
        Require(double.IsFinite(Silence.StopDuration) && Silence.StopDuration >= 0,
            "silence.stopDuration");
        Require(double.IsFinite(Silence.StopSilence) && Silence.StopSilence >= 0,
            "silence.stopSilence");
        Require(double.IsFinite(Silence.Window) && Silence.Window > 0, "silence.window");
        Require(Enum.IsDefined(Silence.Detection), "silence.detection");

        Require(Enum.IsDefined(SleepTimer.Mode), "sleepTimer.mode");
        Require(Enum.IsDefined(SleepTimer.Action), "sleepTimer.action");
        Require(SleepTimer.DurationMinutes >= SleepTimerSettings.MinimumDuration
            && SleepTimer.DurationMinutes <= SleepTimerSettings.MaximumDuration, "sleepTimer.durationMinutes");
        Require(SleepTimer.FadeSeconds >= SleepTimerSettings.MinimumFadeSeconds
            && SleepTimer.FadeSeconds <= SleepTimerSettings.MaximumFadeSeconds, "sleepTimer.fadeSeconds");

        Require(!string.IsNullOrWhiteSpace(Equalizer.Preset), "equalizer.preset");
        // Written out rather than run through Require, because the compiler cannot see that Require throws
        // and would take everything below as a possible null.
        if (Equalizer.Overrides is null)
            throw new JsonException("The settings file contains an invalid equalizer section.");
        foreach (var (preset, bands) in Equalizer.Overrides)
        {
            if (string.IsNullOrWhiteSpace(preset) || bands is null)
                throw new JsonException("The settings file contains an invalid equalizer override.");
            foreach (var band in bands)
            {
                if (band is null)
                    throw new JsonException($"The equalizer override for '{preset}' is missing a band.");
                Require(band.Slot >= 0 && band.Slot < Bands.SlotCount,
                    $"equalizer.overrides.{preset}.slot");
                Require(band.Frequency is not double frequency
                    || FiniteBetween(frequency, Bands.MinimumFrequency, Bands.MaximumFrequency),
                    $"equalizer.overrides.{preset}.frequency");
                Require(band.Q is not double q || FiniteBetween(q, Bands.MinimumQ, Bands.MaximumQ),
                    $"equalizer.overrides.{preset}.q");
                Require(band.Gain is not double gain
                    || FiniteBetween(gain, Bands.MinimumGain, Bands.MaximumGain),
                    $"equalizer.overrides.{preset}.gain");
            }
        }
    }

    internal void Validate()
    {
        // Repeated step-based changes accumulate binary floating-point error; round so the stored
        // value stays the one the user actually selected rather than 1.0000000000000009.
        Audio.Volume = Precision.Normalize(Math.Clamp(Audio.Volume, 0, AudioSettings.MaximumVolume));
        Audio.Speed = Precision.Normalize(Math.Clamp(
            Audio.Speed, AudioSettings.MinimumSpeed, AudioSettings.MaximumSpeed));
        Audio.VolumeStep = Math.Clamp(Audio.VolumeStep, 1, 20);
        Audio.Pitch = Precision.Normalize(Math.Clamp(Audio.Pitch, AudioSettings.MinimumPitch, AudioSettings.MaximumPitch));
        Audio.PitchStep = Precision.Normalize(Math.Clamp(
            Audio.PitchStep > 0 ? Audio.PitchStep : 0.1,
            AudioSettings.MinimumPitchStep, AudioSettings.MaximumPitchStep));
        Audio.Pan = Precision.Normalize(Math.Clamp(Audio.Pan, -100, 100));
        Audio.PanStep = Math.Clamp(Audio.PanStep, 1, 100);
        Audio.SpeedStep = Precision.Normalize(Audio.SpeedStep > 0 ? Audio.SpeedStep : 0.1);
        Audio.CustomSeekStep = Precision.Normalize(Audio.CustomSeekStep > 0 ? Audio.CustomSeekStep : 5);
        Audio.SeekStepKey = Audio.SeekStepKey.Length == 1 && "1234567890-".Contains(Audio.SeekStepKey, StringComparison.Ordinal)
            ? Audio.SeekStepKey : "2";
        YouTube.SearchResultCount = Math.Clamp(YouTube.SearchResultCount, 5, 100);
        YouTube.CookiesPath ??= string.Empty;
        // Only that it is one of the rates the player offers at all. Whether the chosen format can be
        // written at it is a question for the encoder, asked when a recording starts rather than here:
        // this runs at load, and loading an encoder to interrogate it is not something to do then.
        Recording.SampleRate = LunaPlayer.Recording.AudioCatalog.SampleRates.Contains(Recording.SampleRate)
            ? Recording.SampleRate : 44100;
        Recording.Channels = Math.Clamp(Recording.Channels, 1, 2);
        // Only sanity bounds. What a format will actually accept is asked of Windows when the list is
        // shown, and differs with the rate and the channel count, so it cannot be settled here.
        Recording.Bitrate = Math.Clamp(Recording.Bitrate, 8000, 512000);
        Recording.Folder = string.IsNullOrWhiteSpace(Recording.Folder)
            ? Paths.DefaultRecordingsDirectory : Recording.Folder.Trim();
        SleepTimer.DurationMinutes = Math.Clamp(
            SleepTimer.DurationMinutes, SleepTimerSettings.MinimumDuration, SleepTimerSettings.MaximumDuration);
        SleepTimer.FadeSeconds = Math.Clamp(
            SleepTimer.FadeSeconds, SleepTimerSettings.MinimumFadeSeconds, SleepTimerSettings.MaximumFadeSeconds);
        Playback.LastPosition = Precision.Normalize(Math.Max(0, Playback.LastPosition));
        Silence.StartPeriods = Math.Max(0, Silence.StartPeriods);
        Silence.StartDuration = Precision.Normalize(Math.Max(0, Silence.StartDuration));
        Silence.Threshold = Precision.Normalize(Silence.Threshold);
        Silence.StopPeriods = Math.Max(-1, Silence.StopPeriods);
        Silence.StopDuration = Precision.Normalize(Math.Max(0, Silence.StopDuration));
        Silence.StopSilence = Precision.Normalize(Math.Max(0, Silence.StopSilence));
        Silence.Window = Precision.Normalize(Silence.Window > 0 ? Silence.Window : 0.02);
        Equalizer.Preset = string.IsNullOrWhiteSpace(Equalizer.Preset)
            ? Presets.DefaultId : Equalizer.Preset.Trim();
        Equalizer.Overrides ??= [];
        // An entry that changes nothing is the same as no entry, and leaving it would keep a preset
        // looking edited in the manager long after the change was taken back out of it.
        foreach (var preset in Equalizer.Overrides.Keys.ToArray())
        {
            var bands = Equalizer.Overrides[preset];
            if (bands is null || bands.TrueForAll(band => band is null || band.IsEmpty))
                Equalizer.Overrides.Remove(preset);
        }
        General.LastDirectory ??= string.Empty;
        General.Language = string.IsNullOrWhiteSpace(General.Language)
            ? Localization.SystemLanguage : General.Language.Trim();
        Audio.Device ??= string.Empty;
        Playback.LastFile ??= string.Empty;
        Shortcuts.Primary ??= [];
        Shortcuts.Secondary ??= [];
        var shortcutManager = new ShortcutManager(ActionRegistry.All);
        shortcutManager.Apply(Shortcuts.Primary, Shortcuts.Secondary);
        Shortcuts.Primary = shortcutManager.PrimaryOverrides();
        Shortcuts.Secondary = shortcutManager.SecondaryOverrides();
        Shortcuts.Global ??= [];
        var globalManager = new ShortcutManager(GlobalActions.All);
        globalManager.Apply(Shortcuts.Global, ReadOnlyDictionary<ActionId, Shortcut>.Empty);
        Shortcuts.Global = globalManager.PrimaryOverrides();
    }

    private static bool FiniteBetween(double value, double minimum, double maximum)
        => double.IsFinite(value) && value >= minimum && value <= maximum;

    private static void ValidateShortcuts(IReadOnlyDictionary<ActionId, Shortcut> shortcuts, string section)
    {
        const ShortcutModifiers knownModifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift
            | ShortcutModifiers.Alt | ShortcutModifiers.Win;
        foreach (var (action, shortcut) in shortcuts)
        {
            var normalized = new Shortcut((shortcut.Key ?? string.Empty).Trim().ToLowerInvariant(),
                shortcut.Modifiers);
            Require(Enum.IsDefined(action), $"{section}.{action}");
            Require((shortcut.Modifiers & ~knownModifiers) == 0 && ShortcutManager.IsValid(normalized),
                $"{section}.{action}");
        }
    }

    private static void Require(bool condition, string name)
    {
        if (!condition)
            throw new JsonException($"The settings value '{name}' is invalid.");
    }
}

internal sealed class GeneralSettings
{
    public string Language { get; set; } = Localization.SystemLanguage;
    public bool RememberLastPosition { get; set; }
    public bool SpeakFileOnNavigation { get; set; }
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public bool SaveOnClose { get; set; } = true;

    /// <summary>Whether the player stays out of the system's media controls entirely: the Windows overlay
    /// for the volume keys and lock screen, and mpv's own media-key and transport-control integration.</summary>
    /// <remarks>
    /// The equivalent of mpv's <c>--no-media-controls</c>, offered because that integration can misbehave with
    /// some headphones - a play/pause button that pauses twice, or reaches the wrong application. Off by
    /// default, so the controls are there for everyone who is not troubled by them.
    /// </remarks>
    public bool DisableMediaControls { get; set; }

    /// <summary>Whether the window title follows the playing track - the title bar showing what is open, and
    /// paused when it is - rather than staying the plain program name.</summary>
    /// <remarks>
    /// Off by default: a screen reader announces the foreground window whenever its title changes, so a title
    /// that tracks playback speaks over the player on every track change and pause. Some users want it anyway,
    /// so it is offered as a choice rather than left out.
    /// </remarks>
    public bool SpeakWindowTitle { get; set; }

    /// <summary>Whether a subtitle line being read aloud is cut off when the next line appears, so the speech
    /// keeps pace with the picture. Off reads each line to the end before the next, so none is lost.</summary>
    /// <remarks>Off by default: a cut-off line is a line the listener never heard in full, and subtitle cues
    /// often sit close enough together to clip one another. Turning it on trades completeness for staying in
    /// step with the picture, which suits dense dialogue.</remarks>
    public bool SubtitleInterrupt { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter<SpeechVerbosity>))]
    public SpeechVerbosity Verbosity { get; set; } = SpeechVerbosity.Beginner;
    [JsonConverter(typeof(JsonStringEnumConverter<OpenFilesMode>))]
    public OpenFilesMode OpenFilesMode { get; set; } = OpenFilesMode.FileOnly;
    [JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
    public AppTheme Theme { get; set; } = AppTheme.SystemDefault;
    public string LastDirectory { get; set; } = string.Empty;
    internal GeneralSettings Copy() => (GeneralSettings)MemberwiseClone();
    internal void Apply(GeneralSettings source)
    {
        Language = source.Language;
        RememberLastPosition = source.RememberLastPosition;
        SpeakFileOnNavigation = source.SpeakFileOnNavigation;
        CheckUpdatesOnStartup = source.CheckUpdatesOnStartup;
        SaveOnClose = source.SaveOnClose;
        DisableMediaControls = source.DisableMediaControls;
        SpeakWindowTitle = source.SpeakWindowTitle;
        SubtitleInterrupt = source.SubtitleInterrupt;
        Verbosity = source.Verbosity;
        OpenFilesMode = source.OpenFilesMode;
        Theme = source.Theme;
        LastDirectory = source.LastDirectory;
    }
}

internal sealed class AudioSettings
{
    internal const double MaximumVolume = 2000;
    internal const double MinimumSpeed = 0.5;
    internal const double MaximumSpeed = 4;
    internal const double MinimumPitch = -12;
    internal const double MaximumPitch = 12;
    internal const double MinimumPitchStep = 0.001;
    internal const double MaximumPitchStep = 12;

    public double Volume { get; set; } = 100;
    public double Speed { get; set; } = 1;
    public double Pitch { get; set; }
    public double PitchStep { get; set; } = 0.1;
    public string Device { get; set; } = string.Empty;
    public int VolumeStep { get; set; } = 5;
    public double Pan { get; set; }
    public int PanStep { get; set; } = 5;
    public double SpeedStep { get; set; } = 0.1;
    public string SeekStepKey { get; set; } = "2";
    public double CustomSeekStep { get; set; } = 5;
    [JsonConverter(typeof(JsonStringEnumConverter<EndBehavior>))]
    public EndBehavior EndBehavior { get; set; } = EndBehavior.Advance;
    public bool WrapPlaylist { get; set; }
    public bool SaveFilePositions { get; set; }
    public bool NormalizeAudio { get; set; } = true;
    public bool MonoAudio { get; set; }
    internal AudioSettings Copy() => (AudioSettings)MemberwiseClone();
    internal void Apply(AudioSettings source)
    {
        Volume = source.Volume;
        Speed = source.Speed;
        Pitch = source.Pitch;
        PitchStep = source.PitchStep;
        Device = source.Device;
        VolumeStep = source.VolumeStep;
        Pan = source.Pan;
        PanStep = source.PanStep;
        SpeedStep = source.SpeedStep;
        SeekStepKey = source.SeekStepKey;
        CustomSeekStep = source.CustomSeekStep;
        EndBehavior = source.EndBehavior;
        WrapPlaylist = source.WrapPlaylist;
        SaveFilePositions = source.SaveFilePositions;
        NormalizeAudio = source.NormalizeAudio;
        MonoAudio = source.MonoAudio;
    }
}

internal sealed class PlaybackSettings
{
    public string LastFile { get; set; } = string.Empty;
    public double LastPosition { get; set; }
    internal PlaybackSettings Copy() => (PlaybackSettings)MemberwiseClone();
    internal void Apply(PlaybackSettings source)
    {
        LastFile = source.LastFile;
        LastPosition = source.LastPosition;
    }
}

internal sealed class SilenceSettings
{
    public bool Enabled { get; set; }
    public bool Advanced { get; set; }
    public int StartPeriods { get; set; } = 1;
    public double StartDuration { get; set; } = 0.2;
    public double Threshold { get; set; } = -30;
    public int StopPeriods { get; set; } = -1;
    public double StopDuration { get; set; } = 0.5;
    public double StopSilence { get; set; } = 0.2;
    public double Window { get; set; } = 0.02;
    [JsonConverter(typeof(JsonStringEnumConverter<SilenceDetection>))]
    public SilenceDetection Detection { get; set; } = SilenceDetection.Peak;
    internal SilenceSettings Copy() => (SilenceSettings)MemberwiseClone();
    internal void Apply(SilenceSettings source)
    {
        Enabled = source.Enabled;
        Advanced = source.Advanced;
        StartPeriods = source.StartPeriods;
        StartDuration = source.StartDuration;
        Threshold = source.Threshold;
        StopPeriods = source.StopPeriods;
        StopDuration = source.StopDuration;
        StopSilence = source.StopSilence;
        Window = source.Window;
        Detection = source.Detection;
    }
}

internal sealed class ShortcutSettings
{
    public Dictionary<ActionId, Shortcut> Primary { get; set; } = [];
    public Dictionary<ActionId, Shortcut> Secondary { get; set; } = [];
    /// <summary>System-wide hot keys, keyed by the action they trigger. Separate from <see cref="Primary"/>
    /// because the same action can hold a local and a global binding at once.</summary>
    public Dictionary<ActionId, Shortcut> Global { get; set; } = [];
    internal ShortcutSettings Copy() => new()
    {
        Primary = new(Primary),
        Secondary = new(Secondary),
        Global = new(Global),
    };
    internal void Apply(ShortcutSettings source)
    {
        Primary = new(source.Primary);
        Secondary = new(source.Secondary);
        Global = new(source.Global);
    }
}

internal sealed class YouTubeSettings
{
    /// <summary>The picture height a video is played and prefetched at, when Enter plays the video.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<VideoQuality>))]
    public VideoQuality VideoQuality { get; set; } = VideoQuality.P720;

    /// <summary>The bitrate the sound is played and prefetched at, when Ctrl+Enter plays the audio.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<AudioQuality>))]
    public AudioQuality AudioQuality { get; set; } = AudioQuality.Kbps256;

    /// <summary>How many search results to ask for.</summary>
    /// <remarks>
    /// A target rather than an exact number: YouTube answers a search in batches of its own choosing, so the
    /// player keeps taking batches until it has this many and stops on the first one that takes it past.
    /// </remarks>
    public int SearchResultCount { get; set; } = 50;

    /// <summary>Whether the search box offers live suggestions as the user types.</summary>
    /// <remarks>
    /// On by default. Each keystroke that settles fetches a short list of
    /// completions from YouTube, so somebody who would rather type undisturbed - or is on a slow or metered
    /// connection - can turn the network chatter off here.
    /// </remarks>
    public bool SearchSuggestions { get; set; } = true;

    /// <summary>What to do with a link that names a video and a playlist at once.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<MixedLinkBehavior>))]
    public MixedLinkBehavior MixedLink { get; set; } = MixedLinkBehavior.Ask;

    [JsonConverter(typeof(JsonStringEnumConverter<YtDlpChannel>))]
    public YtDlpChannel Channel { get; set; } = YtDlpChannel.Stable;

    public bool CheckComponentUpdates { get; set; }

    /// <summary>A Netscape-format cookie file yt-dlp is given with <c>--cookies</c>, or empty for none.</summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="CookiesFromFirefox"/>: choosing a file clears the Firefox flag and
    /// vice versa, so only one cookie source is ever handed to yt-dlp.
    /// </remarks>
    public string CookiesPath { get; set; } = "";

    /// <summary>Whether yt-dlp is told to read cookies live from Firefox (<c>--cookies-from-browser
    /// firefox</c>) rather than from a saved file.</summary>
    public bool CookiesFromFirefox { get; set; }

    internal YouTubeSettings Copy() => (YouTubeSettings)MemberwiseClone();

    internal void Apply(YouTubeSettings source)
    {
        VideoQuality = source.VideoQuality;
        AudioQuality = source.AudioQuality;
        SearchResultCount = source.SearchResultCount;
        SearchSuggestions = source.SearchSuggestions;
        MixedLink = source.MixedLink;
        Channel = source.Channel;
        CheckComponentUpdates = source.CheckComponentUpdates;
        CookiesPath = source.CookiesPath;
        CookiesFromFirefox = source.CookiesFromFirefox;
    }
}

/// <summary>How a recording is written, when nothing more particular has been asked for.</summary>
///
/// <remarks>
/// Recording shortcuts use these defaults when no sources are configured. The recording window works on
/// a session copy, so temporary changes are not persisted automatically.
/// </remarks>
internal sealed class RecordingSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter<RecordingFormat>))]
    public RecordingFormat Format { get; set; } = RecordingFormat.Wav;

    public int SampleRate { get; set; } = 44100;

    public int Channels { get; set; } = 2;

    /// <summary>Bits per second, for the formats that compress. Ignored by the rest.</summary>
    public int Bitrate { get; set; } = 192000;

    public string Folder { get; set; } = Paths.DefaultRecordingsDirectory;

    internal RecordingSettings Copy() => (RecordingSettings)MemberwiseClone();

    internal void Apply(RecordingSettings source)
    {
        Format = source.Format;
        SampleRate = source.SampleRate;
        Channels = source.Channels;
        Bitrate = source.Bitrate;
        Folder = source.Folder;
    }
}

/// <summary>The sleep-timer choices remembered between sessions, so the window opens on what was set last.
/// </summary>
///
/// <remarks>
/// These are the last-used preferences, not a timer that survives a restart: closing the player always
/// ends any countdown. The window is prefilled from here, and its choices are written back here when the
/// user sets a timer.
/// </remarks>
internal sealed class SleepTimerSettings
{
    internal const int MinimumDuration = 1;
    internal const int MaximumDuration = 24 * 60;
    internal const int MinimumFadeSeconds = 1;
    internal const int MaximumFadeSeconds = 120;

    [JsonConverter(typeof(JsonStringEnumConverter<SleepTimerMode>))]
    public SleepTimerMode Mode { get; set; } = SleepTimerMode.Duration;

    [JsonConverter(typeof(JsonStringEnumConverter<SleepTimerAction>))]
    public SleepTimerAction Action { get; set; } = SleepTimerAction.Pause;

    /// <summary>How long a duration timer runs, in minutes.</summary>
    public int DurationMinutes { get; set; } = 30;

    /// <summary>Whether the volume is faded down over the last seconds before the timer fires.</summary>
    public bool Fade { get; set; } = true;

    /// <summary>How many seconds the fade lasts.</summary>
    public int FadeSeconds { get; set; } = 20;

    internal SleepTimerSettings Copy() => (SleepTimerSettings)MemberwiseClone();

    internal void Apply(SleepTimerSettings source)
    {
        Mode = source.Mode;
        Action = source.Action;
        DurationMinutes = source.DurationMinutes;
        Fade = source.Fade;
        FadeSeconds = source.FadeSeconds;
    }
}

/// <summary>One band of a preset the player ships, as the user has changed it.</summary>
///
/// <remarks>
/// Only what was changed is written. A band the user left alone has all three of these null and follows
/// whatever the player ships, so improving a preset in a later release still reaches everyone who only
/// moved one band of it.
/// </remarks>
internal sealed class EqualizerBandOverride
{
    /// <summary>Which of the equalizer's slots this changes.</summary>
    public int Slot { get; set; }

    public double? Frequency { get; set; }
    public double? Q { get; set; }
    public double? Gain { get; set; }

    internal EqualizerBandOverride Copy()
        => new() { Slot = Slot, Frequency = Frequency, Q = Q, Gain = Gain };

    internal bool IsEmpty => Frequency is null && Q is null && Gain is null;
}

internal sealed class EqualizerSettings
{
    public bool Enabled { get; set; }

    /// <summary>The preset's stable name, not the one shown in the menu.</summary>
    /// <remarks>
    /// Kept even while the equalizer is switched off, so that turning it back on returns to the curve the
    /// user last chose rather than to whatever comes first in the list.
    /// </remarks>
    public string Preset { get; set; } = Presets.DefaultId;

    /// <summary>What the user has changed about the presets the player ships, keyed by preset name.
    /// </summary>
    /// <remarks>
    /// Presets the user made themselves are not here. Those are whole presets and live in their own file;
    /// these are differences, and a difference is meaningless apart from the thing it differs from.
    /// A preset with no entry here is exactly as it ships, which is also what resetting one to its
    /// defaults does: the entry is removed rather than filled with the defaults.
    /// </remarks>
    public Dictionary<string, List<EqualizerBandOverride>> Overrides { get; set; } = [];

    internal EqualizerSettings Copy()
    {
        var copy = new EqualizerSettings { Enabled = Enabled, Preset = Preset };
        foreach (var (preset, bands) in Overrides)
            copy.Overrides[preset] = [.. bands.Select(band => band.Copy())];
        return copy;
    }

    internal void Apply(EqualizerSettings source)
    {
        Enabled = source.Enabled;
        Preset = source.Preset;
        Overrides.Clear();
        foreach (var (preset, bands) in source.Overrides)
            Overrides[preset] = [.. bands.Select(band => band.Copy())];
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PlayerSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext;
