using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Browsing;

/// <summary>Only source-file characteristics; a requested transcode format is not measured quality.</summary>
internal readonly record struct Quality(string Format, int? BitRate, int? SampleRate, int? BitDepth, int? Channels)
{
    internal static Quality From(Entry? song) => song is null ? new("", null, null, null, null)
        : new(song.Suffix.Length > 0 ? song.Suffix.ToUpperInvariant() : song.ContentType,
            Positive(song.BitRate), Positive(song.SamplingRate), Positive(song.BitDepth), Positive(song.ChannelCount));
    private static int? Positive(int? value) => value is > 0 ? value : null;
}
