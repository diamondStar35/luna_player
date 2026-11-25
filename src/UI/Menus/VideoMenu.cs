using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI.Menus;

internal static partial class Builder
{
    private sealed record VideoMenuParts(Menu Menu, IReadOnlyList<MenuItem> Items);

    private static VideoMenuParts BuildVideoMenu(
        IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        var videoItems = new List<MenuItem>();
        var videoMenu = new Menu();
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
