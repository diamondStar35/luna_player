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
            Report(outcome);
    }

    private void Report(ConversionOutcome outcome)
    {
        if (outcome.Failures.Count == 0)
        {
            _view.ShowInfo(
                // Translators: Shown when every file was converted. {count} is how many.
                TrPluralFormat("Converted {count} file.", "Converted {count} files.",
                    outcome.Converted, outcome.Converted),
                Title);
            return;
        }
        _view.ShowWarning(
            // Translators: Shown when some files converted and some did not. {converted} of {total} succeeded;
            // {names} lists the ones that failed, separated by commas.
            TrFormat("Converted {converted} of {total} files. These could not be converted: {names}",
                outcome.Converted, outcome.Total, string.Join(", ", outcome.Failures)),
            Title);
    }

    private static string Title =>
        // Translators: Title of the messages the media converter shows.
        Tr("Media converter");
}
