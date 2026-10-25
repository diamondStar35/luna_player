using LunaPlayer.Radio;
using WxSharp;

namespace LunaPlayer.UI.Radio;

internal sealed partial class BrowserDialog
{
    private readonly Dictionary<Category, DirectoryItems> _directories = [];
    private readonly Dictionary<Category, (Category Parent, int Position)> _directoryParents = [];
    private readonly Dictionary<Category, uint> _stationCounts = [];
    private CancellationTokenSource? _directoryRequest;
    private Category? _directoryActive;

    private sealed class DirectoryItems
    {
        internal readonly HashSet<Category> Added = [];
        internal readonly List<DirectoryItem> Items = [];
        internal uint Offset;
        internal bool HasMore = true;
        internal bool Loading;
    }

    private static bool IsDirectory(Category category) => category.Kind is CategoryKind.Countries
        or CategoryKind.Languages or CategoryKind.Tags or CategoryKind.Codecs or CategoryKind.Regions;

    private void ExpandDirectory(TreeItemId item)
    {
        if (!_categories.TryGetValue(item, out var category)) return;
        _expanded.Add(category);
        if (category.Kind == CategoryKind.Country) { AddCountryChildren(category); return; }
        if (!_changingTree && IsDirectory(category)) LoadDirectory(category);
    }

    private void LoadDirectory(Category category, bool refresh = false)
    {
        if (_disposed || !_visible || !IsDirectory(category) || !_nodes.ContainsKey(category)) return;
        if (!_directories.TryGetValue(category, out var directory))
            _directories.Add(category, directory = new DirectoryItems());
        if (directory.Loading || !directory.HasMore) return;
        CancelDirectory();
        _directoryActive = category;
        directory.Loading = true;
        _directoryRequest = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var token = _directoryRequest.Token;
        var offset = directory.Offset;
        Run(cancellation => _catalog.DirectoryAsync(category, offset, cancellation, refresh,
            (items, readToken) => PublishDirectoryAsync(category, directory, items, readToken)), page =>
        {
            directory.Offset = page.NextOffset;
            directory.HasMore = page.HasMore;
            directory.Loading = false;
            _directoryActive = null;
            if (directory.Items.Count == 0 && !page.HasMore) _tree.SetItemHasChildren(_nodes[category], false);
            ContinueDirectory();
        }, token, () => { directory.Loading = false; _directoryActive = null; }, background: true);
    }

    private ValueTask PublishDirectoryAsync(Category category, DirectoryItems directory,
        IReadOnlyList<DirectoryItem> items, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var offset = 0;
        _updates.Post(() =>
        {
            if (!_visible) return true;
            offset = AppendDirectory(category, directory, items, offset);
            return offset >= items.Count;
        }, token, background: true);
        return ValueTask.CompletedTask;
    }

    private int AppendDirectory(Category category, DirectoryItems directory, IReadOnlyList<DirectoryItem> items, int offset)
    {
        if (!_nodes.TryGetValue(category, out var parent)) return items.Count;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var stop = Math.Min(items.Count, offset + 16);
        var frozen = false;
        try
        {
            while (offset < stop)
            {
                var item = items[offset++];
                if (item.Name.Length == 0 || item.Category.Value.Length == 0 || directory.Added.Contains(item.Category)) continue;
                if (!_nodes.ContainsKey(item.Category))
                {
                    // Tree insertion and child hints otherwise redraw synchronously for each row.
                    // Suppress painting only within this bounded UI turn, never across an await.
                    if (!frozen) { _tree.Freeze(); frozen = true; }
                    AddCategory(parent, item.Category, CountLabel(item.Name, item.StationCount));
                    _stationCounts[item.Category] = item.StationCount;
                    _directoryParents[item.Category] = (category, directory.Items.Count);
                }
                directory.Added.Add(item.Category);
                directory.Items.Add(item);
                if (System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds >= 4) break;
            }
        }
        finally { if (frozen) _tree.Thaw(); }
        // The initial expand request can finish before any children exist. Expand once after the
        // first child arrives if Windows rejected that empty expansion. Never collapse a live branch.
        if (_expanded.Contains(category) && _tree.GetFirstChild(parent).IsValid && !_tree.IsExpanded(parent))
            _tree.Expand(parent);
        return offset;
    }

    private void ContinueDirectory()
    {
        if (!_visible || _disposed) return;
        var preferred = IsDirectory(_selectedCategory) ? _selectedCategory
            : _directoryParents.TryGetValue(_selectedCategory, out var selected) ? selected.Parent : null;
        bool NeedsPage(Category category)
        {
            if (!IsDirectory(category) || !_expanded.Contains(category)) return false;
            if (!_directories.TryGetValue(category, out var directory)) return true;
            if (!directory.HasMore || directory.Loading) return false;
            if (category.Kind != CategoryKind.Tags || directory.Items.Count == 0) return true;
            return _directoryParents.TryGetValue(_selectedCategory, out var entry)
                && entry.Parent == category && entry.Position >= directory.Items.Count - 32;
        }
        if (preferred is not null && NeedsPage(preferred)) { LoadDirectory(preferred); return; }
        if (_directoryActive is not null) return;
        foreach (var category in _expanded)
            if (NeedsPage(category)) { LoadDirectory(category); return; }
    }

    private void CancelDirectory()
    {
        var request = _directoryRequest;
        _directoryRequest = null;
        if (_directoryActive is { } active && _directories.TryGetValue(active, out var directory)) directory.Loading = false;
        _directoryActive = null;
        if (request is not null) _ = CancelAsync(request);
    }

    private void RefreshDirectory(Category category)
    {
        if (!IsDirectory(category) || !_nodes.TryGetValue(category, out var parent)) return;
        CancelDirectory();
        _changingTree = true;
        try
        {
            foreach (var child in _tree.GetChildren(parent)) RemoveTreeItem(child);
        }
        finally { _changingTree = false; }
        _directories.Remove(category);
        _tree.SetItemHasChildren(parent);
        LoadDirectory(category, refresh: true);
    }

    private void RemoveTreeItem(TreeItemId item)
    {
        foreach (var child in _tree.GetChildren(item)) RemoveTreeItem(child);
        if (_categories.Remove(item, out var category))
        {
            _nodes.Remove(category);
            _directories.Remove(category);
            _directoryParents.Remove(category);
            _stationCounts.Remove(category);
            _expanded.Remove(category);
        }
        _tree.Remove(item);
    }
}
