using System.Runtime.ExceptionServices;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Equalizer;
using LunaPlayer.Tests.Infrastructure;
using LunaPlayer.UI;
using WxSharp;

namespace LunaPlayer.Tests.Presentation;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class EqualizerMenuTests
{
    [TestMethod]
    public void RebuildingPresetsPreservesPermanentCommandsAndRadioSelection()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                NativeHost.Initialize();
                using var app = new App();
                using var frame = new Frame(title: "Equalizer menu test");
                var shortcuts = new ShortcutManager(ActionRegistry.All);
                shortcuts.Set(ActionId.EditEqualizerPreset, new Shortcut("e", ShortcutModifiers.Alt));
                var menus = new MenuController(frame, ActionRegistry.All, shortcuts, [], new([], [], []));
                var bar = frame.GetMenuBar()!;
                var editId = menus.CommandIds[ActionId.EditEqualizerPreset];
                var manageId = menus.CommandIds[ActionId.ManageEqualizerPresets];
                // Disabled state is attached to the native item and must survive rebuilding presets.
                bar.FindItem(editId)!.Enabled = false;
                bar.FindItem(manageId)!.Enabled = false;
                for (var cycle = 0; cycle < 3; cycle++)
                {
                    PresetEntry[] presets = [new("one", "First", false), new("two", "Second", false)];
                    menus.RebuildEqualizerMenu(presets, "two");
                    Verify(presets, "two");
                    menus.RebuildEqualizerMenu([new("two", "Renamed", false)], "two");
                    Verify([new("two", "Renamed", false)], "two");
                    menus.RebuildEqualizerMenu([], null);
                    Verify([], null);
                }

                void Verify(PresetEntry[] presets, string? selected)
                {
                    Assert.IsFalse(bar.FindItem(editId)!.Enabled);
                    Assert.IsFalse(bar.FindItem(manageId)!.Enabled);
                    StringAssert.EndsWith(bar.FindItem(editId)!.Label, "\tAlt+E");
                    Assert.IsTrue(menus.TryGetAction(editId, out var action));
                    Assert.AreEqual(ActionId.EditEqualizerPreset, action);
                    var player = bar[5];
                    var menu = Enumerable.Range(0, player.Count).Select(i => player[i])
                        .Single(item => item.Label == "Equalizer").SubMenu!;
                    Assert.AreEqual(presets.Length + 4, menu.Count);
                    for (var i = 0; i <= presets.Length; i++)
                    {
                        var item = menu[i];
                        string? expected = i == 0 ? null : presets[i - 1].Id;
                        Assert.AreEqual(MenuItemKind.Radio, item.Kind);
                        Assert.AreEqual(i == 0 ? "Off" : presets[i - 1].Name, item.Label);
                        Assert.IsTrue(menus.TryGetEqualizerPreset(item.Id, out var actual));
                        Assert.AreEqual(expected, actual);
                        Assert.AreEqual(expected == selected, item.Checked);
                    }
                    Assert.AreEqual(MenuItemKind.Separator, menu[presets.Length + 1].Kind);
                    Assert.AreEqual(editId, menu[presets.Length + 2].Id);
                    Assert.AreEqual(manageId, menu[presets.Length + 3].Id);
                }
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
