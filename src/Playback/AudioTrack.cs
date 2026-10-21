namespace LunaPlayer.Playback;

/// <param name="Id">mpv's track ID, written to <c>aid</c> when selecting this track.</param>
/// <param name="Title">The title stored in the media, or null when it has none.</param>
/// <param name="Language">The raw language code, such as "eng", or null when it has none.</param>
/// <param name="Channels">The channel count, or 0 when mpv has not reported it.</param>
/// <param name="Selected">Whether this track is currently playing.</param>
internal readonly record struct AudioTrack(int Id, string? Title, string? Language, int Channels, bool Selected);
