using LunaPlayer.Application;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.YouTube;

/// <summary>A modeless view of one download owned by <see cref="Downloads.DownloadQueue"/>. The queue
/// outlives this window, which lets the user minimize the job into it without interrupting yt-dlp.</summary>
internal sealed class DownloadProgressDialog : IDisposable
{
    private const int Range = 1000;
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(150);

    private readonly Dialog _dialog;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly Downloads.DownloadQueue _queue;
    private readonly Guid _itemId;
    private readonly TextCtrl _text;
    private readonly Gauge _gauge;

    private IDisposable? _ticker;
    private bool _finished;
    private bool _confirming;
    private bool _disposed;

    internal DownloadProgressDialog(
        IApplicationDispatcher dispatcher,
        Downloads.DownloadQueue queue,
        Guid itemId)
    {
        _dispatcher = dispatcher;
        _queue = queue;
        _itemId = itemId;
        // Translators: Title of the window shown while a video is being downloaded.
        _dialog = new Dialog(
            parent: null,
            // Translators: Title of the window showing progress for one YouTube download.
            title: Tr("Downloading"),
            size: new Size(520, 300),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        var sizer = new BoxSizer(Orientation.Vertical);
        _text = new TextCtrl(
            _dialog,
            // Translators: Shown in the download window before the first progress has arrived.
            value: Tr("Preparing to download…"),
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        sizer.Add(_text, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        _gauge = new Gauge(_dialog, range: Range);
        sizer.Add(_gauge, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);

        var buttons = new BoxSizer(Orientation.Horizontal);
        // Translators: Hides this progress window while leaving its download visible in the queue.
        var minimize = new Button(_dialog, label: Tr("Minimize to queue"));
        minimize.Click += (_, _) => MinimizeToQueue();
        buttons.Add(minimize);
        buttons.AddStretchSpacer();
        // A plain button, not the standard Cancel, so Escape and clicks route through the confirmation.
        // Translators: The button that stops a running download. It asks for confirmation first.
        var cancel = new Button(_dialog, label: Tr("Cancel"));
        cancel.Click += (_, _) => RequestStop();
        buttons.Add(cancel);
        sizer.Add(buttons, flags: SizerFlags.All | SizerFlags.Expand, border: 8);

        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(520, 300);
        _dialog.Center(onParent: false);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        _dialog.Closing += (_, args) =>
        {
            args.Veto();
            RequestStop();
        };
        cancel.Focus();
    }

    /// <summary>Raised after this modeless window has released all of its UI resources. A null result means
    /// the download was cancelled or moved into the queue and needs no message box.</summary>
    internal event Action<DownloadProgressDialog, Downloads.Outcome?>? Closed;

    internal void Show()
    {
        _ticker = _dispatcher.Repeat(TickInterval, Tick);
        // Show(), unlike ShowModal(), returns immediately and does not disable the application's main window.
        _dialog.Show();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _finished = true;
        _ticker?.Dispose();
        _ticker = null;
        _dialog.Dispose();
    }

    private void Tick()
    {
        if (_finished || _confirming)
            return;
        var item = _queue.Snapshot(_itemId);
        if (item is not Downloads.QueueItemSnapshot current)
        {
            Close(null);
            return;
        }
        if (current.Status == Downloads.QueueItemStatus.Completed)
        {
            _queue.Remove(_itemId);
            Close(Downloads.Outcome.Saved(current.FilePath));
            return;
        }
        if (current.Status == Downloads.QueueItemStatus.Failed)
        {
            _queue.Remove(_itemId);
            Close(Downloads.Outcome.Failed(current.Error));
            return;
        }
        Render(current);
    }

    private void Render(Downloads.QueueItemSnapshot item)
    {
        _gauge.Value = Math.Clamp((int)Math.Round(item.Percent * (Range / 100.0)), 0, Range);
        if (item.Status == Downloads.QueueItemStatus.Downloaded || item.PostProcessor.Length > 0)
        {
            var activity = PostProcessText(item.PostProcessor, item.PostProcessStatus);
            _dialog.Title = activity;
            if (_text.Value != activity)
                _text.Value = activity;
            return;
        }

        // Translators: Title of the progress window while a YouTube file is downloading.
        _dialog.Title = Tr("Downloading");
        var total = item.TotalBytes > 0
            ? Size(item.TotalBytes)
            // Translators: Shown for the total download size before yt-dlp reports it.
            : Tr("unknown");
        var speed = item.Speed > 0
            // Translators: Download speed. {size} is a file size and "/s" means per second.
            ? TrFormat("{size}/s", Size((long)item.Speed))
            // Translators: Shown for download speed before enough data exists to measure it.
            : Tr("calculating…");
        var eta = item.Eta > 0
            ? FormatDuration(TimeSpan.FromSeconds(item.Eta))
            // Translators: Shown for estimated time left before enough data exists to calculate it.
            : Tr("calculating…");
        _text.Value = string.Join(Environment.NewLine,
            // Translators: How much of the file has downloaded. {done} and {total} are sizes such as "4.2 MB".
            TrFormat("Downloaded: {done} of {total}",
                Size(item.DownloadedBytes),
                total),
            // Translators: How far the download has got. {percent} is a whole number.
            TrFormat("Percentage: {percent}%", (int)Math.Round(item.Percent)),
            // Translators: The current download speed. {speed} is a size per second such as "1.3 MB/s".
            TrFormat("Speed: {speed}", speed),
            // Translators: A guess at the time left. {time} is a clock such as 3:20, or the word for a guess
            // not yet worth making.
            TrFormat("Estimated time left: {time}", eta));
    }

    private static string PostProcessText(string processor, string status)
    {
        if (status.Equals("finished", StringComparison.OrdinalIgnoreCase))
            // Translators: A download has finished conversion and is being finalized.
            return Tr("Finishing…");
        if (processor.Contains("Merger", StringComparison.OrdinalIgnoreCase))
            // Translators: yt-dlp is joining the downloaded audio and video streams.
            return Tr("Merging video and audio…");
        if (processor.Contains("ExtractAudio", StringComparison.OrdinalIgnoreCase)
            || processor.Contains("Convert", StringComparison.OrdinalIgnoreCase)
            || processor.Contains("Remux", StringComparison.OrdinalIgnoreCase))
            // Translators: yt-dlp/ffmpeg is converting the download to the chosen format.
            return Tr("Converting…");
        if (processor.Contains("Embed", StringComparison.OrdinalIgnoreCase))
            // Translators: yt-dlp is embedding metadata, artwork, subtitles, or chapters.
            return Tr("Embedding information…");
        if (processor.Contains("Chapter", StringComparison.OrdinalIgnoreCase)
            || processor.Contains("Sponsor", StringComparison.OrdinalIgnoreCase))
            // Translators: yt-dlp is modifying chapters or removing sponsored sections.
            return Tr("Processing chapters…");
        if (processor.Contains("MoveFiles", StringComparison.OrdinalIgnoreCase))
            // Translators: yt-dlp is moving the finished file to its destination.
            return Tr("Finishing…");
        // Translators: Generic state while yt-dlp performs a finishing operation after downloading.
        return Tr("Post-processing…");
    }

    private void RequestStop()
    {
        if (_finished || _confirming)
            return;
        var item = _queue.Snapshot(_itemId);
        if (item is null || item.Value.Status is Downloads.QueueItemStatus.Completed
            or Downloads.QueueItemStatus.Failed)
        {
            Tick();
            return;
        }
        _confirming = true;
        var stop = Wx.MessageBox(
            // Translators: Asks the user to confirm stopping a download that is still running.
            Tr("The download is still running. Would you like to stop it?"),
            // Translators: Title of the prompt shown when the user tries to stop a running download.
            Tr("Warning"), MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, _dialog) == MessageBoxStyle.Yes;
        _confirming = false;
        if (!stop)
            return;
        _queue.Remove(_itemId);
        Close(null);
    }

    private void MinimizeToQueue()
    {
        if (_finished)
            return;
        _queue.Reveal(_itemId);
        Close(null);
    }

    private void Close(Downloads.Outcome? outcome)
    {
        if (_finished)
            return;
        _finished = true;
        _ticker?.Dispose();
        _ticker = null;
        _dialog.Hide();
        Dispose();
        Closed?.Invoke(this, outcome);
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
            RequestStop();
        else
            args.Skip();
    }

    /// <summary>A count of bytes in the largest unit that keeps it readable.</summary>
    private static string Size(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0 ? $"{bytes} {units[unit]}" : $"{value:0.0} {units[unit]}";
    }

    private static string FormatDuration(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes}:{span.Seconds:00}";
    }
}
