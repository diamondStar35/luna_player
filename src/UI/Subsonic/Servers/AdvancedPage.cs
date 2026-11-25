using LunaPlayer.Subsonic.Servers;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed class AdvancedPage
{
    private readonly SpinCtrl _queuePoll, _queueSave, _reports, _scan, _retry;
    private readonly SpinCtrl _connectionCache, _streamCache, _libraryCache, _browserRefresh, _searchDelay;
    private readonly SpinCtrl _requestTimeout, _aiSearch, _aiGeneration;
    internal TextCtrl Headers { get; }

    internal AdvancedPage(Window parent, BoxSizer body, Profile profile, Form form)
    {
        (Window, BoxSizer) Group(string label)
        {
            var box = new StaticBox(parent, label); var group = new StaticBoxSizer(box);
            var fields = new BoxSizer(Orientation.Vertical);
            group.Add(fields, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
            body.Add(group, flags: SizerFlags.Expand | SizerFlags.All, border: 5);
            return (box, fields);
        }
        SpinCtrl Number(Window panel, BoxSizer fields, string label, string help, int value, int minimum = 1,
            int maximum = TimingSettings.MaximumIntervalSeconds)
        {
            var control = Form.Field(panel, fields, label, () => new SpinCtrl(panel, value, minimum, maximum));
            form.Help(control, help); return control;
        }
        var timing = profile.Timing;
        var (updates, updateFields) = Group(Words.BackgroundUpdates);
        _queuePoll = Number(updates, updateFields, Words.QueuePollInterval, Words.HelpQueuePollInterval, timing.QueuePollSeconds);
        _queueSave = Number(updates, updateFields, Words.QueueSaveInterval, Words.HelpQueueSaveInterval, timing.QueueSaveSeconds);
        _reports = Number(updates, updateFields, Words.PlaybackReportInterval, Words.HelpPlaybackReportInterval, timing.PlaybackReportSeconds);
        _scan = Number(updates, updateFields, Words.ScanPollInterval, Words.HelpScanPollInterval, timing.ScanPollMilliseconds,
            maximum: TimingSettings.MaximumDelayMilliseconds);
        _retry = Number(updates, updateFields, Words.SynchronizationRetryInterval, Words.HelpSynchronizationRetryInterval, timing.SynchronizationRetrySeconds);

        var (cache, cacheFields) = Group(Words.CacheAndBrowsing);
        _connectionCache = Number(cache, cacheFields, Words.ConnectionCacheDuration, Words.HelpConnectionCacheDuration, timing.ConnectionCacheSeconds, 0, TimingSettings.MaximumCacheSeconds);
        _streamCache = Number(cache, cacheFields, Words.StreamCacheDuration, Words.HelpStreamCacheDuration, timing.StreamCacheSeconds, 0, TimingSettings.MaximumCacheSeconds);
        _libraryCache = Number(cache, cacheFields, Words.LibraryCacheDuration, Words.HelpLibraryCacheDuration, timing.LibraryCacheSeconds, 0, TimingSettings.MaximumCacheSeconds);
        _browserRefresh = Number(cache, cacheFields, Words.BrowserRefreshInterval, Words.HelpBrowserRefreshInterval, timing.BrowserRefreshSeconds, 0, TimingSettings.MaximumCacheSeconds);
        _searchDelay = Number(cache, cacheFields, Words.SearchDelay, Words.HelpSearchDelay, timing.SearchDelayMilliseconds, 0, TimingSettings.MaximumDelayMilliseconds);

        var (timeouts, timeoutFields) = Group(Words.RequestTimeouts);
        _requestTimeout = Number(timeouts, timeoutFields, Words.RequestTimeout, Words.HelpRequestTimeout, timing.RequestTimeoutSeconds);
        _aiSearch = Number(timeouts, timeoutFields, Words.AudioMuseSearchTimeout, Words.HelpAudioMuseSearchTimeout, timing.AudioMuseSearchSeconds);
        _aiGeneration = Number(timeouts, timeoutFields, Words.AudioMuseGenerationTimeout, Words.HelpAudioMuseGenerationTimeout, timing.AudioMuseGenerationSeconds);
        Headers = Form.Field(parent, body, Words.Headers, () => new TextCtrl(parent,
            value: string.Join(Environment.NewLine, profile.Headers.Select(header => header.Key + ": " + header.Value)), style: TextCtrlStyle.MultiLine));
        Headers.MinSize = new Size(300, 120); form.Help(Headers, Words.HelpHeaders);
    }

    internal TimingSettings Read() => new()
    {
        QueuePollSeconds = _queuePoll.Value, QueueSaveSeconds = _queueSave.Value, PlaybackReportSeconds = _reports.Value,
        ScanPollMilliseconds = _scan.Value, SynchronizationRetrySeconds = _retry.Value,
        ConnectionCacheSeconds = _connectionCache.Value, StreamCacheSeconds = _streamCache.Value,
        LibraryCacheSeconds = _libraryCache.Value, BrowserRefreshSeconds = _browserRefresh.Value, SearchDelayMilliseconds = _searchDelay.Value,
        RequestTimeoutSeconds = _requestTimeout.Value, AudioMuseSearchSeconds = _aiSearch.Value, AudioMuseGenerationSeconds = _aiGeneration.Value
    };

    internal string Signature => string.Join("|", _queuePoll.Value, _queueSave.Value, _reports.Value, _scan.Value, _retry.Value,
        _connectionCache.Value, _streamCache.Value, _libraryCache.Value, _browserRefresh.Value, _searchDelay.Value,
        _requestTimeout.Value, _aiSearch.Value, _aiGeneration.Value, Headers.Value);
}
