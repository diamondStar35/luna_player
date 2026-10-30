using System.Globalization;
using System.Text.Json;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Playback;

[TestClass, TestCategory("Unit")]
public sealed class SofaTests
{
    [TestMethod]
    public void DisabledSpatializationHasNoGraph()
        => Assert.AreEqual(string.Empty, AudioFilters.SofaGraph(new AudioSettings()));

    [TestMethod]
    public void GraphEscapesWindowsPathsAndUsesInvariantNumbers()
    {
        using var workspace = new Workspace();
        var path = workspace.File("HRTF's [test], semi; مثال.SOFA");
        var settings = new AudioSettings
        {
            SofaEnabled = true, SofaFile = path, SofaGain = -2.5, SofaRotation = 30.5, SofaElevation = -5.5,
        };
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var graph = AudioFilters.SofaGraph(settings);
            StringAssert.Contains(graph, "sofalizer=sofa=C\\\\:/");
            StringAssert.Contains(graph, "HRTF\\\\\\'s\\\\\\ \\[test\\]\\,\\\\\\ semi\\;\\\\\\ مثال.SOFA");
            StringAssert.Contains(graph, ":gain=-2.5:rotation=30.5:elevation=-5.5:type=freq");
            Assert.IsTrue(AudioFilters.IsSofaFile(path));
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    [TestMethod]
    public void ConfigurationSurvivesNavigationAndCanBeDisabled()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new AudioSettings { SofaEnabled = true, SofaFile = workspace.File("test.sofa") };
        Assert.IsTrue(player.ConfigureSofa(settings));
        var graph = engine.SofaGraph;
        Assert.IsTrue(player.OpenFiles(["one", "two"]));
        Assert.IsTrue(player.Next(false));
        Assert.AreEqual(graph, engine.SofaGraph);
        settings.SofaEnabled = false;
        Assert.IsTrue(player.ConfigureSofa(settings));
        Assert.AreEqual(string.Empty, engine.SofaGraph);
    }

    [TestMethod]
    public void MissingFileAndWrongExtensionLeaveTheCurrentGraphAlone()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new AudioSettings { SofaEnabled = true, SofaFile = workspace.File("test.sofa") };
        Assert.IsTrue(player.ConfigureSofa(settings));
        var graph = engine.SofaGraph;
        settings.SofaFile = Path.Combine(workspace.Root, "missing.sofa");
        Assert.IsFalse(player.ConfigureSofa(settings));
        settings.SofaFile = workspace.File("test.wav");
        Assert.IsFalse(player.ConfigureSofa(settings));
        Assert.AreEqual(graph, engine.SofaGraph);
    }

    [TestMethod]
    public void FilterFailureDoesNotEnableSpatialization()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine { RejectSofa = true };
        using var player = new Player(engine);
        var settings = new AudioSettings { SofaEnabled = true, SofaFile = workspace.File("test.sofa") };
        Assert.IsFalse(player.ConfigureSofa(settings));
        Assert.AreEqual(string.Empty, engine.SofaGraph);
    }

    [TestMethod]
    public void SettingsCopyAndApplyPreserveSpatialization()
    {
        var settings = new Settings();
        settings.Audio.SofaEnabled = true;
        settings.Audio.SofaFile = "test.sofa";
        settings.Audio.SofaGain = -3;
        settings.Audio.SofaRotation = 45;
        settings.Audio.SofaElevation = 10;
        var copy = settings.Copy();
        settings.Audio.SofaEnabled = false;
        settings.Audio.Apply(copy.Audio);
        Assert.IsTrue(settings.Audio.SofaEnabled);
        Assert.AreEqual("test.sofa", settings.Audio.SofaFile);
        Assert.AreEqual(-3d, settings.Audio.SofaGain);
        Assert.AreEqual(45d, settings.Audio.SofaRotation);
        Assert.AreEqual(10d, settings.Audio.SofaElevation);
    }

    [TestMethod]
    public void SpatializationSettingsRoundTripThroughTheGeneratedSerializer()
    {
        var settings = new Settings();
        settings.Audio.SofaEnabled = true;
        settings.Audio.SofaFile = "test.sofa";
        settings.Audio.SofaGain = -3.5;
        settings.Audio.SofaRotation = 45.5;
        settings.Audio.SofaElevation = 10.5;
        var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        var restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings)!;
        restored.ValidateStored();
        Assert.IsTrue(restored.Audio.SofaEnabled);
        Assert.AreEqual(settings.Audio.SofaFile, restored.Audio.SofaFile);
        Assert.AreEqual(settings.Audio.SofaGain, restored.Audio.SofaGain);
        Assert.AreEqual(settings.Audio.SofaRotation, restored.Audio.SofaRotation);
        Assert.AreEqual(settings.Audio.SofaElevation, restored.Audio.SofaElevation);
    }

    [TestMethod]
    public void LegacySettingsLeaveSpatializationDisabled()
    {
        var settings = JsonSerializer.Deserialize("{\"audio\":{\"volume\":80}}", SettingsJsonContext.Default.Settings)!;
        settings.ValidateStored();
        Assert.IsFalse(settings.Audio.SofaEnabled);
        Assert.AreEqual(string.Empty, settings.Audio.SofaFile);
    }

    [TestMethod]
    public void EnabledSpatializationRequiresAStoredFilePath()
    {
        var settings = new Settings();
        settings.Audio.SofaEnabled = true;
        Assert.ThrowsExactly<JsonException>(() => settings.ValidateStored());
    }

    [TestMethod]
    [DataRow(-21d, 0d, 0d)]
    [DataRow(41d, 0d, 0d)]
    [DataRow(0d, -361d, 0d)]
    [DataRow(0d, 361d, 0d)]
    [DataRow(0d, 0d, -91d)]
    [DataRow(0d, 0d, 91d)]
    [DataRow(double.NaN, 0d, 0d)]
    [DataRow(0d, double.PositiveInfinity, 0d)]
    public void InvalidSpatializationValuesAreRejected(double gain, double rotation, double elevation)
    {
        var settings = new Settings();
        settings.Audio.SofaGain = gain;
        settings.Audio.SofaRotation = rotation;
        settings.Audio.SofaElevation = elevation;
        Assert.ThrowsExactly<System.Text.Json.JsonException>(() => settings.ValidateStored());
    }
}
