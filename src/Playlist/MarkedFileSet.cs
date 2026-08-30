namespace LunaPlayer.Playlist;

internal sealed class MarkedFileSet
{
    private readonly HashSet<string> _keys = new(StringComparer.OrdinalIgnoreCase);

    internal bool? Toggle(string? path)
    {
        if (path is null) return null;
        var key = Key(path);
        if (_keys.Remove(key)) return false;
        _keys.Add(key);
        return true;
    }

    internal bool ToggleAll(IReadOnlyList<string> files)
    {
        var mark = !AreAll(files);
        _keys.Clear();
        if (mark) foreach (var path in files) _keys.Add(Key(path));
        return mark;
    }

    internal bool Clear()
    {
        var changed = _keys.Count > 0;
        _keys.Clear();
        return changed;
    }

    internal void Replace(string oldPath, string newPath)
    { if (_keys.Remove(Key(oldPath))) _keys.Add(Key(newPath)); }
    internal void Remove(IEnumerable<string> paths) { foreach (var path in paths) _keys.Remove(Key(path)); }
    internal bool Contains(string? path) => path is not null && _keys.Contains(Key(path));
    internal int Count(IReadOnlyList<string> files) => files.Count(path => Contains(path));
    internal bool AreAll(IReadOnlyList<string> files) => files.Count > 0 && Count(files) == files.Count;
    internal IReadOnlyList<string> Files(IReadOnlyList<string> files) => files.Where(Contains).ToArray();

    private static string Key(string path)
    {
        try { return Path.GetFullPath(path); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException) { return path; }
    }
}
