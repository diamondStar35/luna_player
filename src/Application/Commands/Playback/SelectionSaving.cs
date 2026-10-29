using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Commands.Playback;

/// <summary>The single- and multi-slot save commands. Both go through one exporter, so format handling,
/// precise FFmpeg cutting, cancellation and temporary-file cleanup stay identical.</summary>
internal sealed partial class Playback
{
    private readonly SelectionExporter _selectionExporter = new();

    private void SaveSelection()
    {
        // Translators: Title of messages and file pickers for saving one selection.
        var title = Tr("Save selection");
        if (!TryGetSelectionSource(title, out var source, out var video))
            return;
        SelectionRange range;
        if (_selections.Sole is SelectionRange sole)
            range = sole;
        else if (_selections.Current is SelectionRange current)
            range = current;
        else
        {
            _view.Dialogs.ShowInfo(
                // Translators: Shown when one selection is requested but the file contains several and the
                // user is in the free slot rather than one of them.
                Tr("There are multiple selection points in the current file. Save all selection points to separate files, or move to the selection point you want to save."));
            return;
        }
        range = new SelectionRange(_player.ToSourceTime(range.Start), _player.ToSourceTime(range.End));
        var destination = _view.Dialogs.ChooseSelectionDestination(source, video);
        if (string.IsNullOrWhiteSpace(destination))
            return;
        var prompt = new ProgressPrompt(
            // Translators: Title of the progress window shown while a selected part is being saved.
            Tr("Saving selection"),
            // Translators: Initial text in the progress window while a selected part begins saving.
            Tr("Preparing the selected part..."),
            update =>
                // Translators: Progress text while a selected part is being saved. {name} is the output file name.
                TrFormat("Saving {name}", update.Name));
        BackgroundProgress.Start(
            _view, _dispatcher, prompt,
            (report, token) => _selectionExporter.Export(source, destination, range, video, report, token),
            outcome => ReportSelectionExport(destination, outcome));
    }

    private void SaveSelections()
    {
        // Translators: Title of the dialog and messages for saving every selection point.
        var title = Tr("Save selection points");
        if (!TryGetSelectionSource(title, out var source, out var video)
            || _selections.Snapshot() is not { Count: > 0 } ranges
            || _view.Dialogs.ChooseSelectionBatch(source, video) is not LunaPlayer.UI.Selections.SelectionSaveOptions options)
            return;
        var request = new SelectionExportBatchRequest(
            source,
            ranges.Select(range => new SelectionRange(_player.ToSourceTime(range.Start), _player.ToSourceTime(range.End))).ToArray(),
            options.Folder,
            options.Name,
            options.Extension,
            video,
            options.MaxConcurrency);
        var prompt = new ProgressPrompt(
            // Translators: Title of the progress window while all selection points are being saved.
            Tr("Saving selection points"),
            // Translators: Initial status before a batch of selection files starts.
            Tr("Preparing the selection files..."),
            update => string.Join(Environment.NewLine,
                // Translators: Batch-selection status. {number} is the current selection and {total} is the
                // number of selections being saved.
                TrFormat("Saving selection {number} of {total}", update.ItemNumber, request.Ranges.Count),
                // Translators: Progress of the individual selection currently named in the status box.
                // {percent} is a whole number from 0 to 100.
                TrFormat("Percentage: {percent}%", CurrentPercent(update))))
        {
            Detailed = true,
        };
        BackgroundProgress.Start(
            _view,
            _dispatcher,
            prompt,
            (report, token) => _selectionExporter.ExportBatch(request, report, token),
            ReportSelectionBatch);
    }

    private bool TryGetSelectionSource(string title, out string source, out bool video)
    {
        source = _player.CurrentPath ?? string.Empty;
        video = _player.CurrentKind == MediaKind.Video;
        if (source.Length == 0 || _selections.Count == 0)
            return false;
        if (File.Exists(source))
            return true;
        _view.Dialogs.ShowWarning(
            // Translators: Shown when saving one or more selections is attempted from a web stream.
            Tr("Only selections from files on this computer can be saved."));
        return false;
    }

    private void ReportSelectionExport(string destination, SelectionExportOutcome outcome)
    {
        if (outcome.Success)
        {
            _view.Dialogs.ShowInfo(
                // Translators: Shown after a selected part has been saved. {file} is the output file name.
                TrFormat("The selection was saved as {file}.", Path.GetFileName(destination)));
            return;
        }
        // Translators: Fallback reason when FFmpeg did not say why a selected part could not be saved.
        var error = outcome.Error ?? Tr("Unknown error.");
        _view.Dialogs.ShowError(
            // Translators: Shown when a selected part could not be saved. {error} is FFmpeg's reason.
            TrFormat("The selection could not be saved.\n\n{error}", error));
    }

    private void ReportSelectionBatch(SelectionExportBatchOutcome outcome)
    {
        var summary =
            // Translators: Result of saving selection points. {saved} is how many files were written and
            // {total} is how many selection points were in the batch.
            TrPluralFormat(
                "Saved {saved} of {total} selection file.",
                "Saved {saved} of {total} selection files.",
                outcome.Total,
                outcome.Saved,
                outcome.Total);
        if (outcome.Failures.Count == 0)
        {
            _view.Dialogs.ShowInfo(
                summary);
            return;
        }
        var details = string.Join(
            Environment.NewLine + Environment.NewLine,
            outcome.Failures.Select(failure =>
                // Translators: Heading for one selection that could not be saved. {number} is its slot number
                // and {file} is its intended output file name.
                TrFormat("Selection {number}: {file}", failure.Number, Path.GetFileName(failure.File))
                + Environment.NewLine
                + (failure.Error.Length > 0
                    ? failure.Error
                    // Translators: Fallback reason when FFmpeg did not explain a failed selection file.
                    : Tr("Unknown error."))));
        var message = summary + Environment.NewLine + Environment.NewLine
            // Translators: Introduces the detailed list after some selection files could not be saved.
            + Tr("Some selections could not be saved. Check the details below.");
        _view.Dialogs.ShowConversionReport(
            // Translators: Title of the report shown when some selection files could not be saved.
            Tr("Warning"),
            message,
            details);
    }

    private static int CurrentPercent(ProgressUpdate update)
        => update.ItemTotal > 0
            ? (int)Math.Round(100.0 * update.ItemValue / update.ItemTotal)
            : 0;
}
