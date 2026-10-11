namespace LunaPlayer.Actions;

/// <summary>The commands for playing videos from YouTube.</summary>
///
/// <remarks>
/// Kept apart from <see cref="MediaActions"/> because they are a feature that can be finished,
/// changed or taken away as a whole, and because the three video commands share a condition none of the
/// others have: they apply only while what is playing came from YouTube.
/// </remarks>
internal static class YouTubeActions
{
    internal static IReadOnlyList<ActionDefinition> All { get; } =
    [
        // Translators: Name of the command that asks for a YouTube address and plays what it points at.
        new(ActionId.OpenYouTubeLink, Tr("Open a YouTube link"),
            new("y", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
        // Translators: Name of the command that asks what to look for on YouTube and lists what it finds.
        new(ActionId.SearchYouTube, Tr("Search YouTube"), new("y", ShortcutModifiers.Control)),
        // Translators: Name of the command that opens the window listing the links the user has saved.
        new(ActionId.OpenFavorites, Tr("Favorite videos dialog"),
            new("f", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
        // Translators: Name of the command that saves the video being played to a folder on this computer.
        new(ActionId.VideoDownload, Tr("Download the current video"), new("d", ShortcutModifiers.Control)),
        // Translators: Name of the command that shows the text the uploader wrote under the video.
        new(ActionId.VideoDescription, Tr("Show the video description"), new("d", ShortcutModifiers.Alt)),
        // Translators: Name of the command that copies the address of the video being played.
        new(ActionId.VideoCopyLink, Tr("Copy the video link"),
            new("c", ShortcutModifiers.Control | ShortcutModifiers.Shift)),
        // Translators: Name of the command that opens the video being played in the web browser.
        new(ActionId.VideoOpenInBrowser, Tr("Open the current video in browser")),
        // Translators: Name of the command that opens the channel of the video being played in the web browser.
        new(ActionId.VideoOpenChannelInBrowser, Tr("Open the channel in browser")),
        // Translators: Name of the command that opens the channel of the video being played inside the player.
        new(ActionId.VideoGoToChannel, Tr("Go to channel")),
        // Translators: Name of the command that fetches a newer yt-dlp. "yt-dlp" is a program name and is
        // not translated.
        new(ActionId.UpdateYouTubeComponents, Tr("Update YouTube components")),
    ];
}
