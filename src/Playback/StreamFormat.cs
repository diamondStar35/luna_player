namespace LunaPlayer.Playback;

internal sealed record StreamFormat(string? Codec = null, string? Container = null, int? BitRate = null,
    int? SampleRate = null, int? BitDepth = null, int? Channels = null, bool? Transcoded = null);
