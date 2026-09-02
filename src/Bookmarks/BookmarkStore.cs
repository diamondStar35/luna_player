using System.Text.Json;
using LunaPlayer.Configuration;

namespace LunaPlayer.Bookmarks;

internal sealed class BookmarkStore
{
    private readonly string _path;

    internal BookmarkStore(string path) => _path = path;
    internal string FilePath => _path;

    /// <summary>Why the last read or write failed, or an empty string when it did not.</summary>
    internal string LastError { get; private set; } = string.Empty;

    internal bool Export(string destination)
    {
        var document = Load();
        return Report(SaveTo(destination, document, out var error), error);
    }

    internal bool Import(string source)
    {
        if (!TryLoad(source, out var document, out var read)) return Report(false, read);
        return Report(SaveTo(_path, document, out var written), written);
    }

    private bool Report(bool success, string error)
    {
        LastError = success ? string.Empty : error;
        return success;
    }

    internal IReadOnlyList<Bookmark> ListFor(string path)
    {
        var document = Load();
        return document.Files.TryGetValue(Paths.Key(path), out var bookmarks)
            ? Sort(bookmarks.Where(value => value.Id.Length > 0 && value.Name.Length > 0 && value.Path.Length > 0))
            : [];
    }

    internal Bookmark Add(string path, string name, double position)
    {
        var document = Load();
        var key = Paths.Key(path);
        if (!document.Files.TryGetValue(key, out var bookmarks))
        {
            bookmarks = [];
            document.Files[key] = bookmarks;
        }
        var bookmark = new Bookmark
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Path = Paths.Absolute(path),
            Position = Math.Max(0, position),
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        bookmarks.Add(bookmark);
        Save(document);
        return bookmark;
    }

    internal bool Rename(string path, string id, string name)
    {
        var document = Load();
        if (!document.Files.TryGetValue(Paths.Key(path), out var bookmarks))
            return false;
        var bookmark = bookmarks.FirstOrDefault(value => value.Id == id);
        if (bookmark is null)
            return false;
        bookmark.Name = name;
        Save(document);
        return true;
    }

    internal bool Delete(string path, string id)
    {
        var document = Load();
        var key = Paths.Key(path);
        if (!document.Files.TryGetValue(key, out var bookmarks))
            return false;
        var removed = bookmarks.RemoveAll(value => value.Id == id) > 0;
        if (!removed)
            return false;
        if (bookmarks.Count == 0)
            document.Files.Remove(key);
        Save(document);
        return true;
    }

    internal Bookmark? Slot(string path, int slot)
        => slot is >= 1 and <= 10 ? ListFor(path).ElementAtOrDefault(slot - 1) : null;

    private BookmarkDocument Load()
    {
        return TryLoad(_path, out var document, out _) ? document : new BookmarkDocument();
    }

    private static bool TryLoad(string path, out BookmarkDocument document, out string error)
    {
        error = string.Empty;
        if (!File.Exists(path))
        {
            error = $"The file was not found: {path}";
            document = new BookmarkDocument();
            return false;
        }
        try
        {
            using var stream = File.OpenRead(path);
            document = JsonSerializer.Deserialize(stream, BookmarkJsonContext.Default.BookmarkDocument)
                ?? throw new JsonException("The bookmarks file is empty.");
            document.Files ??= [];
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            error = exception.Message;
            document = new BookmarkDocument();
            return false;
        }
    }

    private void Save(BookmarkDocument document)
        => SaveTo(_path, document, out _);

    private static bool SaveTo(string path, BookmarkDocument document, out string error)
    {
        error = string.Empty;
        try
        {
            Paths.EnsureDirectoryFor(path);
            var temporaryPath = Paths.TemporaryFor(path);
            using (var stream = File.Create(temporaryPath))
                JsonSerializer.Serialize(stream, document, BookmarkJsonContext.Default.BookmarkDocument);
            File.Move(temporaryPath, path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error = exception.Message;
            return false;
        }
    }

    private static IReadOnlyList<Bookmark> Sort(IEnumerable<Bookmark> bookmarks)
        => bookmarks.OrderBy(value => value.Position)
            .ThenBy(value => value.Created)
            .ThenBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
