using System.Text.Json.Serialization;

namespace LunaPlayer.Actions;

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<ShortcutModifiers>))]
internal enum ShortcutModifiers
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4,
}

internal enum ShortcutSlot
{
    Primary,
    Secondary,
}

internal readonly record struct Shortcut(string Key, ShortcutModifiers Modifiers = ShortcutModifiers.None)
{
    internal string ToDisplayString()
    {
        var parts = new List<string>(4);
        if ((Modifiers & ShortcutModifiers.Control) != 0)
            parts.Add("Ctrl");
        if ((Modifiers & ShortcutModifiers.Shift) != 0)
            parts.Add("Shift");
        if ((Modifiers & ShortcutModifiers.Alt) != 0)
            parts.Add("Alt");
        parts.Add(FormatKey(Key));
        return string.Join('+', parts);
    }

    private static string FormatKey(string key) => key.ToLowerInvariant() switch
    {
        "page_up" => "PageUp",
        "page_down" => "PageDown",
        "space" => "Space",
        "tab" => "Tab",
        "enter" => "Enter",
        "left" => "Left",
        "right" => "Right",
        "up" => "Up",
        "down" => "Down",
        "home" => "Home",
        "end" => "End",
        "backspace" => "Backspace",
        "delete" => "Delete",
        _ when key.Length > 1 && key[0] is 'f' or 'F' && int.TryParse(key[1..], out _) => key.ToUpperInvariant(),
        _ when key.Length == 1 => key.ToUpperInvariant(),
        _ => key,
    };
}

internal readonly record struct ShortcutBinding(ActionId Action, ShortcutSlot Slot, Shortcut Shortcut);

internal sealed class ShortcutManager
{
    private readonly Dictionary<ActionId, Shortcut> _defaultPrimary = [];
    private readonly Dictionary<ActionId, Shortcut> _defaultSecondary = [];
    private readonly Dictionary<ActionId, Shortcut> _primary = [];
    private readonly Dictionary<ActionId, Shortcut> _secondary = [];

    internal ShortcutManager(IEnumerable<ActionDefinition> actions)
    {
        foreach (var action in actions)
        {
            if (action.PrimaryShortcut is Shortcut primary)
                _defaultPrimary[action.Id] = _primary[action.Id] = primary;
            if (action.SecondaryShortcut is Shortcut secondary)
                _defaultSecondary[action.Id] = _secondary[action.Id] = secondary;
        }
    }

    internal Shortcut? Get(ActionId action, ShortcutSlot slot = ShortcutSlot.Primary)
    {
        var source = slot == ShortcutSlot.Primary ? _primary : _secondary;
        return source.TryGetValue(action, out var shortcut) ? shortcut : null;
    }

    internal void Set(ActionId action, Shortcut? shortcut, ShortcutSlot slot = ShortcutSlot.Primary)
    {
        var target = slot == ShortcutSlot.Primary ? _primary : _secondary;
        if (shortcut is Shortcut value)
            target[action] = value;
        else
            target.Remove(action);
    }

    internal IReadOnlyList<ShortcutBinding> GetBindings()
    {
        var bindings = new List<ShortcutBinding>(_primary.Count + _secondary.Count);
        foreach (var pair in _primary)
            bindings.Add(new ShortcutBinding(pair.Key, ShortcutSlot.Primary, pair.Value));
        foreach (var pair in _secondary)
            bindings.Add(new ShortcutBinding(pair.Key, ShortcutSlot.Secondary, pair.Value));
        return bindings;
    }

    internal void Apply(IReadOnlyDictionary<ActionId, Shortcut> primary, IReadOnlyDictionary<ActionId, Shortcut> secondary)
    {
        _primary.Clear();
        _secondary.Clear();
        foreach (var pair in _defaultPrimary) _primary[pair.Key] = pair.Value;
        foreach (var pair in _defaultSecondary) _secondary[pair.Key] = pair.Value;
        var occupied = new HashSet<Shortcut>(_primary.Values.Concat(_secondary.Values));
        ApplyOverrides(primary, _primary, _defaultPrimary, occupied);
        ApplyOverrides(secondary, _secondary, _defaultSecondary, occupied);
    }

    internal Dictionary<ActionId, Shortcut> PrimaryOverrides()
        => Overrides(_primary, _defaultPrimary);

    internal Dictionary<ActionId, Shortcut> SecondaryOverrides()
        => Overrides(_secondary, _defaultSecondary);

    private static Dictionary<ActionId, Shortcut> Overrides(
        IReadOnlyDictionary<ActionId, Shortcut> effective,
        IReadOnlyDictionary<ActionId, Shortcut> defaults)
    {
        var result = new Dictionary<ActionId, Shortcut>();
        foreach (var pair in effective)
            if (!defaults.TryGetValue(pair.Key, out var value) || value != pair.Value) result[pair.Key] = pair.Value;
        return result;
    }

    private static void ApplyOverrides(
        IReadOnlyDictionary<ActionId, Shortcut> overrides,
        IDictionary<ActionId, Shortcut> effective,
        IReadOnlyDictionary<ActionId, Shortcut> defaults,
        ISet<Shortcut> occupied)
    {
        foreach (var pair in overrides.OrderBy(pair => pair.Key))
        {
            if (!defaults.ContainsKey(pair.Key) || !effective.TryGetValue(pair.Key, out var previous)) continue;
            occupied.Remove(previous);
            var value = new Shortcut(pair.Value.Key.Trim().ToLowerInvariant(), pair.Value.Modifiers);
            if (IsValid(value) && !occupied.Contains(value)) effective[pair.Key] = value;
            occupied.Add(effective[pair.Key]);
        }
    }

    private static bool IsValid(Shortcut shortcut)
    {
        var key = shortcut.Key;
        if (key.Length == 1 && key[0] is >= ' ' and <= '~') return true;
        if (key is "space" or "tab" or "enter" or "left" or "right" or "up" or "down"
            or "page_up" or "page_down" or "home" or "end" or "backspace" or "delete") return true;
        return key.Length is 2 or 3 && key[0] == 'f' && int.TryParse(key[1..], out var function) && function is >= 1 and <= 24;
    }
}
