namespace LunaPlayer.Subsonic.Servers;

/// <summary>Per-account network scheduling. Playback continues to sample on the app's one-second clock.</summary>
internal sealed record TimingSettings
{
    internal const int MaximumIntervalSeconds = 86400;
    internal const int MaximumCacheSeconds = 604800;
    internal const int MaximumDelayMilliseconds = 60000;
    public int QueuePollSeconds { get; set; } = 5;
    public int QueueSaveSeconds { get; set; } = 5;
    public int PlaybackReportSeconds { get; set; } = 1;
    public int ScanPollMilliseconds { get; set; } = 1500;
    public int SynchronizationRetrySeconds { get; set; } = 30;
    public int ConnectionCacheSeconds { get; set; } = 300;
    public int StreamCacheSeconds { get; set; } = 300;
    public int LibraryCacheSeconds { get; set; } = 1800;
    public int BrowserRefreshSeconds { get; set; } = 1800;
    public int SearchDelayMilliseconds { get; set; } = 300;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int AudioMuseSearchSeconds { get; set; } = 60;
    public int AudioMuseGenerationSeconds { get; set; } = 300;

    internal void Validate()
    {
        if (new[] { QueuePollSeconds, QueueSaveSeconds, PlaybackReportSeconds, SynchronizationRetrySeconds,
            RequestTimeoutSeconds, AudioMuseSearchSeconds, AudioMuseGenerationSeconds }.Any(value => value is < 1 or > MaximumIntervalSeconds)
            || new[] { ConnectionCacheSeconds, StreamCacheSeconds, LibraryCacheSeconds, BrowserRefreshSeconds }.Any(value => value is < 0 or > MaximumCacheSeconds)
            || ScanPollMilliseconds is < 1 or > MaximumDelayMilliseconds || SearchDelayMilliseconds is < 0 or > MaximumDelayMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(TimingSettings));
    }
}
