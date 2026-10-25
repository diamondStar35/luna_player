using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.YouTube;
using LunaPlayer.YouTube.Playback;

namespace LunaPlayer.Application.Commands.YouTube;

internal sealed class Playlist
{
    private readonly IMainView _view;
    private readonly Player _player;
    private readonly Settings _settings;
    private readonly ISpeechOutput _speech;
    private readonly MediaGuard _guard;
    private readonly PlaybackSessions _sessions;

    internal Playlist(
        ActionRouter router,
        IMainView view,
        Player player,
        Settings settings,
        ISpeechOutput speech,
        PlaybackSessions sessions)
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
        switch (_sessions.TryPrevious())
        {
            case NavigationOutcome.Advanced:
                SetSwitched(true, announce: true);
                return;
            case NavigationOutcome.Pending:
            case NavigationOutcome.Exhausted:
                return;
        }
        if (!_player.Previous(_settings.Audio.WrapPlaylist))
            return;
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
            case NavigationOutcome.Advanced:
                SetSwitched(true, announce: true);
                return;
            case NavigationOutcome.Pending:
                return;
            case NavigationOutcome.Exhausted:
                SetSwitched(false, announce: true);
                return;
        }
        if (!_player.Next(_settings.Audio.WrapPlaylist))
            return;
        SetSwitched(true, announce: true);
    }
    private void MoveFirst() => SetSwitched(_sessions.Radio.IsActive
        ? _sessions.Radio.GoToIndex(0) == NavigationOutcome.Advanced : _player.First(), announce: true);
    private void MoveLast() => SetSwitched(_sessions.Radio.IsActive
        ? _sessions.Radio.GoToIndex(_sessions.Radio.Count - 1) == NavigationOutcome.Advanced : _player.Last(), announce: true);

    private void GoToFile()
    {
        var radio = _sessions.Radio.IsActive;
        var count = radio ? _sessions.Radio.Count : _player.Count;
        var index = radio ? _sessions.Radio.CurrentIndex : _player.CurrentIndex;
        if (count <= 1)
        {
            _speech.Speak(
                // Translators: Spoken when the user asks to jump to another file but only one is loaded.
                Tr("No other files are loaded."),
                // Translators: The short wording spoken when only one file is loaded.
                Tr("No other files."));
            return;
        }
        var value = _view.Dialogs.PromptText(
            // Translators: Asks the user which file in the playlist to move to. {count} is how many files are loaded.
            TrFormat("Enter file number (1-{count})", count),
            // Translators: Title of the window that asks which file in the playlist to move to.
            Tr("Go To File"), (index + 1).ToString());
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
        if (number < 1 || number > count)
        {
            _speech.Speak(
                // Translators: Spoken when the file number the user typed is higher or lower than the playlist holds.
                Tr("File number out of range."),
                // Translators: The short wording spoken when the file number typed is outside the playlist.
                Tr("Out of range."));
            return;
        }
        SetSwitched(radio ? _sessions.Radio.GoToIndex(number - 1) == NavigationOutcome.Advanced
            : _player.GoToIndex(number - 1), announce: true);
    }

    private void ToggleShuffle()
    {
        if (!_sessions.Radio.IsActive && !_guard.RequireFile(out _))
            return;
        var enabled = _sessions.ToggleShuffle();
        _view.SetShuffleChecked(enabled);
        if (enabled)
        {
            // Translators: Spoken once the player has started playing files in a random order.
            var message = Tr("Shuffle on");
            // Translators: Short announcement once shuffle has been enabled.
            var brief = Tr("Shuffle on");
            _speech.Speak(message, brief);
        }
        else
        {
            // Translators: Spoken once the player has returned to playing files in their listed order.
            var message = Tr("Shuffle off");
            // Translators: Short announcement once shuffle has been disabled.
            var brief = Tr("Shuffle off");
            _speech.Speak(message, brief);
        }
    }

    private void ToggleRepeatFile()
    {
        if (!_guard.RequireFile(out _))
            return;
        var enabled = _player.ToggleRepeatFile();
        _view.SetRepeatFileChecked(enabled);
        if (enabled)
        {
            // Translators: Spoken once the player has started playing the same file repeatedly.
            var message = Tr("Repeat on");
            // Translators: Short announcement once repeating the current file has been enabled.
            var brief = Tr("Repeat on");
            _speech.Speak(message, brief);
        }
        else
        {
            // Translators: Spoken once the player has stopped playing the same file repeatedly.
            var message = Tr("Repeat off");
            // Translators: Short announcement once repeating the current file has been disabled.
            var brief = Tr("Repeat off");
            _speech.Speak(message, brief);
        }
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
