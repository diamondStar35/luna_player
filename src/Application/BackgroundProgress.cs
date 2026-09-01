using System.Collections.Concurrent;
using LunaPlayer.Media;
using LunaPlayer.UI;

namespace LunaPlayer.Application;

/// <summary>The wording of the progress window a background job is shown behind.</summary>
/// <param name="Title">The window's title, naming the job.</param>
/// <param name="Starting">What it says before the job has reported anything.</param>
/// <param name="Working">What it says while the job is busy but has nothing new to report.</param>
/// <param name="Describe">The line naming the file being dealt with right now.</param>
internal sealed record ProgressPrompt(
    string Title,
    string Starting,
    string Working,
    Func<ProgressUpdate, string> Describe);

/// <summary>Runs a job that works through a set of files off the UI thread, behind a progress window the user
/// can abort from.</summary>
///
/// <remarks>
/// The job reports itself by calling the action it is handed, from its own thread; the reports are queued and
/// read here, because a progress dialog can only be touched from the thread that owns it. Aborting sets the
/// token the job was given, so a job cooperates with the Cancel button rather than being killed.
///
/// The wait is a poll rather than an await: the actions that use this are called from a menu or a key press
/// and their callers are written straight through, so this has to finish before it returns. The window stays
/// responsive because the progress dialog pumps messages while it is updated. The UI thread is otherwise held
/// for as long as the job runs, which is the price of a modal progress window.
/// </remarks>
internal static class BackgroundProgress
{
    /// <summary>How long to wait between reading the queue. Short enough that the Cancel button answers at
    /// once, long enough not to spin.</summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(25);

    /// <summary>Runs <paramref name="work"/> and returns what it produced, or false when the user aborted it.
    /// </summary>
    /// <param name="maximum">How many steps the job will report, so the bar can show a proportion.</param>
    /// <param name="work">The job. It is handed something to report progress to and a token that is set when
    /// the user aborts; it may either return early or throw <see cref="OperationCanceledException"/>.</param>
    internal static bool TryRun<TResult>(
        IMainView view,
        ProgressPrompt prompt,
        int maximum,
        Func<Action<ProgressUpdate>, CancellationToken, TResult> work,
        out TResult result)
    {
        var updates = new ConcurrentQueue<ProgressUpdate>();
        using var cancellation = new CancellationTokenSource();
        var task = Task.Run(() => work(updates.Enqueue, cancellation.Token));
        // Closed before the result is used, so the job's own window or message box does not open behind a
        // progress dialog that has finished with.
        using (var progress = view.BeginProgress(prompt.Title, prompt.Starting, maximum))
        {
            while (!task.IsCompleted)
            {
                var reported = false;
                while (updates.TryDequeue(out var update))
                {
                    reported = true;
                    if (!progress.Update(update.Value, prompt.Describe(update)))
                        cancellation.Cancel();
                }
                if (!reported && !progress.Pulse(prompt.Working))
                    cancellation.Cancel();
                Thread.Sleep(PollInterval);
            }
        }
        try
        {
            result = task.GetAwaiter().GetResult();
            return true;
        }
        catch (OperationCanceledException)
        {
            result = default!;
            return false;
        }
    }
}
