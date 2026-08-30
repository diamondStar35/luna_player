namespace LunaPlayer.Actions;

internal readonly record struct SeekStepAction(ActionId Id, string Key, string Label, double Seconds);
internal readonly record struct PercentJumpAction(ActionId Id, int Percent);

internal static class PlaybackActionDefinitions
{
    internal static IReadOnlyList<SeekStepAction> SeekSteps { get; } =
    [
        new(ActionId.SeekStep1, "1", "1 second", 1),
        new(ActionId.SeekStep2, "2", "5 seconds", 5),
        new(ActionId.SeekStep3, "3", "10 seconds", 10),
        new(ActionId.SeekStep4, "4", "20 seconds", 20),
        new(ActionId.SeekStep5, "5", "30 seconds", 30),
        new(ActionId.SeekStep6, "6", "1 minute", 60),
        new(ActionId.SeekStep7, "7", "2 minutes", 120),
        new(ActionId.SeekStep8, "8", "3 minutes", 180),
        new(ActionId.SeekStep9, "9", "5 minutes", 300),
        new(ActionId.SeekStep0, "0", "10 minutes", 600),
        new(ActionId.SeekStepCustom, "-", "Custom value", 0),
    ];

    internal static IReadOnlyList<PercentJumpAction> PercentJumps { get; } =
    [
        new(ActionId.JumpPercent10, 10),
        new(ActionId.JumpPercent15, 15),
        new(ActionId.JumpPercent20, 20),
        new(ActionId.JumpPercent25, 25),
        new(ActionId.JumpPercent30, 30),
        new(ActionId.JumpPercent35, 35),
        new(ActionId.JumpPercent40, 40),
        new(ActionId.JumpPercent45, 45),
        new(ActionId.JumpPercent50, 50),
        new(ActionId.JumpPercent55, 55),
        new(ActionId.JumpPercent60, 60),
        new(ActionId.JumpPercent65, 65),
        new(ActionId.JumpPercent70, 70),
        new(ActionId.JumpPercent75, 75),
        new(ActionId.JumpPercent80, 80),
        new(ActionId.JumpPercent85, 85),
        new(ActionId.JumpPercent90, 90),
        new(ActionId.JumpPercent95, 95),
        new(ActionId.JumpPercent100, 100),
    ];

    internal static IReadOnlyList<ActionDefinition> All { get; } = Build();

    private static IReadOnlyList<ActionDefinition> Build()
    {
        var actions = new List<ActionDefinition>
        {
            new(ActionId.PlayPause, "Play/Pause", new("space"), new("enter")),
            new(ActionId.SeekBackward, "Seek Backward", new("left")),
            new(ActionId.SeekForward, "Seek Forward", new("right")),
            new(ActionId.SeekBackwardX2, "Seek Backward x2", new("left", ShortcutModifiers.Control)),
            new(ActionId.SeekForwardX2, "Seek Forward x2", new("right", ShortcutModifiers.Control)),
            new(ActionId.SeekBackwardX4, "Seek Backward x4", new("left", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.SeekForwardX4, "Seek Forward x4", new("right", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.SeekBackwardX8, "Seek Backward x8", new("left", ShortcutModifiers.Shift)),
            new(ActionId.SeekForwardX8, "Seek Forward x8", new("right", ShortcutModifiers.Shift)),
            new(ActionId.SeekStart, "Seek Start", new("home")),
            new(ActionId.SeekEnd, "Seek End", new("end")),
            new(ActionId.GoToTime, "Go To Time", new("g", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.SoundCards, "Sound Cards", new("a", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.VolumeUp, "Volume Up", new("up")),
            new(ActionId.VolumeDown, "Volume Down", new("down")),
            new(ActionId.VolumeMaximize, "Volume Max", new("up", ShortcutModifiers.Shift)),
            new(ActionId.VolumeMinimize, "Volume Min", new("down", ShortcutModifiers.Shift)),
            new(ActionId.AnnounceVolume, "Announce Volume", new("v")),
            new(ActionId.AnnounceElapsed, "Announce Elapsed", new("e")),
            new(ActionId.AnnounceRemaining, "Announce Remaining", new("r")),
            new(ActionId.AnnounceDuration, "Announce Duration", new("t")),
            new(ActionId.AnnouncePercent, "Announce Percent", new("p")),
            new(ActionId.AnnounceSpeed, "Announce Speed", new("s")),
            new(ActionId.ToggleVerbosity, "Toggle Verbosity", new("v", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            new(ActionId.SpeedUp, "Speed Up", new("up", ShortcutModifiers.Control)),
            new(ActionId.SpeedDown, "Speed Down", new("down", ShortcutModifiers.Control)),
            new(ActionId.ResetSpeed, "Reset Speed", new("y", ShortcutModifiers.Alt)),
            new(ActionId.ToggleSilenceRemoval, "Silence Removal", new("m", ShortcutModifiers.Control)),
            new(ActionId.StartSelection, "Start Selection", new("[")),
            new(ActionId.EndSelection, "End Selection", new("]")),
            new(ActionId.ClearSelection, "Clear Selection", new("backspace")),
        };

        actions.AddRange(SeekSteps.Select(step => new ActionDefinition(
            step.Id,
            $"Seek Step: {step.Label}",
            new Shortcut(step.Key, ShortcutModifiers.Shift))));

        foreach (var jump in PercentJumps)
        {
            var digit = jump.Percent == 100 ? "0" : (jump.Percent / 10).ToString();
            var modifiers = jump.Percent % 10 == 5
                ? ShortcutModifiers.Control | ShortcutModifiers.Shift
                : ShortcutModifiers.Control;
            Shortcut? secondary = jump.Percent == 100
                ? new Shortcut("0", ShortcutModifiers.Control | ShortcutModifiers.Shift)
                : null;
            actions.Add(new ActionDefinition(
                jump.Id,
                $"Jump to {jump.Percent}%",
                new Shortcut(digit, modifiers),
                secondary));
        }

        return actions;
    }
}
