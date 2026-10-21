using LunaPlayer.Application;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.YouTube;

/// <summary>The live, modal view of the application-wide download queue.</summary>
internal sealed class DownloadQueueDialog : IDisposable
{
    private const int GaugeRange = 1000;
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(200);

    private readonly Dialog _dialog;
    private readonly Downloads.DownloadQueue _queue;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly Func<IReadOnlyList<string>, string?> _addLinks;
    private readonly Action _start;
    private readonly TextCtrl _status;
    private readonly ListCtrl _list;
    private readonly Gauge _gauge;
    private readonly Button _stop;
    private readonly Button _startButton;
    private readonly Button _stopAll;
    private readonly List<Guid> _shownIds = [];
    private readonly List<Downloads.QueueItemStatus> _shownStatuses = [];
    private IDisposable? _ticker;
    private Downloads.QueueSnapshot? _snapshot;

    internal DownloadQueueDialog(
        Window parent,
        IApplicationDispatcher dispatcher,
        Downloads.DownloadQueue queue,
        Func<IReadOnlyList<string>, string?> addLinks,
        Action start)
    {
        _dispatcher = dispatcher;
        _queue = queue;
        _addLinks = addLinks;
        _start = start;
        // Translators: Title of the window listing queued YouTube downloads.
        _dialog = new Dialog(parent, title: Tr("Download queue"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        _status = new TextCtrl(_dialog, style:
            TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        _list = new ListCtrl(_dialog, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection);
        // Translators: First column in the download queue, holding a video title or a manually entered link.
        _list.InsertColumn(0, Tr("Video"), 480);
        // Translators: Second column in the download queue, holding states such as In queue or Downloading.
        _list.InsertColumn(1, Tr("Status"), 150);
        _gauge = new Gauge(_dialog, GaugeRange);

        // yt-dlp provides cancellation but has no supported pause/resume operation, so the queue deliberately
        // offers no pause buttons and does not bind Space.
        // Translators: Stops the selected download and returns it to the queue.
        _stop = new Button(_dialog, label: Tr("Stop"));
        _stop.Click += (_, _) => StopSelected();
        // Translators: Opens a box where one or more YouTube links can be added to the queue.
        var add = new Button(_dialog, label: Tr("Add..."));
        add.Click += (_, _) => AddLinks();
        // Translators: Starts every unfinished item in the download queue.
        _startButton = new Button(_dialog, label: Tr("Start"));
        _startButton.Click += (_, _) => _start();
        // Translators: Stops every active download and returns it to the queue.
        _stopAll = new Button(_dialog, label: Tr("Stop all"));
        _stopAll.Click += (_, _) => _queue.StopAll();
        // Translators: Closes the queue window. Active downloads continue in the background.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));
        _dialog.SetEscapeId(StandardId.Cancel);

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(_stop, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(add, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_startButton, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_stopAll);
        buttons.AddStretchSpacer();
        buttons.Add(close);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_status, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        sizer.Add(_list, proportion: 1,
            flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
            border: 8);
        sizer.Add(_gauge,
            flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
            border: 8);
        sizer.Add(buttons,
            flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
            border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(760, 480);
        _dialog.Center(onParent: true);

        _list.ItemSelected += (_, _) => SyncButtons();
        _list.ItemDeselected += (_, _) => SyncButtons();
        _list.ItemFocused += (_, _) => SyncButtons();
        _list.ItemKeyDown += (_, args) =>
        {
            if (args.Code is Key.Delete or Key.NumpadDelete)
                RemoveSelected();
        };
        Refresh();
        _list.Focus();
    }

    internal void Show()
    {
        _ticker = _dispatcher.Repeat(TickInterval, Refresh);
        try
        {
            _dialog.ShowModal();
        }
        finally
        {
            // Completed rows remain available for review until the user closes this window.
            _queue.RemoveCompleted();
        }
    }

    public void Dispose()
    {
        _ticker?.Dispose();
        _ticker = null;
        _dialog.Dispose();
    }

    private void Refresh()
    {
        var snapshot = _queue.Snapshot();
        _snapshot = snapshot;
        RefreshList(snapshot);
        var gaugeValue = Math.Clamp(
            (int)Math.Round(snapshot.TotalPercent * (GaugeRange / 100.0)), 0, GaugeRange);
        if (_gauge.Value != gaugeValue)
            _gauge.Value = gaugeValue;
        var hasItems = snapshot.Items.Count > 0;
        var statusVisibilityChanged = SetVisible(_status, hasItems);
        if (hasItems)
        {
            var statusText = StatusText(snapshot);
            if (_status.Value != statusText)
            {
                // Updating a wxTextCtrl normally moves its caret to the beginning. Preserve the user's
                // reading position while live progress changes the numbers around it.
                var selection = _status.Selection;
                var insertionPoint = _status.InsertionPoint;
                _status.ChangeValue(statusText);
                var lastPosition = _status.LastPosition;
                _status.Selection = (
                    Math.Min(selection.From, lastPosition),
                    Math.Min(selection.To, lastPosition));
                if (selection.From == selection.To)
                    _status.InsertionPoint = Math.Min(insertionPoint, lastPosition);
            }
        }
        if (statusVisibilityChanged)
            _dialog.Layout();
        SyncButtons();
    }

    private void RefreshList(Downloads.QueueSnapshot snapshot)
    {
        var selectedId = SelectedId();
        var rebuild = _shownIds.Count != snapshot.Items.Count
            || !_shownIds.SequenceEqual(snapshot.Items.Select(item => item.Id));
        if (rebuild)
        {
            _list.Clear();
            _shownIds.Clear();
            _shownStatuses.Clear();
            foreach (var item in snapshot.Items)
            {
                var row = _list.AddItem(item.Title);
                _list.SetItem(row, 1, StatusLabel(item.Status));
                _list.SetItemData(row, item.Id);
                _shownIds.Add(item.Id);
                _shownStatuses.Add(item.Status);
            }
            var selected = selectedId is Guid id ? _shownIds.IndexOf(id) : -1;
            if (selected < 0 && _shownIds.Count > 0)
                selected = 0;
            if (selected >= 0)
            {
                _list.SelectedIndex = selected;
                _list.EnsureVisible(selected);
            }
            return;
        }
        for (var index = 0; index < snapshot.Items.Count; index++)
        {
            if (_shownStatuses[index] == snapshot.Items[index].Status)
                continue;
            _shownStatuses[index] = snapshot.Items[index].Status;
            _list.SetItem(index, 1, StatusLabel(snapshot.Items[index].Status));
        }
    }

    private void SyncButtons()
    {
        if (_snapshot is not { } snapshot)
            return;
        var selected = SelectedItem(snapshot);
        var activeSelected = selected is
        {
            Status: Downloads.QueueItemStatus.Downloading
            or Downloads.QueueItemStatus.Downloaded
        };
        var canStart = !snapshot.Running && snapshot.Items.Any(item =>
            item.Status is Downloads.QueueItemStatus.InQueue or Downloads.QueueItemStatus.Failed);
        var showStopAll = snapshot.Running && snapshot.ActiveCount > 0;
        var changed = SetVisible(_stop, activeSelected)
            | SetVisible(_startButton, canStart)
            | SetVisible(_stopAll, showStopAll);
        if (changed)
            _dialog.Layout();
    }

    private void StopSelected()
    {
        if (SelectedId() is Guid id)
            _queue.Stop(id);
        Refresh();
    }

    private void RemoveSelected()
    {
        if (SelectedId() is not Guid id)
            return;
        var remove = Wx.MessageBox(
            // Translators: Confirmation shown before an item is removed from the download queue.
            Tr("Remove the selected video from the download queue?"),
            // Translators: Title of the prompt confirming removal from the download queue.
            Tr("Remove download"),
            MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning,
            _dialog) == MessageBoxStyle.Yes;
        if (!remove)
            return;
        _queue.Remove(id);
        Refresh();
    }

    private void AddLinks()
    {
        using var dialog = new DownloadQueueLinksDialog(_dialog, _addLinks);
        if (dialog.Show())
            Refresh();
    }

    private Guid? SelectedId()
    {
        var row = _list.FocusedIndex >= 0 ? _list.FocusedIndex : _list.SelectedIndex;
        return row >= 0 && _list.GetItemData(row) is Guid id ? id : null;
    }

    private Downloads.QueueItemSnapshot? SelectedItem(Downloads.QueueSnapshot snapshot)
    {
        if (SelectedId() is not Guid id)
            return null;
        foreach (var item in snapshot.Items)
            if (item.Id == id)
                return item;
        return null;
    }

    private static string StatusText(Downloads.QueueSnapshot snapshot)
    {
        if (snapshot.Items.All(item => item.Status == Downloads.QueueItemStatus.Completed))
            // Translators: Queue status after every item has finished successfully.
            return Tr("All downloads are complete.");

        if (snapshot.ActiveCount == 0 || snapshot.CurrentIndex < 0)
        {
            var queuedFailure = snapshot.Items.FirstOrDefault(item =>
                item.Status == Downloads.QueueItemStatus.Failed && item.Error.Length > 0);
            // Translators: Queue status when no download is running and unfinished items are waiting.
            var ready = Tr("Ready to download.");
            if (string.IsNullOrEmpty(queuedFailure.Error))
                return ready;
            // Translators: Technical reason supplied by yt-dlp for a failed queue item.
            var error = TrFormat("Error: {details}", queuedFailure.Error);
            return string.Join(Environment.NewLine, ready, error);
        }

        var current = snapshot.Items[snapshot.CurrentIndex];
        var speed = snapshot.Speed > 0
            // Translators: Combined queue speed. {size} is a file size and "/s" means per second.
            ? TrFormat("{size}/s", Size((long)snapshot.Speed))
            // Translators: Shown for queue speed before enough data exists to measure it.
            : Tr("calculating…");
        var estimated = snapshot.EstimatedSeconds > 0
            ? FormatDuration(TimeSpan.FromSeconds(snapshot.EstimatedSeconds))
            // Translators: Shown for queue time remaining before enough data exists to calculate it.
            : Tr("calculating…");

        var lines = new List<string>
        {
            // Translators: Queue progress. {current} and {total} are video counts; {percent} is a whole number.
            TrFormat("Downloading video {current} of {total}, {percent}%",
                snapshot.CurrentIndex + 1, snapshot.Items.Count, (int)Math.Round(current.Percent)),
            // Translators: Aggregate progress through all videos in the download queue.
            TrFormat("Total percentage: {percent}%", (int)Math.Round(snapshot.TotalPercent)),
            // Translators: Combined download speed of all active queue items.
            TrFormat("Speed: {speed}", speed),
            // Translators: Estimated time left for all unfinished queue items.
            TrFormat("Estimated time left: {time}", estimated),
        };
        var failed = snapshot.Items.FirstOrDefault(item =>
            item.Status == Downloads.QueueItemStatus.Failed && item.Error.Length > 0);
        if (!string.IsNullOrEmpty(failed.Error))
            // Translators: Technical reason supplied by yt-dlp for a failed queue item.
            lines.Add(TrFormat("Error: {details}", failed.Error));
        return string.Join(Environment.NewLine, lines);
    }

    private static string StatusLabel(Downloads.QueueItemStatus status) => status switch
    {
        // Translators: Status of a queue item while it is downloading.
        Downloads.QueueItemStatus.Downloading => Tr("Downloading"),
        // Translators: Status after media bytes are downloaded but finishing work may remain.
        Downloads.QueueItemStatus.Downloaded => Tr("Downloaded"),
        // Translators: Status of a queue item that finished successfully.
        Downloads.QueueItemStatus.Completed => Tr("Completed"),
        // Translators: Status of a queue item whose download failed.
        Downloads.QueueItemStatus.Failed => Tr("Failed"),
        // Translators: Status of a queue item waiting to start.
        _ => Tr("In queue"),
    };

    private static bool SetVisible(Window window, bool visible)
    {
        if (window.Visible == visible)
            return false;
        window.Show(visible);
        return true;
    }

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
        => span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes}:{span.Seconds:00}";
}

/// <summary>Asks for one YouTube link per line.</summary>
internal sealed class DownloadQueueLinksDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly TextCtrl _links;
    private readonly Func<IReadOnlyList<string>, string?> _addLinks;
    private bool _added;

    internal DownloadQueueLinksDialog(Window parent, Func<IReadOnlyList<string>, string?> addLinks)
    {
        _addLinks = addLinks;
        // Translators: Title of the window used to add YouTube links to the download queue.
        _dialog = new Dialog(parent, title: Tr("Add links to download queue"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        // Translators: Label above the multi-line box used to add links to the download queue.
        var label = new StaticText(_dialog, label: Tr("Enter one YouTube video link on each line."));
        _links = new TextCtrl(_dialog,
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.DontWrap);
        // Translators: Confirms adding the entered links to the download queue.
        var add = new Button(_dialog, StandardId.Ok, Tr("Add"));
        add.SetDefault();
        add.Click += (_, _) => Add();
        // Translators: Closes the link-entry window without adding anything.
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        _dialog.SetEscapeId(StandardId.Cancel);

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        buttons.Add(add, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(cancel);
        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(label, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        sizer.Add(_links, proportion: 1,
            flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
            border: 8);
        sizer.Add(buttons,
            flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
            border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(600, 320);
        _dialog.Center(onParent: true);
        _links.Focus();
    }

    internal bool Show()
    {
        _added = false;
        _dialog.ShowModal();
        return _added;
    }

    private void Add()
    {
        var links = _links.Value
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(link => link.Length > 0)
            .ToArray();
        if (links.Length == 0)
        {
            Wx.MessageBox(
                // Translators: Shown when Add is pressed without entering any links.
                Tr("Enter at least one YouTube video link."),
                // Translators: Title of a warning shown in the queue link-entry window.
                Tr("Add links to download queue"),
                MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            _links.Focus();
            return;
        }
        var error = _addLinks(links);
        if (!string.IsNullOrEmpty(error))
        {
            Wx.MessageBox(
                error,
                // Translators: Title of a warning shown for an invalid YouTube link.
                Tr("Invalid YouTube link"),
                MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            _links.Focus();
            return;
        }
        _added = true;
        _dialog.EndModal(StandardId.Ok);
    }

    public void Dispose() => _dialog.Dispose();
}
