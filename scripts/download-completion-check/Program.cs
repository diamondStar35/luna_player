using System.Runtime.InteropServices;
using LunaPlayer.Application;
using LunaPlayer.UI.YouTube;
using LunaPlayer.YouTube.Download;
using WxSharp;

// Windows native UI regression, no download/network required:
// dotnet run --project scripts/download-completion-check/Check.csproj -- [plain] [overlap] [failure]
// Default: complete while another modal is open. plain: no existing modal.
// overlap: two completions. failure: failed download instead of success.
internal static class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? cls, string title);
    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    [STAThread]
    private static int Main(string[] args)
    {
        using var app = new App { ExitOnFrameDelete = false };
        using var frame = new Frame(title: "Download completion regression");
        app.TopWindow = frame;
        frame.Show();
        var dispatcher = new Dispatcher();
        var status = args.Contains("failure") ? QueueItemStatus.Failed : QueueItemStatus.Completed;
        using var progress = new DownloadProgressDialog(frame, dispatcher, new DownloadQueue(status), Guid.NewGuid());
        using var second = new DownloadProgressDialog(frame, dispatcher, new DownloadQueue(status), Guid.NewGuid());
        var count = args.Contains("overlap") ? 2 : 1;
        var closed = 0;
        var depth = 0;
        var finished = false;
        var available = true;
        void Completed(DownloadProgressDialog sender, Outcome? outcome)
        {
            depth++;
            available &= depth == 1 && frame.Enabled
                && outcome?.Success == (status == QueueItemStatus.Completed);
            Wx.MessageBox("Download finished", "Completion regression OK", MessageBoxStyle.Ok, frame);
            available &= frame.Enabled;
            depth--;
            finished = ++closed == count;
        }
        progress.Closed += Completed;
        second.Closed += Completed;
        // Dismiss only our native success box, from outside its nested event loop.
        using var cancellation = new CancellationTokenSource();
        var clicker = Task.Run(async () =>
        {
            while (!cancellation.IsCancellationRequested)
            {
                var box = FindWindow(null, "Completion regression OK");
                if (box != 0)
                {
                    // Keep it open across timer ticks to exercise overlapping completion.
                    await Task.Delay(350);
                    PostMessage(box, 0x0111, 1, 0); // WM_COMMAND, IDOK
                }
                await Task.Delay(30);
            }
        });
        using var modal = new Dialog(frame, title: "Existing application dialog");
        using var closeModal = new WxSharp.Timer(app);
        closeModal.Tick += (_, _) => modal.EndModal(StandardId.Cancel);
        using var watchdog = new WxSharp.Timer(app);
        watchdog.Tick += (_, _) => { Console.Error.WriteLine("Timed out"); Environment.Exit(2); };
        watchdog.Start(5000, oneShot: true);
        using var check = new WxSharp.Timer(app);
        check.Tick += (_, _) =>
        {
            if (!finished || !frame.Enabled) return;
            watchdog.Dispose();
            closeModal.Dispose();
            check.Dispose();
            progress.Dispose();
            second.Dispose();
            modal.Dispose();
            frame.Dispose();
            app.ExitMainLoop();
        };
        check.Start(50);
        Wx.CallAfter(() =>
        {
            progress.Show();
            if (count == 2) second.Show();
            if (!args.Contains("plain"))
            {
                closeModal.Start(900, oneShot: true);
                modal.ShowModal();
            }
        });
        app.MainLoop();
        cancellation.Cancel();
        clicker.GetAwaiter().GetResult();
        Console.WriteLine(available ? "PASS: main window available after completion OK" : "FAIL: main window disabled after completion OK");
        return available ? 0 : 1;
    }

    private sealed class Dispatcher : IApplicationDispatcher
    {
        public IDisposable Repeat(TimeSpan interval, Action action)
        {
            var timer = new WxSharp.Timer(App.Current!);
            timer.Tick += (_, _) => action();
            timer.Start((int)interval.TotalMilliseconds);
            return timer;
        }
    }
}
