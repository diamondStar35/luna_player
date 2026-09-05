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
        if (!TryLoadCurrent(out var document))
            return false;
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
        if (!TryLoadCurrent(out var document))
            return [];
        return document.Files.TryGetValue(Paths.Key(path), out var bookmarks)
            ? Sort(bookmarks.Where(value => value.Id.Length > 0 && value.Name.Length > 0 && value.Path.Length > 0))
            : [];
    }

    internal Bookmark? Add(string path, string name, double position)
    {
        if (!TryLoadCurrent(out var document))
            return null;
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
        return Save(document) ? bookmark : null;
    }

    internal bool Rename(string path, string id, string name)
    {
        if (!TryLoadCurrent(out var document))
            return false;
        if (!document.Files.TryGetValue(Paths.Key(path), out var bookmarks))
            return false;
        var bookmark = bookmarks.FirstOrDefault(value => value.Id == id);
        if (bookmark is null)
            return false;
        bookmark.Name = name;
        return Save(document);
    }

    internal bool Delete(string path, string id)
    {
        if (!TryLoadCurrent(out var document))
            return false;
        var key = Paths.Key(path);
        if (!document.Files.TryGetValue(key, out var bookmarks))
            return false;
        var removed = bookmarks.RemoveAll(value => value.Id == id) > 0;
        if (!removed)
            return false;
        if (bookmarks.Count == 0)
            document.Files.Remove(key);
        return Save(document);
    }

    internal Bookmark? Slot(string path, int slot)
        => slot is >= 1 and <= 10 ? ListFor(path).ElementAtOrDefault(slot - 1) : null;

    /// <summary>Loads the live file. A missing file is a valid empty store; a file that exists but cannot
    /// be read is an error and must not be replaced by a later mutation.</summary>
    private bool TryLoadCurrent(out BookmarkDocument document)
    {
        if (!File.Exists(_path))
        {
            document = new BookmarkDocument();
            return Report(true, string.Empty);
        }
        return Report(TryLoad(_path, out document, out var error), error);
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
            Validate(document);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            error = exception.Message;
            document = new BookmarkDocument();
            return false;
        }
    }

    /// <summary>Rejects a document that parsed as JSON but is not a usable bookmarks document. Treating
    /// malformed collections or entries as empty would let the next change replace the original file and
    /// silently discard everything it contained.</summary>
    private static void Validate(BookmarkDocument document)
    {
        if (document.Files is null)
            throw new JsonException("The bookmarks file does not contain a valid files collection.");
        foreach (var (key, bookmarks) in document.Files)
        {
            if (string.IsNullOrWhiteSpace(key) || bookmarks is null)
                throw new JsonException("The bookmarks file contains an invalid file entry.");
            foreach (var bookmark in bookmarks)
            {
                if (bookmark is null
                    || string.IsNullOrWhiteSpace(bookmark.Id)
                    || string.IsNullOrWhiteSpace(bookmark.Name)
                    || string.IsNullOrWhiteSpace(bookmark.Path)
                    || !double.IsFinite(bookmark.Position)
                    || bookmark.Position < 0)
                {
                    throw new JsonException("The bookmarks file contains an invalid bookmark.");
                }
            }
        }
    }

    private bool Save(BookmarkDocument document)
        => Report(SaveTo(_path, document, out var error), error);

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
