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
    /// <remarks>
    /// The title goes above the text: the window is opened from a
    /// keystroke rather than from a list, so without it there is nothing saying which video this is about.
    /// </remarks>
    private void ShowDescription()
    {
        if (!RequireVideo(out var watchUrl))
            return;
        var title = _player.CurrentDisplayName ?? string.Empty;
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while the text under a video is being fetched.
            Tr("Loading video details"),
            // Translators: Message shown while the text the uploader wrote under a video is being fetched.
            Tr("Loading video details..."),
            update => update.Name) { Proportional = false };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (_, token) => _backend.Describe(watchUrl, token),
            found =>
            {
                if (found.Failure is ResolveFailure.Cancelled)
                    return;
                if (found.Text is null)
                {
                    ShowError(Utils.Describe(found.Failure, found.Detail), Tr("YouTube"));
                    return;
                }
                var text = found.Text.Trim().Length > 0
                    ? found.Text.Trim()
                    // Translators: Shown in place of the text under a video when the uploader wrote none.
                    : Tr("No description is available.");
                _view.ShowVideoDescription(title, text);
            });
    }

    private void CopyLink()
    {
        if (RequireVideo(out var watchUrl))
            CopyToClipboard(watchUrl);
    }

    private void OpenCurrentInBrowser()
    {
        if (RequireVideo(out var watchUrl))
            OpenInBrowser(watchUrl);
    }

    private void OpenCurrentChannelInBrowser()
    {
        if (RequireVideo(out _))
            WithActiveChannel(OpenInBrowser);
    }

    private void GoToCurrentChannel()
    {
        if (RequireVideo(out _))
            WithActiveChannel(url => _sessions.GoToChannel(url, string.Empty));
    }

    /// <summary>Runs <paramref name="action"/> on the playing video's channel address, or says so when it is
    /// not known - a video opened from a bare link carries no channel.</summary>
    private void WithActiveChannel(Action<string> action)
    {
        var channel = _sessions.ActiveChannelUrl;
        if (channel.Length > 0)
        {
            action(channel);
            return;
        }
        _speech.Speak(
            // Translators: Spoken when the playing video does not say which channel published it.
            Tr("Channel link is not available."),
            // Translators: The short wording spoken when the playing video does not name its channel.
            Tr("No channel link."));
    }

    internal void CopyToClipboard(string url)
    {
        if (_clipboard.SetText(url))
            _speech.Speak(
                // Translators: Spoken once the address of a video has been put on the clipboard.
                Tr("Link copied."),
                // Translators: The short wording spoken once the address of a video has been copied.
                Tr("Copied."));
        else
            _speech.Speak(
                // Translators: Spoken when the address of a video could not be put on the clipboard.
                Tr("Could not copy link."),
                // Translators: The short wording spoken when the address of a video could not be copied.
                Tr("Copy failed."));
    }

    /// <remarks>
    /// Two attempts: wxWidgets asks the system for the browser registered
    /// for http, and where nothing answers to that the shell is asked to open the address as it would from
    /// the Run box. The second catches a machine whose default browser is set but not associated.
    /// </remarks>
    internal void OpenInBrowser(string url)
    {
        if (Wx.LaunchDefaultBrowser(url) || Shell(url))
            return;
        _speech.Speak(
            // Translators: Spoken when the web browser could not be started to show a video.
            Tr("Could not open browser."),
            // Translators: The short wording spoken when the web browser could not be started.
            Tr("Browser open failed."));
    }

    private static bool Shell(string url)
    {
        try
        {
            using var opened = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            return true;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception
            or InvalidOperationException or IOException)
        {
            return false;
        }
    }
}
