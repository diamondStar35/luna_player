namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string Folder =>
        // Translators: Download folder. Used in music-server original-file download options.
        Tr("Download folder");
    internal static string Browse =>
        // Translators: Browse. Used in music-server original-file download options.
        Tr("Browse...");
    internal static string AlbumFolders =>
        // Translators: Organize downloads into artist and album folders. Used in music-server original-file download options.
        Tr("Organize downloads into artist and album folders");
    internal static string Download =>
        // Translators: Download. Used in music-server original-file download options.
        Tr("Download...");
    internal static string Direct =>
        // Translators: Direct download. Used in music-server original-file download options.
        Tr("Direct download");
    internal static string Enqueue =>
        // Translators: Add to download queue. Used in music-server original-file download options.
        Tr("Add to download queue");
    internal static string DownloadQueued =>
        // Translators: Short spoken confirmation after adding music-server files to the download queue.
        Tr("Added to download queue.");
    internal static string Queue =>
        // Translators: Download queue. Used in music-server original-file download options.
        Tr("Download queue...");
    internal static string FileName =>
        // Translators: File name without extension (optional). Used in music-server original-file download options.
        Tr("File name without extension (optional)");
    internal static string DownloadNow =>
        // Translators: Download. Used in music-server original-file download options.
        Tr("Download");
    internal static string UnavailableEpisode =>
        // Translators: This podcast episode must be downloaded to the server before it can be played. Used in music-server original-file download options.
        Tr("This podcast episode must be downloaded to the server before it can be played.");
    internal static string DownloadCount(int count) =>
        // Translators: Shared download queue title for a group of original music files. {count} is the number of files.
        TrFormat("Download {count} files", count);
}
