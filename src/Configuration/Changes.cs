using System.Text.Json;
using System.Text.Json.Nodes;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Configuration;

/// <summary>Changes made to a preferences draft, relative to the values it opened with.</summary>
internal sealed class Changes
{
    private readonly JsonObject _before;
    private readonly JsonObject _after;
    internal bool ReplacesAll { get; }
    internal bool Any => ReplacesAll || !JsonNode.DeepEquals(_before, _after);

    internal Changes(Settings before, Settings after, bool replaceAll = false)
    {
        _before = Snapshot(before);
        _after = Snapshot(after);
        ReplacesAll = replaceAll;
    }

    // Paths follow the persisted names, including nested sections and shortcut dictionaries.
    internal bool Contains(params string[] paths)
        => ReplacesAll || paths.Any(path => !JsonNode.DeepEquals(Read(_before, path), Read(_after, path)));

    internal bool ApplyTo(Settings settings)
    {
        if (!Any) return false;
        var merged = ReplacesAll ? (JsonObject)_after.DeepClone() : Snapshot(settings);
        if (!ReplacesAll) Merge(merged, _before, _after);
        settings.ApplyValues(merged.Deserialize(SettingsJsonContext.Default.Settings)!);
        return true;
    }

    private static JsonObject Snapshot(Settings settings)
        => (JsonObject)JsonSerializer.SerializeToNode(settings, SettingsJsonContext.Default.Settings)!;

    private static JsonNode? Read(JsonObject settings, string path)
    {
        JsonNode? value = settings;
        foreach (var key in path.Split('.'))
            value = value is JsonObject section ? section[key] : null;
        return value;
    }

    private static void Merge(JsonObject target, JsonObject before, JsonObject after)
    {
        foreach (var key in before.Select(entry => entry.Key).Union(after.Select(entry => entry.Key)))
        {
            var existed = before.TryGetPropertyValue(key, out var oldValue);
            var exists = after.TryGetPropertyValue(key, out var newValue);
            if (existed == exists && JsonNode.DeepEquals(oldValue, newValue)) continue;
            if (!exists)
                target.Remove(key);
            else if (oldValue is JsonObject oldObject && newValue is JsonObject newObject
                && target[key] is JsonObject currentObject)
                Merge(currentObject, oldObject, newObject);
            else
                target[key] = newValue?.DeepClone();
        }
    }
}
