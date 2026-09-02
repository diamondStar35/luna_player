using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using LunaPlayer.Media;
using LunaPlayer.UI;

namespace LunaPlayer.Application;

/// <summary>The wording of the progress window a background job is shown behind.</summary>
/// <param name="Title">The window's title, naming the job.</param>
/// <param name="Starting">What it says before the job has reported anything.</param>
/// <param name="Describe">The line naming the file being dealt with right now.</param>
internal sealed record ProgressPrompt(
    string Title,
    string Starting,
    Func<ProgressUpdate, string> Describe);

/// <summary>Runs a job that works through a set of files off the UI thread, behind a progress window the user
/// can abort from.</summary>
///
/// <remarks>
/// <see cref="Start"/> returns as soon as the job has been handed off, and the window goes back to its event
/// loop. The progress dialog is then driven from a timer, which is the only arrangement that keeps the player
/// answering while a long scan runs: a loop that waits for the job holds the UI thread, and a held UI thread
/// cannot paint the very dialog it is waiting on.
///
/// The job reports itself by calling the action it is handed, from its own thread; the reports are queued and
/// read on each tick, because a progress dialog can only be touched from the thread that owns it. Aborting
/// sets the token the job was given, so a job cooperates with the Cancel button rather than being killed.
/// </remarks>
internal static class BackgroundProgress
{
    /// <summary>How often the queue is drained and the bar redrawn. Short enough that the Cancel button
    /// answers at once, long enough to leave the event loop most of its time.</summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>The jobs that are still running.</summary>
    ///
    /// <remarks>
    /// This is what keeps them alive, and it is not optional. Once started, a job is referenced only by the
    /// timer driving it, and that timer only by the job: a cycle with nothing outside it, which the collector
    /// is free to take away while the scan is still running. The ticks then stop, and the progress window is
    /// left on screen with a bar that never moves and a Cancel button that answers to nothing.
    ///
    /// Only ever touched from the UI thread, which is where jobs start and finish.
    /// </remarks>
    private static readonly HashSet<object> Running = [];

    /// <summary>Starts <paramref name="work"/> and returns immediately. <paramref name="completed"/> runs on
    /// the UI thread once the job has finished, and not at all if the user aborted it.</summary>
    /// <param name="maximum">How many steps the job will report, so the bar can show a proportion.</param>
    /// <param name="work">The job. It is handed something to report progress to and a token that is set when
    /// the user aborts; it may either return early or throw <see cref="OperationCanceledException"/>.</param>
    internal static void Start<TResult>(
        IMainView view,
        IApplicationDispatcher dispatcher,
        ProgressPrompt prompt,
        int maximum,
        Func<Action<ProgressUpdate>, CancellationToken, TResult> work,
        Action<TResult> completed)
    {
        var job = new Job<TResult>(view, dispatcher, prompt, maximum, work, completed);
        Running.Add(job);
        job.Start();
    }

    private sealed class Job<TResult>
    {
        private readonly ConcurrentQueue<ProgressUpdate> _updates = new();
        private readonly CancellationTokenSource _cancellation = new();
        private readonly IApplicationDispatcher _dispatcher;
        private readonly ProgressPrompt _prompt;
        private readonly IProgressView _progress;
        private readonly Task<TResult> _task;
        private readonly Action<TResult> _completed;
        private IDisposable? _ticker;
        private bool _finished;
        private int _value;

        internal Job(
            IMainView view,
            IApplicationDispatcher dispatcher,
            ProgressPrompt prompt,
            int maximum,
            Func<Action<ProgressUpdate>, CancellationToken, TResult> work,
            Action<TResult> completed)
        {
            _dispatcher = dispatcher;
            _prompt = prompt;
            _completed = completed;
            _progress = view.BeginProgress(prompt.Title, prompt.Starting, maximum);
            _task = Task.Run(() => work(_updates.Enqueue, _cancellation.Token));
        }

        // Started after construction rather than inside it, so the field is set before the first tick can
        // run. Both happen on the UI thread, which cannot deliver a timer event until it goes back to its
        // event loop, so there is no window in which a tick could find no ticker to stop.
        internal void Start() => _ticker = _dispatcher.Repeat(TickInterval, Tick);

        private void Tick()
        {
            if (_finished)
                return;
            // Only the newest report is worth drawing; the ones behind it name files already dealt with.
            // The value only ever goes forward, as the Python player's pull() does with max().
            ProgressUpdate? latest = null;
            while (_updates.TryDequeue(out var update))
                latest = update;
            if (latest is ProgressUpdate shown && shown.Value > _value)
            {
                _value = shown.Value;
                _progress.Update(_value, _prompt.Describe(shown));
            }
            if (_progress.Cancelled)
                _cancellation.Cancel();
            if (_task.IsCompleted)
                Finish();
        }

        /// <summary>Ends the job. Everything that matters is posted rather than done here: this runs inside
        /// the timer's own callback, and both destroying that timer and opening the windows the result calls
        /// for are unsafe on a stack that is still inside it. The Python player does the same, finishing
        /// through CallAfter rather than from the tick.</summary>
        private void Finish()
        {
            _finished = true;
            var ticker = _ticker;
            _ticker = null;
            _dispatcher.Post(() =>
            {
                ticker?.Dispose();
                // Closed before the result is used, so the job's own window or message box does not open
                // behind a progress dialog that has finished with.
                _progress.Dispose();
                _cancellation.Dispose();
                Running.Remove(this);
                if (_task.IsCompletedSuccessfully)
                {
                    _completed(_task.Result);
                    return;
                }
                // A cancelled run is the user's own doing and says nothing. Anything else is a fault, and
                // swallowing it would hide a bug behind a progress window that simply closed.
                if (_task.IsFaulted && _task.Exception?.InnerException is Exception failure)
                    ExceptionDispatchInfo.Capture(failure).Throw();
            });
        }
    }
}
