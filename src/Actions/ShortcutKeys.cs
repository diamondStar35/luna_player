using WxSharp;

namespace LunaPlayer.Actions;

/// <summary>Translates between the names a <see cref="Shortcut"/> stores, the key codes wxWidgets reports and
/// the virtual-key codes Windows reports. One table serves every direction, so a name that can be captured is
/// always a name that can be watched for.</summary>
internal static class ShortcutKeys
{
    private const int VirtualKeyF1 = 0x70;

    private static readonly (string Name, Key Code, int VirtualKey)[] Named =
    [
        ("space", Key.Space, 0x20), ("tab", Key.Tab, 0x09), ("enter", Key.Enter, 0x0D),
        ("left", Key.Left, 0x25), ("right", Key.Right, 0x27), ("up", Key.Up, 0x26), ("down", Key.Down, 0x28),
        ("page_up", Key.PageUp, 0x21), ("page_down", Key.PageDown, 0x22),
        ("home", Key.Home, 0x24), ("end", Key.End, 0x23),
        ("backspace", Key.Back, 0x08), ("delete", Key.Delete, 0x2E),
    ];

    /// <summary>The stored name for a key that has just been pressed, or null when it is not one a shortcut
    /// can hold - a bare modifier, or a key with no printable character.</summary>
    internal static string? NameOf(Key code, int character)
    {
        if (code is >= Key.F1 and <= Key.F24) return $"f{code - Key.F1 + 1}";
        // Numpad Enter reports its own code but means the same key to the user.
        if (code == Key.NumpadEnter) return "enter";
        foreach (var (name, candidate, _) in Named)
            if (candidate == code) return name;
        return character is >= 32 and < 127 ? char.ToLowerInvariant((char)character).ToString() : null;
    }

    /// <summary>The virtual-key code a stored name arrives as in a keyboard hook. Fails for punctuation, whose
    /// virtual-key code depends on the keyboard layout and so cannot be derived from the character.</summary>
    internal static bool TryGetVirtualKey(string name, out int virtualKey)
    {
        virtualKey = 0;
        if (TryGetFunctionKey(name, out var function))
        {
            virtualKey = VirtualKeyF1 + (function - 1);
            return true;
        }
        foreach (var (candidate, _, value) in Named)
        {
            if (candidate != name) continue;
            virtualKey = value;
            return true;
        }
        if (name.Length != 1) return false;
        // For letters and digits the virtual-key code is the upper-case character. No other character lines up.
        var upper = char.ToUpperInvariant(name[0]);
        if (!char.IsAsciiLetterOrDigit(upper)) return false;
        virtualKey = upper;
        return true;
    }

    private static bool TryGetFunctionKey(string name, out int number)
    {
        number = 0;
        return name.Length > 1 && name[0] == 'f' && int.TryParse(name[1..], out number) && number is >= 1 and <= 24;
    }
}
