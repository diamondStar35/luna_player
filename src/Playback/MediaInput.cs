using LunaPlayer.Media;

namespace LunaPlayer.Playback;

internal sealed record MediaInput(string Address, MediaTags Tags, double? Duration = null,
    IReadOnlyDictionary<string, string>? Headers = null, bool Live = false, double Offset = 0, bool SeekByReopening = false,
    StreamFormat? Format = null, double? RequestedPosition = null);

/// <summary>Separates saved source identity from a short-lived, authenticated playback request.</summary>
internal interface IMediaResolver
{
    bool Handles(string address);
    Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token);
}
