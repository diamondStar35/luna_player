using System.Globalization;
using System.Text.RegularExpressions;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Listening;

internal static partial class LyricsParser
{
    [GeneratedRegex(@"\[(\d+):(\d{1,2})(?:[.:](\d{1,3}))?\]", RegexOptions.CultureInvariant)]
    private static partial Regex Timestamp();
    [GeneratedRegex(@"^\[offset:\s*([+-]?\d+)\s*\]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Offset();
    [GeneratedRegex(@"^\[(?:ar|al|ti|au|by|re|ve|length|id|offset):.*\]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Metadata();

    internal static StructuredLyrics Parse(string text)
    {
        var timed = new List<LyricLine>(); var plain = new List<LyricLine>(); double? offset = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').TrimStart('\uFEFF');
            var metadata = line.Trim();
            var offsetTag = Offset().Match(metadata);
            if (offsetTag.Success && long.TryParse(offsetTag.Groups[1].Value, CultureInfo.InvariantCulture, out var value)) offset = value;
            if (Metadata().IsMatch(metadata)) continue;
            var starts = new List<long>(); var cursor = 0;
            while (cursor < line.Length)
            {
                var match = Timestamp().Match(line, cursor);
                if (!match.Success || match.Index != cursor
                    || !long.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var minutes)
                    || !int.TryParse(match.Groups[2].Value, out var seconds) || seconds >= 60
                    || minutes > (long.MaxValue - 60000) / 60000) break;
                var fraction = match.Groups[3].Value.PadRight(3, '0');
                starts.Add(minutes * 60000 + seconds * 1000 + int.Parse(fraction, CultureInfo.InvariantCulture));
                cursor += match.Length;
            }
            if (starts.Count == 0) plain.Add(new() { Value = line });
            else foreach (var start in starts) timed.Add(new() { Start = start, Value = line[cursor..] });
        }
        if (timed.Count > 0) return new() { Synced = true, Offset = offset, Lines = timed.OrderBy(line => line.Start).ToArray() };
        while (plain.Count > 0 && plain[^1].Value.Length == 0) plain.RemoveAt(plain.Count - 1);
        return new() { Offset = offset, Lines = plain.ToArray() };
    }
}
