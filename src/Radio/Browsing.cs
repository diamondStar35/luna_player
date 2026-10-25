namespace LunaPlayer.Radio;

internal enum CategoryKind
{
    All, Favorites, Recent, Discover, MostPlayed, Trending, MostVoted, RecentClicks, Updated,
    Countries, Country, Regions, Region, Languages, Language, Tags, Tag, Codecs, Codec,
}

/// <summary>A directory location, independent of its translated tree label.</summary>
internal sealed record Category(CategoryKind Kind, string Value = "", string CountryCode = "", string CountryName = "")
{
    internal bool IsContainer => Kind is CategoryKind.Discover or CategoryKind.Countries or CategoryKind.Country
        or CategoryKind.Regions or CategoryKind.Languages or CategoryKind.Tags or CategoryKind.Codecs;
}

internal sealed record DirectoryItem(Category Category, string Name, uint StationCount);
internal sealed record DirectoryPage(IReadOnlyList<DirectoryItem> Items, uint NextOffset, bool HasMore);
internal sealed record StationPage(IReadOnlyList<Station> Stations, uint NextOffset, bool HasMore);

/// <summary>Additional restrictions. Empty category filters leave the global restrictions intact.</summary>
internal sealed record Filters
{
    public string CountryCode { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public string Codec { get; init; } = string.Empty;
    public uint MinimumBitrate { get; init; }
    public uint MaximumBitrate { get; init; }
    public bool OnlyWorking { get; init; }
    public bool HttpsOnly { get; init; }

    internal bool Matches(Station station)
        => (CountryCode.Length == 0 || station.CountryCode.Equals(CountryCode, StringComparison.OrdinalIgnoreCase))
        && (Region.Length == 0 || station.State.Equals(Region, StringComparison.OrdinalIgnoreCase))
        && (Language.Length == 0 || Contains(station.Language, Language))
        && Terms(Tags).All(tag => Contains(station.Tags, tag))
        && (Codec.Length == 0 || station.Codec.Equals(Codec, StringComparison.OrdinalIgnoreCase))
        && station.Bitrate >= MinimumBitrate
        && (MaximumBitrate == 0 || station.Bitrate <= MaximumBitrate)
        && (!OnlyWorking || station.LastCheckOk)
        && (!HttpsOnly || Catalog.StreamUrl(station)?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true);

    internal static string[] Terms(string value)
        => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static bool Contains(string values, string term)
        => Terms(values).Contains(term, StringComparer.OrdinalIgnoreCase);
}

internal sealed record FilterChoice(Filters Filters, bool Global);

/// <summary>A loaded view is retained when another category is selected. Playback takes a separate copy.</summary>
internal sealed class StationView(Category category)
{
    private readonly HashSet<Guid> _stationIds = [];
    internal Category Category { get; } = category;
    internal string Query { get; set; } = string.Empty;
    internal Filters GlobalFilters { get; set; } = new() { OnlyWorking = true };
    internal Filters LocalFilters { get; set; } = new();
    internal List<Station> Stations { get; } = [];
    internal uint Offset { get; set; }
    internal bool HasMore { get; set; } = true;
    internal bool Loaded { get; set; }
    internal Guid? Selected { get; set; }
    internal int Revision { get; private set; }
    internal List<Station> Updates { get; } = [];

    internal void Add(Station station)
    {
        if (_stationIds.Add(station.StationUuid)) Stations.Add(station);
    }

    internal void Append(StationPage page)
    {
        foreach (var station in page.Stations) Add(station);
        Offset = page.NextOffset;
        HasMore = page.HasMore;
        Loaded = true;
    }

    internal StationView Snapshot()
    {
        var copy = Request();
        copy.Stations.AddRange(Stations);
        copy._stationIds.UnionWith(_stationIds);
        return copy;
    }

    internal StationView Request() => new(Category)
    {
        Query = Query, GlobalFilters = GlobalFilters, LocalFilters = LocalFilters,
        Offset = Offset, HasMore = HasMore, Loaded = Loaded, Selected = Selected,
    };

    internal void Reset()
    {
        Stations.Clear();
        _stationIds.Clear();
        Updates.Clear();
        Offset = 0;
        HasMore = true;
        Loaded = false;
        Revision++;
    }

    internal void Restore(StationView source)
    {
        // Playback owns a snapshot and can append pages or refresh station metadata. Preserve the
        // browser's source identity when its existing rows still form the same ordered prefix.
        var prefix = Query == source.Query && GlobalFilters == source.GlobalFilters && LocalFilters == source.LocalFilters
            && Stations.Count <= source.Stations.Count;
        for (var index = 0; prefix && index < Stations.Count; index++)
            prefix = Stations[index].StationUuid == source.Stations[index].StationUuid;
        if (!prefix) Reset();
        var previousCount = Stations.Count;
        for (var index = 0; index < previousCount; index++)
        {
            var station = source.Stations[index];
            if (ReferenceEquals(Stations[index], station)) continue;
            Stations[index] = station;
            Updates.Add(station);
        }
        foreach (var station in source.Stations.Skip(previousCount)) Add(station);
        Query = source.Query;
        GlobalFilters = source.GlobalFilters;
        LocalFilters = source.LocalFilters;
        Offset = source.Offset;
        HasMore = source.HasMore;
        Loaded = source.Loaded;
        Selected = source.Selected;
    }

    internal void Remove(Guid id)
    {
        Stations.RemoveAll(station => station.StationUuid == id);
        _stationIds.Remove(id);
    }
}

/// <summary>UI-thread browsing state. No directories or stations are requested by construction.</summary>
internal sealed class Browsing
{
    private readonly Dictionary<Category, StationView> _views = [];
    private readonly Dictionary<Category, Filters> _filters = [];
    internal Category ActiveCategory { get; set; } = new(CategoryKind.All);
    internal Filters GlobalFilters { get; private set; } = new() { OnlyWorking = true };

    internal StationView GetView(Category category)
    {
        if (!_views.TryGetValue(category, out var view))
        {
            view = new StationView(category) { GlobalFilters = GlobalFilters, LocalFilters = LocalFilters(category) };
            _views.Add(category, view);
        }
        return view;
    }

    internal bool HasLocalFilters(Category category) => _filters.ContainsKey(category);
    internal Filters LocalFilters(Category category) => _filters.GetValueOrDefault(category) ?? new();

    internal void Invalidate(CategoryKind kind)
    {
        foreach (var view in _views.Values)
            if (view.Category.Kind == kind) view.Reset();
    }

    internal void Apply(Category category, FilterChoice choice)
    {
        if (choice.Global) GlobalFilters = choice.Filters;
        else _filters[category] = choice.Filters;
        foreach (var view in _views.Values)
        {
            if (!choice.Global && view.Category != category) continue;
            view.GlobalFilters = GlobalFilters;
            view.LocalFilters = LocalFilters(view.Category);
            view.Reset();
        }
    }

    internal void Restore(StationView view)
    {
        ActiveCategory = view.Category;
        GetView(view.Category).Restore(view);
    }

    internal static Category Root(Category category) => category.CountryCode.Length > 0
        ? new(CategoryKind.Countries) : category.Kind switch
        {
            CategoryKind.Country or CategoryKind.Region or CategoryKind.Regions => new(CategoryKind.Countries),
            CategoryKind.Language => new(CategoryKind.Languages),
            CategoryKind.Tag => new(CategoryKind.Tags),
            CategoryKind.Codec => new(CategoryKind.Codecs),
            _ => new(category.Kind),
        };

}
