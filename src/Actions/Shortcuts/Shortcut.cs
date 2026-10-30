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
    /// <summary>The Windows key. Only a global shortcut can use it: an accelerator table has no way to
    /// express this modifier, so a local binding carrying it is skipped. The value matches
    /// <see cref="WxSharp.AcceleratorModifiers.Win"/>.</summary>
    Win = 8,
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
        if ((Modifiers & ShortcutModifiers.Win) != 0)
            parts.Add("Win");
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
    private readonly HashSet<ActionId> _actions = [];
    private readonly Dictionary<ActionId, Shortcut> _defaultPrimary = [];
    private readonly Dictionary<ActionId, Shortcut> _defaultSecondary = [];
    private readonly Dictionary<ActionId, Shortcut> _primary = [];
    private readonly Dictionary<ActionId, Shortcut> _secondary = [];

    internal ShortcutManager(IEnumerable<ActionDefinition> actions)
    {
        foreach (var action in actions)
        {
            _actions.Add(action.Id);
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
        // Resolve all requested assignments together: a key freed by a later action (or the
        // other slot) must already be available to an earlier action, including shortcut swaps.
        var defaults = new Dictionary<(ActionId Action, ShortcutSlot Slot), Shortcut>();
        foreach (var pair in _defaultPrimary) defaults[(pair.Key, ShortcutSlot.Primary)] = pair.Value;
        foreach (var pair in _defaultSecondary) defaults[(pair.Key, ShortcutSlot.Secondary)] = pair.Value;
        var requested = new Dictionary<(ActionId Action, ShortcutSlot Slot), Shortcut>();
        CollectOverrides(primary, _actions, ShortcutSlot.Primary, defaults, requested);
        CollectOverrides(secondary, _defaultSecondary.Keys, ShortcutSlot.Secondary, defaults, requested);

        Dictionary<(ActionId Action, ShortcutSlot Slot), Shortcut> effective;
        bool rejected;
        do
        {
            effective = new(defaults);
            foreach (var pair in requested) effective[pair.Key] = pair.Value;
            rejected = false;
            foreach (var group in effective.GroupBy(pair => pair.Value))
            {
                // An unchanged binding wins a conflict. Otherwise use a stable slot/action order.
                var losers = group.OrderBy(pair => requested.ContainsKey(pair.Key))
                    .ThenBy(pair => pair.Key.Slot).ThenBy(pair => pair.Key.Action).Skip(1);
                foreach (var pair in losers)
                    rejected |= requested.Remove(pair.Key);
            }
            // Restoring a rejected override's default can conflict with another request. Repeat
            // until all retained requests are compatible, rather than silently losing a default.
        } while (rejected);

        _primary.Clear();
        _secondary.Clear();
        foreach (var pair in effective)
            (pair.Key.Slot == ShortcutSlot.Primary ? _primary : _secondary)[pair.Key.Action] = pair.Value;
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

    // Primary slots exist for every action; secondary slots only for actions defining a default.
    private static void CollectOverrides(
        IReadOnlyDictionary<ActionId, Shortcut> overrides,
        IReadOnlyCollection<ActionId> allowed,
        ShortcutSlot slot,
        IReadOnlyDictionary<(ActionId Action, ShortcutSlot Slot), Shortcut> defaults,
        IDictionary<(ActionId Action, ShortcutSlot Slot), Shortcut> requested)
    {
        foreach (var pair in overrides)
        {
            if (!allowed.Contains(pair.Key)) continue;
            var value = new Shortcut(pair.Value.Key.Trim().ToLowerInvariant(), pair.Value.Modifiers);
            var key = (pair.Key, slot);
            if (IsValid(value) && (!defaults.TryGetValue(key, out var original) || original != value))
                requested[key] = value;
        }
    }

    internal static bool IsValid(Shortcut shortcut)
    {
        var key = shortcut.Key;
        if (key.Length == 1 && key[0] is >= ' ' and <= '~') return true;
        if (key is "space" or "tab" or "enter" or "left" or "right" or "up" or "down"
            or "page_up" or "page_down" or "home" or "end" or "backspace" or "delete") return true;
        return key.Length is 2 or 3 && key[0] == 'f' && int.TryParse(key[1..], out var function) && function is >= 1 and <= 24;
    }
}
