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
            if (_playlist.CurrentItem?.Chapter is Chapter chapter)
                return ChapterTitle(chapter);
            if (CurrentPath is not string path)
                return null;
            if (_playlist.GetSource(path) is null && _playlist.GetTitle(path) is null)
                RememberTitle(path);
            return _playlist.GetTitle(path);
        }
    }

    /// <summary>The display name of whatever is playing, or null when nothing is.</summary>
    internal string DisplayName(Playlist.PlaylistItem item)
        => item.Chapter is Chapter chapter ? ChapterTitle(chapter) : DisplayName(item.Path);

    internal string? CurrentDisplayName => _playlist.CurrentItem is { } item ? DisplayName(item) : null;

    internal string? CurrentArtist => CurrentMetadata.Display.Artist;

    /// <summary>The live programme metadata, distinct from the station's directory name.</summary>
    internal MediaTags CurrentBroadcast => InRadioSession ? CurrentMetadata.Display : MediaTags.None;

    internal Metadata CurrentMetadata
    {
        get
        {
            if (!_loaded) return Metadata.None;
            var metadata = _engine.GetMetadata();
            if (!InRadioSession) return metadata;
            var tags = metadata.Display;
            if (tags.Artist.Length > 0) return metadata;
            // ICY commonly provides one "Artist - Title" field instead of separate tags. Only split
            // that explicit separator; an unsplit programme title does not identify an artist.
            var separator = tags.Title.IndexOf(" - ", StringComparison.Ordinal);
            if (separator <= 0 || separator + 3 >= tags.Title.Length) return metadata;
            var artist = tags.Title[..separator].Trim();
            var title = tags.Title[(separator + 3)..].Trim();
            return artist.Length > 0 && title.Length > 0
                ? metadata with { Display = tags with { Artist = artist, Title = title } }
                : metadata;
        }
    }

    /// <summary>The file name for local media, or the stored title for a network entry.</summary>
    internal string? CurrentName => _playlist.CurrentItem?.Chapter is Chapter chapter ? ChapterTitle(chapter)
        : CurrentPath is string path
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
