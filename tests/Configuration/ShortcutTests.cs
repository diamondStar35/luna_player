using LunaPlayer.Actions;

namespace LunaPlayer.Tests.Configuration;

[TestClass, TestCategory("Unit")]
public sealed class ShortcutTests
{
    private static readonly Shortcut Left = new("left", ShortcutModifiers.Control | ShortcutModifiers.Shift);
    private static readonly Shortcut Right = new("right", ShortcutModifiers.Control | ShortcutModifiers.Shift);
    private static readonly Shortcut AltLeft = new("l", ShortcutModifiers.Alt);
    private static readonly Shortcut AltRight = new("r", ShortcutModifiers.Alt);

    [TestMethod]
    public void LegacySeekBindingsCanBeReclaimedAfterMovingRotationShortcuts()
    {
        ActionDefinition[] definitions =
        [
            new(ActionId.SeekBackwardX4, "Seek backward four steps"),
            new(ActionId.SeekForwardX4, "Seek forward four steps"),
            new(ActionId.RotateSofaLeft, "Rotate left", Left),
            new(ActionId.RotateSofaRight, "Rotate right", Right),
        ];
        var shortcuts = new ShortcutManager(definitions);
        shortcuts.Set(ActionId.RotateSofaLeft, AltLeft);
        shortcuts.Set(ActionId.RotateSofaRight, AltRight);
        shortcuts.Set(ActionId.SeekBackwardX4, Left);
        shortcuts.Set(ActionId.SeekForwardX4, Right);
        var reloaded = new ShortcutManager(definitions);
        reloaded.Apply(shortcuts.PrimaryOverrides(), shortcuts.SecondaryOverrides());
        Assert.AreEqual(Left, reloaded.Get(ActionId.SeekBackwardX4));
        Assert.AreEqual(Right, reloaded.Get(ActionId.SeekForwardX4));
        Assert.AreEqual(AltLeft, reloaded.Get(ActionId.RotateSofaLeft));
        Assert.AreEqual(AltRight, reloaded.Get(ActionId.RotateSofaRight));
        AssertUnique(reloaded);
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
