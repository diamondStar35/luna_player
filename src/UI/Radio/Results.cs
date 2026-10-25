using LunaPlayer.Radio;

namespace LunaPlayer.UI.Radio;

internal sealed record ResultRow(string[] Cells, Station Station);

/// <summary>Managed rows for one browsing location. Native list items are never stored or rebuilt.</summary>
internal sealed class Results(Category category, StationView? view)
{
    private readonly Dictionary<Guid, int> _stations = [];

    internal Category Category { get; } = category;
    internal StationView? View { get; } = view;
    internal int Revision { get; } = view?.Revision ?? 0;
    internal List<ResultRow> Rows { get; } = [];
    internal bool IsDirectory => View is null;
    internal bool HasMore => View?.HasMore ?? false;
    internal bool Loaded => View?.Loaded ?? true;
    internal bool Loading { get; set; }

    internal void Append(IReadOnlyList<ResultRow> rows)
    {
        foreach (var row in rows)
        {
            if (!_stations.TryAdd(row.Station.StationUuid, Rows.Count)) continue;
            View!.Add(row.Station);
            Rows.Add(row);
        }
    }

    internal void Remove(Guid id)
    {
        if (!_stations.TryGetValue(id, out var index)) return;
        _stations.Remove(id);
        Rows.RemoveAt(index);
        for (var row = index; row < Rows.Count; row++)
            _stations[Rows[row].Station.StationUuid] = row;
        View!.Remove(id);
        View.Offset = (uint)View.Stations.Count;
    }

    internal int UpdateStation(Guid id, Func<Station, ResultRow> text)
    {
        if (!_stations.TryGetValue(id, out var index)) return -1;
        Rows[index] = text(Rows[index].Station);
        return index;
    }

    internal bool Update(ResultRow row)
    {
        if (!_stations.TryGetValue(row.Station.StationUuid, out var index)) return false;
        Rows[index] = row;
        return true;
    }
}

/// <summary>Templates are translated on the UI thread; row formatting can then run on a worker.</summary>
internal sealed class RowText(Library library)
{
    // Translators: Name of a saved station in the radio list. {name} is the station's name.
    private readonly string _favorite = Tr("{name} (favorite)");
    // Translators: Radio stream bitrate in a station-list cell. {bitrate} is in kilobits per second.
    private readonly string _bitrate = Tr("{bitrate} kbps");

    internal ResultRow Station(Station station) => new([
        library.IsFavorite(station.StationUuid) ? FormatTranslatedValue(_favorite, station.Name) : station.Name,
        station.Country, station.Language, station.Codec,
        station.Bitrate > 0 ? FormatTranslatedValue(_bitrate, station.Bitrate) : string.Empty,
    ], Station: station);
}
