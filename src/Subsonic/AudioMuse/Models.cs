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
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Author { get; set; } = "";
    public string Album { get; set; } = "";
    public double? Similarity { get; set; }
    public string Credit => string.IsNullOrEmpty(Artist) ? Author : Artist;
}
internal sealed record Result(Track[] Tracks, string Message, string? PlaylistId = null, Models.Entry[]? Songs = null);
internal sealed record ProgressEvent(string Kind, string Message, Result? Result = null);
internal sealed record TextRequest(string Query, int Limit, string? Server);
internal sealed record PlaylistRequest(string UserInput, int N, string? Server);
internal sealed record AlchemyItem(string Id, string Type, string Op);
internal sealed record AlchemyRequest(AlchemyItem[] Items, int N, string? Server);
internal sealed record Generation
{
    [JsonPropertyName("query_results")] public Track[] QueryResults { get; set; } = [];
    public string Response { get; set; } = "";
    [JsonPropertyName("playlist_id")] public string? PlaylistId { get; set; }
}
internal sealed record Reply
{
    public Track[]? Results { get; set; }
    public JsonElement Response { get; set; }
    public string? Error { get; set; }
    [JsonPropertyName("error_message")] public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public string? Type { get; set; }
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TextRequest))]
[JsonSerializable(typeof(PlaylistRequest))]
[JsonSerializable(typeof(AlchemyRequest))]
[JsonSerializable(typeof(Generation))]
[JsonSerializable(typeof(Reply))]
internal partial class AudioMuseJson : JsonSerializerContext;
