using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using LunaPlayer.Application;
using LunaPlayer.Media;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>The window shown while a batch of files is converted: what is being written now, how far it and
/// the whole batch have got, how long is left, and a way to stop.</summary>
///
/// <remarks>
/// Not the plain progress window a scan or a download is shown behind. That one carries a single line; this
/// carries a block - the file in hand, its percentage, how many are left, the time remaining and the batch
/// total - in a read-only text area, because a label that grew and shrank on every tick would move the
/// Cancel button under the user's pointer, and because a screen reader reading a block is what the user asked
/// for.
///
/// Modal, the way the recording window is: <see cref="Show"/> does not return until the batch has finished or
/// been stopped, and the window's own event loop keeps it painted and its Cancel button live meanwhile. The
/// conversion itself runs on a worker; a timer drains its reports and redraws the window, which is the only
/// arrangement that keeps the loop free - a loop that waited on the worker could not paint the very window it
/// was waiting behind.
///
/// Stopping is confirmed first, whether it is asked for with the Cancel button, the Escape key or the window's
/// close box. The confirmation opens a message box, which pumps the event loop and so lets a tick arrive
/// while it is up; <see cref="_confirming"/> holds those ticks off, so the window is never torn down under
/// the question the user is still answering.
/// </remarks>
internal sealed class ConversionProgressDialog : IDisposable
{
    /// <summary>The gauge's range: a percentage to one decimal place, so a long file does not sit on the
    /// same step for seconds on end. The same figure the plain progress window uses.</summary>
    private const int Range = 1000;

    /// <summary>How often the reports are drained and the window redrawn. Short enough that Cancel answers
    /// at once, long enough to leave the event loop most of its time.</summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);

    private readonly Dialog _dialog;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly TextCtrl _text;
    private readonly Gauge _gauge;
    private readonly ConcurrentQueue<ConversionProgress> _updates = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Func<Action<ConversionProgress>, CancellationToken, ConversionOutcome> _work;

    private Task<ConversionOutcome>? _task;
    private IDisposable? _ticker;
    private ConversionOutcome? _outcome;

    /// <summary>Set once the batch has ended, so no later tick touches a window on its way out.</summary>
    private bool _finished;

    /// <summary>Set while the stop-confirmation message box is open. That box pumps the event loop, so a
    /// tick can arrive while the user is deciding; this holds those ticks off, so the window is not finished
    /// and disposed under the question.</summary>
    private bool _confirming;

    internal ConversionProgressDialog(
        Window parent,
        IApplicationDispatcher dispatcher,
        Func<Action<ConversionProgress>, CancellationToken, ConversionOutcome> work)
    {
        _dispatcher = dispatcher;
        _work = work;
        _dialog = new Dialog(parent, title: Title, style: DialogStyle.Default | DialogStyle.ResizeBorder);

        var sizer = new BoxSizer(Orientation.Vertical);
        _text = new TextCtrl(
            _dialog,
            // Translators: Shown in the converter's progress window before the first file has reported.
            value: Tr("Preparing to convert…"),
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        sizer.Add(_text, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);

        _gauge = new Gauge(_dialog, range: Range);
        sizer.Add(_gauge, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);

        // A plain button rather than the standard Cancel one: a dialog treats a wxID_CANCEL button as its
        // way out and would end the modal loop on Escape or a press without asking. Stopping here has to be
        // confirmed first, so the way out is routed by hand instead.
        // Translators: The button that stops a running conversion. It asks for confirmation first.
        var cancel = new Button(_dialog, label: Tr("Cancel"));
        cancel.Click += (_, _) => RequestStop();
        sizer.Add(cancel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.AlignRight, border: 8);

        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(560, 320);
        _dialog.Center(onParent: true);
        // Escape asks to stop rather than closing the window behind the batch's back. Swallowed, so the
        // toolkit does not also act on it.
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        // The close box means the same as Cancel, and is refused until the batch has actually stopped: letting
        // it through would destroy a window the timer is still writing to.
        _dialog.Closing += (_, args) =>
        {
            args.Veto();
            RequestStop();
        };
        // So a screen reader lands on the way out; the text area is read by moving to it.
        cancel.Focus();
    }

    /// <summary>Runs the batch behind the window and does not return until it has finished or been stopped.
    /// Gives back what the batch produced, or null when the user stopped it.</summary>
    internal ConversionOutcome? Show()
    {
        // The token goes to Task.Run as well as to the job, so a stopped run ends as Canceled rather than
        // Faulted - the cancellation exception's token has to match the task's for .NET to read it as the
        // abort it is, otherwise it would be rethrown at the user as a failure.
        _task = Task.Run(() => _work(_updates.Enqueue, _cancellation.Token), _cancellation.Token);
        // Set before the loop starts pumping, so the first tick always finds a ticker to stop. Both this and
        // ShowModal run on the UI thread, which cannot deliver a tick until ShowModal hands it the loop.
        _ticker = _dispatcher.Repeat(TickInterval, Tick);
        _dialog.ShowModal();
        return _outcome;
    }

    public void Dispose()
    {
        _ticker?.Dispose();
        _ticker = null;
        _cancellation.Dispose();
        _dialog.Dispose();
    }

    private void Tick()
    {
        // Nothing while the batch has ended or while the stop question is up: the first would write to a
        // window on its way out, the second would tear it down under the user's answer.
        if (_finished || _confirming)
            return;
        // Only the newest report is worth drawing; the ones behind it name work already moved past. The
        // batch total climbs on its own, since a file's share only ever rises.
        ConversionProgress? latest = null;
        while (_updates.TryDequeue(out var update))
            latest = update;
        if (latest is ConversionProgress shown)
            Render(shown);
        if (_task!.IsCompleted)
            Finish();
    }

    private void Render(ConversionProgress progress)
    {
        _gauge.Value = Math.Clamp((int)Math.Round(progress.TotalPercent * (Range / 100.0)), 0, Range);
        _text.Value = string.Join(Environment.NewLine,
            // Translators: The converter's progress. {number} is the file's place, {total} how many files
            // there are, {name} the file being converted right now.
            TrFormat("Converting file {number} of {total}: {name}",
                progress.FileNumber, progress.TotalFiles, progress.CurrentName),
            // Translators: How far the file being converted right now has got. {percent} is a whole number.
            TrFormat("Percentage: {percent}%", (int)Math.Round(progress.FilePercent)),
            // Translators: How many files have not been converted yet. {count} is a whole number.
            TrFormat("Remaining files: {count}", progress.RemainingFiles),
            // Translators: A guess at the time left. {time} is a clock such as 3:20 or 1:05:00, or the word
            // for a guess not yet worth making.
            TrFormat("Estimated time left: {time}",
                progress.EstimatedRemaining is TimeSpan left
                    ? FormatDuration(left)
                    // Translators: Shown for the estimated time left before enough is done to guess it.
                    : Tr("calculating…")),
            // Translators: How far the whole batch has got. {percent} is a whole number.
            TrFormat("Total percentage: {percent}%", (int)Math.Round(progress.TotalPercent)));
    }

    /// <summary>Asks whether to stop, and stops the batch if the answer is yes. Does nothing when the batch
    /// has already ended or a question is already up, so a second press stacks nothing on the first.</summary>
    private void RequestStop()
    {
        if (_finished || _confirming)
            return;
        _confirming = true;
        var stop = Wx.MessageBox(
            // Translators: Asks the user to confirm stopping a conversion that is still running.
            Tr("The conversion is still running. Would you like to stop it?"),
            // Translators: Title of the prompt shown when the user tries to stop a running conversion.
            Tr("Warning"), MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, _dialog) == MessageBoxStyle.Yes;
        _confirming = false;
        // The batch may have finished while the question was up. If so, leave it to the next tick to close
        // the window with the result rather than cancelling a run that is already done.
        if (stop && !_task!.IsCompleted)
            _cancellation.Cancel();
    }

    /// <summary>Ends the batch and closes the window. Posted rather than done here, because this runs inside
    /// the timer's own callback, where destroying that timer and ending the modal loop are unsafe on a stack
    /// still inside it.</summary>
    private void Finish()
    {
        _finished = true;
        var ticker = _ticker;
        _ticker = null;
        _dispatcher.Post(() =>
        {
            ticker?.Dispose();
            _dialog.EndModal(StandardId.Ok);
            if (_task!.IsCompletedSuccessfully)
            {
                _outcome = _task.Result;
                return;
            }
            // A stopped run is the user's own doing and leaves nothing to report. Anything else is a fault,
            // and swallowing it would hide a bug behind a window that merely closed; a cancellation carrying
            // somebody else's token still counts as a stop, so it is checked for by type.
            if (_task.IsFaulted && _task.Exception?.InnerException is Exception failure
                and not OperationCanceledException)
                ExceptionDispatchInfo.Capture(failure).Throw();
        });
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
            RequestStop();
        else
            args.Skip();
    }

    /// <summary>A span as a clock: minutes and seconds, with an hours field only once there is an hour to
    /// show.</summary>
    private static string FormatDuration(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes}:{span.Seconds:00}";
    }

    private static string Title =>
        // Translators: Title of the window shown while files are being converted.
        Tr("Converting files");
}
