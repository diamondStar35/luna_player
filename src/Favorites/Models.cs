using System.Text.Json.Serialization;

namespace LunaPlayer.Favorites;

/// <summary>What a saved link points at, which decides how the player opens it.</summary>
/// <remarks>
/// The names written to disk are the ones the Python player used, so a favourites file carried over from it
/// is read without conversion.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<FavoriteKind>))]
internal enum FavoriteKind
{
    /// <summary>A YouTube address naming a single video.</summary>
    [JsonStringEnumMemberName("video")] Video,

    /// <summary>A YouTube address naming a playlist.</summary>
    [JsonStringEnumMemberName("playlist")] Playlist,

    /// <summary>A YouTube address naming both a video and the playlist it sits in. Kept apart from the other
    /// two because the player has to ask which of them the user meant.</summary>
    [JsonStringEnumMemberName("combined")] Combined,

    /// <summary>Any other http address, played as a network stream.</summary>
    [JsonStringEnumMemberName("generic_stream")] Stream,
}

/// <summary>One saved link.</summary>
internal sealed class Favorite
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("kind")] public FavoriteKind Kind { get; set; }
    [JsonPropertyName("link")] public string Link { get; set; } = string.Empty;

    /// <summary>When it was saved, in seconds since the Unix epoch. Written as a number with a fractional
    /// part by the Python player, so it is read as one.</summary>
    [JsonPropertyName("created")] public double Created { get; set; }
}

internal sealed class FavoriteDocument
{
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("items")] public List<Favorite> Items { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(FavoriteDocument))]
internal partial class FavoriteJsonContext : JsonSerializerContext;
