using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.Media;
using System.Diagnostics;

namespace LunaPlayer.Application.Commands.Files;

internal sealed partial class Files
{
    private void OnRecentRequested(RecentCommand command)
    {
        if (command.Action == RecentAction.Clear)
        {
            _recents.Clear(command.Kind);
            RefreshRecents();
            _speech.Speak(
                // Translators: Spoken after the user empties one of the Recents lists.
                Tr("Recent list cleared."),
                // Translators: The short wording spoken after a Recents list is emptied.
                Tr("Cleared."));
            return;
        }
        // Opening re-records the item (moving it to the top), exactly as a normal open would.
        if (OpenLocalPath(command.Path))
            return;
        // The path has moved or been deleted since it was recorded; drop it and say so.
        _recents.Remove(command.Kind, command.Path);
        RefreshRecents();
        _speech.Speak(
            // Translators: Spoken when a chosen recent item can no longer be opened because it is gone.
            Tr("That item is no longer available."),
            // Translators: The short wording spoken when a recent item can no longer be opened.
            Tr("Not available."));
    }

    private void RefreshRecents()
        => _view.RebuildRecentsMenu(
            BuildRecentEntries(RecentKind.File),
            BuildRecentEntries(RecentKind.Folder),
            BuildRecentEntries(RecentKind.Playlist));

    private IReadOnlyList<RecentMenuEntry> BuildRecentEntries(RecentKind kind)
    {
        var paths = _recents.Get(kind);
        var entries = new List<RecentMenuEntry>(paths.Count);
        for (var i = 0; i < paths.Count; i++)
            entries.Add(new RecentMenuEntry(paths[i], RecentLabel(i + 1, paths[i])));
        return entries;
    }

    // "1. song.mp3 — C:\Music\Album". A & in a name is doubled so wxWidgets does not read it as a mnemonic.
    private static string RecentLabel(int number, string path)
    {
        var trimmed = path.TrimEnd('\\', '/');
        var name = (Path.GetFileName(trimmed) is { Length: > 0 } fileName ? fileName : path).Replace("&", "&&");
        var parent = (Path.GetDirectoryName(trimmed) ?? string.Empty).Replace("&", "&&");
        return parent.Length > 0
            // Translators: A recent-item menu label. {number} is its place in the list, {name} the file or folder, {parent} the folder holding it.
            ? TrFormat("{number}. {name} — {parent}", number, name, parent)
            // Translators: A recent-item menu label with no parent folder. {number} is its place in the list, {name} the item.
            : TrFormat("{number}. {name}", number, name);
    }
}
