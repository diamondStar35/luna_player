using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Tests.Infrastructure;
namespace LunaPlayer.Tests.Configuration;

[TestClass, TestCategory("Unit")]
public sealed class StorageTests
{
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
