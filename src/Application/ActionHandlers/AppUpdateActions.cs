using System.Globalization;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Media;
using LunaPlayer.UI;
using LunaPlayer.Update;

namespace LunaPlayer.Application.ActionHandlers;

/// <summary>Coordinates manual and startup checks with the update dialogs and helper executable.</summary>
internal sealed class AppUpdateActions : IDisposable
{
    private readonly IMainView _view;
    private readonly PlayerSettings _settings;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly AppUpdateService _service = new();
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;

    internal AppUpdateActions(
        ActionRouter router,
        IMainView view,
        PlayerSettings settings,
        IApplicationDispatcher dispatcher)
    {
        _view = view;
        _settings = settings;
        _dispatcher = dispatcher;
        router.Register(ActionId.CheckAppUpdates, CheckNow);
    }

    /// <summary>Checks without opening a progress window and says nothing unless an update exists.</summary>
    internal void CheckAtStartup()
    {
        if (!_settings.General.CheckUpdatesOnStartup || _disposed)
            return;
        _ = CheckAtStartupAsync();
    }

    private async Task CheckAtStartupAsync()
    {
        CheckOutcome outcome;
        try
        {
            outcome = await Task.Run(() => RunCheck(_lifetime.Token), _lifetime.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (!outcome.Newer || !outcome.PackageAvailable || _disposed)
            return;
        _dispatcher.Post(() =>
        {
            if (!_disposed)
                ProcessCheck(outcome, showLatest: false);
        });
    }

    private void CheckNow()
    {
        var prompt = new ProgressPrompt(
            // Translators: Title of the progress window shown during a manual application update check.
            Tr("Checking for app updates"),
            // Translators: Initial message shown while contacting the application update server.
            Tr("Checking for updates..."),
            _ => Tr("Checking for updates...")) { Proportional = false };
        BackgroundProgress.Start(
            _view,
            _dispatcher,
            prompt,
            (_, token) => RunCheck(token),
            outcome => ProcessCheck(outcome, showLatest: true));
    }

    private CheckOutcome RunCheck(CancellationToken token)
    {
        try
        {
            var update = _service.Fetch(token);
            var newer = AppUpdateService.IsNewer(update.Version, AppInfo.Version);
            return new CheckOutcome(update, newer, !newer || _service.PackageExists(update, token));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception failure)
        {
            return new CheckOutcome(default, false, false, failure.Message);
        }
    }

    private void ProcessCheck(CheckOutcome outcome, bool showLatest)
    {
        if (outcome.Error.Length > 0)
        {
            if (showLatest)
            {
                _view.ShowError(
                    // Translators: Shown when a manual application update check fails. {reason} is the
                    // network or data error reported by the system.
                    TrFormat("Could not check for updates.\n\n{reason}", outcome.Error),
                    Tr("Application update"));
            }
            return;
        }
        if (!outcome.Newer)
        {
            if (showLatest)
            {
                // Translators: Shown after a manual check finds no newer Luna Player release.
                _view.ShowInfo(Tr("You are using the latest version."), Tr("No update"));
            }
            return;
        }
        if (!outcome.PackageAvailable)
        {
            if (showLatest)
            {
                _view.ShowInfo(
                    // Translators: Shown when info.json announces a newer application release before its
                    // installer or portable archive has finished being published.
                    Tr("A new version has been announced, but its download is not available yet. Please try again later."),
                    Tr("Application update"));
            }
            return;
        }

        var update = outcome.Update;
        if (_view.OfferAppUpdate(new AppUpdatePrompt(AppInfo.Version, update.Version, update.Changes)))
            Download(update);
    }

    private void Download(AppUpdateInfo update)
    {
        var empty = new ProgressUpdate(0, 0, string.Empty);
        var prompt = new ProgressPrompt(
            // Translators: Title of the window shown while a Luna Player update package is downloaded.
            Tr("Downloading update"),
            DownloadStatus(empty),
            DownloadStatus) { Detailed = true };
        BackgroundProgress.Start(
            _view,
            _dispatcher,
            prompt,
            (report, token) => RunDownload(update, report, token),
            FinishDownload);
    }

    private DownloadOutcome RunDownload(
        AppUpdateInfo update,
        Action<ProgressUpdate> report,
        CancellationToken token)
    {
        try
        {
            var path = _service.Download(update, (downloaded, total) => report(new ProgressUpdate(
                (int)Math.Min(downloaded, int.MaxValue),
                (int)Math.Min(total, int.MaxValue),
                string.Empty)), token);
            return new DownloadOutcome(path, string.Empty);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception failure)
        {
            return new DownloadOutcome(string.Empty, failure.Message);
        }
    }

    private void FinishDownload(DownloadOutcome outcome)
    {
        if (outcome.Error.Length > 0)
        {
            _view.ShowError(
                // Translators: Shown when the application update package cannot be downloaded. {reason}
                // is the network or file error reported by the system.
                TrFormat("Could not download update.\n\n{reason}", outcome.Error),
                Tr("Application update"));
            return;
        }
        try
        {
            _service.LaunchUpdater(outcome.Path);
        }
        catch (Exception failure)
        {
            _view.ShowError(
                // Translators: Shown when the downloaded update cannot be handed to Updater.exe. {reason}
                // is the error reported by the system.
                TrFormat("Could not start the updater.\n\n{reason}", failure.Message),
                Tr("Application update"));
            return;
        }
        _view.Close();
    }

    private static string DownloadStatus(ProgressUpdate update)
    {
        var percent = update.Total > 0 ? 100.0 * update.Value / update.Total : 0;
        // Translators: The three lines in the application update download window. {total} and {downloaded}
        // are file sizes and {percent} is a number between 0 and 100 with two decimal places.
        return TrFormat(
            "Total size: {total}\nDownloaded: {downloaded}\nPercentage: {percent}%",
            Size(update.Total),
            Size(update.Value),
            percent.ToString("F2", CultureInfo.CurrentCulture));
    }

    private static string Size(long value)
    {
        if (value <= 0)
            // Translators: Stands in for a file size the download server did not state.
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

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _lifetime.Cancel();
        _service.Dispose();
        _lifetime.Dispose();
    }

    private readonly record struct CheckOutcome(
        AppUpdateInfo Update, bool Newer, bool PackageAvailable, string Error = "");
    private readonly record struct DownloadOutcome(string Path, string Error);
}
