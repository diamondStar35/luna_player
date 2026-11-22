using System.Text.Json;
using System.Text.Json.Serialization;

namespace LunaPlayer.Subsonic.AudioMuse;

internal sealed record Options
{
    public required Uri Server { get; init; }
    public string Token { get; init; } = "";
    public string? MusicServer { get; init; }
    public int ResultCount { get; init; } = 50;
    public TimeSpan SearchTimeout { get; init; } = TimeSpan.FromSeconds(60);
    public TimeSpan GenerationTimeout { get; init; } = TimeSpan.FromMinutes(5);
}
internal enum SeedKind { Song, Artist }
internal enum Operation { Add, Subtract }
internal sealed record Seed(string Id, SeedKind Kind = SeedKind.Song, Operation Operation = Operation.Add);
internal sealed record Track
{
    [JsonPropertyName("item_id"), JsonConverter(typeof(Transport.IdentifierConverter))]
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Artist { get; init; } = "";
    public string Author { get; init; } = "";
    public string Album { get; init; } = "";
    public double? Similarity { get; init; }
    public string Credit => string.IsNullOrEmpty(Artist) ? Author : Artist;
}
internal sealed record Result(Track[] Tracks, string Message, string? PlaylistId = null);
internal sealed record ProgressEvent(string Kind, string Message, Result? Result = null);
internal sealed record TextRequest(string Query, int Limit, string? Server);
internal sealed record PlaylistRequest(string UserInput, int N, string? Server);
internal sealed record AlchemyItem(string Id, string Type, string Op);
internal sealed record AlchemyRequest(AlchemyItem[] Items, int N, string? Server);
internal sealed record Generation
{
    [JsonPropertyName("query_results")] public Track[] QueryResults { get; init; } = [];
    public string Response { get; init; } = "";
    [JsonPropertyName("playlist_id")] public string? PlaylistId { get; init; }
}
internal sealed record Reply
{
    public Track[]? Results { get; init; }
    public JsonElement Response { get; init; }
    public string? Error { get; init; }
    [JsonPropertyName("error_message")] public string? ErrorMessage { get; init; }
    public string? Message { get; init; }
    public string? Type { get; init; }
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TextRequest))]
[JsonSerializable(typeof(PlaylistRequest))]
[JsonSerializable(typeof(AlchemyRequest))]
[JsonSerializable(typeof(Generation))]
[JsonSerializable(typeof(Reply))]
internal partial class AudioMuseJson : JsonSerializerContext;
