using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Tests.Infrastructure;
namespace LunaPlayer.Tests.Configuration;

[TestClass, TestCategory("Unit")]
public sealed class StorageTests
{
    [TestMethod]
    [DataRow("{}", false)]
    [DataRow("{\"general\":{}}", false)]
    [DataRow("{\"general\":{\"startMaximized\":true}}", true)]
    [DataRow("{\"general\":{\"startMaximized\":false}}", false)]
    [DataRow("{\"general\":{\"startMaximized\":\"true\"}}", false)]
    [DataRow("{\"general\":null}", false)]
    [DataRow("[]", false)]
    [DataRow("invalid json", false)]
    public void StartupWindowPreferenceUsesOnlyAnExplicitTrue(string json, bool expected)
    {
        using var workspace = new Workspace();
        var path = workspace.File("settings.json", json);
        Assert.AreEqual(expected, SettingsStore.PeekStartMaximized(path));
    }

    [TestMethod]
    public void MissingSettingsKeepTheNormalWindowSize()
    {
        using var workspace = new Workspace();
        Assert.IsFalse(SettingsStore.PeekStartMaximized(Path.Combine(workspace.Root, "missing.json")));
        Assert.IsFalse(new Settings().General.StartMaximized);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void StartupWindowPreferenceSurvivesCopyApplyAndSerialization(bool maximized)
    {
        using var workspace = new Workspace();
        var settings = new Settings();
        settings.General.StartMaximized = maximized;
        var applied = new GeneralSettings { StartMaximized = !maximized };
        applied.Apply(settings.Copy().General);
        Assert.AreEqual(maximized, applied.StartMaximized);
        settings.General = applied;
        var json = System.Text.Json.JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        var path = workspace.File("settings.json", json);
        Assert.AreEqual(maximized, SettingsStore.PeekStartMaximized(path));
        var loaded = System.Text.Json.JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings)!;
        Assert.AreEqual(maximized, loaded.General.StartMaximized);
    }

    [TestMethod]
    public void AtomicSaveReplacesTheWholeFile()
    {
        using var workspace = new Workspace();
        var path = workspace.File("settings.txt", "old long contents");
        Assert.IsTrue(AtomicFile.WriteBytes(path, "new"u8, out var error), error);
        Assert.AreEqual("new", File.ReadAllText(path));
    }
    [TestMethod]
    public void FailedAtomicSaveReportsTheError()
    {
        using var workspace = new Workspace();
        var parent = workspace.File("not-a-directory", "keep");
        Assert.IsFalse(AtomicFile.WriteBytes(Path.Combine(parent, "child"), "new"u8, out var error));
        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        Assert.AreEqual("keep", File.ReadAllText(parent));
    }
    [TestMethod]
    public void PositionRoundTripDoesNotAffectOtherFiles()
    {
        using var workspace = new Workspace();
        var store = new PositionStore(Path.Combine(workspace.Root, "positions.json"));
        var one = workspace.File("one.mp3");
        var two = workspace.File("two.mp3");
        Assert.IsTrue(store.Set(one, 12.5));
        Assert.IsTrue(store.Set(two, 20));
        Assert.AreEqual(12.5, store.Get(one));
        Assert.AreEqual(20d, store.Get(two));
    }
    [TestMethod]
    public void SettingsCopyDoesNotShareMutableAudioState()
    {
        var original = new Settings();
        var copy = original.Copy();
        copy.Audio.Volume = 25;
        Assert.AreEqual(100d, original.Audio.Volume);
    }

    [TestMethod]
    [DataRow("", true)]
    [DataRow(" \t ", true)]
    [DataRow("", false)]
    [DataRow(" \t ", false)]
    public void StartupRecoversEmptySofaPathsWithoutDiscardingOtherSettings(string sofaFile, bool enabled)
    {
        using var workspace = new Workspace();
        var settings = new Settings();
        settings.General.StartMaximized = true;
        settings.Audio.Volume = 45;
        settings.Audio.SofaEnabled = enabled;
        settings.Audio.SofaFile = sofaFile;
        settings.Audio.SofaGain = -2.5;
        var json = System.Text.Json.JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        var path = workspace.File("settings.json", json);

        var store = new SettingsStore(path);
        var loaded = store.LoadForStartup();

        Assert.AreEqual(string.Empty, store.LastError);
        Assert.IsFalse(loaded.Audio.SofaEnabled);
        Assert.AreEqual(sofaFile, loaded.Audio.SofaFile);
        Assert.AreEqual(-2.5, loaded.Audio.SofaGain);
        Assert.AreEqual(45d, loaded.Audio.Volume);
        Assert.IsTrue(loaded.General.StartMaximized);
        Assert.AreEqual(json, File.ReadAllText(path));
    }

    [TestMethod]
    [DataRow(-1d)]
    [DataRow(2001d)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    public void InvalidVolumeIsRejectedBeforeSettingsAreSaved(double volume)
    {
        var settings = new Settings(); settings.Audio.Volume = volume;
        Assert.ThrowsExactly<System.Text.Json.JsonException>(() => settings.ValidateStored());
    }
}
