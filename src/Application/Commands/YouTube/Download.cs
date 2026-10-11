using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.YouTube;
using LunaPlayer.YouTube.Playback;
using WxSharp;

namespace LunaPlayer.Application.Commands.YouTube;

internal sealed partial class YouTube
{
    private void Download()
    {
        if (RequireVideo(out var watchUrl))
            DownloadTo(watchUrl);
    }

    /// <remarks>
    /// Off the UI thread behind a progress window, because a download is as long as the file is. The job
    /// gets only the strings it needs; everything it reports is turned into words back on this thread.
    ///
    /// Saving goes through yt-dlp, the only thing that can turn a video into a file, so it needs the
    /// programs present. When they are not, the offer is made here rather than a refusal shown: accepting
    /// it fetches them and comes back to this.
    /// </remarks>
    internal void DownloadTo(string url)
    {
        if (!Backend.HasComponents
            && _components.Ensure(_settings.YouTube.Channel, () => DownloadTo(url))
                is not LunaPlayer.YouTube.Components.Service.ComponentsState.Ready)
            return;
        var folder = _view.ChooseFolder(
            _settings.General.LastDirectory,
            // Translators: Title of the window that asks where a video should be saved.
            Tr("Select download folder"));
        if (string.IsNullOrEmpty(folder))
            return;
        _settings.General.LastDirectory = folder;
        // A download saves the picture at the video quality. The audio-only setting that once governed this
        // is gone - playback now chooses picture or sound per play, but a saved file is a video - so the
        // picker below lists heights, and the sound is carried inside the file as yt-dlp muxes it.
        const bool audioOnly = false;
        var quality = (int)_settings.YouTube.VideoQuality;
        // Ask yt-dlp which qualities this video really offers, off-thread behind a short window, so the
        // picker only ever lists qualities the download can honour. It sweeps rather than fills, because a
        // single probe cannot say how far through it is.
        var probe = new ProgressPrompt(
            // Translators: Title of the short window shown while a video's available qualities are read.
            Tr("Reading qualities"),
            // Translators: Message shown while a video's available download qualities are being read.
            Tr("Reading available qualities..."),
            _ => Tr("Reading available qualities...")) { Proportional = false };
        BackgroundProgress.Start(_view, _dispatcher, probe,
            (report, token) => _backend.AvailableQualities(url, audioOnly, token),
            options => StartDownload(url, folder, audioOnly, quality, options));
    }

    /// <summary>Shows the quality picker, then saves the video at the chosen quality.</summary>
    /// <remarks>
    /// On the UI thread, once the probe has come back. A video that offered qualities gets the picker, and
    /// backing out of it abandons the download; one whose formats could not be read comes back with nothing
    /// to offer and is saved at the settings quality, exactly as it was before the picker existed.
    /// </remarks>
    private void StartDownload(
        string url, string folder, bool audioOnly, int quality, IReadOnlyList<int> options)
    {
        int? exactQuality = null;
        if (options.Count > 0)
        {
            var chosen = _view.ChooseYouTubeQuality(options, audioOnly);
            if (chosen is null)
                return;
            exactQuality = chosen;
        }
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while a video is being saved to this computer.
            Tr("Downloading video"),
            // Translators: First message in the download window, before anything has arrived.
            Tr("Starting download..."),
            Describe) { Detailed = true };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (report, token) => _backend.Download(url, folder, audioOnly, quality, report, token, exactQuality),
            Saved);
    }

    /// <summary>The lines the download window shows.</summary>
    /// <remarks>
    /// Called from the progress window's own tick, which is on the UI thread, so it may translate. The
    /// name is only known once the first bytes arrive, so the heading stands alone until then rather than
    /// naming an empty file.
    /// </remarks>
    private static string Describe(Media.ProgressUpdate update)
    {
        var heading = update.Name.Length > 0
            // Translators: Progress heading while a video is being saved. {name} is the file being written.
            ? TrFormat("Downloading {name}", update.Name)
            // Translators: First message in the download window, before anything has arrived.
            : Tr("Starting download...");
        return heading + "\n" + LunaPlayer.YouTube.Components.Service.Sizes(update);
    }

    /// <remarks>
    /// Spoken rather than shown, in both directions: a download runs for minutes behind a window the user
    /// has probably stopped looking at, and a message box that has to
    /// be dismissed before anything else works is the wrong way to say a file arrived.
    /// </remarks>
    private void Saved(YouTubeOutcome outcome)
    {
        if (outcome.Success)
        {
            _speech.Speak(
                // Translators: Spoken once a video has been saved to this computer.
                Tr("Download completed."),
                // Translators: The short wording spoken once a video has been saved.
                Tr("Download completed."));
            return;
        }
        var message = outcome.Error.Length > 0
            ? outcome.Error
            // Translators: Spoken when a video could not be saved and nothing said why.
            : Tr("Download failed.");
        _speech.Speak(message, message);
    }
}
