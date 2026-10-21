using System.Text.Json;
using System.Text.Json.Serialization;

namespace LunaPlayer.Configuration;

/// <summary>Which list a recent path belongs to.</summary>
internal enum RecentKind { File, Folder, Playlist }

/// <summary>The recently-opened files, folders and playlists, newest first, kept in recents.json. Local paths
/// only, up to 32 per kind. Loads once and writes atomically, the same shape as <see cref="PositionStore"/>.
/// </summary>
internal sealed class RecentsStore
{
    private const int MaxPerKind = 32;
    private readonly string _path;
    private readonly RecentsDocument _document;

    internal RecentsStore(string path)
    {
        _path = path;
        _document = Load();
    }

    /// <summary>The paths of one kind, newest first.</summary>
    internal IReadOnlyList<string> Get(RecentKind kind) => List(kind);

    /// <summary>Records a path at the top of its kind, moving an existing entry up rather than duplicating it
    /// (case-insensitively), and dropping the oldest once the kind holds more than 32.</summary>
    internal bool Add(RecentKind kind, string path)
    {
        var absolute = Paths.Absolute((path ?? string.Empty).Trim());
        if (absolute.Length == 0)
            return false;
        var list = List(kind);
        // Restoring the last file usually selects the first recent entry again. There is no change to save
        // or announce to native menus in that case, and both operations can delay playback on slow systems.
        if (list.Count > 0 && string.Equals(list[0], absolute, StringComparison.Ordinal))
            return false;
        list.RemoveAll(existing => string.Equals(existing, absolute, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, absolute);
        if (list.Count > MaxPerKind)
            list.RemoveRange(MaxPerKind, list.Count - MaxPerKind);
        Save();
        return true;
    }

    /// <summary>Removes one path from a kind - used when a recent turns out to be gone.</summary>
    internal void Remove(RecentKind kind, string path)
    {
        if (List(kind).RemoveAll(existing => string.Equals(existing, path, StringComparison.OrdinalIgnoreCase)) > 0)
            Save();
    }

    /// <summary>Empties one kind.</summary>
    internal void Clear(RecentKind kind)
    {
        var list = List(kind);
        if (list.Count == 0)
            return;
        list.Clear();
        Save();
    }

    private List<string> List(RecentKind kind) => kind switch
    {
        RecentKind.File => _document.Files,
        RecentKind.Folder => _document.Folders,
        _ => _document.Playlists,
    };

    // Drop paths no longer on disk, fold duplicates, and cap each kind. A deleted file simply stops
    // appearing; the cleaned lists stay in memory and are written out on the next change.
    private RecentsDocument Load()
    {
        var document = Read();
        Prune(document.Files, File.Exists);
        Prune(document.Playlists, File.Exists);
        Prune(document.Folders, Directory.Exists);
        return document;
    }

    private RecentsDocument Read()
    {
        if (!File.Exists(_path))
            return new();
        try
        {
            using var stream = File.OpenRead(_path);
            return JsonSerializer.Deserialize(stream, RecentsJsonContext.Default.RecentsDocument) ?? new();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    private static void Prune(List<string> paths, Func<string, bool> exists)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kept = new List<string>(paths.Count);
        foreach (var raw in paths)
        {
            var path = Paths.Absolute((raw ?? string.Empty).Trim());
            if (path.Length == 0 || !seen.Add(path) || !exists(path))
                continue;
            kept.Add(path);
            if (kept.Count >= MaxPerKind)
                break;
        }
        paths.Clear();
        paths.AddRange(kept);
    }

    private bool Save()
    {
        return AtomicFile.WriteJson(
            _path, _document, RecentsJsonContext.Default.RecentsDocument, out _);
    }
}

internal sealed class RecentsDocument
{
    public int Version { get; set; } = 1;
    public List<string> Files { get; set; } = [];
    public List<string> Folders { get; set; } = [];
    public List<string> Playlists { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RecentsDocument))]
internal partial class RecentsJsonContext : JsonSerializerContext;
