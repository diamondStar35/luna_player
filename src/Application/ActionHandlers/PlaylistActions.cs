using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed class PlaylistActions
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly ISpeechOutput _speech;

    internal PlaylistActions(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        ISpeechOutput speech)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _speech = speech;
        router.Register(ActionId.PreviousTrack, MovePrevious);
        router.Register(ActionId.NextTrack, MoveNext);
        router.Register(ActionId.FirstTrack, MoveFirst);
        router.Register(ActionId.GoToFile, GoToFile);
        router.Register(ActionId.LastTrack, MoveLast);
        router.Register(ActionId.ToggleShuffle, ToggleShuffle);
        router.Register(ActionId.ToggleRepeatFile, ToggleRepeatFile);
    }

    private void MovePrevious() => SetSwitched(_player.Previous(_settings.Audio.WrapPlaylist), announce: true);
    private void MoveNext() => SetSwitched(_player.Next(_settings.Audio.WrapPlaylist), announce: true);
    private void MoveFirst() => SetSwitched(_player.First(), announce: false);
    private void MoveLast() => SetSwitched(_player.Last(), announce: false);

    private void GoToFile()
    {
        if (_player.Count <= 1)
        {
            _speech.Speak("No other files are loaded.", "No other files.");
            return;
        }
        var value = _view.PromptText($"Enter file number (1-{_player.Count})", "Go To File", (_player.CurrentIndex + 1).ToString());
        if (value is null)
            return;
        if (!int.TryParse(value, out var number))
        {
            _speech.Speak("Invalid file number.", "Invalid number.");
            return;
        }
        if (number < 1 || number > _player.Count)
        {
            _speech.Speak("File number out of range.", "Out of range.");
            return;
        }
        SetSwitched(_player.GoToIndex(number - 1), announce: false);
    }

    private void ToggleShuffle()
    {
        if (!EnsureFile())
            return;
        var enabled = _player.ToggleShuffle();
        _view.SetShuffleChecked(enabled);
        _speech.Speak(enabled ? "Shuffle on" : "Shuffle off", enabled ? "Shuffle on" : "Shuffle off");
    }

    private void ToggleRepeatFile()
    {
        if (!EnsureFile())
            return;
        var enabled = _player.ToggleRepeatFile();
        _view.SetRepeatFileChecked(enabled);
        _speech.Speak(enabled ? "Repeat on" : "Repeat off", enabled ? "Repeat on" : "Repeat off");
    }

    private bool EnsureFile()
    {
        if (!string.IsNullOrEmpty(_player.CurrentPath))
            return true;
        _speech.Speak("No file loaded.", "No file.");
        return false;
    }

    private void SetSwitched(bool moved, bool announce)
    {
        if (!moved)
            return;
        _view.SetPlaying(true);
        if (announce && _settings.General.SpeakFileOnNavigation && _player.CurrentPath is string path)
        {
            var name = Path.GetFileName(path);
            _speech.Speak(name, name);
        }
    }
}
