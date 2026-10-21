using LunaPlayer.Configuration.Models;
using System.Text.Json;

namespace LunaPlayer.Configuration;

internal sealed class SettingsStore
{
    private readonly string _jsonPath;
    private bool _writeBlocked;

    /// <summary>Why the last read or write failed, or an empty string when it did not. Kept because a
    /// message telling the user only that something could not be saved leaves them nothing to act on.
    /// </summary>
    internal string LastError { get; private set; } = string.Empty;

    internal SettingsStore(string jsonPath) => _jsonPath = jsonPath;

    internal string Path => _jsonPath;

    /// <summary>Reads only the saved theme from the settings file, without the full <see cref="Load"/>. The
    /// complete load validates the settings, which builds the translated action tables and so needs a
    /// running <c>App</c>; the theme is needed earlier than that, because it is applied as a wxWidgets system
    /// option before the App is created. Falls back to <see cref="AppTheme.SystemDefault"/> when the file is
    /// absent, unreadable, or has no valid theme.</summary>
    internal static AppTheme PeekTheme(string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath))
                return AppTheme.SystemDefault;
            using var stream = File.OpenRead(jsonPath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.TryGetProperty("general", out var general)
                && general.TryGetProperty("theme", out var theme)
                && theme.ValueKind == JsonValueKind.String
                && Enum.TryParse<AppTheme>(theme.GetString(), ignoreCase: true, out var parsed)
                && Enum.IsDefined(parsed))
                return parsed;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
        }
        return AppTheme.SystemDefault;
    }

    /// <summary>Reads only the setting that decides where a shell-opened file goes. A secondary process
    /// needs this before constructing the application, so loading and validating the complete settings
    /// object would both do unnecessary work and require wxWidgets to be initialized.</summary>
    internal static bool PeekOpenFilesInExistingWindow(string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath))
                return true;
            using var stream = File.OpenRead(jsonPath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.TryGetProperty("general", out var general)
                && general.TryGetProperty("openFilesInExistingWindow", out var value)
                && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return value.GetBoolean();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
        }
        return true;
    }

    internal Settings Load() => Load(normalize: true);

    /// <summary>Reads and structurally validates settings on a worker during application startup. The host
    /// normalizes only shortcuts after it has initialized localization on the UI thread, because shortcut
    /// normalization builds the translated action table.</summary>
    internal Settings LoadForStartup()
    {
        var settings = Load(normalize: false);
        settings.NormalizeValues();
        return settings;
    }

    private Settings Load(bool normalize)
    {
        if (!File.Exists(_jsonPath))
        {
            LastError = string.Empty;
            _writeBlocked = false;
            return new Settings();
        }
        if (TryReadFile(_jsonPath, normalize, out var settings, out var error))
        {
            LastError = string.Empty;
            _writeBlocked = false;
            return settings;
        }
        LastError = error;
        _writeBlocked = true;
        return new Settings();
    }

    internal bool SaveExplicit(Settings settings)
    {
        if (_writeBlocked)
            return false;
        settings.Validate();
        return Write(settings);
    }

    /// <summary>Replaces a settings file that failed to load after the user imports a valid file or confirms
    /// a reset.</summary>
    internal bool ReplaceExplicit(Settings settings)
    {
        settings.Validate();
        var written = Write(settings);
        if (written)
            _writeBlocked = false;
        return written;
    }

    /// <summary>Validates and snapshots the settings on the caller's thread, then performs only the file
    /// write on a worker. Shortcut validation uses localized action definitions and must remain on the wx
    /// thread; the disk access is the part that can be delayed by storage or antivirus during shutdown.</summary>
    internal Task<bool> SaveSessionAsync(Settings settings)
    {
        if (!settings.General.SaveOnClose)
            return Task.FromResult(true);
        if (_writeBlocked)
            return Task.FromResult(false);
        settings.Validate();
        var content = JsonSerializer.SerializeToUtf8Bytes(
            settings, SettingsJsonContext.Default.Settings);
        return Task.Run(() => Write(content));
    }

    internal bool TryRead(string path, out Settings settings)
    {
        var read = TryReadFile(path, normalize: true, out settings, out var error);
        LastError = error;
        return read;
    }

    private static bool TryReadFile(
        string path,
        bool normalize,
        out Settings settings,
        out string error)
    {
        try
        {
            using var stream = File.OpenRead(path);
            settings = JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.Settings)
                ?? throw new JsonException("The settings file is empty.");
            settings.ValidateStored();
            if (normalize)
                settings.Validate();
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            settings = new Settings();
            error = exception.Message;
            return false;
        }
    }

    private bool Write(Settings settings)
        => Write(JsonSerializer.SerializeToUtf8Bytes(settings, SettingsJsonContext.Default.Settings));

    private bool Write(byte[] content)
    {
        var written = AtomicFile.WriteBytes(_jsonPath, content, out var error);
        LastError = error;
        return written;
    }

}
