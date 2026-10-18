using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI;

internal static partial class MainMenuBuilder
{
    private sealed record VideoMenuParts(Menu Menu, IReadOnlyList<MenuItem> Items);

    private static VideoMenuParts BuildVideoMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        var videoItems = new List<MenuItem>();
        var videoMenu = new Menu();
        // Translators: Item in the Video options menu that opens the download window to choose how the video being played is saved.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoDownload, Tr("Download..."));
        // Translators: Item in the Video options menu that saves the video being played straight to disk with the saved download settings.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoDirectDownload, Tr("Direct download"));
        // Translators: Item in the Video options menu that adds the video being played to the download queue.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.AddToDownloadQueue, Tr("Add to download queue"));
        // Translators: Item in the Video options menu that shows the text the uploader wrote under the video.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoDescription, Tr("Video description..."));
        // Translators: Item in the Video options menu that copies the address of the video being played.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoCopyLink, Tr("Copy video link"));
        // Translators: Item in the Video options menu that opens the video being played in the web browser.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoOpenInBrowser, Tr("Open video in browser"));
        // Translators: Item in the Video options menu that opens the channel of the video being played in the web browser.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoOpenChannelInBrowser, Tr("Open channel in browser"));
        // Translators: Item in the Video options menu that opens the channel of the video being played inside the player.
        Add(videoMenu, videoItems, commandIds, shortcuts, ActionId.VideoGoToChannel, Tr("Go to channel"));
        return new VideoMenuParts(videoMenu, videoItems);
    }
}
