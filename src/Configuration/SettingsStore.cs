using System.Globalization;
using System.Text.Json;

namespace LunaPlayer.Configuration;

internal sealed class SettingsStore
{
    private readonly string _jsonPath;
    private readonly string _legacyIniPath;

    /// <summary>Why the last read or write failed, or an empty string when it did not. Kept because a
    /// message telling the user only that something could not be saved leaves them nothing to act on.
    /// </summary>
    internal string LastError { get; private set; } = string.Empty;

    internal SettingsStore(string jsonPath, string legacyIniPath)
    {
        _jsonPath = jsonPath;
        _legacyIniPath = legacyIniPath;
    }

    internal string Path => _jsonPath;

    internal PlayerSettings Load()
    {
        var settings = File.Exists(_jsonPath) && TryRead(_jsonPath, out var stored) ? stored : LoadLegacyIni();
        settings.Validate();
        return settings;
    }

    internal bool SaveExplicit(PlayerSettings settings)
    {
        settings.Validate();
        return Write(settings);
    }

    internal bool SaveSession(PlayerSettings settings)
        => !settings.General.SaveOnClose || SaveExplicit(settings);

    internal bool TryRead(string path, out PlayerSettings settings)
    {
        try
        {
            using var stream = File.OpenRead(path);
            settings = JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.PlayerSettings)
                ?? throw new JsonException("The settings file is empty.");
            settings.Validate();
            LastError = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            LastError = exception.Message;
            settings = new PlayerSettings();
            return false;
        }
    }

    internal PlayerSettings Reset()
    {
        var settings = new PlayerSettings();
        SaveExplicit(settings);
        return settings;
    }

    private PlayerSettings LoadLegacyIni()
    {
        var settings = new PlayerSettings();
        if (!File.Exists(_legacyIniPath)) return settings;
        try
        {
            var values = IniReader.Read(_legacyIniPath);
            settings.Audio.Volume = IniReader.Double(values, "audio", "volume", 100);
            settings.Audio.Speed = IniReader.Double(values, "audio", "speed", 1);
            settings.Audio.Device = IniReader.Value(values, "audio", "device") ?? string.Empty;
            settings.Audio.VolumeStep = IniReader.Integer(values, "audio", "volume_step", 5);
            settings.Audio.SpeedStep = IniReader.Double(values, "audio", "speed_step", 0.1);
            settings.Audio.SeekStepKey = IniReader.Value(values, "audio", "seek_step_key") ?? "2";
            settings.Audio.CustomSeekStep = IniReader.Double(values, "audio", "seek_step_custom", 5);
            settings.Audio.EndBehavior = ParseEndBehavior(IniReader.Value(values, "audio", "end_behavior"));
            settings.Audio.WrapPlaylist = IniReader.Boolean(values, "audio", "wrap_playlist");
            settings.Audio.SaveFilePositions = IniReader.Boolean(values, "audio", "save_file_pos");
            settings.Audio.NormalizeAudio = IniReader.Boolean(values, "audio", "audio_normalize_enabled", true);
            settings.Audio.MonoAudio = IniReader.Boolean(values, "audio", "audio_mono_enabled");
            settings.General.LastDirectory = IniReader.Value(values, "ui", "last_dir") ?? string.Empty;
            settings.General.Verbosity = string.Equals(IniReader.Value(values, "ui", "verbosity"), "advanced", StringComparison.OrdinalIgnoreCase)
                ? SpeechVerbosity.Advanced : SpeechVerbosity.Beginner;
            settings.General.OpenFilesMode = ParseOpenMode(IniReader.Value(values, "ui", "open_with_files_mode"));
            settings.General.SaveOnClose = IniReader.Boolean(values, "ui", "save_on_close", true);
            settings.General.CheckUpdatesOnStartup = IniReader.Boolean(values, "ui", "check_app_updates", true);
            settings.General.Language = IniReader.Value(values, "ui", "language") ?? "system";
            settings.General.SpeakFileOnNavigation = IniReader.Boolean(values, "ui", "speak_file_on_nav");
            settings.General.RememberLastPosition = IniReader.Boolean(values, "playback", "remember_position");
            settings.Playback.LastFile = IniReader.Value(values, "playback", "last_file") ?? string.Empty;
            settings.Playback.LastPosition = IniReader.Double(values, "playback", "last_position", 0);
            settings.Silence.Enabled = IniReader.Boolean(values, "silence_removal", "enabled");
            settings.Silence.Advanced = IniReader.Boolean(values, "silence_removal", "advanced");
            settings.Silence.StartPeriods = IniReader.Integer(values, "silence_removal", "start_periods", 1);
            settings.Silence.StartDuration = IniReader.Double(values, "silence_removal", "start_duration", 0.2);
            settings.Silence.Threshold = IniReader.Double(values, "silence_removal", "threshold", -30);
            settings.Silence.StopPeriods = IniReader.Integer(values, "silence_removal", "stop_periods", -1);
            settings.Silence.StopDuration = IniReader.Double(values, "silence_removal", "stop_duration", 0.5);
            settings.Silence.StopSilence = IniReader.Double(values, "silence_removal", "stop_silence", 0.2);
            settings.Silence.Window = IniReader.Double(values, "silence_removal", "window", 0.02);
            settings.Silence.Detection = string.Equals(IniReader.Value(values, "silence_removal", "detection"), "rms", StringComparison.OrdinalIgnoreCase)
                ? SilenceDetection.Rms : SilenceDetection.Peak;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new PlayerSettings();
        }
        settings.Validate();
        Write(settings);
        return settings;
    }

    private bool Write(PlayerSettings settings)
    {
        try
        {
            Paths.EnsureDirectoryFor(_jsonPath);
            var temporary = Paths.TemporaryFor(_jsonPath);
            using (var stream = File.Create(temporary))
                JsonSerializer.Serialize(stream, settings, SettingsJsonContext.Default.PlayerSettings);
            File.Move(temporary, _jsonPath, overwrite: true);
            LastError = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LastError = exception.Message;
            return false;
        }
    }

    private static EndBehavior ParseEndBehavior(string? value) => value?.ToLowerInvariant() switch
    { "loop" => EndBehavior.Loop, "none" => EndBehavior.None, _ => EndBehavior.Advance };
    private static OpenFilesMode ParseOpenMode(string? value) => value?.ToLowerInvariant() switch
    { "main_folder" => OpenFilesMode.MainFolder, "main_and_subfolders" => OpenFilesMode.MainAndSubfolders, _ => OpenFilesMode.FileOnly };

    private static class IniReader
    {
        internal static Dictionary<string, Dictionary<string, string>> Read(string path)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string>? section = null;
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] is ';' or '#') continue;
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    var name = line[1..^1].Trim();
                    if (!result.TryGetValue(name, out section)) result[name] = section = new(StringComparer.OrdinalIgnoreCase);
                    continue;
                }
                var separator = line.IndexOf('=');
                if (section is not null && separator > 0) section[line[..separator].Trim()] = line[(separator + 1)..].Trim();
            }
            return result;
        }
        internal static string? Value(Dictionary<string, Dictionary<string, string>> values, string section, string key)
            => values.TryGetValue(section, out var entries) && entries.TryGetValue(key, out var value) ? value : null;
        internal static double Double(Dictionary<string, Dictionary<string, string>> values, string section, string key, double fallback)
            => double.TryParse(Value(values, section, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        internal static int Integer(Dictionary<string, Dictionary<string, string>> values, string section, string key, int fallback)
            => int.TryParse(Value(values, section, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        internal static bool Boolean(Dictionary<string, Dictionary<string, string>> values, string section, string key, bool fallback = false)
            => bool.TryParse(Value(values, section, key), out var value) ? value : fallback;
    }
}
