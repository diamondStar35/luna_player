global using static CheckText;

// Only data and application infrastructure are substituted. The progress window,
// message box, timers and modal event loops are the production UI and native wxWidgets.
internal static class CheckText
{
    public static string Tr(string value) => value;
    public static string TrFormat(string value, params object[] args) => value;
}
namespace LunaPlayer.Application
{
    internal interface IApplicationDispatcher
    {
        IDisposable Repeat(TimeSpan interval, Action action);
    }
}
namespace LunaPlayer.YouTube.Download
{
    internal enum QueueItemStatus { Downloading, Downloaded, Completed, Failed }
    internal readonly record struct Outcome(bool Success, string FilePath, string Error)
    {
        internal static Outcome Saved(string path) => new(true, path, "");
        internal static Outcome Failed(string error) => new(false, "", error);
    }
    internal readonly record struct QueueItemSnapshot(
        QueueItemStatus Status, string FilePath = "download.mp3", string Error = "failure",
        double Percent = 100, string PostProcessor = "", string PostProcessStatus = "",
        long TotalBytes = 0, double Speed = 0, double Eta = 0, long DownloadedBytes = 0);
    internal sealed class DownloadQueue(QueueItemStatus status = QueueItemStatus.Completed)
    {
        internal QueueItemSnapshot? Snapshot(Guid id) => new(status);
        internal void Remove(Guid id) { }
        internal void Reveal(Guid id) { }
    }
}
