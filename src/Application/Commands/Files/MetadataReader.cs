using LunaPlayer.Application.Presentation;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.YouTube;

namespace LunaPlayer.Application.Commands.Files;

internal sealed class MetadataReader(Player player)
{
    private readonly MediaTagsCache _tags = new();

    internal Metadata Read(YouTubeResult? video = null)
    {
        var metadata = player.CurrentMetadata;
        if (!player.IsLoaded || player.CurrentPath is not string path || player.InRadioSession)
            return metadata;
        // Local announcements use the same file headers as the media overlay. Empty tags
        // are authoritative, so an outgoing native snapshot cannot supply a missing field.
        if (!Uri.TryCreate(path, UriKind.Absolute, out var address) || address.IsFile)
            return metadata with { Display = _tags.Original(player) };
        if (video is not { } item || item.Url != player.CurrentSource || !LinkValidator.IsYouTubeUrl(item.Url))
            return metadata;
        var artist = string.IsNullOrWhiteSpace(metadata.Display.Artist) ? item.Author : metadata.Display.Artist;
        return metadata with
        {
            Display = metadata.Display with { Title = player.CurrentTitle ?? item.Title, Artist = artist },
        };
    }
}
