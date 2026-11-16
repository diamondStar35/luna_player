using LunaPlayer.Media.Discs;
using LunaPlayer.UI.Discs;

namespace LunaPlayer.Application.Commands.Files;

internal sealed partial class Files
{
    private long _discRequest;

    private void OpenDiscFromDialog()
    {
        var request = ++_discRequest;
        var selection = _view.Dialogs.ChooseDisc(_settings.General.LastDirectory);
        if (selection is null) return;
        OpenDisc(selection.Inspection, selection.Title, request, alwaysShowProgress: false);
    }

    internal void OpenDisc(LunaPlayer.Media.Discs.Request disc)
    {
        var request = ++_discRequest;
        if (string.IsNullOrWhiteSpace(disc.Location))
        {
            _view.Dialogs.ShowMainError(Picker.Describe(new DiscException(Failure.InvalidDisc), disc.Format));
            return;
        }
        OpenDisc(new Inspection(disc), null, request, alwaysShowProgress: true);
    }

    private void OpenDisc(Inspection inspection, int? selectedTitle, long request, bool alwaysShowProgress)
    {
        var source = _player.SourceIdentity;
        void Complete(DiscoveryResult result) => CompleteDiscOpen(result, inspection.Request, selectedTitle, request, source);
        if (!alwaysShowProgress && inspection.Result.IsCompletedSuccessfully)
        {
            using (inspection) Complete(inspection.Result.GetAwaiter().GetResult());
            return;
        }
        // Translators: Caption of the progress window while a selected DVD is opened.
        var title = Tr("Opening DVD");
        // Translators: Progress text while the selected DVD is inspected before playback.
        var text = Tr("Reading DVD titles...");
        var prompt = new ProgressPrompt(title, text, _ => text) { Proportional = false, Modal = false };
        try
        {
            BackgroundProgress.Observe(_view, _dispatcher, prompt,
                inspection.Result, inspection.Cancel, Complete, inspection.Dispose);
        }
        catch
        {
            inspection.Dispose();
            throw;
        }
    }

    private void CompleteDiscOpen(DiscoveryResult result, LunaPlayer.Media.Discs.Request disc,
        int? selectedTitle, long request, string source)
    {
        if (request != _discRequest || source != _player.SourceIdentity) return;
        if (result.Error is { } error)
        {
            _view.Dialogs.ShowMainError(Picker.Describe(error, disc.Format));
            return;
        }
        _cancelSpotifyStart();
        if (!_player.OpenDisc(result.Disc!, selectedTitle))
        {
            // Translators: Error when a DVD title selected in the picker is unavailable or cannot be loaded.
            _view.Dialogs.ShowMainError(Tr("Could not open the selected DVD title."));
            return;
        }
        _settings.General.LastDirectory = disc.Location;
    }
}
