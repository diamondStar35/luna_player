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
        _speech.Speak($"Start selection: {formatted}", $"Start {formatted}");
    }

    private void EndSelection()
    {
        var path = _player.CurrentPath;
        if (string.IsNullOrEmpty(path)) { _speech.Speak("No file loaded.", "No file."); return; }
        if (!_selection.IsActive(path, requireEnd: false) || _selection.Start is not double start)
        { _speech.Speak("Start selection not set.", "No start."); return; }
        if (_player.Elapsed is not double elapsed) { _speech.Speak("Time not available", "Unavailable"); return; }
        if (elapsed <= start) { _speech.Speak("End selection must be after start.", "Invalid end."); return; }
        if (!_player.SetLoopEnd(elapsed)) { _speech.Speak("Time not available", "Unavailable"); return; }
        _selection.SetEnd(elapsed);
        var formatted = PlaybackTimeFormatter.Format(elapsed)!;
        _speech.Speak($"End selection: {formatted}", $"End {formatted}");
        _player.SeekAbsolute(start);
        _player.Play();
    }

    private void ClearSelection()
    {
        if (!_selection.IsActive(_player.CurrentPath) || _selection.End is not double end)
        { _speech.Speak("No selection to clear.", "No selection."); return; }
        _player.ClearLoop();
        _player.SeekAbsolute(end);
        _player.Play();
        _selection.Reset();
        if (_settings.General.Verbosity == SpeechVerbosity.Beginner) _speech.SpeakText("Selection cleared");
    }

    private bool TryGetCurrentTime(out string path, out double elapsed)
    {
        path = _player.CurrentPath ?? string.Empty;
        elapsed = 0;
        if (path.Length == 0) { _speech.Speak("No file loaded.", "No file."); return false; }
        if (_player.Elapsed is not double current) { _speech.Speak("Time not available", "Unavailable"); return false; }
        elapsed = current;
        return true;
    }
}
