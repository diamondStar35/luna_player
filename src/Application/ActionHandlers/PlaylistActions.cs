using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.YouTube;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed class PlaylistActions
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly ISpeechOutput _speech;
    private readonly MediaGuard _guard;
    private readonly YouTubeSessions _sessions;

    internal PlaylistActions(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        PlayerSettings settings,
        ISpeechOutput speech,
        YouTubeSessions sessions)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _speech = speech;
        _guard = new MediaGuard(player, speech);
        _sessions = sessions;
        router.Register(ActionId.PreviousTrack, MovePrevious);
        router.Register(ActionId.NextTrack, MoveNext);
        router.Register(ActionId.FirstTrack, MoveFirst);
        router.Register(ActionId.GoToFile, GoToFile);
        router.Register(ActionId.LastTrack, MoveLast);
        router.Register(ActionId.ToggleShuffle, ToggleShuffle);
        router.Register(ActionId.ToggleRepeatFile, ToggleRepeatFile);
    }

    private void MovePrevious()
    {
        if (!_player.Previous(_settings.Audio.WrapPlaylist))
            return;
        // Landing on a session video this way skips TryNext, which is what usually keeps the session's
        // idea of the current row current.
        _sessions.SyncSelection();
        SetSwitched(true, announce: true);
    }
    /// <remarks>
    /// A video from YouTube goes to the next one in the list the user was shown, which is not the same as
    /// the next playlist entry: only the videos already played are in the playlist, so the ordinary move
    /// would find nothing after the last of them. When the next one still has to be fetched the session
    /// says so and starts it, announcing itself when it arrives - so nothing is spoken here.
    /// </remarks>
    private void MoveNext()
    {
        switch (_sessions.TryNext())
        {
            case NextOutcome.Advanced:
                SetSwitched(true, announce: true);
                return;
            case NextOutcome.Pending:
                return;
            case NextOutcome.Exhausted:
                SetSwitched(false, announce: true);
                return;
        }
        if (!_player.Next(_settings.Audio.WrapPlaylist))
            return;
        _sessions.SyncSelection();
        SetSwitched(true, announce: true);
    }
    private void MoveFirst() => SetSwitched(_player.First(), announce: false);
    private void MoveLast() => SetSwitched(_player.Last(), announce: false);

    private void GoToFile()
    {
        if (_player.Count <= 1)
        {
            _speech.Speak(
                // Translators: Spoken when the user asks to jump to another file but only one is loaded.
                Tr("No other files are loaded."),
                // Translators: The short wording spoken when only one file is loaded.
                Tr("No other files."));
            return;
        }
        var value = _view.PromptText(
            // Translators: Asks the user which file in the playlist to move to. {count} is how many files are loaded.
            TrFormat("Enter file number (1-{count})", _player.Count),
            // Translators: Title of the window that asks which file in the playlist to move to.
            Tr("Go To File"), (_player.CurrentIndex + 1).ToString());
        if (value is null)
            return;
        if (!int.TryParse(value, out var number))
        {
            _speech.Speak(
                // Translators: Spoken when what the user typed as a file number is not a number.
                Tr("Invalid file number."),
                // Translators: The short wording spoken when what was typed as a file number is not a number.
                Tr("Invalid number."));
            return;
        }
        if (number < 1 || number > _player.Count)
        {
            _speech.Speak(
                // Translators: Spoken when the file number the user typed is higher or lower than the playlist holds.
                Tr("File number out of range."),
                // Translators: The short wording spoken when the file number typed is outside the playlist.
                Tr("Out of range."));
            return;
        }
        SetSwitched(_player.GoToIndex(number - 1), announce: false);
    }

    private void ToggleShuffle()
    {
        if (!_guard.RequireFile(out _))
            return;
        var enabled = _player.ToggleShuffle();
        _view.SetShuffleChecked(enabled);
        _speech.Speak(enabled
                // Translators: Spoken once the player has started playing the files in a random order.
                ? Tr("Shuffle on")
                // Translators: Spoken once the player has gone back to playing the files in their listed order.
                : Tr("Shuffle off"),
            enabled ? Tr("Shuffle on") : Tr("Shuffle off"));
    }

    private void ToggleRepeatFile()
    {
        if (!_guard.RequireFile(out _))
            return;
        var enabled = _player.ToggleRepeatFile();
        _view.SetRepeatFileChecked(enabled);
        _speech.Speak(enabled
                // Translators: Spoken once the player has started playing the same file over and over.
                ? Tr("Repeat on")
                // Translators: Spoken once the player has stopped playing the same file over and over.
                : Tr("Repeat off"),
            enabled ? Tr("Repeat on") : Tr("Repeat off"));
    }

    private void SetSwitched(bool moved, bool announce)
    {
        if (!moved)
            return;
        _view.SetPlaying(true);
        if (announce && _settings.General.SpeakFileOnNavigation && _player.CurrentDisplayName is string name)
            _speech.Speak(name, name);
    }
}
