using LunaPlayer.Accessibility;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Presentation;

/// <summary>Keeps the main window's menu enable/check state in line with what the player is doing - what is
/// loaded, playing, markable, a video, and so on.</summary>
internal sealed class ViewStatePresenter
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly ISpeechOutput _speech;

    internal ViewStatePresenter(IMainView view, MediaPlayer player, ISpeechOutput speech)
    {
        _view = view;
        _player = player;
        _speech = speech;
    }

    /// <summary>The starting state, before anything is open.</summary>
    internal void PushInitial()
    {
        _view.SetMediaLoaded(false);
        _view.SetPlaying(false);
        _view.SetEditState(false, false);
        _view.SetBookmarkState(false);
        _view.SetMarkState(false, false);
        _view.SetMarkedActionsEnabled(false);
        _view.SetVideoOptionsEnabled(false);
        _view.SetFullScreenAvailable(false);
        _view.SetAudioTrackControlsEnabled(false);
        _view.SetSubtitleControlsEnabled(false);
        _view.SetSilenceRemovalChecked(_player.IsSilenceRemovalEnabled);
        // Both belong to a playlist rather than to the player, so they change when a list of videos is put
        // in front of the one the user opened and change back when it goes.
        _view.SetShuffleChecked(_player.IsShuffleEnabled);
        _view.SetRepeatFileChecked(_player.IsRepeatFileEnabled);
    }

    internal void Sync()
    {
        var loaded = _player.CurrentPath is not null;
        var local = _player.CurrentPath is string path && File.Exists(path);
        _view.SetMediaLoaded(loaded);
        _view.SetPlaying(_player.IsPlaying);
        _view.SetEditState(local, loaded);
        _view.SetBookmarkState(local);
        _view.SetMarkState(_player.IsCurrentMarked, _player.AreAllMarked);
        _view.SetMarkedActionsEnabled(loaded && _player.MarkedCount > 0);
        // The source rather than the path: a resolved video plays from a signed stream address that is
        // not on a YouTube host at all, and it is the watch page these commands act on. No test for a file
        // being loaded either - with nothing open there is neither a source nor a path.
        _view.SetVideoOptionsEnabled(
            LinkValidator.IsYouTubeUrl(_player.CurrentSource ?? _player.CurrentPath));
        _view.SetFullScreenAvailable(_player.HasVideo);
        // More than one audio track to switch between, or the commands stay greyed out. Like HasVideo, the
        // count settles a moment after a load, when AudioTracksChanged brings us back through here.
        _view.SetAudioTrackControlsEnabled(_player.GetAudioTracks().Count > 1);
        // The Subtitles menu is open whenever a file is - embedded subtitles or not - because one can always
        // be loaded from a file or a URL; it is only shut when nothing is loaded to attach one to.
        _view.SetSubtitleControlsEnabled(loaded);
        // Leave full screen when the current item stops being a video; once out, IsFullScreen is false so it
        // will not fire again. Moving between two videos keeps HasVideo true and stays silent.
        if (!_player.HasVideo && _view.IsFullScreen)
        {
            _view.SetFullScreen(false);
            // Translators: Spoken when full-screen mode ends on its own because the video stopped playing.
            // Translators: The short advanced-verbosity wording for the same automatic end of full-screen mode.
            _speech.Speak(Tr("Full-screen mode off."), Tr("Full-screen off"));
        }
        _view.SetSilenceRemovalChecked(_player.IsSilenceRemovalEnabled);
    }
}
