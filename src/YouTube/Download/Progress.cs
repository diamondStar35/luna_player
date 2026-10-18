namespace LunaPlayer.YouTube.Download;

/// <summary>One progress update parsed from yt-dlp. Bytes, speed and eta are 0 when not yet known.</summary>
internal readonly record struct Progress(
    string Status,
    long DownloadedBytes,
    long TotalBytes,
    double Percent,
    double Speed,
    double Eta,
    string PostProcessor = "")
{
    internal bool IsPostProcessing => PostProcessor.Length > 0;
}

/// <summary>A finished download: the saved file on success, or why it failed.</summary>
internal readonly record struct Outcome(bool Success, string FilePath, string Error)
{
    internal static Outcome Saved(string filePath) => new(true, filePath, string.Empty);

    internal static Outcome Failed(string error) => new(false, string.Empty, error);
}
