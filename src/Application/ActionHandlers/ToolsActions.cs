using LunaPlayer.Actions;
using LunaPlayer.Media;
using LunaPlayer.UI;

namespace LunaPlayer.Application.ActionHandlers;

/// <summary>The commands under the Tools menu.</summary>
///
/// <remarks>
/// The converter window gathers what to convert and where it goes and hands back a request; running it is
/// this handler's job, behind the converter's own progress window, so the setup window can close while the
/// files convert. That split - the window decides, the handler drives - is the one the rest of the player
/// follows, rather than the recording window's, which stays open because recording is watched as it happens.
///
/// The same path is taken whether the user opened the converter from the Tools menu or Windows Explorer sent
/// a file or a folder to it with "Convert with Luna": <see cref="OpenFor"/> pre-loads the list, and
/// everything after is shared.
/// </remarks>
internal sealed class ToolsActions
{
    private readonly IMainView _view;
    private readonly MediaConverter _converter = new();

    internal ToolsActions(ActionRouter router, IMainView view)
    {
        _view = view;
        router.Register(ActionId.OpenMediaConverter, Open);
    }

    /// <summary>Opens the converter with a set of files already in it, for the "Convert with Luna" verb
    /// Windows Explorer launches the player with. The files have already had any folders among them expanded
    /// to what is under them.</summary>
    internal void OpenFor(IReadOnlyList<string> initialFiles) => Run(initialFiles);

    private void Open() => Run(null);

    private void Run(IReadOnlyList<string>? initialFiles)
    {
        if (_view.ShowMediaConverter(initialFiles) is not { Jobs.Count: > 0 } request)
            return;
        // Runs behind the converter's own progress window, which does not return until the batch has finished
        // or the user stopped it. A stopped batch gives back nothing, and nothing is said about it.
        if (_view.RunConversion((report, token) => _converter.Run(request, report, token)) is ConversionOutcome outcome)
            Report(request, outcome);
    }

    /// <summary>Tells the user how the batch went: a plain confirmation when every file converted, and a
    /// report listing each file that did not, with its reason, when some failed.</summary>
    private void Report(ConversionRequest request, ConversionOutcome outcome)
    {
        var summary = Summarize(request.Format, outcome);
        if (outcome.Failures.Count == 0)
        {
            // Translators: Title of the message shown when every file in a batch was converted.
            _view.ShowInfo(summary, Tr("Success"));
            return;
        }
        // One block per failed file: the file on its own line, the reason below it, a blank line between blocks.
        var details = string.Join(
            Environment.NewLine + Environment.NewLine,
            outcome.Failures.Select(failure =>
                // Translators: Names a file the converter could not convert. {path} is its full path, shown on
                // the line above the reason.
                TrFormat("File: {path}", failure.File) + Environment.NewLine + failure.Error));
        var message = summary + Environment.NewLine + Environment.NewLine
            // Translators: Follows the summary when some files failed, pointing at the list below it.
            + Tr("Some files were not converted. Check the details below.");
        // Translators: Title of the report shown when a batch converted but some files failed.
        _view.ShowConversionReport(Tr("Warning"), message, details);
    }

    /// <summary>The one-line result: how many files were written, to what format, and how long it took.
    /// </summary>
    private static string Summarize(string format, ConversionOutcome outcome) =>
        // Translators: The converter's result. {count} is how many files were written, {format} the format
        // they were written to, {time} how long it took, such as 0:45 or 1:05:00.
        TrPluralFormat(
            "Converted {count} file to {format}. The operation took {time}.",
            "Converted {count} files to {format}. The operation took {time}.",
            outcome.Converted, outcome.Converted, format, FormatDuration(outcome.Elapsed));

    /// <summary>A span as M:SS, or H:MM:SS once it runs to an hour.</summary>
    private static string FormatDuration(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes}:{span.Seconds:00}";
    }
}
