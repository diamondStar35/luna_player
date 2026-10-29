using System.Globalization;
using LunaPlayer.Application.Presentation;
using LunaPlayer.Playback;

namespace LunaPlayer.Application.Commands.Playback;

internal sealed partial class Playback
{
    private void StartAbSelection()
    {
        if (_selections.PlaybackActive)
        {
            var position = _player.Elapsed ?? _selections.Current?.End ?? 0;
            _player.ExitSelectionRange(position, play: false);
            _selections.DeactivatePlayback();
            SyncSelectionState();
        }
        StartSelection();
    }

    private void SetSelectionStartMarker()
    {
        if (!_guard.RequireFile(out var path) || !_guard.RequireElapsed(out var elapsed))
            return;
        SetSelectionStart(path, elapsed);
    }

    private void SetSelectionStartTime()
    {
        if (!TryChooseSelectionTime(
            // Translators: Title of the dialog for entering the exact beginning of a selection.
            Tr("Set selection start time"),
            // Translators: Prompt above the time fields for the exact beginning of a selection.
            Tr("Enter the selection start time:"), out var path, out var time))
            return;
        SetSelectionStart(path, time);
    }

    private void SetSelectionStart(string path, double time)
    {
        if (_selections.PlaybackActive)
        {
            var position = _player.Elapsed ?? _selections.Current?.End ?? time;
            _player.ExitSelectionRange(position, play: false);
        }
        else
            _player.ClearSelectionRange();
        _selections.SetStart(path, time);
        var formatted = FormatSelectionTime(time);
        _speech.Speak(
            // Translators: Spoken after the beginning of a selection is marked. {time} includes milliseconds.
            TrFormat("Selection start: {time}", formatted),
            // Translators: Brief announcement after the beginning of a selection is marked. {time} includes milliseconds.
            TrFormat("Start {time}", formatted));
        SyncSelectionState();
    }

    private void SetSelectionEndMarker()
    {
        if (!_guard.RequireFile(out _) || !_guard.RequireElapsed(out var elapsed))
            return;
        SetSelectionEnd(elapsed);
    }

    private void SetSelectionEndTime()
    {
        if (_selections.PendingStart is null)
        {
            AnnounceMissingSelectionStart();
            return;
        }
        if (!TryChooseSelectionTime(
            // Translators: Title of the dialog for entering the exact end of a selection.
            Tr("Set selection end time"),
            // Translators: Prompt above the time fields for the exact end of a selection.
            Tr("Enter the selection end time:"), out _, out var time))
            return;
        SetSelectionEnd(time);
    }

    private void SetSelectionEnd(double time)
    {
        if (_selections.PendingStart is not double start)
        {
            AnnounceMissingSelectionStart();
            return;
        }
        if (time <= start)
        {
            _speech.Speak(
                // Translators: Spoken when a selection's end would be at or before its beginning.
                Tr("The selection end must be after the selection start."),
                // Translators: Brief announcement when a selection's end would be at or before its beginning.
                Tr("Invalid end."));
            return;
        }
        if (!_selections.TrySetEnd(time, out var range))
            return;
        var formatted = FormatSelectionTime(range.End);
        _speech.Speak(
            // Translators: Spoken after the end of a selection is marked. {time} includes milliseconds.
            TrFormat("Selection end: {time}", formatted),
            // Translators: Brief announcement after the end of a selection is marked. {time} includes milliseconds.
            TrFormat("End {time}", formatted));
        SyncSelectionState();
    }

    private void PreviousSelection() => MoveSelection(-1);

    private void NextSelection() => MoveSelection(1);

    private void MoveSelection(int direction)
    {
        var resumeFromCompletedSelection = direction > 0
            && _selections.OneShotActive
            && _player.SelectionRangeCompleted;
        if (!_selections.TryMove(direction, out var freeSlot))
            return;
        AnnounceSelectionNumber(_selections.CurrentIndex + 1);
        if (freeSlot)
        {
            // Ordinarily the free successor only removes the boundary and leaves playback untouched. A
            // completed one-shot is held at an artificial EOF, however, so leaving it also uses the
            // engine's exact recovery path to continue from that boundary without changing the timestamp.
            _player.ClearSelectionRange(restoreEndBehavior: false);
            if (resumeFromCompletedSelection)
                _player.Play();
        }
        else if (_selections.Current is SelectionRange range)
            PlaySelection(range);
        SyncSelectionState();
    }

    private void ClearSelectionPoint()
    {
        var active = _selections.PlaybackActive;
        if (!_selections.RemoveCurrent())
        {
            _speech.Speak(
                // Translators: Spoken when the current selection slot contains nothing to remove.
                Tr("No selection here."),
                // Translators: Brief announcement when the current selection slot contains nothing to remove.
                Tr("No selection here."));
            return;
        }
        if (active)
        {
            _player.ClearSelectionRange(restoreEndBehavior: false);
            _player.Play();
        }
        // Translators: Spoken after the current selection slot is removed.
        _speech.SpeakText(Tr("Selection cleared"));
        SyncSelectionState();
    }

    private void ClearAllSelections()
    {
        var active = _selections.PlaybackActive;
        if (!_selections.ClearAll())
            return;
        if (active)
        {
            _player.ClearSelectionRange(restoreEndBehavior: false);
            _player.Play();
        }
        // Translators: Spoken after every selection belonging to the current file is removed.
        _speech.SpeakText(Tr("All selections cleared"));
        SyncSelectionState();
    }

    private void ToggleRepeatSelections()
    {
        var wasActive = _selections.PlaybackActive;
        var range = _selections.Current;
        var wasAtEnd = range.HasValue && IsAtSelectionEnd(range.Value);
        var repeat = !_selections.Repeat;
        _selections.SetRepeat(repeat);
        if (wasActive && range.HasValue)
        {
            _selections.ActivatePlayback();
            if (repeat && wasAtEnd)
                PlaySelection(range.Value);
            else if (!_player.SetSelectionRange(range.Value.Start, range.Value.End, repeat))
            {
                _selections.DeactivatePlayback();
                _guard.ReportTimeUnavailable();
            }
        }
        _speech.SpeakText(repeat
            // Translators: Spoken when selections begin repeating.
            ? Tr("Repeat selections on")
            // Translators: Spoken when selections stop repeating.
            : Tr("Repeat selections off"));
        SyncSelectionState();
    }

    private bool TryChooseSelectionTime(
        string title, string prompt, out string path, out double time)
    {
        path = string.Empty;
        time = 0;
        if (!_guard.RequireFile(out var chosenPath) || _player.Duration is not double duration || duration <= 0)
        {
            _view.Dialogs.ShowWarning(
                // Translators: Shown when the player cannot offer exact time fields for the current item.
                Tr("Time is not available for the current file."));
            return false;
        }
        path = chosenPath;
        var chosen = _view.Dialogs.ChooseSelectionTime(
            title, prompt, duration, Math.Clamp(_player.Elapsed ?? 0, 0, duration));
        if (!chosen.HasValue)
            return false;
        time = chosen.Value;
        return true;
    }

    private void PlaySelection(SelectionRange range)
    {
        _selection.Reset();
        _selections.ActivatePlayback();
        if (!_player.PlaySelectionRange(range.Start, range.End, _selections.Repeat))
        {
            _selections.DeactivatePlayback();
            _guard.ReportTimeUnavailable();
            return;
        }
    }

    private void AnnounceMissingSelectionStart() => _speech.Speak(
        // Translators: Spoken when an end marker is requested before a start marker has been set.
        Tr("The selection start has not been set."),
        // Translators: Brief announcement when an end marker is requested before a start marker.
        Tr("No start."));

    private void AnnounceSelectionNumber(int number) =>
        // Translators: Spoken after moving to a selection slot. {number} is its one-based number.
        _speech.SpeakText(TrFormat("Selection {number}", number));

    private bool IsAtSelectionEnd(SelectionRange range)
        => _player.Elapsed is double elapsed && elapsed >= range.End - 0.001;

    internal void SyncSelectionState()
        => SelectionStatePresenter.Sync(_view, _player, _selections);

    private static string FormatSelectionTime(double seconds)
    {
        var value = TimeSpan.FromMilliseconds(Math.Max(0, Math.Round(seconds * 1000, MidpointRounding.AwayFromZero)));
        var hours = (int)value.TotalHours;
        return hours > 0
            ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}.{3:000}", hours, value.Minutes, value.Seconds, value.Milliseconds)
            : string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}.{2:000}", value.Minutes, value.Seconds, value.Milliseconds);
    }
}
