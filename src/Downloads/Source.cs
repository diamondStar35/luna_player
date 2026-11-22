using LunaPlayer.Media;
using LunaPlayer.Subsonic.Playback;
using MediaKind = LunaPlayer.Subsonic.Playback.MediaKind;

namespace LunaPlayer.Downloads;

internal enum SourceKind { None, YouTube, MusicServer, HttpFile }

internal static class Source
{
    internal static SourceKind Resolve(string? path, string? source, bool live = false)
    {
        if (LinkValidator.IsYouTubeUrl(source ?? path)) return SourceKind.YouTube;
        if (Reference.Parse(path) is { } reference)
            return reference.Kind is MediaKind.Song or MediaKind.Podcast ? SourceKind.MusicServer : SourceKind.None;
        if (live || !Uri.TryCreate(path, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return SourceKind.None;
        var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        return extension is ".m3u" or ".m3u8" or ".mpd" or ".pls" ? SourceKind.None : SourceKind.HttpFile;
    }

    internal static string FileName(Uri address, string title)
    {
        var original = Path.GetFileName(Uri.UnescapeDataString(address.AbsolutePath));
        var extension = Path.GetExtension(original);
        var name = string.IsNullOrWhiteSpace(title) ? original : title.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? title : title + extension;
        var safe = Configuration.Paths.SafeFileName(name, "download");
        if (extension.Length > 0 && !safe.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            safe = safe[..Math.Min(safe.Length, Math.Max(1, 120 - extension.Length))] + extension;
        var stem = Path.GetFileNameWithoutExtension(safe).ToUpperInvariant();
        if (stem is "CON" or "PRN" or "AUX" or "NUL" || stem.Length == 4
            && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] is >= '1' and <= '9') safe = "_" + safe;
        return string.IsNullOrWhiteSpace(safe) ? "download" : safe;
    }
}
