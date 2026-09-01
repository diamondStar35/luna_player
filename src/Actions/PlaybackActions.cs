namespace LunaPlayer.Actions;

internal readonly record struct SeekStepAction(ActionId Id, string Key, string Label, double Seconds);
internal readonly record struct PercentJumpAction(ActionId Id, int Percent);

internal static class PlaybackActionDefinitions
{
    internal static IReadOnlyList<SeekStepAction> SeekSteps { get; } =
    [
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep1, "1", Tr("1 second"), 1),
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep2, "2", Tr("5 seconds"), 5),
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep3, "3", Tr("10 seconds"), 10),
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep4, "4", Tr("20 seconds"), 20),
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep5, "5", Tr("30 seconds"), 30),
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep6, "6", Tr("1 minute"), 60),
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep7, "7", Tr("2 minutes"), 120),
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep8, "8", Tr("3 minutes"), 180),
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep9, "9", Tr("5 minutes"), 300),
        // Translators: One of the amounts a single seek can move by, offered in the seek step list and spoken when it is chosen.
        new(ActionId.SeekStep0, "0", Tr("10 minutes"), 600),
        // Translators: The last entry in the seek step list: it asks the user for a number of seconds instead of using a preset amount.
        new(ActionId.SeekStepCustom, "-", Tr("Custom value"), 0),
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
            // Translators: Name of the command that starts playing, or pauses playing, in the menus and the shortcuts list.
            new(ActionId.PlayPause, Tr("Play/Pause"), new("space"), new("enter")),
            // Translators: Name of the command that moves back in the file by one seek step.
            new(ActionId.SeekBackward, Tr("Seek Backward"), new("left")),
            // Translators: Name of the command that moves forward in the file by one seek step.
            new(ActionId.SeekForward, Tr("Seek Forward"), new("right")),
            // Translators: Name of the command that moves back by twice the seek step.
            // "x2" means two times and is usually left as it is.
            new(ActionId.SeekBackwardX2, Tr("Seek Backward x2"), new("left", ShortcutModifiers.Control)),
            // Translators: Name of the command that moves forward by twice the seek step.
            // "x2" means two times and is usually left as it is.
            new(ActionId.SeekForwardX2, Tr("Seek Forward x2"), new("right", ShortcutModifiers.Control)),
            // Translators: Name of the command that moves back by four times the seek step.
            // "x4" means four times and is usually left as it is.
            new(ActionId.SeekBackwardX4, Tr("Seek Backward x4"), new("left", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that moves forward by four times the seek step.
            // "x4" means four times and is usually left as it is.
            new(ActionId.SeekForwardX4, Tr("Seek Forward x4"), new("right", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that moves back by eight times the seek step.
            // "x8" means eight times and is usually left as it is.
            new(ActionId.SeekBackwardX8, Tr("Seek Backward x8"), new("left", ShortcutModifiers.Shift)),
            // Translators: Name of the command that moves forward by eight times the seek step.
            // "x8" means eight times and is usually left as it is.
            new(ActionId.SeekForwardX8, Tr("Seek Forward x8"), new("right", ShortcutModifiers.Shift)),
            // Translators: Name of the command that jumps to the beginning of the file.
            new(ActionId.SeekStart, Tr("Seek Start"), new("home")),
            // Translators: Name of the command that jumps to the end of the file.
            new(ActionId.SeekEnd, Tr("Seek End"), new("end")),
            // Translators: Name of the command that asks for a time and jumps to it.
            new(ActionId.GoToTime, Tr("Go To Time"), new("g", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that opens the list of audio output devices to play through.
            new(ActionId.SoundCards, Tr("Sound Cards"), new("a", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that makes the sound louder.
            new(ActionId.VolumeUp, Tr("Volume Up"), new("up")),
            // Translators: Name of the command that makes the sound quieter.
            new(ActionId.VolumeDown, Tr("Volume Down"), new("down")),
            // Translators: Name of the command that sets the volume to the loudest setting.
            new(ActionId.VolumeMaximize, Tr("Volume Max"), new("up", ShortcutModifiers.Shift)),
            // Translators: Name of the command that sets the volume to the quietest setting.
            new(ActionId.VolumeMinimize, Tr("Volume Min"), new("down", ShortcutModifiers.Shift)),
            // Translators: Name of the command that speaks the current volume.
            new(ActionId.AnnounceVolume, Tr("Announce Volume"), new("v")),
            // Translators: Name of the command that speaks how much of the file has already played.
            new(ActionId.AnnounceElapsed, Tr("Announce Elapsed"), new("e")),
            // Translators: Name of the command that speaks how much of the file is left to play.
            new(ActionId.AnnounceRemaining, Tr("Announce Remaining"), new("r")),
            // Translators: Name of the command that speaks the total length of the file.
            new(ActionId.AnnounceDuration, Tr("Announce Duration"), new("t")),
            // Translators: Name of the command that speaks how far through the file playing has reached, as a percentage.
            new(ActionId.AnnouncePercent, Tr("Announce Percent"), new("p")),
            // Translators: Name of the command that speaks the current playing speed.
            new(ActionId.AnnounceSpeed, Tr("Announce Speed"), new("s")),
            // Translators: Name of the command that switches between short and detailed spoken announcements.
            new(ActionId.ToggleVerbosity, Tr("Toggle Verbosity"), new("v", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that plays the file faster.
            new(ActionId.SpeedUp, Tr("Speed Up"), new("up", ShortcutModifiers.Control)),
            // Translators: Name of the command that plays the file slower.
            new(ActionId.SpeedDown, Tr("Speed Down"), new("down", ShortcutModifiers.Control)),
            // Translators: Name of the command that returns the playing speed to normal.
            new(ActionId.ResetSpeed, Tr("Reset Speed"), new("y", ShortcutModifiers.Alt)),
            // Translators: Name of the command that turns skipping silent parts of the file on or off.
            new(ActionId.ToggleSilenceRemoval, Tr("Silence Removal"), new("m", ShortcutModifiers.Control)),
            // Translators: Name of the command that marks the beginning of a part of the file to play on its own.
            new(ActionId.StartSelection, Tr("Start Selection"), new("[")),
            // Translators: Name of the command that marks the end of a part of the file to play on its own.
            new(ActionId.EndSelection, Tr("End Selection"), new("]")),
            // Translators: Name of the command that forgets the marked part and plays the whole file again.
            new(ActionId.ClearSelection, Tr("Clear Selection"), new("backspace")),
        };

        actions.AddRange(SeekSteps.Select(step => new ActionDefinition(
            step.Id,
            // Translators: Name of the command that sets how far one seek moves, in the menus and the shortcuts list.
            // {amount} is one of the seek step amounts, such as "10 seconds".
            TrFormat("Seek Step: {amount}", step.Label),
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
                // Translators: Name of the command that jumps to a position given as a percentage of the file.
                // {percent} is a whole number from 10 to 100.
                TrFormat("Jump to {percent}%", jump.Percent),
                new Shortcut(digit, modifiers),
                secondary));
        }

        return actions;
    }
}
