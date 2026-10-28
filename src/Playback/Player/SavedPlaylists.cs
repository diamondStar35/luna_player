using LunaPlayer.Playlist;
using LunaPlayer.Media;
using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    // Keep folder references alongside the resolved queue; saving that queue must not turn a folder
    // into a fixed list of the files it happened to contain when it was opened.
    internal IReadOnlyList<SavedEntry> SnapshotSaved()
        => _playlist.SavedEntries ?? _playlist.Sources.Select(item => new SavedEntry(item.SourceId,
            LinkValidator.IsHttpUrl(item.Path) ? EntryKind.Stream : EntryKind.File,
            _playlist.GetSource(item.Path) ?? item.Path, _playlist.GetTitle(item.Path))).ToList();

    internal void OpenSaved(IReadOnlyList<SavedEntry> entries, IReadOnlyList<M3uEntry> files)
    {
        if (files.Count == 0) return;
        OpenPlaylist(files);
        _playlist.SavedEntries = [.. entries];
    }

    internal void AppendSaved(IReadOnlyList<SavedEntry> entries, IReadOnlyList<M3uEntry> files)
    {
        var logical = SnapshotSaved().ToList();
        logical.AddRange(entries);
        var empty = _playlist.Count == 0;
        foreach (var file in files)
        {
            _playlist.Append(file.Location, jump: empty && _playlist.Count == 0);
            _playlist.SetTitle(file.Location, file.Title);
        }
        _playlist.SavedEntries = logical;
        if (empty && files.Count > 0) LoadCurrent();
        else { PrepareNext(); StateChanged?.Invoke(); }
    }
    private void ForgetSavedPath(string path)
        => _playlist.SavedEntries?.RemoveAll(entry => entry.Kind != EntryKind.Folder && Paths.AreSame(entry.Location, path));
    private void RenameSavedPath(string oldPath, string newPath)
    {
        if (_playlist.SavedEntries is not { } entries) return;
        for (var i = 0; i < entries.Count; i++)
            if (Paths.AreSame(entries[i].Location, oldPath)) entries[i] = entries[i] with { Location = newPath };
    }
}
