using System.Text.Json.Serialization;
using LunaPlayer.Actions;

namespace LunaPlayer.Configuration;

internal enum SpeechVerbosity { Beginner, Advanced }
internal enum OpenFilesMode { FileOnly, MainFolder, MainAndSubfolders }
internal enum EndBehavior { Advance, Loop, None }
internal enum SilenceDetection { Peak, Rms }

internal sealed class PlayerSettings
{
    public int Version { get; set; } = 2;
    public GeneralSettings General { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public SilenceSettings Silence { get; set; } = new();
    public ShortcutSettings Shortcuts { get; set; } = new();

    internal PlayerSettings Copy() => new()
    {
        Version = Version,
        General = General.Copy(),
        Audio = Audio.Copy(),
        Playback = Playback.Copy(),
        Silence = Silence.Copy(),
        Shortcuts = Shortcuts.Copy(),
    };

    internal void Apply(PlayerSettings source)
    {
        Version = Math.Max(2, source.Version);
        General.Apply(source.General);
        Audio.Apply(source.Audio);
        Playback.Apply(source.Playback);
        Silence.Apply(source.Silence);
        Shortcuts.Apply(source.Shortcuts);
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
        Silence.StartPeriods = Math.Max(0, Silence.StartPeriods);
        Silence.StartDuration = Math.Max(0, Silence.StartDuration);
        Silence.StopPeriods = Math.Max(-1, Silence.StopPeriods);
        Silence.StopDuration = Math.Max(0, Silence.StopDuration);
        Silence.StopSilence = Math.Max(0, Silence.StopSilence);
        Silence.Window = Silence.Window > 0 ? Silence.Window : 0.02;
        General.LastDirectory ??= string.Empty;
        General.Language = string.IsNullOrWhiteSpace(General.Language) ? "system" : General.Language.Trim();
        Audio.Device ??= string.Empty;
        Playback.LastFile ??= string.Empty;
        Shortcuts.Primary ??= [];
        Shortcuts.Secondary ??= [];
        var shortcutManager = new ShortcutManager(ActionRegistry.All);
        shortcutManager.Apply(Shortcuts.Primary, Shortcuts.Secondary);
        Shortcuts.Primary = shortcutManager.PrimaryOverrides();
        Shortcuts.Secondary = shortcutManager.SecondaryOverrides();
    }
}

internal sealed class GeneralSettings
{
    public string Language { get; set; } = "system";
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
    internal ShortcutSettings Copy() => new()
    {
        Primary = new(Primary),
        Secondary = new(Secondary),
    };
    internal void Apply(ShortcutSettings source)
    {
        Primary = new(source.Primary);
        Secondary = new(source.Secondary);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PlayerSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext;
