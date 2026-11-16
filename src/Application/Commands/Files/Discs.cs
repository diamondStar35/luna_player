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
        OpenDisc(new LunaPlayer.Media.Discs.Request(selection.Format, selection.Location), selection.Title, request);
    }

    internal void OpenDisc(LunaPlayer.Media.Discs.Request disc)
    {
        var request = ++_discRequest;
        if (string.IsNullOrWhiteSpace(disc.Location))
        {
            _view.Dialogs.ShowMainError(Picker.Describe(new DiscException(Failure.InvalidDisc), disc.Format));
            return;
        }
        OpenDisc(disc, null, request);
    }

    private void OpenDisc(LunaPlayer.Media.Discs.Request disc, int? selectedTitle, long request)
    {
        // Translators: Caption of the progress window while a selected DVD is opened.
        var title = Tr("Opening DVD");
        // Translators: Progress text while the selected DVD is inspected before playback.
        var text = Tr("Reading DVD titles...");
        var prompt = new ProgressPrompt(title, text, _ => text) { Proportional = false, Modal = false };
        var source = _player.SourceIdentity;
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (_, token) =>
            {
                try { return new DiscoveryResult(Catalog.Read(disc, token), null); }
                catch (DiscException error) { return new DiscoveryResult(null, error); }
            },
            result =>
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
            });
    }
}
