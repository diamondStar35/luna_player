using LunaPlayer.UI.Services;
using LunaPlayer.Application;
using LunaPlayer.Recording;
using WxSharp;

namespace LunaPlayer.UI.Recording;
internal sealed partial class MainDialog
{

    // ---- running ----

    /// <summary>Starts the recording, or ends the one that is running.</summary>
    /// <remarks>
    /// One button and one handler rather than two of each, so pressing it never moves the keyboard
    /// somewhere else: the button the user is on stays the button they are on, and only what it says
    /// changes. A screen reader announces the new name, which is the whole of what happened.
    /// </remarks>
    private void Record()
    {
        // Ignored rather than prevented. See _busy: the button cannot be disabled without taking the
        // keyboard away from it.
        if (_busy)
            return;
        if (_engine.State is not RecordingState.Idle)
        {
            Stop();
            return;
        }
        if (_sources.Count == 0)
        {
            Messages.Show(
                // Translators: Shown when recording was started with nothing set up to record from.
                Tr("Add at least one source before recording."), MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            return;
        }
        // Kept, so the settings are still here the next time the window is opened in this session.
        _sources.Options = Options();
        var options = _sources.Options;
        var sources = _sources.All.Select(source => source.Copy()).ToList();
        _busy = true;
        _session.Start(options, sources, Started);
    }

    private void Started(RecordingOutcome outcome, IReadOnlyList<string> failures)
    {
        _busy = false;
        if (_closed)
            return;
        SyncButtons();
        if (!outcome.Success)
        {
            Messages.Show(RecordingSources.Describe(outcome.Failure, outcome.Detail),
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
            return;
        }
        if (failures.Count > 0)
        {
            Messages.Show(
                // Translators: Shown when recording started but some sources would not open. {names} is a
                // list of what the user called them, separated by commas.
                TrFormat("Recording started, but these sources could not be opened: {names}",
                    string.Join(", ", failures)), MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
        }
    }

    private void TogglePause()
    {
        if (_engine.State is RecordingState.Paused)
            _engine.Resume();
        else
            _engine.Pause();
        SyncButtons();
    }

    private void Stop()
    {
        _busy = true;
        _session.Stop(Stopped);
    }

    private void Stopped(RecordingOutcome outcome)
    {
        _busy = false;
        if (_closed)
            return;
        SyncButtons();
        if (!outcome.Success)
            Messages.Show(RecordingSources.Describe(outcome.Failure, outcome.Detail),
                MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
    }

    /// <summary>Puts every control into the state the recorder is in.</summary>
    /// <remarks>
    /// Everything about what is being recorded is fixed once recording starts. The mixer a recording is
    /// built on has no way to take an input away again, so a source cannot be added or removed part way
    /// through, and saying so by greying the buttons is better than refusing afterwards.
    /// </remarks>
    private void SyncButtons()
    {
        var idle = _engine.State is RecordingState.Idle;
        var has = _sources.Count > 0 && _list.SelectedIndex >= 0;
        // The label carries the state, not a second button. Enabled either way: with nothing set up
        // there is nothing to start, but a recording that is running can always be stopped.
        _record.Enabled = idle ? _sources.Count > 0 : true;
        _record.Label = idle
            // Translators: Button that begins recording.
            ? Tr("Start")
            // Translators: Button that ends a recording and closes the file.
            : Tr("Stop");
        _pause.Enabled = !idle;
        _pause.Label = _engine.State is RecordingState.Paused
            // Translators: Button that starts a paused recording again.
            ? Tr("Resume")
            // Translators: Button that holds a recording where it is.
            : Tr("Pause");
        _add.Enabled = idle;
        _edit.Enabled = idle && has;
        _remove.Enabled = idle && has;
        _volume.Enabled = idle && has;
        _format.Enabled = idle;
        _rate.Enabled = idle;
        _channels.Enabled = idle;
        _bitrate.Enabled = idle && AudioCatalog.HasBitrate(Format()) && _bitrates.Count > 0;
        _folder.Enabled = idle;
        _browse.Enabled = idle;
        _bitrateLabel.Show(idle);
        _bitrate.Show(idle);
        _browse.Show(idle);
        _dialog.Layout();
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
            _dialog.EndModal(StandardId.Cancel);
        else
            args.Skip();
    }

}
