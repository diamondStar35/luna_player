using System.Text;
using System.Text.Json;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Discord;

/// <summary>Builds NativeAOT-safe RPC JSON from playback snapshots.</summary>
internal sealed class ActivityBuilder
{
    private readonly Timeline _timeline;

    internal ActivityBuilder(DateTimeOffset started) => _timeline = new(started);

    internal string? Build(DiscordSettings settings, PresenceMedia? media, PresenceText text, DateTimeOffset now, string? applicationIcon = null)
        => BuildSnapshot(PresenceOptions.Capture(settings), media, text, now, applicationIcon);

    internal string? BuildSnapshot(PresenceOptions settings, PresenceMedia? media, PresenceText text, DateTimeOffset now, string? applicationIcon = null)
    {
        if (!settings.Enabled || media is null && !settings.ShowIdle) return null;
        var (start, end) = _timeline.Sample(media, now);
        var link = settings.ShowDetails && media is not null ? PublicLinks.Catalog(media.PublicUrl) : null;
        var radioLink = settings.ShowDetails && media?.Kind == SourceKind.Radio ? PublicLinks.Stream(media.PublicUrl) : null;
        var heading = media is { Playing: false } ? text.PausedApplication(AppInfo.Name) : AppInfo.Name;
        var title = Text(media is null ? text.Idle : !settings.ShowDetails ? text.Generic : media.Title);
        if (title.Length == 0) title = Text(text.Untitled);
        var artworkLabel = media is null || !settings.ShowDetails ? null
            : media.Kind == SourceKind.Book ? Text(media.Chapter) : Text(media.Artist);
        var mediaLayout = media is not null;
        var watching = media is { Video: true };
        var includeArtistCredit = watching && settings.ShowDetails && media!.Kind != SourceKind.YouTube;
        var details = mediaLayout ? Text(heading) : title;
        var state = includeArtistCredit && !string.IsNullOrEmpty(artworkLabel)
            ? WatchingState(title, text.By(artworkLabel)) : mediaLayout ? title : null;
        var name = !mediaLayout ? heading : !settings.ShowDetails ? "Luna" : media!.Kind switch
        {
            SourceKind.YouTube => text.YouTube,
            SourceKind.Radio => text.Radio,
            SourceKind.Spotify => text.Spotify,
            SourceKind.Book => text.Audiobook,
            SourceKind.Local => media.Video ? text.Video : text.Music,
            _ => "Luna",
        };
        string? artwork = null;
        if (settings.ShowDetails && media is not null)
        {
            if (link is { VideoId: not null }) artwork = $"https://i.ytimg.com/vi/{link.VideoId}/hqdefault.jpg";
            else if (media.Kind is SourceKind.Radio or SourceKind.Spotify) artwork = PublicLinks.Stream(media.ArtworkUrl);
        }
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            // YouTube keeps the artist in the artwork label, leaving the full state field for its title.
            // Listening retains its app heading, title row and artist/chapter caption.
            json.WriteNumber("type", !mediaLayout ? 0 : watching ? 3 : 2);
            json.WriteNumber("status_display_type", 0);
            json.WriteString("name", Text(name));
            json.WriteString("details", Text(details));
            // State is already sanitized; reprocessing it would strip the intentional line break.
            if (!string.IsNullOrEmpty(state)) json.WriteString("state", state);
            if (start is long s)
            {
                json.WriteStartObject("timestamps");
                json.WriteNumber("start", s);
                if (end is long e) json.WriteNumber("end", e);
                json.WriteEndObject();
            }
            json.WriteStartObject("assets");
            json.WriteString("large_image", artwork ?? applicationIcon ?? "luna");
            // Discord also exposes this as the artwork's accessible label: keep it to the
            // artist/chapter itself, without prefixes or application branding.
            var imageText = mediaLayout ? artworkLabel : null;
            if (!string.IsNullOrEmpty(imageText)) json.WriteString("large_text", Text(imageText));
            json.WriteEndObject();
            if (settings.ShowButtons)
            {
                json.WriteStartArray("buttons");
                if (link is not null) Button(json, link.VideoId is not null
                    ? media!.Video ? text.WatchYouTube : text.ListenYouTube : text.OpenSpotify, link.Url);
                else if (radioLink is not null) Button(json, text.Watch, radioLink);
                Button(json, text.Download, AppInfo.RepositoryUrl + "/releases");
                json.WriteEndArray();
            }
            json.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static string Text(string? value, int maxBytes = 128)
    {
        var result = new StringBuilder();
        var bytes = 0;
        var characters = 0;
        foreach (var rune in (value ?? string.Empty).Trim().EnumerateRunes())
        {
            if (Rune.IsControl(rune)) continue;
            if (bytes + rune.Utf8SequenceLength > maxBytes) break;
            result.Append(rune.ToString());
            bytes += rune.Utf8SequenceLength;
            characters++;
        }
        // Discord requires at least two characters for text and button labels.
        if (characters == 1 && bytes < maxBytes) result.Append(' ');
        return result.ToString();
    }

    private static string WatchingState(string title, string artistCredit)
    {
        // Both lines share Discord's 128-byte field limit. Bound the credit so a long artist
        // name cannot displace the title, and reserve room for it when the title is long.
        var credit = Text(artistCredit, 64);
        return Text(title, 127 - Encoding.UTF8.GetByteCount(credit)) + "\n" + credit;
    }

    private static void Button(Utf8JsonWriter json, string label, string url)
    {
        if (Encoding.UTF8.GetByteCount(url) > 512) return;
        json.WriteStartObject();
        json.WriteString("label", Text(label, 32));
        json.WriteString("url", url);
        json.WriteEndObject();
    }
}
