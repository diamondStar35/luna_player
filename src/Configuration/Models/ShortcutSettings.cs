using System.Text.Json.Serialization;
using LunaPlayer.Actions;

namespace LunaPlayer.Configuration.Models;

internal sealed class ShortcutSettings
{
    [JsonConverter(typeof(ShortcutOverrides))]
    public Dictionary<ActionId, Shortcut> Primary { get; set; } = [];
    [JsonConverter(typeof(ShortcutOverrides))]
    public Dictionary<ActionId, Shortcut> Secondary { get; set; } = [];
    /// <summary>System-wide hot keys, keyed by the action they trigger. Separate from <see cref="Primary"/>
    /// because the same action can hold a local and a global binding at once.</summary>
    [JsonConverter(typeof(ShortcutOverrides))]
    public Dictionary<ActionId, Shortcut> Global { get; set; } = [];
    internal ShortcutSettings Copy() => new()
    {
        Primary = new(Primary),
        Secondary = new(Secondary),
        Global = new(Global),
    };
    internal void Apply(ShortcutSettings source)
    {
        Primary = new(source.Primary);
        Secondary = new(source.Secondary);
        Global = new(source.Global);
    }
}
