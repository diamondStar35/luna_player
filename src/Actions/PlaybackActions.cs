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
            // Translators: Name of the command that starts playing, or pauses playing, in the shortcuts list.
            new(ActionId.PlayPause, Tr("Play or pause"), new("space"), new("enter")),
            // Translators: Name of the command that moves back in the file by the chosen seek step.
            new(ActionId.SeekBackward, Tr("Rewind by one step"), new("left")),
            // Translators: Name of the command that moves forward in the file by the chosen seek step.
            new(ActionId.SeekForward, Tr("Fast forward by one step"), new("right")),
            // Translators: Name of the command that moves back by twice the chosen seek step.
            new(ActionId.SeekBackwardX2, Tr("Rewind by two steps"), new("left", ShortcutModifiers.Shift)),
            // Translators: Name of the command that moves forward by twice the chosen seek step.
            new(ActionId.SeekForwardX2, Tr("Fast forward by two steps"), new("right", ShortcutModifiers.Shift)),
            // Translators: Name of the command that moves back by four times the chosen seek step.
            new(ActionId.SeekBackwardX4, Tr("Rewind by four steps"), new("left", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that moves forward by four times the chosen seek step.
            new(ActionId.SeekForwardX4, Tr("Fast forward by four steps"), new("right", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that jumps to the very start of the file.
            new(ActionId.SeekStart, Tr("Beginning of the file"), new("home")),
            // Translators: Name of the command that jumps to the very end of the file.
            new(ActionId.SeekEnd, Tr("End of the file"), new("end")),
            // Translators: Name of the command that opens the window asking for a time to jump to.
            new(ActionId.GoToTime, Tr("Go to time dialog"), new("g", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that opens the window listing the audio output devices to play through.
            new(ActionId.SoundCards, Tr("Sound cards dialog"), new("a", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that makes the sound louder.
            new(ActionId.VolumeUp, Tr("Increase volume"), new("up")),
            // Translators: Name of the command that makes the sound quieter.
            new(ActionId.VolumeDown, Tr("Decrease volume"), new("down")),
            // Translators: Name of the command that sets the volume to the loudest setting in one step.
            new(ActionId.VolumeMaximize, Tr("Set volume to maximum"), new("up", ShortcutModifiers.Shift)),
            // Translators: Name of the command that sets the volume to the quietest setting in one step.
            new(ActionId.VolumeMinimize, Tr("Set volume to minimum"), new("down", ShortcutModifiers.Shift)),
            // Translators: Name of the command that speaks the current volume.
            new(ActionId.AnnounceVolume, Tr("Speak current volume"), new("v")),
            // Translators: Name of the command that speaks how much of the file has already played.
            new(ActionId.AnnounceElapsed, Tr("Speak elapsed time"), new("e")),
            // Translators: Name of the command that speaks how much of the file is left to play.
            new(ActionId.AnnounceRemaining, Tr("Speak remaining time"), new("r")),
            // Translators: Name of the command that speaks the total length of the file.
            new(ActionId.AnnounceDuration, Tr("Speak total duration"), new("t")),
            // Translators: Name of the command that speaks how far through the file playing has reached, as a percentage.
            new(ActionId.AnnouncePercent, Tr("Speak position as a percentage"), new("p")),
            // Translators: Name of the command that speaks how fast the file is playing.
            new(ActionId.AnnounceSpeed, Tr("Speak playback speed"), new("s")),
            // Translators: Name of the command that switches between short and detailed spoken announcements.
            new(ActionId.ToggleVerbosity, Tr("Switch between brief and detailed announcements"), new("v", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
            // Translators: Name of the command that plays the file faster.
            new(ActionId.SpeedUp, Tr("Increase playback speed"), new("up", ShortcutModifiers.Control)),
            // Translators: Name of the command that plays the file slower.
            new(ActionId.SpeedDown, Tr("Decrease playback speed"), new("down", ShortcutModifiers.Control)),
            // Translators: Name of the command that returns the playing speed to normal.
            new(ActionId.ResetSpeed, Tr("Reset playback speed to normal"), new("y", ShortcutModifiers.Alt)),
            // Translators: Name of the command that turns skipping the silent parts of the file on or off.
            new(ActionId.ToggleSilenceRemoval, Tr("Turn silence removal on or off"), new("m", ShortcutModifiers.Control)),
            // Translators: Name of the command that marks the beginning of a part of the file to play on its own.
            new(ActionId.StartSelection, Tr("Mark start of selection"), new("[")),
            // Translators: Name of the command that marks the end of a part of the file to play on its own.
            new(ActionId.EndSelection, Tr("Mark end of selection"), new("]")),
            // Translators: Name of the command that forgets the marked part and plays the whole file again.
            new(ActionId.ClearSelection, Tr("Clear the selection"), new("backspace")),
        };

        actions.AddRange(SeekSteps.Select(step => new ActionDefinition(
            step.Id,
            // Translators: Name of the command that chooses how far a single rewind or fast forward moves.
            // {amount} is one of the seek step amounts, such as "10 seconds".
            TrFormat("Set seek step to {amount}", step.Label),
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
                TrFormat("Jump to {percent}% of the file", jump.Percent),
                new Shortcut(digit, modifiers),
                secondary));
        }

        return actions;
    }
}
