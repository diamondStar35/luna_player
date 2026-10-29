using System.Text;
using System.Text.Json;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Discord;

// Immutable snapshots cross the UI/worker boundary; neither settings nor Player are read by IPC code.
internal sealed record PresenceMedia(string Identity, string Title, string Artist, string? PublicUrl,
    bool Playing, bool Video, double? Position, double? Duration, double Speed);
internal sealed record PresenceText(string Idle, string Playing, string Paused, string Generic,
    string Untitled, string Watch, string Listen, string Download);

/// <summary>Builds NativeAOT-safe RPC JSON. Only recognized public catalog links may leave Luna;
/// raw streams often contain IPTV passwords, signed tokens, or private network addresses.</summary>
internal sealed class ActivityBuilder
{
    private readonly long _sessionStart;
    private string? _identity;
    private long? _start;
    private long? _end;
    private double _speed;

    internal ActivityBuilder(DateTimeOffset started) => _sessionStart = started.ToUnixTimeSeconds();

    internal string? Build(DiscordSettings settings, PresenceMedia? media, PresenceText text, DateTimeOffset now)
    {
        if (!settings.Enabled || media is null && !settings.ShowIdle) return null;
        long? start = null, end = null;
        if (media is null) start = _sessionStart;
        else if (media.Playing)
        {
            var speed = double.IsFinite(media.Speed) && media.Speed > 0 ? Math.Clamp(media.Speed, 0.1, 100) : 1;
            var position = SafeSeconds(media.Position);
            var duration = SafeSeconds(media.Duration);
            start = Math.Max(0, now.ToUnixTimeSeconds() - (long)(position / speed));
            if (duration > position) end = start + (long)(duration / speed);
            // Sampling and rounding can differ by a second. Keep the original anchor unless a seek,
            // speed change or new item actually moved it. Paused activities have no running clock.
            if (_identity == media.Identity && _speed == speed && _start is long previous
                && Math.Abs(start.Value - previous) <= 1 && SameEnd(end, _end))
            {
                start = _start;
                end = _end;
            }
            _speed = speed;
        }
        _identity = media?.Identity;
        _start = start;
        _end = end;
        var link = settings.ShowDetails && media is not null ? PublicLink(media.PublicUrl) : null;
        var state = media is null ? $"{AppInfo.Name} v{AppInfo.Version}" : media.Playing ? text.Playing : text.Paused;
        if (settings.ShowDetails && media is not null && !string.IsNullOrWhiteSpace(media.Artist))
            state = $"{state} • {media.Artist}";
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            json.WriteNumber("type", media is null ? 0 : media.Video ? 3 : 2); // Playing / Watching / Listening
            json.WriteString("details", Text(media is null ? text.Idle : !settings.ShowDetails ? text.Generic
                : string.IsNullOrWhiteSpace(media.Title) ? text.Untitled : media.Title));
            json.WriteString("state", Text(state));
            if (start is long s)
            {
                json.WriteStartObject("timestamps");
                json.WriteNumber("start", s);
                if (end is long e) json.WriteNumber("end", e);
                json.WriteEndObject();
            }
            json.WriteStartObject("assets");
            json.WriteString("large_image", link is { VideoId: not null }
                ? $"https://i.ytimg.com/vi/{link.VideoId}/hqdefault.jpg" : "luna");
            json.WriteString("large_text", Text($"{AppInfo.Name} v{AppInfo.Version}"));
            json.WriteEndObject();
            if (settings.ShowButtons)
            {
                json.WriteStartArray("buttons");
                if (link is not null) Button(json, media!.Video ? text.Watch : text.Listen, link.Url);
                Button(json, text.Download, AppInfo.RepositoryUrl + "/releases");
                json.WriteEndArray();
            }
            json.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static double SafeSeconds(double? value)
        => value is double v && double.IsFinite(v) ? Math.Clamp(v, 0, 315360000) : 0;
    private static bool SameEnd(long? left, long? right)
        => left is long l && right is long r ? Math.Abs(l - r) <= 1 : left == right;

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

    private static void Button(Utf8JsonWriter json, string label, string url)
    {
        if (Encoding.UTF8.GetByteCount(url) > 512) return;
        json.WriteStartObject();
        json.WriteString("label", Text(label, 32));
        json.WriteString("url", url);
        json.WriteEndObject();
    }

    internal sealed record Link(string Url, string? VideoId = null);
    internal static Link? PublicLink(string? source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || uri.UserInfo.Length > 0 || !uri.IsDefaultPort) return null;
        var host = uri.IdnHost.ToLowerInvariant();
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? id = null;
        if (host is "youtu.be" or "www.youtu.be") id = parts.FirstOrDefault();
        else if (host is "youtube.com" or "www.youtube.com" or "m.youtube.com" or "music.youtube.com")
        {
            if (parts.Length == 2 && parts[0] is "shorts" or "embed" or "v" or "live") id = parts[1];
            else if (uri.AbsolutePath == "/watch")
                id = uri.Query.TrimStart('?').Split('&').FirstOrDefault(p => p.StartsWith("v=", StringComparison.Ordinal))?[2..];
        }
        else if (host == "open.spotify.com" && parts.Length == 2 && parts[0] == "track"
            && parts[1].Length == 22 && parts[1].All(char.IsAsciiLetterOrDigit))
            return new($"https://open.spotify.com/track/{parts[1]}");
        return id is { Length: 11 } && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
            ? new($"https://www.youtube.com/watch?v={id}", id) : null;
    }
}
