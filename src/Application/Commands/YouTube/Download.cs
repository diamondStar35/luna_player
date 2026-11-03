using LunaPlayer.Configuration;
using LunaPlayer.Media;
using LunaPlayer.YouTube;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.Application.Commands.YouTube;

internal sealed partial class YouTube
{
    /// <summary>Direct-downloads the current video with the saved settings.</summary>
    private void DirectDownload()
    {
        if (RequireVideo(out var watchUrl))
            DirectDownloadTo(watchUrl, _player.CurrentTitle ?? _player.CurrentDisplayName ?? string.Empty);
    }

    /// <summary>Opens the download dialog for the current video.</summary>
    private void Download()
    {
        if (RequireVideo(out var watchUrl))
            DownloadTo(watchUrl, _player.CurrentTitle ?? string.Empty);
    }

    /// <summary>Adds the active YouTube video with the saved defaults.</summary>
    private void AddCurrentToDownloadQueue()
    {
        if (RequireVideo(out var watchUrl))
            AddToDownloadQueue(watchUrl, _player.CurrentTitle ?? _player.CurrentDisplayName ?? string.Empty);
    }

    /// <summary>Downloads a video with the saved settings.</summary>
    internal void DirectDownloadTo(string url, string title = "")
    {
        if (!EnsureComponents(() => DirectDownloadTo(url, title)))
            return;
        Run(Downloads.Request.FromSettings(_settings.YouTube.Download, url), title);
    }

    /// <summary>Opens the download dialog for a video, then downloads what was chosen.</summary>
    internal void DownloadTo(string url, string title)
    {
        var stem = string.IsNullOrWhiteSpace(title) ? string.Empty : Paths.SafeFileName(title);
        var defaults = Downloads.Request.FromSettings(_settings.YouTube.Download, url, stem);
        if (_view.Dialogs.ShowDownloadOptions(defaults) is not { } choice)
            return;
        if (choice.AddToQueue)
            AddToDownloadQueue(choice.Request, title);
        else if (EnsureComponents(() => Run(choice.Request, title)))
            Run(choice.Request, title);
    }

    /// <summary>Adds a search result or other known video with the saved default settings.</summary>
    internal void AddToDownloadQueue(string url, string title)
        => AddToDownloadQueue(Downloads.Request.FromSettings(_settings.YouTube.Download, url), title);

    private void AddToDownloadQueue(Downloads.Request request, string title)
    {
        _downloadQueue.Add(title, request);
        _speech.Speak(
            // Translators: Spoken after a video has been put in the download queue.
            Tr("Added to download queue"),
            // Translators: Short announcement after a video has been put in the download queue.
            Tr("Added to download queue"));
    }

    internal void OpenDownloadQueue()
        => _view.Dialogs.ShowDownloadQueue(_downloadQueue, AddManualLinks, StartDownloadQueue);

    /// <summary>Validates all manually entered links before adding any of them. Null means success.</summary>
    private string? AddManualLinks(IReadOnlyList<string> links)
    {
        foreach (var link in links)
        {
            var info = LinkValidator.Parse(link);
            if (!info.IsHttp || !info.IsYouTube || !info.HasVideo)
                // Translators: Shown when the queue's multi-link box contains a non-video YouTube address.
                return TrFormat("This is not a valid YouTube video link: {link}", link);
        }
        foreach (var link in links)
        {
            var trimmed = link.Trim();
            _downloadQueue.Add(trimmed,
                Downloads.Request.FromSettings(_settings.YouTube.Download, trimmed));
        }
        return null;
    }

    private void StartDownloadQueue()
    {
        if (EnsureComponents(StartDownloadQueue))
            _downloadQueue.Start();
    }

    /// <summary>Starts a download owned by the queue and opens its modeless progress window.</summary>
    private void Run(Downloads.Request request, string title)
    {
        var itemId = _downloadQueue.StartSingle(title, request);
        _view.Dialogs.ShowDownloadProgress(_downloadQueue, itemId);
    }

    /// <summary>Whether the yt-dlp programs are present; offers to fetch them and retries if not.</summary>
    private bool EnsureComponents(System.Action retry)
    {
        if (Backend.HasComponents)
            return true;
        return _components.Ensure(_settings.YouTube.Channel, retry)
            is LunaPlayer.YouTube.Components.Service.ComponentsState.Ready;
    }
}
