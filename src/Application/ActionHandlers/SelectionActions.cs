using LunaPlayer.Configuration;
using LunaPlayer.Playback;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed partial class PlaybackActions
{
    private void StartSelection()
    {
        if (!TryGetCurrentTime(out var path, out var elapsed)) return;
        _selection.SetStart(path, elapsed);
        _player.SetLoopStart(elapsed);
        var formatted = PlaybackTimeFormatter.Format(elapsed)!;
        _speech.Speak(
            // Translators: Spoken once the beginning of a repeating stretch of the file has been marked. {time} is that
            // point in the file, such as 00:01:20.
            TrFormat("Start selection: {time}", formatted),
            // Translators: The short wording spoken once the beginning of a repeating stretch has been marked. {time} is
            // that point in the file, such as 00:01:20.
            TrFormat("Start {time}", formatted));
    }

    private void EndSelection()
    {
        var path = _player.CurrentPath;
        if (string.IsNullOrEmpty(path)) { _speech.Speak(Tr("No file loaded."), Tr("No file.")); return; }
        if (!_selection.IsActive(path, requireEnd: false) || _selection.Start is not double start)
        {
            _speech.Speak(
                // Translators: Spoken when the user marks the end of a repeating stretch of the file without having
                // marked its beginning first.
                Tr("Start selection not set."),
                // Translators: The short wording spoken when the beginning of a repeating stretch has not been marked yet.
                Tr("No start."));
            return;
        }
        if (_player.Elapsed is not double elapsed) { _speech.Speak(Tr("Time not available"), Tr("Unavailable")); return; }
        if (elapsed <= start)
        {
            _speech.Speak(
                // Translators: Spoken when the end of a repeating stretch of the file would fall before its beginning.
                Tr("End selection must be after start."),
                // Translators: The short wording spoken when the end of a repeating stretch would fall before its beginning.
                Tr("Invalid end."));
            return;
        }
        if (!_player.SetLoopEnd(elapsed)) { _speech.Speak(Tr("Time not available"), Tr("Unavailable")); return; }
        _selection.SetEnd(elapsed);
        var formatted = PlaybackTimeFormatter.Format(elapsed)!;
        _speech.Speak(
            // Translators: Spoken once the end of a repeating stretch of the file has been marked. {time} is that point
            // in the file, such as 00:01:20.
            TrFormat("End selection: {time}", formatted),
            // Translators: The short wording spoken once the end of a repeating stretch has been marked. {time} is that
            // point in the file, such as 00:01:20.
            TrFormat("End {time}", formatted));
        _player.SeekAbsolute(start);
        _player.Play();
    }

    private void ClearSelection()
    {
        if (!_selection.IsActive(_player.CurrentPath) || _selection.End is not double end)
        {
            _speech.Speak(
                // Translators: Spoken when the user clears the repeating stretch of the file but none is marked.
                Tr("No selection to clear."),
                // Translators: The short wording spoken when no repeating stretch of the file is marked.
                Tr("No selection."));
            return;
        }
        _player.ClearLoop();
        _player.SeekAbsolute(end);
        _player.Play();
        _selection.Reset();
        // Translators: Spoken once the repeating stretch of the file has been dropped, so playing carries on normally.
        if (_settings.General.Verbosity == SpeechVerbosity.Beginner) _speech.SpeakText(Tr("Selection cleared"));
    }

    private bool TryGetCurrentTime(out string path, out double elapsed)
    {
        path = _player.CurrentPath ?? string.Empty;
        elapsed = 0;
        if (path.Length == 0) { _speech.Speak(Tr("No file loaded."), Tr("No file.")); return false; }
        if (_player.Elapsed is not double current) { _speech.Speak(Tr("Time not available"), Tr("Unavailable")); return false; }
        elapsed = current;
        return true;
    }
}
