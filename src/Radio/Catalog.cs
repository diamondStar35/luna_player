using LunaPlayer.Radio.Client;

namespace LunaPlayer.Radio;

/// <summary>Turns browser categories and filters into bounded directory requests.</summary>
internal sealed class Catalog : IDisposable
{
    internal const uint PageSize = 100;
    internal const uint DirectoryPageSize = 200;
    private readonly Lazy<Browser> _client = new(() => new Browser(new Options { MaximumAttempts = 1 }));
    private readonly DirectoryCache _directories = new();
    internal Browser Client => _client.Value;

    internal async Task<StationPage> StationsAsync(StationView view, Library library, CancellationToken token,
        Func<IReadOnlyList<Station>, CancellationToken, ValueTask>? receive = null)
    {
        var delivery = new Delivery<Station>(receive);
        bool Matches(Station station) => view.GlobalFilters.Matches(station) && view.LocalFilters.Matches(station)
            && (view.Query.Length == 0 || station.Name.Contains(view.Query, StringComparison.OrdinalIgnoreCase));
        if (view.Category.Kind is CategoryKind.Favorites or CategoryKind.Recent)
        {
            await library.LoadAsync().WaitAsync(token).ConfigureAwait(false);
            IEnumerable<Station> items = view.Category.Kind == CategoryKind.Favorites ? library.Favorites() : library.Recent();
            if (view.Category.Kind == CategoryKind.Favorites) items = items.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase);
            var page = items.Where(Matches).Skip(checked((int)view.Offset)).Take((int)PageSize + 1).ToArray();
            foreach (var station in page.Take((int)PageSize)) await delivery.AddAsync(station, token).ConfigureAwait(false);
            await delivery.FlushAsync(token).ConfigureAwait(false);
            return new StationPage(page.Take((int)PageSize).ToArray(),
                view.Offset + (uint)Math.Min(page.Length, (int)PageSize), page.Length > PageSize);
        }
        var options = CreateSearch(view);
        if (options is null) return new StationPage([], view.Offset, false);
        if (receive is null)
        {
            var page = await ReadAsync(cancellation => Client.SearchStationsAsync(options, cancellation), token).ConfigureAwait(false);
            return new StationPage(page.Where(station => station is not null && station.StationUuid != Guid.Empty)
                .Select(Normalize).Where(station => station.Name.Length > 0 && Matches(station)).ToArray(),
                view.Offset + (uint)page.Length, page.Length == PageSize);
        }
        List<Station> stations = [];
        HashSet<Guid> ids = [];
        var count = await ReadAsync(cancellation => Client.StreamStationsAsync(options, async (station, cancellation) =>
        {
            if (station.StationUuid == Guid.Empty) return;
            station = Normalize(station);
            if (station.Name.Length == 0 || !Matches(station) || !ids.Add(station.StationUuid)) return;
            stations.Add(station);
            await delivery.AddAsync(station, cancellation).ConfigureAwait(false);
        }, cancellation), token).ConfigureAwait(false);
        await delivery.FlushAsync(token).ConfigureAwait(false);
        return new StationPage(stations, view.Offset + (uint)count, count == PageSize);
    }

    private static SearchOptions? CreateSearch(StationView view)
    {
        var category = view.Category;
        var global = view.GlobalFilters;
        var local = view.LocalFilters;
        var country = category.Kind == CategoryKind.Country ? category.Value : category.CountryCode;
        var tags = Filters.Terms(global.Tags).Concat(Filters.Terms(local.Tags))
            .Concat(category.Kind == CategoryKind.Tag ? [category.Value] : Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        static string? First(params string[] values) => values.FirstOrDefault(value => value.Length > 0);
        var order = category.Kind switch
        {
            CategoryKind.All or CategoryKind.Country => "name",
            CategoryKind.Trending => "clicktrend",
            CategoryKind.MostVoted => "votes",
            CategoryKind.RecentClicks => "clicktimestamp",
            CategoryKind.Updated => "changetimestamp",
            _ => "clickcount",
        };
        var maxima = new[] { global.MaximumBitrate, local.MaximumBitrate }.Where(value => value > 0).ToArray();
        var minimum = Math.Max(global.MinimumBitrate, local.MinimumBitrate);
        var maximum = maxima.Length == 0 ? (uint?)null : maxima.Min();
        var region = category.Kind == CategoryKind.Region ? category.Value : string.Empty;
        var codec = category.Kind == CategoryKind.Codec ? category.Value : string.Empty;
        static bool Conflicts(params string[] values) => values.Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).Skip(1).Any();
        if (maximum.HasValue && minimum > maximum.Value
            || Conflicts(country, local.CountryCode, global.CountryCode)
            || Conflicts(region, local.Region, global.Region)
            || Conflicts(codec, local.Codec, global.Codec))
            return null;
        return new SearchOptions
        {
            Name = view.Query.Length == 0 ? null : view.Query,
            CountryCode = First(country, local.CountryCode, global.CountryCode),
            State = First(region, local.Region, global.Region),
            StateExact = true,
            Language = First(category.Kind == CategoryKind.Language ? category.Value : string.Empty, local.Language, global.Language),
            LanguageExact = true,
            Tags = tags.Length == 0 ? null : tags,
            Codec = First(codec, local.Codec, global.Codec),
            BitrateMin = minimum,
            BitrateMax = maximum,
            HideBroken = global.OnlyWorking || local.OnlyWorking,
            IsHttps = global.HttpsOnly || local.HttpsOnly ? true : null,
            Order = order, Reverse = order != "name", Offset = view.Offset, Limit = PageSize,
        };
    }

    internal async Task<DirectoryPage> DirectoryAsync(Category category, uint offset, CancellationToken token,
        bool refresh = false, Func<IReadOnlyList<DirectoryItem>, CancellationToken, ValueTask>? receive = null)
    {
        var delivery = new Delivery<DirectoryItem>(receive);
        if (refresh) await _directories.InvalidateAsync(category, token).ConfigureAwait(false);
        else if (await _directories.GetAsync(category, offset, token).ConfigureAwait(false) is { } cached)
        {
            foreach (var item in cached.Items) await delivery.AddAsync(item, token).ConfigureAwait(false);
            await delivery.FlushAsync(token).ConfigureAwait(false);
            return cached;
        }
        // Counts describe working stations, without adding the same stations again for rankings.
        var options = new ListOptions { Order = "name", HideBroken = true, Offset = offset, Limit = DirectoryPageSize };
        var filter = category.Value.Length > 0 ? category.Value : null;
        List<DirectoryItem> items = [];
        HashSet<Category> seen = [];
        async ValueTask Add(DirectoryItem item, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrEmpty(item.Category.Value)
                || item.Category.Kind == CategoryKind.Country && item.Category.Value.Length != 2
                || !seen.Add(item.Category)) return;
            items.Add(item);
            await delivery.AddAsync(item, cancellation).ConfigureAwait(false);
        }
        var count = category.Kind switch
        {
            CategoryKind.Countries => await ReadAsync(cancellation => Client.StreamCountriesAsync(options,
                (item, readToken) => Add(new DirectoryItem(new Category(CategoryKind.Country, item.Code ?? string.Empty,
                    CountryName: item.Name ?? string.Empty), item.Name ?? string.Empty, item.StationCount), readToken),
                cancellation, filter), token).ConfigureAwait(false),
            CategoryKind.Regions => await ReadAsync(cancellation => Client.StreamStatesAsync(category.CountryName, options,
                (item, readToken) => Add(new DirectoryItem(new Category(CategoryKind.Region, item.Name ?? string.Empty,
                    category.CountryCode, category.CountryName), item.Name ?? string.Empty, item.StationCount), readToken),
                cancellation, filter), token).ConfigureAwait(false),
            CategoryKind.Languages => await ReadAsync(cancellation => Client.StreamLanguagesAsync(options,
                (item, readToken) => Add(new DirectoryItem(new Category(CategoryKind.Language, item.Name ?? string.Empty),
                    item.Name ?? string.Empty, item.StationCount), readToken),
                cancellation, filter), token).ConfigureAwait(false),
            CategoryKind.Tags => await ReadAsync(cancellation => Client.StreamTagsAsync(options,
                (item, readToken) => Add(new DirectoryItem(new Category(CategoryKind.Tag, item.Name ?? string.Empty),
                    item.Name ?? string.Empty, item.StationCount), readToken),
                cancellation, filter), token).ConfigureAwait(false),
            CategoryKind.Codecs => await ReadAsync(cancellation => Client.StreamCodecsAsync(options,
                (item, readToken) => Add(new DirectoryItem(new Category(CategoryKind.Codec, item.Name ?? string.Empty),
                    item.Name ?? string.Empty, item.StationCount), readToken),
                cancellation, filter), token).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(category)),
        };
        await delivery.FlushAsync(token).ConfigureAwait(false);
        var page = new DirectoryPage(items, offset + (uint)count, count == DirectoryPageSize);
        _directories.Store(category, offset, page);
        return page;
    }

    private sealed class Delivery<T>(Func<IReadOnlyList<T>, CancellationToken, ValueTask>? receive)
    {
        private readonly List<T> _pending = [];
        private bool _first = true;
        private long _sent = System.Diagnostics.Stopwatch.GetTimestamp();

        internal async ValueTask AddAsync(T item, CancellationToken token)
        {
            if (receive is null) return;
            token.ThrowIfCancellationRequested();
            if (_pending.Count >= 16) await FlushAsync(token).ConfigureAwait(false);
            _pending.Add(item);
            // Publish the first result promptly, then bound UI work and the number of posted calls.
            if (_first || _pending.Count >= 16 || System.Diagnostics.Stopwatch.GetElapsedTime(_sent).TotalMilliseconds >= 50)
                await FlushAsync(token).ConfigureAwait(false);
        }

        internal async ValueTask FlushAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (receive is null || _pending.Count == 0) return;
            var batch = _pending.ToArray();
            await receive(batch, token).ConfigureAwait(false);
            _pending.Clear();
            _first = false;
            _sent = System.Diagnostics.Stopwatch.GetTimestamp();
        }
    }

    internal Task<Station[]> FindStationAsync(Guid id, CancellationToken token)
        => ReadAsync(cancellation => Client.FindStationsByUuidsAsync([id], cancellation), token);

    private static async Task<T> ReadAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            token.ThrowIfCancellationRequested();
            try { return await read(token).ConfigureAwait(false); }
            catch (RequestException error) when (attempt < 2 && !token.IsCancellationRequested
                && (error.Kind is FailureKind.Transport or FailureKind.Timeout or FailureKind.Discovery
                    || error.Kind == FailureKind.Http && (error.StatusCode == System.Net.HttpStatusCode.RequestTimeout
                        || (int?)error.StatusCode >= 500)))
            {
                // Only reads are retried: three attempts in total, with a cancellable one-second delay.
                await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
            }
        }
    }

    internal static Station Normalize(Station station) => station with
    {
        Name = station.Name ?? string.Empty, Country = station.Country ?? string.Empty,
        CountryCode = station.CountryCode ?? string.Empty, State = station.State ?? string.Empty,
        Language = station.Language ?? string.Empty, Tags = station.Tags ?? string.Empty,
        Codec = station.Codec ?? string.Empty, Url = station.Url ?? string.Empty,
        ResolvedUrl = station.ResolvedUrl ?? string.Empty, Homepage = station.Homepage ?? string.Empty,
        LastCheckTime = station.LastCheckTime ?? string.Empty, LastChangeTime = station.LastChangeTime ?? string.Empty,
    };

    internal static string? StreamUrl(Station station)
        => HttpUrl(station.ResolvedUrl) ?? HttpUrl(station.Url);

    internal static string? HttpUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            && uri.UserInfo.Length == 0 ? uri.AbsoluteUri : null;

    internal static string DescribeError(Exception exception)
    {
        // Translators: Error shown when a radio directory request or a local radio-library operation fails. {reason} is the diagnostic detail.
        return TrFormat("The radio operation could not be completed.\n\n{reason}", exception is RequestException request
            ? RequestReason(request) : exception.Message);
    }

    private static string RequestReason(RequestException exception) => exception.Kind switch
    {
        // Translators: Radio Browser could not locate an available directory server.
        FailureKind.Discovery => Tr("No radio directory server is available. Please try again."),
        // Translators: A radio directory request exceeded its allowed time.
        FailureKind.Timeout => Tr("The radio directory did not respond in time."),
        // Translators: A radio directory server returned data the player could not understand.
        FailureKind.InvalidResponse => Tr("The radio directory returned an invalid response."),
        // Translators: A radio directory rejected an action. {reason} is the server's explanation or HTTP status.
        FailureKind.Rejected or FailureKind.Http => TrFormat("The radio directory rejected the request: {reason}",
            exception.ServerMessage ?? exception.StatusCode?.ToString() ?? string.Empty),
        // Translators: A network failure prevented a radio directory request.
        _ => Tr("Could not reach the radio directory. Check your internet connection."),
    };

    public void Dispose()
    {
        if (_client.IsValueCreated) _client.Value.Dispose();
    }
}
