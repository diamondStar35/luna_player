using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Configuration;

namespace LunaPlayer.Radio;

internal sealed record PlayedStation(Station Station, DateTimeOffset PlayedAt);

internal sealed class LibraryDocument
{
    public int Version { get; init; } = 1;
    public List<Station> Favorites { get; init; } = [];
    public List<PlayedStation> Recent { get; init; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(LibraryDocument))]
internal partial class LibrarySerialization : JsonSerializerContext;

/// <summary>Radio metadata only. Reads and serialized atomic writes run outside the UI thread.</summary>
internal sealed class Library
{
    private readonly object _gate = new();
    private readonly SemaphoreSlim _writes = new(1, 1);
    private Task? _loading;
    private LibraryDocument _document = new();
    private HashSet<Guid> _favorites = [];
    private static string FilePath => Path.Combine(Paths.RootDirectory, "radio.json");

    internal Task LoadAsync()
    {
        lock (_gate)
        {
            if (_loading is null || _loading.IsFaulted)
                _loading = Task.Run(Load);
            return _loading;
        }
    }

    private void Load()
    {
        if (!File.Exists(FilePath)) return;
        var document = JsonSerializer.Deserialize(File.ReadAllBytes(FilePath), LibrarySerialization.Default.LibraryDocument);
        if (document is null || document.Version != 1 || document.Favorites is null || document.Recent is null
            || document.Favorites.Any(station => station is null || station.StationUuid == Guid.Empty)
            || document.Recent.Any(item => item is null || item.Station is null || item.Station.StationUuid == Guid.Empty))
            throw new JsonException();
        var normalized = new LibraryDocument
        {
            Favorites = document.Favorites.Select(Catalog.Normalize).DistinctBy(station => station.StationUuid).ToList(),
            Recent = document.Recent.OrderByDescending(item => item.PlayedAt)
                .DistinctBy(item => item.Station.StationUuid).Take(100)
                .Select(item => item with { Station = Catalog.Normalize(item.Station) }).ToList(),
        };
        lock (_gate)
        {
            _document = normalized;
            Volatile.Write(ref _favorites, normalized.Favorites.Select(station => station.StationUuid).ToHashSet());
        }
    }

    // Published sets are never mutated. Virtual-list callbacks must not wait for a library writer.
    internal bool IsFavorite(Guid id) => Volatile.Read(ref _favorites).Contains(id);

    internal Station[] Favorites()
    {
        lock (_gate) return _document.Favorites.ToArray();
    }

    internal Station[] Recent()
    {
        lock (_gate) return _document.Recent.OrderByDescending(item => item.PlayedAt).Select(item => item.Station).ToArray();
    }

    internal async Task<bool> ToggleFavoriteAsync(Station station)
    {
        var added = false;
        await UpdateAsync(document =>
        {
            added = document.Favorites.RemoveAll(item => item.StationUuid == station.StationUuid) == 0;
            if (added) document.Favorites.Add(station);
        }).ConfigureAwait(false);
        return added;
    }

    internal Task RememberAsync(Station station) => UpdateAsync(document =>
    {
        document.Recent.RemoveAll(item => item.Station.StationUuid == station.StationUuid);
        document.Recent.Insert(0, new PlayedStation(station, DateTimeOffset.UtcNow));
        if (document.Recent.Count > 100) document.Recent.RemoveRange(100, document.Recent.Count - 100);
    });

    internal Task ClearHistoryAsync() => UpdateAsync(document => document.Recent.Clear());

    internal async Task FlushAsync()
    {
        // All mutations enter the same queue, including those initiated from a browser that just closed.
        await _writes.WaitAsync().ConfigureAwait(false);
        _writes.Release();
    }

    private async Task UpdateAsync(Action<LibraryDocument> update)
    {
        await LoadAsync().ConfigureAwait(false);
        await _writes.WaitAsync().ConfigureAwait(false);
        try
        {
            LibraryDocument next;
            lock (_gate) next = new LibraryDocument { Favorites = [.. _document.Favorites], Recent = [.. _document.Recent] };
            update(next);
            await Task.Run(() =>
            {
                if (!AtomicFile.WriteJson(FilePath, next, LibrarySerialization.Default.LibraryDocument, out var error))
                    throw new IOException(error);
            }).ConfigureAwait(false);
            lock (_gate)
            {
                _document = next;
                Volatile.Write(ref _favorites, next.Favorites.Select(station => station.StationUuid).ToHashSet());
            }
        }
        finally { _writes.Release(); }
    }
}
