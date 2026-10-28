using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Spotify;
using WxSharp;

namespace LunaPlayer.UI.Spotify;

internal sealed record DialogOutcome(Track? Track = null,
    IReadOnlyList<Track>? Queue = null, Collection? Collection = null,
    string? NextCursor = null,
    Func<string?, CancellationToken, Task<TrackPage>>? LoadPage = null);
