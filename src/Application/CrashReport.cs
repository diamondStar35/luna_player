using System.Runtime.ExceptionServices;
using System.Text;
using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.Application;

/// <summary>Catches what would otherwise end the player without a word, and shows it instead.</summary>
///
/// <remarks>
/// A native-ahead-of-time build has no runtime dialog of its own: an exception nothing catches prints to a
/// console that is not there and the process is gone, which from the user's side is the window vanishing.
/// Worse, most of the player's code runs inside callbacks that wxWidgets invokes from its own C++ frames,
/// and an exception cannot be unwound through those - so it has to be caught on this side of the boundary,
/// before it reaches them.
///
/// That is what <see cref="Guard"/> is for, and it is wrapped around the few places where the player's work
/// actually begins: an action from a menu or a shortcut, and anything posted or timed. Nearly everything
/// the player does starts at one of those, so nearly everything is covered by three call sites.
/// <see cref="Install"/> adds the backstops for what does not.
///
/// The report is written to a file before it is shown, because the most useful crash report is the one from
/// the crash that was too severe to show anything.
/// </remarks>
internal static class CrashReport
{
    private static readonly Lock Sync = new();
    private static IClipboardService? _clipboard;
    private static bool _showing;

    /// <summary>Where the reports are kept, beside the settings.</summary>
    internal static string Path { get; } = System.IO.Path.Combine(Paths.RootDirectory, "crash.log");

    /// <summary>Starts catching. Called before anything else the player does.</summary>
    /// <param name="clipboard">Used by the Copy button. Null until the toolkit is up, and set again once
    /// it is.</param>
    internal static void Install(IClipboardService? clipboard = null)
    {
        _clipboard = clipboard ?? _clipboard;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Write(args.ExceptionObject as Exception, "unhandled");
        // A task nobody awaited that failed. Not fatal, and not shown - it would interrupt the user over
        // something they did not ask for - but worth having in the file when a later crash is being read.
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Write(args.Exception, "unobserved");
            args.SetObserved();
        };
    }

    /// <summary>Runs <paramref name="work"/>, showing anything it throws rather than letting it end the
    /// player.</summary>
    /// <remarks>
    /// Cancellation is not a fault and is swallowed: it means something the user stopped, and every place
    /// that stops something has already said so.
    /// </remarks>
    internal static void Guard(Action work)
    {
        try
        {
            work();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception failure)
        {
            Report(failure);
        }
    }

    /// <summary>Writes a failure to the file without interrupting anybody.</summary>
    /// <remarks>
    /// For a failure that has already been reported to the user in its own words, where the words are not
    /// enough to work out what happened. The message box says "the audio format is not supported"; the log
    /// says which call said so, and from where.
    /// </remarks>
    internal static void Note(Exception failure) => Write(failure, "noted");

    /// <summary>Writes a failure down and shows it.</summary>
    internal static void Report(Exception failure)
    {
        Write(failure, "caught");
        Show(failure);
    }

    /// <summary>Appends a report to the file, and never throws doing it.</summary>
    private static void Write(Exception? failure, string kind)
    {
        if (failure is null)
            return;
        try
        {
            Paths.EnsureDirectoryFor(Path);
            File.AppendAllText(Path, Describe(failure, kind), Encoding.UTF8);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The report is the consolation prize for a crash, not something worth crashing over.
        }
    }

    /// <summary>The text the user is shown and the file is given. Deliberately not translated: it is for
    /// whoever reads the report, who is not necessarily the person looking at it.</summary>
    private static string Describe(Exception failure, string kind)
    {
        var text = new StringBuilder()
            .Append("---- ").Append(AppInfo.Name).Append(' ').Append(AppInfo.Version)
            .Append(" — ").Append(kind).Append(" — ")
            .Append(DateTimeOffset.Now.ToString("u", System.Globalization.CultureInfo.InvariantCulture))
            .AppendLine(" ----")
            .AppendLine(failure.ToString());
        return text.AppendLine().ToString();
    }

    /// <remarks>
    /// One at a time. A fault often repeats - a timer whose tick throws does it every tick - and a stack of
    /// identical windows would bury the first one and the player with it.
    /// </remarks>
    private static void Show(Exception failure)
    {
        lock (Sync)
        {
            if (_showing)
                return;
            _showing = true;
        }
        try
        {
            using var dialog = new UI.CrashDialog(Describe(failure, "caught"), Path, _clipboard);
            dialog.Show();
        }
        catch (Exception exception)
        {
            // The window itself would not open, which usually means the toolkit is the thing that is
            // broken. There is nothing left to show it with, so the file is all there is.
            Write(exception, "while reporting");
        }
        finally
        {
            lock (Sync)
                _showing = false;
        }
    }

    /// <summary>Rethrows a failure so it reaches the handlers above, keeping its original stack.</summary>
    internal static void Rethrow(Exception failure) => ExceptionDispatchInfo.Capture(failure).Throw();
}
