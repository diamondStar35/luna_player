using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

/// <summary>One complete, millisecond-precise part of a media file.</summary>
internal readonly record struct SelectionRange(double Start, double End);

/// <summary>Keeps the selection slots for the current playlist entry, including across a reload of it.</summary>
///
/// <remarks>The ranges are session state, not playback-engine state. Only <see cref="PlaybackActive"/>
/// describes a range currently imposed on mpv; reloading the same entry removes that constraint without
/// throwing its slots away. The application clears them when navigation changes playlist entries.</remarks>
internal sealed class SelectionSlots
{
    private readonly Dictionary<string, FileSelections> _files = new(StringComparer.Ordinal);
    private string _pathKey = string.Empty;
    private FileSelections? _current;

    internal bool Repeat { get; private set; }
    internal bool PlaybackActive { get; private set; }
    internal bool OneShotActive => PlaybackActive && !Repeat;
    internal double? PendingStart => _current?.PendingStart;
    internal int Count => _current?.Ranges.Count ?? 0;
    internal int CurrentIndex => _current?.CurrentIndex ?? 0;
    internal bool HasAny => Count > 0 || PendingStart.HasValue;
    internal SelectionRange? Sole => Count == 1 ? _current!.Ranges[0] : null;

    /// <summary>A stable copy of the completed slots, safe to hand to a background export while the user
    /// continues using the player and may change the live slots.</summary>
    internal IReadOnlyList<SelectionRange> Snapshot()
        => _current?.Ranges.ToArray() ?? [];

    internal SelectionRange? Current
    {
        get
        {
            if (_current is null || _current.CurrentIndex < 0 || _current.CurrentIndex >= _current.Ranges.Count)
                return null;
            return _current.Ranges[_current.CurrentIndex];
        }
    }

    internal double? Start => PlaybackActive ? Current?.Start : null;
    internal double? End => PlaybackActive ? Current?.End : null;

    internal double Bound(double position)
        => Start is double start && End is double end
            ? Math.Clamp(position, start, end)
            : position;

    internal void SwitchTo(string? path)
    {
        _pathKey = Paths.Key(path);
        _current = _pathKey.Length > 0 && _files.TryGetValue(_pathKey, out var selections)
            ? selections
            : null;
        DeactivatePlayback();
    }

    /// <summary>Starts a new slot. A second start replaces the unfinished one; a completed range is never
    /// overwritten merely because it is the selected slot.</summary>
    internal void SetStart(string path, double seconds)
    {
        EnsureFile(path);
        _current!.PendingStart = Precision.Normalize(seconds);
        _current.CurrentIndex = _current.Ranges.Count;
        DeactivatePlayback();
    }

    internal bool TrySetEnd(double seconds, out SelectionRange range)
    {
        range = default;
        if (_current?.PendingStart is not double start)
            return false;
        var end = Precision.Normalize(seconds);
        if (end <= start)
            return false;
        range = new SelectionRange(start, end);
        _current.Ranges.Add(range);
        _current.PendingStart = null;
        // A completed range is stored behind the current slot. Remaining in this free slot lets normal
        // playback continue and makes the next pair of markers create a new range instead of replacing it.
        _current.CurrentIndex = _current.Ranges.Count;
        DeactivatePlayback();
        return true;
    }

    internal bool TryMove(int direction, out bool freeSlot)
    {
        freeSlot = false;
        if (_current is null || _current.Ranges.Count == 0)
            return false;
        var count = _current.Ranges.Count;
        if (direction < 0)
        {
            if (_current.CurrentIndex <= 0)
                return count == 1;
            _current.CurrentIndex--;
        }
        else
        {
            if (_current.CurrentIndex >= count)
                return false;
            _current.CurrentIndex++;
        }
        freeSlot = _current.CurrentIndex == count;
        PlaybackActive = !freeSlot;
        return true;
    }

    internal bool RemoveCurrent()
    {
        if (_current is null)
            return false;
        if (_current.CurrentIndex >= 0 && _current.CurrentIndex < _current.Ranges.Count)
        {
            _current.Ranges.RemoveAt(_current.CurrentIndex);
            _current.CurrentIndex = _current.Ranges.Count == 0
                ? 0
                : Math.Min(_current.CurrentIndex, _current.Ranges.Count - 1);
            DeactivatePlayback();
            RemoveEmptyFile();
            return true;
        }
        if (_current.PendingStart.HasValue)
        {
            _current.PendingStart = null;
            _current.CurrentIndex = Math.Max(0, _current.Ranges.Count - 1);
            DeactivatePlayback();
            RemoveEmptyFile();
            return true;
        }
        return false;
    }

    internal bool ClearAll()
    {
        if (!HasAny)
            return false;
        _files.Remove(_pathKey);
        _current = null;
        DeactivatePlayback();
        return true;
    }

    internal void SetRepeat(bool repeat)
        => Repeat = repeat;

    internal void ActivatePlayback()
        => PlaybackActive = Current.HasValue;

    internal void DeactivatePlayback()
        => PlaybackActive = false;

    internal bool CanMovePrevious
        => Count == 1 || (_current is not null && _current.CurrentIndex > 0);

    internal bool CanMoveNext
        => _current is not null && _current.CurrentIndex < Count;

    private void EnsureFile(string path)
    {
        var key = Paths.Key(path);
        if (key.Length == 0)
            throw new ArgumentException("A selection must belong to a file.", nameof(path));
        _pathKey = key;
        if (!_files.TryGetValue(key, out _current))
        {
            _current = new FileSelections();
            _files[key] = _current;
        }
    }

    private void RemoveEmptyFile()
    {
        if (_current is not null && _current.Ranges.Count == 0 && !_current.PendingStart.HasValue)
        {
            _files.Remove(_pathKey);
            _current = null;
        }
    }

    private sealed class FileSelections
    {
        internal List<SelectionRange> Ranges { get; } = [];
        internal int CurrentIndex { get; set; }
        internal double? PendingStart { get; set; }
    }
}
