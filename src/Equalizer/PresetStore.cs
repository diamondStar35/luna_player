using System.Text.Json;
using LunaPlayer.Configuration;

namespace LunaPlayer.Equalizer;

/// <summary>The presets the user has made, kept in their own file.</summary>
///
/// <remarks>
/// Apart from the settings, and deliberately. A preset is content rather than configuration: it is worth
/// keeping when settings are reset, worth copying between machines on its own, and the settings file has
/// a validator that refuses the whole file when one value in it is wrong - which is not a fate a list of
/// presets should share.
///
/// What the user changed about a preset the player ships with is a different thing and does not live
/// here; that is an override, and it belongs with the settings because it is meaningless without the
/// preset it overrides. See <see cref="Library"/>, which puts the two together.
/// </remarks>
internal sealed class PresetStore
{
    private readonly string _path;

    internal PresetStore(string path) => _path = path;

    /// <summary>Why the last read or write failed, or an empty string when it did not.</summary>
    internal string LastError { get; private set; } = string.Empty;

    /// <summary>Every preset the user has made, oldest first.</summary>
    internal IReadOnlyList<StoredPreset> ListAll()
        => TryLoad(out var document)
            ? [.. document.Presets.OrderBy(preset => preset.Created)]
            : [];

    /// <summary>Saves a new preset and gives back the identifier it was filed under, or null when it
    /// could not be written.</summary>
    internal string? Add(string name, IReadOnlyList<Band> bands)
    {
        if (!TryLoad(out var document))
            return null;
        var preset = new StoredPreset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
            Bands = [.. bands.Select(Store)],
            Created = Precision.Normalize(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0),
        };
        document.Presets.Add(preset);
        return Save(document) ? preset.Id : null;
    }

    internal bool Update(string id, string name, IReadOnlyList<Band> bands)
    {
        if (!TryLoad(out var document))
            return false;
        var preset = document.Presets.Find(stored => string.Equals(stored.Id, id, StringComparison.Ordinal));
        if (preset is null)
            return false;
        preset.Name = name.Trim();
        preset.Bands = [.. bands.Select(Store)];
        return Save(document);
    }

    internal bool Delete(string id)
    {
        if (!TryLoad(out var document))
            return false;
        return document.Presets.RemoveAll(
            stored => string.Equals(stored.Id, id, StringComparison.Ordinal)) > 0 && Save(document);
    }

    private static StoredBand Store(Band band) => new()
    {
        Frequency = band.Frequency,
        Q = band.Q,
        Gain = band.Gain,
        Type = band.Type,
    };

    /// <summary>Reads the file, or gives back an empty document when there is not one yet. A file that
    /// cannot be read is a failure rather than an empty list, so that a write does not go on to replace
    /// presets that are still there but momentarily unreadable.</summary>
    private bool TryLoad(out PresetDocument document)
    {
        document = new PresetDocument();
        if (!File.Exists(_path))
        {
            LastError = string.Empty;
            return true;
        }
        try
        {
            using var stream = File.OpenRead(_path);
            document = JsonSerializer.Deserialize(stream, PresetJsonContext.Default.PresetDocument)
                ?? new PresetDocument();
            foreach (var preset in document.Presets)
                preset.Bands ??= [];
            LastError = string.Empty;
            return true;
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            LastError = exception.Message;
            return false;
        }
    }

    private bool Save(PresetDocument document)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? Paths.RootDirectory);
            using (var stream = File.Create(_path))
                JsonSerializer.Serialize(stream, document, PresetJsonContext.Default.PresetDocument);
            LastError = string.Empty;
            return true;
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            LastError = exception.Message;
            return false;
        }
    }
}
