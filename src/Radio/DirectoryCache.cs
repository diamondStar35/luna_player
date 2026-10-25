using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Configuration;

namespace LunaPlayer.Radio;

internal sealed record CachedDirectory(Category Category, uint Offset, DirectoryPage Page, DateTimeOffset SavedAt);

internal sealed class DirectoryDocument
{
    public int Version { get; init; } = 2;
    public List<CachedDirectory> Pages { get; init; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DirectoryDocument))]
internal partial class DirectorySerialization : JsonSerializerContext;

/// <summary>Bounded, persistent directory metadata. No stream content or station-result pages are cached here.</summary>
internal sealed class DirectoryCache
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);
    private const int MaximumPages = 128;
    private readonly object _gate = new();
    private readonly Dictionary<(Category Category, uint Offset), CachedDirectory> _pages = [];
    private Task? _loading;
    private bool _writing;
    private int _revision;
    private static string FilePath => Path.Combine(Paths.RootDirectory, "radio-directory.json");

    private Task LoadAsync()
    {
        lock (_gate) return _loading ??= Task.Run(Load);
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            var document = JsonSerializer.Deserialize(File.ReadAllBytes(FilePath), DirectorySerialization.Default.DirectoryDocument);
            if (document is not { Version: 2, Pages: not null }) return;
            var now = DateTimeOffset.UtcNow;
            lock (_gate)
            {
                foreach (var entry in document.Pages.Where(entry => entry is not null)
                    .OrderByDescending(entry => entry.SavedAt).Take(MaximumPages))
                {
                    if (entry.Category is null || entry.Page?.Items is null || entry.SavedAt > now
                        || now - entry.SavedAt >= Lifetime
                        || entry.Page.Items.Any(item => item is null || item.Category is null || item.Name is null)) continue;
                    _pages.TryAdd((entry.Category, entry.Offset), entry);
                }
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            // Directory metadata is replaceable. An unreadable cache must not block fresh API requests.
        }
    }

    internal async Task<DirectoryPage?> GetAsync(Category category, uint offset, CancellationToken token)
    {
        await LoadAsync().WaitAsync(token).ConfigureAwait(false);
        lock (_gate)
            return _pages.TryGetValue((category, offset), out var entry)
                && DateTimeOffset.UtcNow - entry.SavedAt < Lifetime ? entry.Page : null;
    }

    internal async Task InvalidateAsync(Category category, CancellationToken token)
    {
        await LoadAsync().WaitAsync(token).ConfigureAwait(false);
        lock (_gate)
        {
            foreach (var key in _pages.Keys.Where(key => key.Category == category).ToArray()) _pages.Remove(key);
            QueueWrite();
        }
    }

    internal void Store(Category category, uint offset, DirectoryPage page)
    {
        lock (_gate)
        {
            _pages[(category, offset)] = new CachedDirectory(category, offset, page, DateTimeOffset.UtcNow);
            while (_pages.Count > MaximumPages)
                _pages.Remove(_pages.MinBy(pair => pair.Value.SavedAt).Key);
            QueueWrite();
        }
    }

    private void QueueWrite()
    {
        _revision++;
        if (_writing) return;
        _writing = true;
        _ = Task.Run(Save);
    }

    private void Save()
    {
        while (true)
        {
            DirectoryDocument document;
            int revision;
            lock (_gate)
            {
                revision = _revision;
                document = new DirectoryDocument { Pages = _pages.Values.ToList() };
            }
            // A cache write is optional and never delays displaying the fetched directory. AtomicFile
            // preserves the previous file on failure; the next directory change can attempt another save.
            if (!AtomicFile.WriteJson(FilePath, document, DirectorySerialization.Default.DirectoryDocument, out _))
            {
                lock (_gate) _writing = false;
                return;
            }
            lock (_gate)
            {
                if (_revision != revision) continue;
                _writing = false;
                return;
            }
        }
    }
}
