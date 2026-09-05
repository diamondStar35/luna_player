using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using LunaPlayer.Actions;
using LunaPlayer.Recording;

namespace LunaPlayer.Configuration;

internal enum SpeechVerbosity { Beginner, Advanced }
internal enum OpenFilesMode { FileOnly, MainFolder, MainAndSubfolders }
internal enum EndBehavior { Advance, Loop, None }
internal enum SilenceDetection { Peak, Rms }
internal enum YouTubeQuality { Low, Medium, Best }
internal enum MixedLinkBehavior { Ask, Video, Playlist }
internal enum YtDlpChannel { Stable, Nightly, Master }

internal sealed class PlayerSettings
{
    public int Version { get; set; } = 2;
    public GeneralSettings General { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public SilenceSettings Silence { get; set; } = new();
    public ShortcutSettings Shortcuts { get; set; } = new();
    public YouTubeSettings YouTube { get; set; } = new();
    public RecordingSettings Recording { get; set; } = new();

    internal PlayerSettings Copy() => new()
    {
        Version = Version,
        General = General.Copy(),
        Audio = Audio.Copy(),
        Playback = Playback.Copy(),
        Silence = Silence.Copy(),
        Shortcuts = Shortcuts.Copy(),
        YouTube = YouTube.Copy(),
        Recording = Recording.Copy(),
    };

    internal void Apply(PlayerSettings source)
    {
        Version = Math.Max(2, source.Version);
        General.Apply(source.General);
        Audio.Apply(source.Audio);
        Playback.Apply(source.Playback);
        Silence.Apply(source.Silence);
        Shortcuts.Apply(source.Shortcuts);
        YouTube.Apply(source.YouTube);
        Recording.Apply(source.Recording);
        Validate();
    }

    internal void Validate()
    {
        // Repeated step-based changes accumulate binary floating-point error; round so the stored
        // value stays the one the user actually selected rather than 1.0000000000000009.
        Audio.Volume = Math.Round(Math.Clamp(Audio.Volume, 0, 1000), 3);
        Audio.Speed = Math.Round(Math.Clamp(Audio.Speed, 0.5, 6), 3);
        Audio.VolumeStep = Math.Clamp(Audio.VolumeStep, 1, 20);
        Audio.SpeedStep = Audio.SpeedStep > 0 ? Audio.SpeedStep : 0.1;
        Audio.CustomSeekStep = Audio.CustomSeekStep > 0 ? Audio.CustomSeekStep : 5;
        Audio.SeekStepKey = Audio.SeekStepKey.Length == 1 && "1234567890-".Contains(Audio.SeekStepKey, StringComparison.Ordinal)
            ? Audio.SeekStepKey : "2";
        YouTube.SearchResultCount = Math.Clamp(YouTube.SearchResultCount, 5, 100);
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
        Silence.StartPeriods = Math.Max(0, Silence.StartPeriods);
        Silence.StartDuration = Math.Max(0, Silence.StartDuration);
        Silence.StopPeriods = Math.Max(-1, Silence.StopPeriods);
        Silence.StopDuration = Math.Max(0, Silence.StopDuration);
        Silence.StopSilence = Math.Max(0, Silence.StopSilence);
        Silence.Window = Silence.Window > 0 ? Silence.Window : 0.02;
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
        var globalManager = new ShortcutManager(GlobalActionDefinitions.All);
        globalManager.Apply(Shortcuts.Global, ReadOnlyDictionary<ActionId, Shortcut>.Empty);
        Shortcuts.Global = globalManager.PrimaryOverrides();
    }
}

internal sealed class GeneralSettings
{
    public string Language { get; set; } = Localization.SystemLanguage;
    public bool RememberLastPosition { get; set; }
    public bool SpeakFileOnNavigation { get; set; }
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public bool SaveOnClose { get; set; } = true;
    [JsonConverter(typeof(JsonStringEnumConverter<SpeechVerbosity>))]
    public SpeechVerbosity Verbosity { get; set; } = SpeechVerbosity.Beginner;
    [JsonConverter(typeof(JsonStringEnumConverter<OpenFilesMode>))]
    public OpenFilesMode OpenFilesMode { get; set; } = OpenFilesMode.FileOnly;
    public string LastDirectory { get; set; } = string.Empty;
    internal GeneralSettings Copy() => (GeneralSettings)MemberwiseClone();
    internal void Apply(GeneralSettings source)
    {
        Language = source.Language;
        RememberLastPosition = source.RememberLastPosition;
        SpeakFileOnNavigation = source.SpeakFileOnNavigation;
        CheckUpdatesOnStartup = source.CheckUpdatesOnStartup;
        SaveOnClose = source.SaveOnClose;
        Verbosity = source.Verbosity;
        OpenFilesMode = source.OpenFilesMode;
        LastDirectory = source.LastDirectory;
    }
}

internal sealed class AudioSettings
{
    public double Volume { get; set; } = 100;
    public double Speed { get; set; } = 1;
    public string Device { get; set; } = string.Empty;
    public int VolumeStep { get; set; } = 5;
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
        Device = source.Device;
        VolumeStep = source.VolumeStep;
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
    public bool AudioOnly { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter<YouTubeQuality>))]
    public YouTubeQuality Quality { get; set; } = YouTubeQuality.Medium;

    /// <summary>How many search results to ask for.</summary>
    /// <remarks>
    /// A target rather than an exact number: YouTube answers a search in batches of its own choosing, so the
    /// player keeps taking batches until it has this many and stops on the first one that takes it past.
    /// </remarks>
    public int SearchResultCount { get; set; } = 50;

    /// <summary>What to do with a link that names a video and a playlist at once.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter<MixedLinkBehavior>))]
    public MixedLinkBehavior MixedLink { get; set; } = MixedLinkBehavior.Ask;

    /// <summary>Whether streams are resolved with yt-dlp rather than the player's own resolver.</summary>
    ///
    /// <remarks>
    /// Nothing needs yt-dlp until this is on. It is the switch that makes the program an option rather
    /// than a requirement, and it decides whether the player ever offers to fetch it.
    /// </remarks>
    public bool UseYtDlp { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<YtDlpChannel>))]
    public YtDlpChannel Channel { get; set; } = YtDlpChannel.Stable;

    public bool CheckComponentUpdates { get; set; }

    /// <summary>Whether the user has asked not to be offered the download again.</summary>
    public bool SkipComponentPrompt { get; set; }

    internal YouTubeSettings Copy() => (YouTubeSettings)MemberwiseClone();

    internal void Apply(YouTubeSettings source)
    {
        AudioOnly = source.AudioOnly;
        Quality = source.Quality;
        SearchResultCount = source.SearchResultCount;
        MixedLink = source.MixedLink;
        UseYtDlp = source.UseYtDlp;
        Channel = source.Channel;
        CheckComponentUpdates = source.CheckComponentUpdates;
        SkipComponentPrompt = source.SkipComponentPrompt;
    }
}

/// <summary>How a recording is written, when nothing more particular has been asked for.</summary>
///
/// <remarks>
/// These are the defaults, and they are what the recording shortcuts use when no sources have been set
/// up - which is the whole of recording for somebody who never opens the recording window. The window
/// keeps its own copy of them for the session it is used in, so changing the format there for one
/// afternoon does not rewrite what the player starts with tomorrow.
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

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PlayerSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext;
