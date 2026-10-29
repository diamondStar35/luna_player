using System.Globalization;
using LunaPlayer.Accessibility;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI;

namespace LunaPlayer.YouTube.Components;

/// <summary>Everything about the two programs the optional yt-dlp path needs: whether they are there,
/// fetching them, and keeping them current.</summary>
///
/// <remarks>
/// The offer is made at the point of use rather than at startup. Nothing the player does by default needs
/// either program - searching, playing and saving are its own
/// work - so somebody who never touches YouTube is never asked about it, and somebody who is asked has just
/// tried to search, which is the one thing that cannot go anywhere without them. There is no "do not ask
/// again": the programs are the only route to YouTube, so declining is answered next time by asking again
/// rather than by giving up on the feature for good.
/// </remarks>
internal sealed class Service
{
    /// <summary>What came of asking for the programs.</summary>
    internal enum ComponentsState
    {
        /// <summary>Both are installed. Carry on.</summary>
        Ready,
        /// <summary>They are being fetched. Do not carry on, but do not undo anything either: the caller
        /// will be told when they arrive.</summary>
        Fetching,
        /// <summary>The user said no, or had said so already. Nothing is happening.</summary>
        Declined,
    }

    private readonly IMainView _view;
    private readonly Settings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly Installer _installer;
    private readonly Client.Updater _updater;

    internal Service(
        IMainView view,
        Settings settings,
        ISpeechOutput speech,
        IApplicationDispatcher dispatcher,
        Client.Updater updater,
        Installer installer)
    {
        _view = view;
        _settings = settings;
        _speech = speech;
        _dispatcher = dispatcher;
        _updater = updater;
        _installer = installer;
    }

    /// <summary>Whether both programs are installed.</summary>
    internal static bool Ready => Utils.HasAll;

    /// <summary>Makes sure the programs are there, offering to fetch them if they are not.</summary>
    ///
    /// <remarks>
    /// Three answers rather than two, because a fetch that has just started is neither. Rather than wait for
    /// the download here, holding the whole interface inside a nested event loop for as long as GitHub
    /// takes, this returns at once and calls <paramref name="installed"/> when it is over, so the
    /// player stays usable and the caller can pick up where it left off.
    /// </remarks>
    internal ComponentsState Ensure(YtDlpChannel channel, Action? installed = null)
    {
        if (Ready)
            return ComponentsState.Ready;
        if (!_view.Dialogs.OfferYouTubeComponents())
            return ComponentsState.Declined;
        Install(channel, success =>
        {
            if (success)
                installed?.Invoke();
        });
        return ComponentsState.Fetching;
    }

    /// <summary>Fetches whichever programs are missing, behind a progress window.</summary>
    /// <param name="finished">Run on the UI thread once it is over, with whether both are now present. Not
    /// run at all if the user aborted.</param>
    internal void Install(YtDlpChannel channel, Action<bool>? finished = null)
    {
        if (Ready)
        {
            _view.Dialogs.ShowInfo(
                // Translators: Shown when the extra programs YouTube's yt-dlp support needs are already there.
                Tr("YouTube components are already installed."));
            finished?.Invoke(true);
            return;
        }
        var steps = Installer.MissingCount;
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while the extra programs for yt-dlp are being fetched.
            Tr("Downloading YouTube components"),
            // Translators: First message shown while the extra programs for yt-dlp are being fetched.
            Tr("Contacting the download site..."),
            update =>
            {
                // Translators: Progress heading while an extra program is being fetched. {name} is the
                // program and its version, {step} which one it is and {total} how many there are.
                return TrFormat("Downloading {name}, {step} of {total}", update.Name, update.Found, steps)
                    + "\n" + Sizes(update);
            }) { Detailed = true };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (report, token) => Run(channel, report, token),
            outcome =>
            {
                if (outcome.Success)
                {
                    _view.Dialogs.ShowInfo(
                        // Translators: Shown once the extra programs for yt-dlp have been fetched.
                        Tr("YouTube components were downloaded successfully."));
                    finished?.Invoke(true);
                    return;
                }
                // Translators: Shown when the extra programs needed for YouTube playback could not be installed.
                var installError = Tr("Could not install YouTube components.");
                _view.Dialogs.ShowError(
                    // Translators: Shown when the extra programs for yt-dlp could not be fetched. {reason}
                    // is what went wrong, in the language of the system rather than the player.
                    outcome.Error.Length > 0
                        // Translators: Adds {reason}, the system's explanation, below the YouTube component error in {message}.
                        ? TrFormat("{message}\n{reason}", installError, outcome.Error)
                        : installError);
                finished?.Invoke(false);
            });
    }

    /// <summary>Has yt-dlp update itself, behind a progress window.</summary>
    ///
    /// <remarks>
    /// Its own updater rather than a fresh download. yt-dlp knows how to replace itself while it is
    /// running, which on Windows a plain overwrite cannot do.
    /// </remarks>
    internal void Update()
    {
        if (!Utils.HasYtDlp)
        {
            _view.Dialogs.ShowError(
                // Translators: Shown when the user asks to update yt-dlp and it has not been installed.
                Tr("yt-dlp is not installed yet."));
            return;
        }
        var channel = _settings.YouTube.Channel;
        _speech.Speak(
            // Translators: Spoken when the user asks to update yt-dlp.
            Tr("Updating yt-dlp..."),
            // Translators: The short wording spoken when the user asks to update yt-dlp.
            Tr("Updating yt-dlp..."));
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while yt-dlp is updating itself.
            Tr("Updating yt-dlp"),
            // Translators: First message shown while yt-dlp is updating itself.
            Tr("Checking current yt-dlp version..."),
            update => update.Name) { Proportional = false };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (report, token) => RunUpdate(channel, report, token),
            outcome => Report(outcome, channel));
    }

    /// <summary>Looks for a newer yt-dlp and offers to fetch it, saying nothing when there is none.
    /// </summary>
    ///
    /// <remarks>
    /// Runs at startup when the setting asks for it, and silently: it needs the network, it is nobody's
    /// reason for opening the player, and a machine that is offline should not be told so on every launch.
    /// Only a version that really is newer produces a question.
    /// </remarks>
    internal void CheckForUpdateInBackground()
    {
        if (!_settings.YouTube.CheckComponentUpdates || !Utils.HasYtDlp)
            return;
        var channel = _settings.YouTube.Channel;
        _ = Task.Run(() =>
        {
            string local;
            string remote;
            try
            {
                local = _updater.Version(CancellationToken.None);
                if (local.Length == 0)
                    return;
                remote = Clean(_installer.LatestTag(
                    Client.Updater.ChannelRepository(channel), CancellationToken.None));
            }
            catch (Exception)
            {
                // Nobody asked for this, so nobody is told it failed.
                return;
            }
            if (remote.Length == 0 || string.CompareOrdinal(remote, local) <= 0)
                return;
            _dispatcher.Post(() => OfferUpdate(local, remote, channel));
        });
    }

    private void OfferUpdate(string local, string remote, YtDlpChannel channel)
    {
        if (_view.Dialogs.Confirm(
            // Translators: Asks whether to update yt-dlp now. {channel} is the release line being followed,
            // {current} the version installed and {latest} the one available.
            TrFormat(
                "A newer yt-dlp version is available on the '{channel}' channel.\nCurrent version: {current}\nLatest version: {latest}\n\nDo you want to update now?",
                Client.Updater.ChannelName(channel), local, remote)))
        {
            Update();
        }
    }

    // ---- the background jobs ----

    private YouTubeOutcome Run(YtDlpChannel channel, Action<ProgressUpdate> report, CancellationToken token)
    {
        try
        {
            _installer.Install(channel,
                (name, step, got, size) => report(Installer.Step(name, step, got, size)), token);
            // No detail when one simply is not there afterwards: there is nothing to add that the
            // message the user reads does not already say, and an English sentence invented here would be
            // the one line of the window that stayed in English.
            return Utils.HasAll ? YouTubeOutcome.Ok : new YouTubeOutcome(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception failure)
        {
            return new YouTubeOutcome(false, failure.Message);
        }
    }

    private UpdateOutcome RunUpdate(
        YtDlpChannel channel, Action<ProgressUpdate> report, CancellationToken token)
    {
        try
        {
            var (before, after, updated) = _updater.SelfUpdate(
                channel, line => report(new ProgressUpdate(0, 0, line)), token);
            return new UpdateOutcome(true, before, after, updated, string.Empty);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception failure)
        {
            return new UpdateOutcome(false, string.Empty, string.Empty, false, failure.Message);
        }
    }

    private void Report(UpdateOutcome outcome, YtDlpChannel channel)
    {
        if (!outcome.Success)
        {
            _view.Dialogs.ShowError(
                outcome.Error.Length > 0
                    ? outcome.Error
                    // Translators: Shown when yt-dlp could not update itself and said nothing useful about why.
                    : Tr("Could not update yt-dlp."));
            return;
        }
        // Translators: Stands in for a version number that could not be read.
        var unknown = Tr("unknown");
        var name = Client.Updater.ChannelName(channel);
        var message = outcome.Updated
            // Translators: Shown once yt-dlp has updated itself. {old} and {new} are version numbers and
            // {channel} is the release line being followed.
            ? TrFormat("yt-dlp updated from {old} to {new} on the '{channel}' channel.",
                outcome.Before.Length > 0 ? outcome.Before : unknown,
                outcome.After.Length > 0 ? outcome.After : unknown,
                name)
            // Translators: Shown when yt-dlp was already the newest build. {version} is its version number
            // and {channel} is the release line being followed.
            : TrFormat("yt-dlp is already up to date ({version}) on the '{channel}' channel.",
                outcome.After.Length > 0 ? outcome.After : unknown, name);
        _view.Dialogs.ShowInfo(message);
        _speech.Speak(message, message);
    }

    // ---- wording ----

    /// <summary>The three lines a download window shows under its heading.</summary>
    /// <remarks>Called from a progress window's own tick, which is on the UI thread, so it may
    /// translate.</remarks>
    internal static string Sizes(ProgressUpdate update)
    {
        var percent = update.Total > 0 ? 100.0 * update.Value / update.Total : 0;
        // Translators: The three lines under the heading of a download window. {total} and {got} are file
        // sizes and {percent} is a number between 0 and 100 with two decimal places.
        return TrFormat(
            "Total size: {total}\nDownloaded: {got}\nPercentage: {percent}%",
            Size(update.Total),
            Size(update.Value),
            percent.ToString("F2", CultureInfo.CurrentCulture));
    }

    /// <summary>A number of bytes in the largest unit that leaves it above one.</summary>
    internal static string Size(long value)
    {
        if (value <= 0)
            // Translators: Stands in for a file size the download did not state.
            return Tr("Unknown");
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = value;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0
            ? $"{(long)size} {units[unit]}"
            : $"{size.ToString("F2", CultureInfo.CurrentCulture)} {units[unit]}";
    }

    /// <summary>Trims a release tag down to the version yt-dlp reports for itself, so the two compare.
    /// </summary>
    private static string Clean(string tag) => tag.TrimStart('v', 'V').Trim();

    private readonly record struct UpdateOutcome(
        bool Success, string Before, string After, bool Updated, string Error);
}
