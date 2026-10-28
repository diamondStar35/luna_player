using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;

namespace LunaPlayer.Playlist;

internal sealed partial class PlaylistState
{
    private readonly List<PlaylistItem> _items = [];
    internal IReadOnlyList<SavedEntry>? SavedSnapshot => _sources.SavedSnapshot;
    internal void ReplaceSaved(IEnumerable<SavedEntry>? entries) => _sources.ReplaceSaved(entries);
    internal void ForgetSaved(string path) => _sources.ForgetSaved(path);
    internal void RenameSaved(string oldPath, string newPath) => _sources.RenameSaved(oldPath, newPath);
    internal IReadOnlyList<PlaylistItem> Sources => _sources.Snapshot();
    private readonly SourceCollection _sources = new();
    private readonly ChapterProjection _projection = new();
    private bool _audiobookMode { get => _projection.Enabled; set => _projection.Enabled = value; }
    private readonly Navigation _navigation;
    internal PlaylistState() => _navigation = new Navigation(_items);
    private ShuffleOrder _shuffle => _navigation.Order;
    private readonly MarkedFileSet _marks = new();
    private readonly PlaylistEntries _entries = new();
    private int _currentIndex { get => _navigation.SourceIndex; set => _navigation.SourceIndex = value; }
    private double? _pendingStart { get => _navigation.PendingStart; set => _navigation.PendingStart = value; }

    internal PlaylistItem? CurrentItem => _currentIndex >= 0 && _currentIndex < _items.Count
        ? _items[_currentIndex]
        : null;
    internal string? CurrentPath => CurrentItem?.Path;

    /// <summary>The current entry's position in the order the user is navigating.</summary>
    internal int CurrentIndex => IsShuffleEnabled ? _shuffle.Position : _currentIndex;
    internal int Count => _items.Count;
    /// <summary>The files in their active order: shuffled while shuffle is enabled, source order otherwise.</summary>
    internal IReadOnlyList<PlaylistItem> Items => IsShuffleEnabled
        ? _shuffle.Arrange(_items)
        : _items.AsReadOnly();
    internal string SourceIdentity => string.Join(";", _sources.Select(item => item.Id));
    internal IReadOnlyList<string> SourceFiles => _sources.Select(item => item.Path).DistinctBy(Paths.Key, StringComparer.OrdinalIgnoreCase).ToArray();
    internal bool IsShuffleEnabled => _navigation.Enabled;
    internal bool IsRepeatFileEnabled { get; private set; }

    internal bool ToggleRepeatFile()
    {
        IsRepeatFileEnabled = !IsRepeatFileEnabled;
        return IsRepeatFileEnabled;
    }

    internal bool MoveNext(bool wrap) => _navigation.MoveNext(wrap);
    internal bool MovePrevious(bool wrap) => _navigation.MovePrevious(wrap);
    internal PlaylistItem? PeekNext(bool wrap) => _navigation.PeekNext(wrap);
    internal bool GoToFirst() => _navigation.GoToFirst();
    internal bool GoToLast() => _navigation.GoToLast();
    internal bool GoToIndex(int index) => _navigation.GoToIndex(index);
    internal bool ToggleShuffle() => _navigation.ToggleShuffle();
    internal double? TakePendingStart() => _navigation.TakePendingStart();
    private void SyncShufflePosition() => _navigation.SyncShufflePosition();
    private void CreateShuffleOrder() => _navigation.CreateShuffleOrder();
    private void ClearShuffleOrder() => _navigation.ClearShuffleOrder();

}
