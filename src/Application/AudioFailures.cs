using LunaPlayer.Playback;

namespace LunaPlayer.Application;

internal sealed partial class ApplicationController
{
    private void OnAudioFilterFailed(AudioFilter filter)
        => _dispatcher.Post(() =>
        {
            if (_shutDown) return;
            _player.DisableFailedFilter(filter);
            _view.Dialogs.ShowError(filter switch
            {
                // Translators: Reports a background failure of headphone spatialization on the main window.
                AudioFilter.Spatialization => Tr("Could not apply the SOFA headphone spatialization filter. Choose a valid SOFA HRTF file and use an FFmpeg build with libmysofa support."),
                // Translators: Reports a background failure of an audio processing filter on the main window.
                _ => Tr("Could not apply the audio filter."),
            });
        });
}
