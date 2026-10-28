using LunaPlayer.Configuration;
using LunaPlayer.Media;
namespace LunaPlayer.Playlist;
internal sealed partial class PlaylistState
{

    /// <summary>Remembers the media title for a file, as the player reads it from the media itself. An
    /// empty or whitespace title is treated as no title, so callers fall back to the file name.</summary>
    internal void SetTitle(string path, string? title)
        => _entries.SetTitle(path, title);

    /// <summary>The remembered media title for a file, or null when none is known.</summary>
    internal string? GetTitle(string? path) => _entries.Title(path);

    /// <summary>Records where an entry came from and, for a video, the second stream carrying its sound.
    /// </summary>
    internal void SetSource(string path, string? source, string? audioFile = null, bool audioOnly = false)
        => _entries.SetSource(path, source, audioFile, audioOnly);

    /// <summary>The address an entry came from, or null when its path is the address.</summary>
    internal string? GetSource(string? path) => _entries.Source(path);

    /// <summary>The stream carrying the sound of a video whose path carries only the picture.</summary>
    internal string? GetAudioFile(string? path) => _entries.AudioFile(path);

    /// <summary>Whether an entry should be played as sound only, with any video track left undecoded.</summary>
    internal bool GetAudioOnly(string? path) => _entries.AudioOnly(path);

    internal string? CurrentSource => GetSource(CurrentPath);

}
