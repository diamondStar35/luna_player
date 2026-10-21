namespace LunaPlayer.Playback;

/// <param name="Id">mpv's track ID, written to <c>sid</c> when selecting this track.</param>
/// <param name="Title">The title stored in the media, or null when it has none.</param>
/// <param name="Language">The raw language code, such as "eng", or null when it has none.</param>
/// <param name="Selected">Whether this subtitle track is currently selected.</param>
internal readonly record struct SubtitleTrack(int Id, string? Title, string? Language, bool Selected);
