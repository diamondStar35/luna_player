using LunaPlayer.Media;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    /// <summary>The name to show for a file: its media title when it declares one, otherwise the file
    /// name.</summary>
    internal string DisplayName(string path)
    {
        // mpv loads asynchronously, so the title is not there yet when the file is opened. Re-read it for
        // whatever is playing whenever a name is asked for, and keep it: once a file has been played its
        // title stays available for the playlist, which names entries that are not playing.
        // A stream was named by whoever resolved it, and mpv's guess - often the numeric id in the
        // address - is worse than that name. So a titled entry keeps the title it was given.
        if (string.Equals(path, CurrentPath, StringComparison.Ordinal)
            && _playlist.GetSource(path) is null
            && _playlist.GetTitle(path) is null)
            RememberTitle(path);
        return _playlist.GetTitle(path) ?? MediaLibrary.DisplayName(path);
    }

    /// <summary>The title stored for whatever is playing, distinct from its name on disk.</summary>
    internal string? CurrentTitle
    {
        get
        {
            if (CurrentPath is not string path)
                return null;
            if (_playlist.GetSource(path) is null && _playlist.GetTitle(path) is null)
                RememberTitle(path);
            return _playlist.GetTitle(path);
        }
    }

    /// <summary>The display name of whatever is playing, or null when nothing is.</summary>
    internal string? CurrentDisplayName => CurrentPath is string path ? DisplayName(path) : null;

    /// <summary>The file name for local media, or the stored title for a network entry.</summary>
    internal string? CurrentName => CurrentPath is string path
        ? LinkValidator.IsHttpUrl(path) ? DisplayName(path) : MediaLibrary.DisplayName(path)
        : null;

    // mpv only knows a title once the file is loaded, so it is read here and kept per path: the playlist
    // dialog names every entry, not just the one playing.
    private void RememberTitle(string path)
    {
        // Nothing is written when mpv has no title to give. It has none before the file has loaded and
        // none after Stop unloads it - and Stop asks for a display name on its way out, so writing there
        // would throw away the title the file was playing under a moment earlier.
        if (_engine.MediaTitle is not string title)
            return;
        _playlist.SetTitle(
            path,
            string.Equals(title, MediaLibrary.DisplayName(path), StringComparison.OrdinalIgnoreCase)
                ? null
                : title);
    }

}
