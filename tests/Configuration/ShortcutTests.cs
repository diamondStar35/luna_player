using System.Text.Json;
using LunaPlayer.Actions;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Tests.Configuration;

[TestClass, TestCategory("Unit")]
public sealed class ShortcutTests
{
    private static readonly Shortcut Left = new("left", ShortcutModifiers.Control | ShortcutModifiers.Shift);
    private static readonly Shortcut Right = new("right", ShortcutModifiers.Control | ShortcutModifiers.Shift);
    private static readonly Shortcut AltLeft = new("l", ShortcutModifiers.Alt);
    private static readonly Shortcut AltRight = new("r", ShortcutModifiers.Alt);

    [TestMethod]
    public void RetiredBindingsDoNotInvalidateOtherSettings()
    {
        const string json = """
            {"audio":{"volume":73},"shortcuts":{
              "primary":{"SeekBackwardX4":{"key":"left","modifiers":"Control, Shift"},
                         "PlayPause":{"key":"p","modifiers":"Alt"}},
              "secondary":{"SeekForwardX4":{"key":"right","modifiers":"Control, Shift"}},
              "global":{"SeekBackwardX4":{"key":"left","modifiers":"Win, Alt"}}
            }}
            """;
        var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings)!;
        settings.ValidateStored();
        Assert.AreEqual(73d, settings.Audio.Volume);
        Assert.AreEqual(new Shortcut("p", ShortcutModifiers.Alt), settings.Shortcuts.Primary[ActionId.PlayPause]);
        Assert.HasCount(1, settings.Shortcuts.Primary);
        Assert.HasCount(0, settings.Shortcuts.Secondary);
        Assert.HasCount(0, settings.Shortcuts.Global);
        var saved = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        Assert.IsFalse(saved.Contains("SeekBackwardX4", StringComparison.Ordinal));
        Assert.IsFalse(saved.Contains("SeekForwardX4", StringComparison.Ordinal));
        var restored = JsonSerializer.Deserialize(saved, SettingsJsonContext.Default.Settings)!;
        Assert.AreEqual(settings.Shortcuts.Primary[ActionId.PlayPause], restored.Shortcuts.Primary[ActionId.PlayPause]);
    }

    [TestMethod]
    public void LegacyNumericBindingsKeepTheirOriginalActions()
    {
        const string json = """
            {"shortcuts":{"primary":{"40":{"key":"left"},"41":{"key":"right"},"42":{"key":"home"}}}}
            """;
        var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings)!;
        settings.ValidateStored();
        Assert.HasCount(1, settings.Shortcuts.Primary);
        Assert.AreEqual(new Shortcut("home"), settings.Shortcuts.Primary[ActionId.SeekStart]);
    }

    [TestMethod]
    public void PrimaryShortcutSwapsAreAppliedTogether()
    {
        var shortcuts = new ShortcutManager([
            new(ActionId.RotateSofaLeft, "Left", Left), new(ActionId.RotateSofaRight, "Right", Right)]);
        shortcuts.Apply(new Dictionary<ActionId, Shortcut>
        {
            [ActionId.RotateSofaLeft] = Right,
            [ActionId.RotateSofaRight] = Left,
        }, new Dictionary<ActionId, Shortcut>());
        Assert.AreEqual(Right, shortcuts.Get(ActionId.RotateSofaLeft));
        Assert.AreEqual(Left, shortcuts.Get(ActionId.RotateSofaRight));
        AssertUnique(shortcuts);
    }

    [TestMethod]
    public void SwapsBetweenPrimaryAndSecondarySlotsAreAppliedTogether()
    {
        var enter = new Shortcut("enter");
        var shortcuts = new ShortcutManager([
            new(ActionId.PlayPause, "Play", new("space"), enter),
            new(ActionId.RotateSofaLeft, "Left", Left)]);
        shortcuts.Apply(new Dictionary<ActionId, Shortcut> { [ActionId.RotateSofaLeft] = enter },
            new Dictionary<ActionId, Shortcut> { [ActionId.PlayPause] = Left });
        Assert.AreEqual(enter, shortcuts.Get(ActionId.RotateSofaLeft));
        Assert.AreEqual(Left, shortcuts.Get(ActionId.PlayPause, ShortcutSlot.Secondary));
        AssertUnique(shortcuts);
    }

    [TestMethod]
    public void ConflictsWithUnchangedDefaultsDoNotStealKeys()
    {
        var shortcuts = new ShortcutManager([
            new(ActionId.RotateSofaLeft, "Left", Left), new(ActionId.RotateSofaRight, "Right", Right)]);
        shortcuts.Apply(new Dictionary<ActionId, Shortcut> { [ActionId.RotateSofaLeft] = Right },
            new Dictionary<ActionId, Shortcut>());
        Assert.AreEqual(Left, shortcuts.Get(ActionId.RotateSofaLeft));
        Assert.AreEqual(Right, shortcuts.Get(ActionId.RotateSofaRight));
        AssertUnique(shortcuts);
    }

    [TestMethod]
    public void RejectingOneOverrideRestoresConflictingDependentDefaults()
    {
        var shortcuts = new ShortcutManager([
            new(ActionId.RotateSofaLeft, "Left", Left),
            new(ActionId.RotateSofaRight, "Right", Right),
            new(ActionId.SofaElevationUp, "Up", AltLeft)]);
        shortcuts.Apply(new Dictionary<ActionId, Shortcut>
        {
            [ActionId.RotateSofaLeft] = Right,
            [ActionId.RotateSofaRight] = AltLeft, // Blocked by unchanged elevation shortcut.
        }, new Dictionary<ActionId, Shortcut>());
        Assert.AreEqual(Left, shortcuts.Get(ActionId.RotateSofaLeft));
        Assert.AreEqual(Right, shortcuts.Get(ActionId.RotateSofaRight));
        Assert.AreEqual(AltLeft, shortcuts.Get(ActionId.SofaElevationUp));
        AssertUnique(shortcuts);
    }

    [TestMethod]
    public void InvalidAndUnknownOverridesAreIgnored()
    {
        var shortcuts = new ShortcutManager([new(ActionId.RotateSofaLeft, "Left", Left)]);
        shortcuts.Apply(new Dictionary<ActionId, Shortcut>
        {
            [ActionId.RotateSofaLeft] = new("invalid-key"),
            [ActionId.RotateSofaRight] = AltRight,
        }, new Dictionary<ActionId, Shortcut> { [ActionId.RotateSofaLeft] = AltLeft });
        Assert.AreEqual(Left, shortcuts.Get(ActionId.RotateSofaLeft));
        Assert.IsNull(shortcuts.Get(ActionId.RotateSofaRight));
        Assert.IsNull(shortcuts.Get(ActionId.RotateSofaLeft, ShortcutSlot.Secondary));
        AssertUnique(shortcuts);
    }

    private static void AssertUnique(ShortcutManager shortcuts)
    {
        var bindings = shortcuts.GetBindings();
        Assert.AreEqual(bindings.Count, bindings.Select(binding => binding.Shortcut).Distinct().Count());
    }
}
