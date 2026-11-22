using System.Text.Json.Serialization;
using LunaPlayer.Actions;

namespace LunaPlayer.Configuration.Models;

internal sealed class ShortcutSettings
{
    public bool DisableSecondary { get; set; }
    [JsonConverter(typeof(ShortcutOverrides))]
    public Dictionary<ActionId, Shortcut> Primary { get; set; } = [];
    [JsonConverter(typeof(ShortcutOverrides))]
    public Dictionary<ActionId, Shortcut> Secondary { get; set; } = [];
    /// <summary>System-wide hot keys, keyed by the action they trigger. Separate from <see cref="Primary"/>
    /// because the same action can hold a local and a global binding at once.</summary>
    [JsonConverter(typeof(ShortcutOverrides))]
    public Dictionary<ActionId, Shortcut> Global { get; set; } = [];
    [JsonConverter(typeof(ShortcutOverrides))]
    public Dictionary<ActionId, Shortcut> MusicServers { get; set; } = [];
    internal ShortcutSettings Copy() => new()
    {
        DisableSecondary = DisableSecondary,
        Primary = new(Primary),
        Secondary = new(Secondary),
        Global = new(Global),
        MusicServers = new(MusicServers),
    };
    internal void Apply(ShortcutSettings source)
    {
        DisableSecondary = source.DisableSecondary;
        Primary = new(source.Primary);
        Secondary = new(source.Secondary);
        Global = new(source.Global);
        MusicServers = new(source.MusicServers);
    }
}
