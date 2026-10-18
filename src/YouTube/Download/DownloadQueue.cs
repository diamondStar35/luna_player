using LunaPlayer.Configuration.Models;

namespace LunaPlayer.YouTube.Download;

/// <summary>The states shown for an item in the download queue.</summary>
internal enum QueueItemStatus
{
    InQueue,
    Downloading,
    Downloaded,
    Completed,
    Failed,
}

/// <summary>An immutable view of one queue item, safe to hand to the UI.</summary>
internal readonly record struct QueueItemSnapshot(
    Guid Id,
    string Title,
    QueueItemStatus Status,
    double Percent,
    string Error,
    long DownloadedBytes,
    long TotalBytes,
    double Speed,
    double Eta,
    string PostProcessor,
    string PostProcessStatus,
    string FilePath);

/// <summary>An immutable view of the complete queue and its aggregate progress.</summary>
internal sealed record QueueSnapshot(
    IReadOnlyList<QueueItemSnapshot> Items,
    bool Running,
    int ActiveCount,
    int CurrentIndex,
    double TotalPercent,
    double Speed,
    double EstimatedSeconds);

/// <summary>
/// Owns queued downloads independently of the queue window. Closing the window therefore never stops work,
/// and every item retains the exact request (including custom dialog choices) it was added with.
/// </summary>
internal sealed class DownloadQueue : IDisposable
{
    private sealed class Item
    {
        internal required Guid Id { get; init; }
        internal required string Title { get; init; }
        internal required Request Request { get; init; }
        internal bool Visible { get; set; } = true;
        internal QueueItemStatus Status { get; set; } = QueueItemStatus.InQueue;
        internal long DownloadedBytes { get; set; }
        internal long TotalBytes { get; set; }
        internal double Percent { get; set; }
        internal double Speed { get; set; }
        internal double Eta { get; set; }
        internal string PostProcessor { get; set; } = string.Empty;
        internal string PostProcessStatus { get; set; } = string.Empty;
        internal string FilePath { get; set; } = string.Empty;
        internal string Error { get; set; } = string.Empty;
        internal DateTime StartedAt { get; set; }
        internal TimeSpan Duration { get; set; }
        internal CancellationTokenSource? Cancellation { get; set; }
        internal bool StopRequested { get; set; }
        /// <summary>True after an explicit Stop, so the running batch does not immediately launch it again.</summary>
        internal bool Held { get; set; }
        internal bool Removed { get; set; }
    }

    private readonly Lock _gate = new();
    private readonly Settings _settings;
    private readonly List<Item> _items = [];
    private bool _running;
    private bool _disposed;
    private bool _completionNotified;
    /// <summary>Includes removed jobs whose process is still winding down, so their slots are not reused early.</summary>
    private int _activeWorkers;

    internal DownloadQueue(Settings settings) => _settings = settings;

    /// <summary>Raised once when every visible queue item has completed successfully.</summary>
    internal event Action? AllCompleted;

    /// <summary>Whether closing the application would discard or cancel any unfinished work.</summary>
    internal bool HasUnfinishedItems
    {
        get
        {
            lock (_gate)
                return _items.Any(item => !item.Removed
                    && ((item.Visible && item.Status != QueueItemStatus.Completed) || IsActive(item)));
        }
    }

    /// <summary>Adds an item without starting it. The title may be the URL for manually entered links.</summary>
    internal Guid Add(string title, Request request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(title) ? request.Url : title.Trim(),
            Request = request,
        };
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _items.Add(item);
            _completionNotified = false;
            if (_running)
                ScheduleLocked();
        }
        return item.Id;
    }

    /// <summary>Starts an ordinary one-video download hidden from the queue until its progress window is
    /// minimized there. The queue owns the worker so closing that window never interrupts the process.</summary>
    internal Guid StartSingle(string title, Request request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(title) ? request.Url : title.Trim(),
            Request = request,
            Visible = false,
        };
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _items.Add(item);
            LaunchLocked(item);
        }
        return item.Id;
    }

    /// <summary>Makes a single download visible in the queue without changing or restarting it.</summary>
    internal void Reveal(Guid id)
    {
        var notify = false;
        lock (_gate)
        {
            var item = _items.FirstOrDefault(item => item.Id == id);
            if (item is null || item.Visible)
                return;
            item.Visible = true;
            _completionNotified = false;
            if (IsActive(item))
                _running = true;
            notify = MarkAllCompletedLocked();
        }
        if (notify)
            AllCompleted?.Invoke();
    }

    /// <summary>Starts every unfinished item, retrying failed items and respecting the concurrency setting.</summary>
    internal void Start()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            foreach (var item in _items.Where(item => item.Visible && item.Status == QueueItemStatus.Failed))
            {
                item.Status = QueueItemStatus.InQueue;
                item.Error = string.Empty;
                item.Percent = 0;
            }
            foreach (var item in _items.Where(item => item.Visible))
                item.Held = false;
            _completionNotified = false;
            _running = _items.Any(item => item.Visible && item.Status == QueueItemStatus.InQueue)
                || _items.Any(item => item.Visible && IsActive(item));
            ScheduleLocked();
        }
    }

    /// <summary>Stops one active item and returns it to the queue so Start can try it again.</summary>
    internal void Stop(Guid id)
    {
        lock (_gate)
        {
            var item = _items.FirstOrDefault(item => item.Id == id);
            if (item is null || !IsActive(item))
                return;
            item.StopRequested = true;
            item.Held = true;
            item.Cancellation?.Cancel();
        }
    }

    /// <summary>Stops all active items and leaves every unfinished item available for a later Start.</summary>
    internal void StopAll()
    {
        lock (_gate)
        {
            _running = false;
            foreach (var item in _items.Where(item => item.Visible && IsActive(item)))
            {
                item.StopRequested = true;
                item.Held = true;
                item.Cancellation?.Cancel();
            }
        }
    }

    /// <summary>Removes an item. An active process is cancelled and its late completion is ignored.</summary>
    internal void Remove(Guid id)
    {
        lock (_gate)
        {
            var item = _items.FirstOrDefault(item => item.Id == id);
            if (item is null)
                return;
            item.Removed = true;
            _items.Remove(item);
            item.Cancellation?.Cancel();
            ScheduleLocked();
        }
    }

    /// <summary>Clears successful visible jobs after the user has had a chance to review them.</summary>
    internal void RemoveCompleted()
    {
        lock (_gate)
        {
            _items.RemoveAll(item => item.Visible && item.Status == QueueItemStatus.Completed);
            if (!_items.Any(item => item.Visible))
                _completionNotified = false;
        }
    }

    internal QueueSnapshot Snapshot()
    {
        lock (_gate)
        {
            var visible = _items.Where(item => item.Visible).ToArray();
            var items = visible.Select(ToSnapshot).ToArray();
            var active = visible.Where(IsActive).ToArray();
            var totalPercent = visible.Length == 0 ? 0 : visible.Sum(EffectivePercent) / visible.Length;
            var current = active.Length == 0 ? -1 : Array.IndexOf(visible, active[0]);
            return new QueueSnapshot(
                items,
                _running,
                active.Length,
                current,
                totalPercent,
                active.Sum(item => item.Speed),
                EstimateSecondsLocked(active));
        }
    }

    /// <summary>Returns one item, including hidden single downloads, for its modeless progress window.</summary>
    internal QueueItemSnapshot? Snapshot(Guid id)
    {
        lock (_gate)
        {
            var item = _items.FirstOrDefault(item => item.Id == id);
            return item is null ? null : ToSnapshot(item);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _running = false;
            foreach (var item in _items.Where(IsActive))
                item.Cancellation?.Cancel();
        }
    }

    private void ScheduleLocked()
    {
        if (!_running || _disposed)
            return;
        var limit = Math.Clamp(_settings.YouTube.Download.SimultaneousDownloads, 1, 16);
        var available = Math.Max(0, limit - _activeWorkers);
        foreach (var item in _items.Where(IsRunnable).Take(available))
            LaunchLocked(item);
        if (_activeWorkers == 0 && !_items.Any(IsRunnable))
            _running = false;
    }

    private void LaunchLocked(Item item)
    {
        item.Status = QueueItemStatus.Downloading;
        item.Percent = 0;
        item.Speed = 0;
        item.Eta = 0;
        item.Error = string.Empty;
        item.DownloadedBytes = 0;
        item.TotalBytes = 0;
        item.PostProcessor = string.Empty;
        item.PostProcessStatus = string.Empty;
        item.FilePath = string.Empty;
        item.StopRequested = false;
        item.StartedAt = DateTime.UtcNow;
        item.Cancellation = new CancellationTokenSource();
        _activeWorkers++;
        var token = item.Cancellation.Token;
        _ = Task.Run(() => Run(item, token), CancellationToken.None);
    }

    private void Run(Item item, CancellationToken token)
    {
        Outcome? outcome = null;
        var cancelled = false;
        var notify = false;
        try
        {
            outcome = new Engine(_settings).Run(item.Request, progress => Report(item, progress), token);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception exception)
        {
            outcome = Outcome.Failed(exception.Message);
        }

        lock (_gate)
        {
            var completedVisibleItem = false;
            _activeWorkers--;
            item.Duration = DateTime.UtcNow - item.StartedAt;
            item.Cancellation?.Dispose();
            item.Cancellation = null;
            item.Speed = 0;
            item.Eta = 0;
            if (!item.Removed)
            {
                if (outcome is { Success: true })
                {
                    item.Status = QueueItemStatus.Completed;
                    item.Percent = 100;
                    item.FilePath = outcome.Value.FilePath;
                    item.Held = false;
                    completedVisibleItem = item.Visible;
                }
                else if (cancelled || item.StopRequested)
                {
                    item.Status = QueueItemStatus.InQueue;
                    item.Percent = 0;
                }
                else
                {
                    item.Status = QueueItemStatus.Failed;
                    item.Error = outcome?.Error ?? string.Empty;
                    item.Held = false;
                }
                item.StopRequested = false;
            }
            ScheduleLocked();
            notify = completedVisibleItem && MarkAllCompletedLocked();
        }
        if (notify)
            AllCompleted?.Invoke();
    }

    private void Report(Item item, Progress progress)
    {
        lock (_gate)
        {
            if (item.Removed || item.Cancellation is null)
                return;
            if (progress.IsPostProcessing)
            {
                item.Status = QueueItemStatus.Downloaded;
                item.Percent = 100;
                item.Speed = 0;
                item.Eta = 0;
                item.PostProcessor = progress.PostProcessor;
                item.PostProcessStatus = progress.Status;
                return;
            }
            item.DownloadedBytes = progress.DownloadedBytes;
            item.TotalBytes = progress.TotalBytes;
            item.Percent = progress.Status.Equals("finished", StringComparison.OrdinalIgnoreCase)
                ? 100 : progress.Percent;
            item.Speed = progress.Speed;
            item.Eta = progress.Eta;
            item.PostProcessor = string.Empty;
            item.PostProcessStatus = string.Empty;
            item.Status = progress.Status.Equals("finished", StringComparison.OrdinalIgnoreCase)
                ? QueueItemStatus.Downloaded : QueueItemStatus.Downloading;
        }
    }

    private double EstimateSecondsLocked(IReadOnlyCollection<Item> active)
    {
        if (active.Count == 0)
            return 0;
        var knownDurations = _items
            .Where(item => item.Visible && item.Status == QueueItemStatus.Completed && item.Duration > TimeSpan.Zero)
            .Select(item => item.Duration.TotalSeconds)
            .ToList();
        foreach (var item in active)
        {
            if (item.Percent > 0)
            {
                var elapsed = Math.Max(0, (DateTime.UtcNow - item.StartedAt).TotalSeconds);
                knownDurations.Add(elapsed * 100 / item.Percent);
            }
        }
        var average = knownDurations.Count > 0 ? knownDurations.Average() : 0;
        var activeRemaining = active.Select(item => item.Eta > 0
            ? item.Eta
            : average > 0 ? average * (100 - item.Percent) / 100 : 0).ToArray();
        var remainingWork = activeRemaining.Sum();
        remainingWork += _items.Count(item => item.Visible && item.Status == QueueItemStatus.InQueue) * average;
        var limit = Math.Clamp(_settings.YouTube.Download.SimultaneousDownloads, 1, 16);
        return remainingWork > 0
            ? Math.Max(activeRemaining.Max(), remainingWork / limit)
            : 0;
    }

    private static bool IsActive(Item item)
        => item.Status is QueueItemStatus.Downloading or QueueItemStatus.Downloaded;

    private static bool IsRunnable(Item item)
        => item.Visible && item.Status == QueueItemStatus.InQueue && !item.Held;

    private static double EffectivePercent(Item item)
        => item.Status is QueueItemStatus.Completed or QueueItemStatus.Downloaded ? 100 : item.Percent;

    private static QueueItemSnapshot ToSnapshot(Item item)
        => new(
            item.Id, item.Title, item.Status, item.Percent, item.Error,
            item.DownloadedBytes, item.TotalBytes, item.Speed, item.Eta,
            item.PostProcessor, item.PostProcessStatus, item.FilePath);

    private bool MarkAllCompletedLocked()
    {
        if (_disposed || _completionNotified)
            return false;
        var visible = _items.Where(item => item.Visible).ToArray();
        if (visible.Length == 0 || visible.Any(item => item.Status != QueueItemStatus.Completed))
            return false;
        _completionNotified = true;
        return true;
    }
}
