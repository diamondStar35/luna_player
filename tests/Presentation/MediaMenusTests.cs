using System.Runtime.ExceptionServices;
using LunaPlayer.Actions;
using LunaPlayer.Tests.Infrastructure;
using LunaPlayer.UI;
using WxSharp;

namespace LunaPlayer.Tests.Presentation;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class MediaMenusTests
{
    [TestMethod]
    public void SharedDownloadsAndCurrentServerActionsHaveIndependentAvailability()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                NativeHost.Initialize(); using var app = new App(); using var frame = new Frame(title: "Menu structure");
                Assert.AreEqual(ActionRegistry.All.Count, ActionRegistry.All.Select(action => action.Id).Distinct().Count());
                var shortcuts = new ShortcutManager(ActionRegistry.All);
                Assert.AreEqual(new Shortcut("b", ShortcutModifiers.Control), shortcuts.Get(ActionId.AddBookmark));
                Assert.AreEqual(new Shortcut("b", ShortcutModifiers.Control | ShortcutModifiers.Shift), shortcuts.Get(ActionId.ManageBookmarks));
                Assert.AreEqual(new Shortcut("m", ShortcutModifiers.Control | ShortcutModifiers.Shift), shortcuts.Get(ActionId.OpenMusicServers));
                var menus = new MenuController(frame, ActionRegistry.All, shortcuts, [], new([], [], []));
                var bar = frame.GetMenuBar()!;
                Assert.AreEqual(1, Enumerable.Range(0, bar.Count).Sum(index => CountCommands(bar[index], menus.CommandIds[ActionId.AddBookmark])));
                var downloads = new[] { ActionId.VideoDownload, ActionId.VideoDirectDownload, ActionId.AddToDownloadQueue };
                foreach (var action in downloads)
                {
                    var id = menus.CommandIds[action];
                    Assert.IsTrue(Enumerable.Range(0, bar[0].Count).Any(index => bar[0][index].Id == id));
                    Assert.IsFalse(Enumerable.Range(0, bar[6].Count).Any(index => bar[6][index].Id == id));
                }
                menus.SetVideoOptionsEnabled(false); menus.SetDownloadState(true);
                foreach (var action in downloads) Assert.IsTrue(bar.FindItem(menus.CommandIds[action])!.Enabled);
                menus.SetBookmarkState(false); menus.SetMusicServerState(true, favorite: false);
                Assert.IsTrue(bar.FindItem(menus.CommandIds[ActionId.AddBookmark])!.Enabled);
                Assert.IsFalse(bar.FindItem(menus.CommandIds[ActionId.ManageBookmarks])!.Enabled);
                foreach (var action in CurrentServerActions.All.Where(action => action.Id != ActionId.CurrentServerUnfavorite))
                    Assert.IsTrue(bar.FindItem(menus.CommandIds[action.Id])!.Enabled);
                Assert.IsFalse(bar.FindItem(menus.CommandIds[ActionId.CurrentServerUnfavorite])!.Enabled);
                menus.SetMusicServerState(true, favorite: true);
                Assert.IsFalse(bar.FindItem(menus.CommandIds[ActionId.CurrentServerFavorite])!.Enabled);
                Assert.IsTrue(bar.FindItem(menus.CommandIds[ActionId.CurrentServerUnfavorite])!.Enabled);
                menus.SetMusicServerState(true);
                Assert.IsFalse(bar.FindItem(menus.CommandIds[ActionId.CurrentServerFavorite])!.Enabled);
                Assert.IsFalse(bar.FindItem(menus.CommandIds[ActionId.CurrentServerUnfavorite])!.Enabled);
                menus.SetMusicServerState(false);
                foreach (var action in CurrentServerActions.All) Assert.IsFalse(bar.FindItem(menus.CommandIds[action.Id])!.Enabled);
                Assert.IsTrue(bar.FindItem(menus.CommandIds[ActionId.OpenMusicServers])!.Enabled);
                menus.SetBookmarkState(true); menus.SetMusicServerState(false);
                Assert.IsTrue(bar.FindItem(menus.CommandIds[ActionId.AddBookmark])!.Enabled);
            }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static int CountCommands(Menu menu, int command)
        => Enumerable.Range(0, menu.Count).Sum(index => (menu[index].Id == command ? 1 : 0)
            + (menu[index].SubMenu is { } child ? CountCommands(child, command) : 0));
}
