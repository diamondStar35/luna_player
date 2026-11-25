using LunaPlayer.Playback;

namespace LunaPlayer.UI.Subsonic;

internal static class StreamPresentation
{
    internal static string Describe(StreamFormat format)
    {
        var values = new List<string>();
        if (format.Codec is { Length: > 0 } codec) values.Add(codec.ToUpperInvariant());
        if (format.Container is { Length: > 0 } container && !string.Equals(container, format.Codec, StringComparison.OrdinalIgnoreCase))
            values.Add(container.ToUpperInvariant());
        if (format.BitRate is > 0 and var rate) values.Add(Words.Kilobits((int)Math.Round(rate / 1000d)));
        if (format.SampleRate is > 0 and var samples) values.Add(Words.SampleRate(samples / 1000d));
        if (format.BitDepth is > 0 and var depth) values.Add(Words.BitDepth(depth));
        if (format.Channels is > 0 and var channels) values.Add(Words.Channels(channels));
        return values.Count > 0 ? Words.PlaybackStream(string.Join(", ", values)) : "";
    }
}
