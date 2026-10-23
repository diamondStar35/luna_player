using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LunaPlayer.Radio.Client;

/// <summary>Explicit metadata for every JSON response. No reflection serializer or runtime type lookup
/// is used by the transport. Legacy timestamp strings stay intact alongside typed ISO timestamps.</summary>
[JsonSourceGenerationOptions(
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    Converters = [typeof(FlagConverter), typeof(TimestampConverter), typeof(OptionalGuidConverter)])]
[JsonSerializable(typeof(Station[]))]
[JsonSerializable(typeof(StationChange[]))]
[JsonSerializable(typeof(StationCheck[]))]
[JsonSerializable(typeof(CheckStep[]))]
[JsonSerializable(typeof(StationClick[]))]
[JsonSerializable(typeof(DirectoryEntry[]))]
[JsonSerializable(typeof(Country[]))]
[JsonSerializable(typeof(Language[]))]
[JsonSerializable(typeof(State[]))]
[JsonSerializable(typeof(Mirror[]))]
[JsonSerializable(typeof(StreamingServer[]))]
[JsonSerializable(typeof(ServerConfiguration))]
[JsonSerializable(typeof(ServerStatistics))]
[JsonSerializable(typeof(ActionResult))]
[JsonSerializable(typeof(CreationResult))]
[JsonSerializable(typeof(ClickResult))]
internal partial class Serialization : JsonSerializerContext;

/// <summary>The API mixes JSON booleans, 0/1 flags and string flags across endpoints.</summary>
internal sealed class FlagConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.True or JsonTokenType.False)
            return reader.GetBoolean();
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number) && number is 0 or 1)
            return number == 1;
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (bool.TryParse(text, out var flag))
                return flag;
            if (text is "0" or "1")
                return text == "1";
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);
}

/// <summary>Never-checked stations have null or empty timestamps. Invalid nonempty dates are errors.</summary>
internal sealed class TimestampConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();
        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
            return timestamp;
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

/// <summary>A failed addition returns an empty UUID instead of null.</summary>
internal sealed class OptionalGuidConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();
        var text = reader.GetString();
        if (string.IsNullOrEmpty(text))
            return null;
        if (Guid.TryParse(text, out var uuid))
            return uuid;
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
