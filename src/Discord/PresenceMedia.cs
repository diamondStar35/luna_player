namespace LunaPlayer.Discord;

internal enum SourceKind { Local, YouTube, Radio, Spotify, Book, Stream }

// Immutable snapshots cross the UI/worker boundary; IPC never reads settings or the player.
internal sealed record PresenceMedia(string Identity, string Title, string Artist, string? PublicUrl,
    bool Playing, bool Video, double? Position, double? Duration, double Speed)
{
    internal SourceKind Kind { get; init; }
    internal string? ArtworkUrl { get; init; }
    internal string? Chapter { get; init; }
}
