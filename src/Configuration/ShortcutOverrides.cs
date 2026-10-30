using System.Text.Json;
using System.Text.Json.Serialization;
using LunaPlayer.Actions;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Configuration;

/// <summary>Ignores retired action names without rejecting the user's remaining settings.</summary>
internal sealed class ShortcutOverrides : JsonConverter<Dictionary<ActionId, Shortcut>>
{
    public override Dictionary<ActionId, Shortcut> Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Shortcut overrides must be an object.");
        var result = new Dictionary<ActionId, Shortcut>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return result;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a shortcut action name.");
            var name = reader.GetString();
            if (!reader.Read()) throw new JsonException("Missing shortcut binding.");
            if (Enum.TryParse<ActionId>(name, ignoreCase: true, out var action) && Enum.IsDefined(action))
                result[action] = JsonSerializer.Deserialize(ref reader, SettingsJsonContext.Default.Shortcut);
            else
                reader.Skip();
        }
        throw new JsonException("Incomplete shortcut overrides.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<ActionId, Shortcut> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (action, shortcut) in value)
        {
            writer.WritePropertyName(action.ToString());
            JsonSerializer.Serialize(writer, shortcut, SettingsJsonContext.Default.Shortcut);
        }
        writer.WriteEndObject();
    }
}
