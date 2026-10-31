using System.Runtime.ExceptionServices;
using System.Text.Json;
using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Application.Commands;
using LunaPlayer.Application.Commands.Playback;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;
using LunaPlayer.UI;
using LunaPlayer.UI.Audio;
using WxSharp;

namespace LunaPlayer.Tests.Presentation;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed class SpatializationTests
{
    [TestMethod]
    public void NativeMenuPreferencesAndCommandsKeepSpatializationAccessibleAndSessionOnly()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                NativeHost.Initialize();
                using var app = new App();
                VerifyMenusAndCommands();
                VerifyPreferences();
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void VerifyMenusAndCommands()
    {
        using var workspace = new Workspace();
        using var frame = new Frame(title: "Spatialization test");
        var shortcuts = new ShortcutManager(ActionRegistry.All);
        var menus = new MenuController(frame, ActionRegistry.All, shortcuts, [], new([], [], []));
        var bar = frame.GetMenuBar()!;
        var playerMenu = bar[5];
        var labels = Enumerable.Range(0, playerMenu.Count).Select(index => playerMenu[index].Label).ToArray();
        Assert.AreEqual("Headphone Spatialization", labels[Array.IndexOf(labels, "Pan") + 1]);
        MenuItem Item(ActionId id) => bar.FindItem(menus.CommandIds[id])!;
        var toggle = Item(ActionId.ToggleHeadphoneSpatialization);
        Assert.AreEqual(MenuItemKind.Check, toggle.Kind);
        Assert.AreEqual("Enable Headphone Spatialization\tCtrl+Shift+H", toggle.Label);
        Assert.IsFalse(toggle.Enabled);
        menus.SetMediaLoaded(true);
        Assert.IsFalse(toggle.Enabled);
        var expectedKeys = new Dictionary<ActionId, string>
        {
            [ActionId.ToggleHeadphoneSpatialization] = "h",
            [ActionId.RotateSofaLeft] = "left",
            [ActionId.RotateSofaRight] = "right",
            [ActionId.SofaElevationUp] = "up",
            [ActionId.SofaElevationDown] = "down",
        };
        foreach (var pair in expectedKeys)
        {
            var key = new Shortcut(pair.Value, ShortcutModifiers.Control | ShortcutModifiers.Shift);
            Assert.AreEqual(key, shortcuts.Get(pair.Key));
            Assert.HasCount(1, shortcuts.GetBindings().Where(binding => binding.Shortcut == key).ToArray());
            StringAssert.EndsWith(Item(pair.Key).Label, "\t" + key.ToDisplayString());
        }

        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new Settings();
        var speech = new Speech(settings);
        var router = new ActionRouter();
        _ = new Spatialization(router, player, settings.Audio, speech, menus.SetHeadphoneSpatializationState);
        router.EnsureComplete(ActionRegistry.All.Where(action => expectedKeys.ContainsKey(action.Id)));
        Assert.IsTrue(router.Execute(ActionId.ToggleHeadphoneSpatialization));
        Assert.IsFalse(toggle.Enabled);
        Assert.IsFalse(toggle.Checked);
        Assert.IsFalse(player.IsSofaEnabled);

        settings.Audio.SofaFile = workspace.File("test.sofa");
        settings.Audio.SofaRotation = 275;
        settings.Audio.SofaElevation = -10;
        settings.Audio.SofaElevationStep = 2.5;
        var originalVerbosity = settings.General.Verbosity;
        var saved = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        Assert.IsTrue(player.ConfigureSofa(settings.Audio));
        menus.SetHeadphoneSpatializationState(player.CanToggleSofa, player.IsSofaEnabled);
        Assert.IsTrue(toggle.Enabled);
        Assert.IsFalse(Item(ActionId.RotateSofaLeft).Enabled);
        Assert.IsTrue(router.Execute(ActionId.RotateSofaLeft));
        Assert.AreEqual(275d, player.SofaRotation);
        Assert.IsTrue(router.Execute(ActionId.ToggleHeadphoneSpatialization));
        Assert.IsTrue(toggle.Checked);
        Assert.IsTrue(Item(ActionId.RotateSofaLeft).Enabled);
        menus.SetMediaLoaded(false);
        Assert.IsTrue(toggle.Enabled);
        Assert.IsTrue(Item(ActionId.RotateSofaLeft).Enabled);

        settings.General.Verbosity = SpeechVerbosity.Beginner;
        Assert.IsTrue(router.Execute(ActionId.RotateSofaLeft));
        Assert.AreEqual("Rotation 270 degrees", speech.Last);
        Assert.IsTrue(router.Execute(ActionId.SofaElevationUp));
        Assert.AreEqual("Elevation -7.5 degrees", speech.Last);
        settings.General.Verbosity = SpeechVerbosity.Advanced;
        Assert.IsTrue(router.Execute(ActionId.RotateSofaRight));
        Assert.AreEqual("275 degrees", speech.Last);
        Assert.IsTrue(router.Execute(ActionId.SofaElevationDown));
        Assert.AreEqual("-10 degrees", speech.Last);
        settings.General.Verbosity = originalVerbosity;
        Assert.AreEqual(saved, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings));

        engine.RejectSofa = true;
        Assert.IsTrue(router.Execute(ActionId.RotateSofaLeft));
        Assert.AreEqual(275d, player.SofaRotation);
        StringAssert.Contains(speech.Last, "Could not change");
        Assert.IsTrue(router.Execute(ActionId.ToggleHeadphoneSpatialization));
        Assert.IsFalse(toggle.Checked);
        toggle.Checked = true; // A native check item can pre-toggle before dispatching a failed command.
        Assert.IsTrue(router.Execute(ActionId.ToggleHeadphoneSpatialization));
        Assert.IsFalse(toggle.Checked);
        Assert.IsFalse(Item(ActionId.SofaElevationUp).Enabled);
        Assert.AreEqual(saved, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings));

        // Clearing/reconfiguring preferences updates availability, independently of loaded media.
        settings.Audio.SofaFile = "";
        Assert.IsTrue(player.ConfigureSofa(settings.Audio));
        menus.SetHeadphoneSpatializationState(player.CanToggleSofa, player.IsSofaEnabled);
        Assert.IsFalse(toggle.Enabled);
        shortcuts.Set(ActionId.RotateSofaLeft, new Shortcut("l", ShortcutModifiers.Alt));
        menus.ApplyShortcuts(shortcuts);
        Assert.AreEqual("Rotate Left\tAlt+L", Item(ActionId.RotateSofaLeft).Label);
    }

    private static void VerifyPreferences()
    {
        using var workspace = new Workspace();
        using var frame = new Frame(title: "Spatialization preferences test");
        var settings = new AudioSettings();
        var page = new SofaPreferences(frame, settings);
        var children = page.Window.GetChildren().OfType<Window>().ToArray();
        var enabled = children.OfType<CheckBox>().Single();
        var dependent = children.Where(child => child != enabled).ToArray();
        Assert.IsTrue(enabled.Visible);
        Assert.IsTrue(dependent.All(child => !child.Visible));
        Window Field(string label)
        {
            var index = Array.FindIndex(children, child => child is StaticText text && text.Label == label);
            Assert.IsGreaterThanOrEqualTo(0, index);
            return children[index + 1];
        }
        void Toggle(bool value)
        {
            enabled.Checked = value;
            Assert.IsTrue(Wx.ProcessEvent(enabled, WxEvents.CheckBoxToggled, enabled.Id, value ? 1 : 0));
        }
        Toggle(true);
        Assert.IsTrue(dependent.All(child => child.Visible));
        Assert.AreEqual("Browse...", children.OfType<Button>().Single().Label);
        Assert.HasCount(4, children.OfType<SpinCtrl>().ToArray());
        Assert.HasCount(2, children.OfType<TextCtrl>().ToArray());
        var file = (TextCtrl)Field("SOFA file");
        var gain = (TextCtrl)Field("Gain (dB)");
        var rotation = (SpinCtrl)Field("Rotation (degrees)");
        var elevation = (SpinCtrl)Field("Elevation (degrees)");
        var rotationStep = (SpinCtrl)Field("Rotation step (degrees)");
        var elevationStep = (SpinCtrl)Field("Elevation step (degrees)");
        Assert.AreEqual(-360, rotation.Minimum);
        Assert.AreEqual(360, rotation.Maximum);
        Assert.AreEqual(-90, elevation.Minimum);
        Assert.AreEqual(90, elevation.Maximum);
        Assert.AreEqual(1, rotationStep.Minimum);
        Assert.AreEqual(90, rotationStep.Maximum);
        Assert.AreEqual(1, elevationStep.Minimum);
        Assert.AreEqual(90, elevationStep.Maximum);
        Assert.AreEqual(5, rotationStep.Value);
        Assert.AreEqual(5, elevationStep.Value);
        StringAssert.Contains(page.GetContextHelp(rotationStep), "1 to 90");
        StringAssert.Contains(page.GetContextHelp(elevationStep), "1 to 90");
        file.Value = workspace.File("test.sofa");
        foreach (var step in new[] { rotationStep, elevationStep })
        {
            step.Value = 0;
            Assert.AreEqual(1, step.Value);
            step.Value = 91;
            Assert.AreEqual(90, step.Value);
        }
        rotation.Value = -360;
        elevation.Value = 90;
        rotationStep.Value = 1;
        elevationStep.Value = 90;
        gain.Value = "-2.5";
        Assert.IsNull(page.Validate());
        Toggle(false);
        Assert.IsTrue(dependent.All(child => !child.Visible));
        Assert.IsNull(page.Validate());
        page.Apply();
        Assert.IsFalse(settings.SofaEnabled);
        Assert.AreEqual(file.Value, settings.SofaFile);
        Assert.AreEqual(-2.5, settings.SofaGain);
        Assert.AreEqual(-360d, settings.SofaRotation);
        Assert.AreEqual(90d, settings.SofaElevation);
        Assert.AreEqual(1d, settings.SofaRotationStep);
        Assert.AreEqual(90d, settings.SofaElevationStep);
        // An invalid hidden gain draft neither blocks saving nor overwrites the last valid value.
        gain.Value = "invalid";
        Assert.IsNull(page.Validate());
        page.Apply();
        Assert.AreEqual(1d, settings.SofaRotationStep);
        Assert.AreEqual(-2.5, settings.SofaGain);
        settings.SofaRotation = 12.5;
        settings.SofaElevation = -4.5;
        settings.SofaRotationStep = 2.5;
        settings.SofaElevationStep = 1.5;
        settings.SofaGain = -2.75;
        settings.SofaEnabled = true;
        page.Refresh();
        Assert.IsTrue(dependent.All(child => child.Visible));
        Assert.AreEqual(13, rotation.Value);
        Assert.AreEqual(-5, elevation.Value);
        Assert.AreEqual(3, rotationStep.Value);
        Assert.AreEqual(2, elevationStep.Value);
        Assert.AreEqual("-2.75", gain.Value);
        Assert.IsNull(page.Validate());
        page.Apply();
        Assert.AreEqual(13d, settings.SofaRotation);
        Assert.AreEqual(-5d, settings.SofaElevation);
        Assert.AreEqual(3d, settings.SofaRotationStep);
        Assert.AreEqual(2d, settings.SofaElevationStep);
        Assert.AreEqual(-2.75, settings.SofaGain);
        settings.SofaEnabled = false;
        page.Refresh();
        Assert.IsTrue(dependent.All(child => !child.Visible));
    }

    private sealed class Speech(Settings settings) : ISpeechOutput
    {
        internal string Last { get; private set; } = "";
        public void Speak(string beginnerText, string? advancedText = null, bool interrupt = true)
            => Last = settings.General.Verbosity == SpeechVerbosity.Advanced ? advancedText ?? beginnerText : beginnerText;
        public void SpeakText(string text, bool interrupt = true) => Last = text;
        public void Dispose() { }
    }
}
