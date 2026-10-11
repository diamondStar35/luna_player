namespace LunaPlayer.Actions;

/// <summary>One command the player offers: what it is, what it is called, and the keys that invoke it.</summary>
internal sealed record ActionDefinition(ActionId Id, string Label, Shortcut? PrimaryShortcut = null, Shortcut? SecondaryShortcut = null);
