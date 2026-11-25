using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using LunaPlayer.Subsonic.Models;

namespace LunaPlayer.Subsonic.Transport;

/// <summary>Some older servers encode IDs as JSON numbers; never narrow them to Int32.</summary>
internal sealed class TextConverter : JsonConverter<string>
{
    public override bool HandleNull => true;
    public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? "" : reader.GetString() ?? "";
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}

internal sealed class IdentifierConverter : JsonConverter<string>
{
    public override bool HandleNull => true;
    public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return "";
        if (reader.TokenType == JsonTokenType.String) return reader.GetString()!;
        if (reader.TokenType == JsonTokenType.Number)
        {
            using var value = JsonDocument.ParseValue(ref reader);
            return value.RootElement.GetRawText();
        }
        throw new JsonException();
    }
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

internal sealed class IdentifiersConverter : JsonConverter<string[]>
{
    public override bool HandleNull => true;
    public override string[] Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return [];
        var identifier = new IdentifierConverter();
        if (reader.TokenType != JsonTokenType.StartArray) return [identifier.Read(ref reader, typeof(string), options)];
        var values = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) return values.ToArray();
            values.Add(identifier.Read(ref reader, typeof(string), options));
        }
        throw new JsonException();
    }
    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var id in value) writer.WriteStringValue(id);
        writer.WriteEndArray();
    }
}

/// <summary>Accepts legacy singleton collections without constructing converter types through reflection.</summary>
internal sealed class OneOrMany<T> : JsonConverter<T[]>
{
    public override bool HandleNull => true;
    public override T[] Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return [];
        if (reader.TokenType == JsonTokenType.StartArray)
            return JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<T[]>)ProtocolJson.Default.GetTypeInfo(typeof(T[]))!) ?? [];
        var item = JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<T>)ProtocolJson.Default.GetTypeInfo(typeof(T))!);
        return item is null ? [] : [item];
    }
    public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, (JsonTypeInfo<T[]>)ProtocolJson.Default.GetTypeInfo(typeof(T[]))!);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true, NumberHandling = JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, Converters = [typeof(TextConverter)])]
[JsonSerializable(typeof(Envelope))]
[JsonSerializable(typeof(ClientInfo))]
[JsonSerializable(typeof(MusicFolder[]))]
[JsonSerializable(typeof(Genre[]))]
[JsonSerializable(typeof(Artist[]))]
[JsonSerializable(typeof(ArtistIndex[]))]
[JsonSerializable(typeof(Entry[]))]
[JsonSerializable(typeof(ArtistCredit[]))]
[JsonSerializable(typeof(NamedGenre[]))]
[JsonSerializable(typeof(Album[]))]
[JsonSerializable(typeof(Models.Playlist[]))]
[JsonSerializable(typeof(Bookmark[]))]
[JsonSerializable(typeof(RadioStation[]))]
[JsonSerializable(typeof(PodcastChannel[]))]
[JsonSerializable(typeof(PodcastEpisode[]))]
[JsonSerializable(typeof(NowPlayingEntry[]))]
[JsonSerializable(typeof(StructuredLyrics[]))]
[JsonSerializable(typeof(LyricLine[]))]
[JsonSerializable(typeof(Extension[]))]
[JsonSerializable(typeof(SonicMatch[]))]
[JsonSerializable(typeof(DirectPlayProfile[]))]
[JsonSerializable(typeof(TranscodingProfile[]))]
internal partial class ProtocolJson : JsonSerializerContext;
