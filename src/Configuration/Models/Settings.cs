using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Actions;
using LunaPlayer.Equalizer;
using LunaPlayer.Playback;
using LunaPlayer.Recording;


namespace LunaPlayer.Configuration.Models;

internal sealed class Settings
{
    public int Version { get; set; } = 4;
    public GeneralSettings General { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public SilenceSettings Silence { get; set; } = new();
    public EqualizerSettings Equalizer { get; set; } = new();
    public ShortcutSettings Shortcuts { get; set; } = new();
    public YouTubeSettings YouTube { get; set; } = new();
    public DiscordSettings Discord { get; set; } = new();
    public SpotifySettings Spotify { get; set; } = new();
    public RecordingSettings Recording { get; set; } = new();
    public SleepTimerSettings SleepTimer { get; set; } = new();

    internal Settings Copy() => new()
    {
        Version = Version,
        General = General.Copy(),
        Audio = Audio.Copy(),
        Playback = Playback.Copy(),
        Silence = Silence.Copy(),
        Equalizer = Equalizer.Copy(),
        Shortcuts = Shortcuts.Copy(),
        YouTube = YouTube.Copy(),
        Spotify = Spotify.Copy(),
        Discord = Discord.Copy(),
        Recording = Recording.Copy(),
        SleepTimer = SleepTimer.Copy(),
    };

    internal void Apply(Settings source)
    {
        Version = Math.Max(4, source.Version);
        General.Apply(source.General);
        Audio.Apply(source.Audio);
        Playback.Apply(source.Playback);
        Silence.Apply(source.Silence);
        Equalizer.Apply(source.Equalizer);
        Shortcuts.Apply(source.Shortcuts);
        YouTube.Apply(source.YouTube);
        Spotify.Apply(source.Spotify);
        Discord.Apply(source.Discord);
        Recording.Apply(source.Recording);
        SleepTimer.Apply(source.SleepTimer);
        Validate();
    }

    /// <summary>Rejects malformed persisted settings before normalization can hide the problem.</summary>
    internal void ValidateStored()
    {
        if (General is null || Audio is null || Playback is null || Silence is null
            || Shortcuts is null || YouTube is null || Spotify is null || Recording is null || Equalizer is null
            || SleepTimer is null || Discord is null)
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
        Require(Audio.FadeIntervalMilliseconds is >= 0 and <= AudioSettings.MaximumFadeIntervalMilliseconds,
            "audio.fadeIntervalMilliseconds");
        Require(Audio.SeekFadeIntervalMilliseconds is >= 1 and <= AudioSettings.MaximumFadeIntervalMilliseconds,
            "audio.seekFadeIntervalMilliseconds");
        Require(Enum.IsDefined(Audio.GaplessPlayback), "audio.gaplessPlayback");
        Require(Audio.CrossfadeMilliseconds is >= 1 and <= AudioSettings.MaximumCrossfadeMilliseconds,
            "audio.crossfadeMilliseconds");
        Require(Enum.IsDefined(Audio.CrossfadeCurve), "audio.crossfadeCurve");

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
        NormalizeValues();
        NormalizeShortcuts();
    }

    /// <summary>Normalizes settings that do not depend on localized action definitions. Startup may run this
    /// on a worker so playback settings are ready before the window's menus are built.</summary>
    internal void NormalizeValues()
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
        Audio.FadeIntervalMilliseconds = Math.Clamp(
            Audio.FadeIntervalMilliseconds, 0, AudioSettings.MaximumFadeIntervalMilliseconds);
        Audio.SeekFadeIntervalMilliseconds = Math.Clamp(
            Audio.SeekFadeIntervalMilliseconds, 1, AudioSettings.MaximumFadeIntervalMilliseconds);
        Audio.CrossfadeMilliseconds = Math.Clamp(
            Audio.CrossfadeMilliseconds, 1, AudioSettings.MaximumCrossfadeMilliseconds);
        Audio.SeekStepKey = Audio.SeekStepKey.Length == 1 && "1234567890-".Contains(Audio.SeekStepKey, StringComparison.Ordinal)
            ? Audio.SeekStepKey : "2";
        YouTube.SearchResultCount = Math.Clamp(YouTube.SearchResultCount, 5, 100);
        YouTube.CookiesPath ??= string.Empty;
        Spotify.ClientId = (Spotify.ClientId ?? string.Empty).Trim();
        Spotify.AudioQuality = Spotify.AudioQuality is "NORMAL" or "HIGH" or "VERY_HIGH"
            ? Spotify.AudioQuality : "VERY_HIGH";
        YouTube.Download ??= new();
        // Keep the default implicit. Resolving the user's Downloads known folder calls into the shell and
        // is only needed when a download is actually requested.
        YouTube.Download.Folder = YouTube.Download.Folder?.Trim() ?? string.Empty;
        YouTube.Download.ConcurrentFragments = Math.Clamp(YouTube.Download.ConcurrentFragments, 1, 16);
        YouTube.Download.SimultaneousDownloads = Math.Clamp(YouTube.Download.SimultaneousDownloads, 1, 16);
        // Only that it is one of the rates the player offers at all. Whether the chosen format can be
        // written at it is a question for the encoder, asked when a recording starts rather than here:
        // this runs at load, and loading an encoder to interrogate it is not something to do then.
        Recording.SampleRate = LunaPlayer.Recording.AudioCatalog.SampleRates.Contains(Recording.SampleRate)
            ? Recording.SampleRate : 44100;
        Recording.Channels = Math.Clamp(Recording.Channels, 1, 2);
        // Only sanity bounds. What a format will actually accept is asked of Windows when the list is
        // shown, and differs with the rate and the channel count, so it cannot be settled here.
        Recording.Bitrate = Math.Clamp(Recording.Bitrate, 8000, 512000);
        // Keep the default implicit. Resolving Documents can call into the Windows shell and is only needed
        // when recording is opened or started.
        Recording.Folder = Recording.Folder?.Trim() ?? string.Empty;
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
        Shortcuts.Global ??= [];
    }

    /// <summary>Normalizes shortcut overrides after localization has initialized the translated action
    /// definitions. This remains on the UI thread because their labels come from wxWidgets.</summary>
    internal void NormalizeShortcuts()
    {
        var shortcutManager = new ShortcutManager(ActionRegistry.All);
        shortcutManager.Apply(Shortcuts.Primary, Shortcuts.Secondary);
        Shortcuts.Primary = shortcutManager.PrimaryOverrides();
        Shortcuts.Secondary = shortcutManager.SecondaryOverrides();
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

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Settings))]
internal partial class SettingsJsonContext : JsonSerializerContext;
