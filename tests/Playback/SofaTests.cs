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
            StringAssert.Contains(graph, ":gain=-2.5:rotation=-30.5:elevation=-5.5:type=freq");
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
    public void SofaPathsArePassedToTheEngineWithoutFileValidation()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new AudioSettings { SofaEnabled = true, SofaFile = workspace.File("test.sofa") };
        Assert.IsTrue(player.ConfigureSofa(settings));
        var graph = engine.SofaGraph;
        settings.SofaFile = Path.Combine(workspace.Root, "missing.sofa");
        Assert.IsTrue(player.ConfigureSofa(settings));
        Assert.AreNotEqual(graph, engine.SofaGraph);
        settings.SofaFile = workspace.File("test.wav");
        Assert.IsTrue(player.ConfigureSofa(settings));
        Assert.AreEqual(AudioFilters.SofaGraph(settings), engine.SofaGraph);
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
        settings.Audio.SofaRotationStep = 2.5;
        settings.Audio.SofaElevationStep = 15;
        var copy = settings.Copy();
        settings.Audio.SofaEnabled = false;
        settings.Audio.Apply(copy.Audio);
        Assert.IsTrue(settings.Audio.SofaEnabled);
        Assert.AreEqual("test.sofa", settings.Audio.SofaFile);
        Assert.AreEqual(-3d, settings.Audio.SofaGain);
        Assert.AreEqual(45d, settings.Audio.SofaRotation);
        Assert.AreEqual(10d, settings.Audio.SofaElevation);
        Assert.AreEqual(2.5, settings.Audio.SofaRotationStep);
        Assert.AreEqual(15d, settings.Audio.SofaElevationStep);
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
        settings.Audio.SofaRotationStep = 1.5;
        settings.Audio.SofaElevationStep = 90;
        var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        var restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings)!;
        restored.ValidateStored();
        Assert.IsTrue(restored.Audio.SofaEnabled);
        Assert.AreEqual(settings.Audio.SofaFile, restored.Audio.SofaFile);
        Assert.AreEqual(settings.Audio.SofaGain, restored.Audio.SofaGain);
        Assert.AreEqual(settings.Audio.SofaRotation, restored.Audio.SofaRotation);
        Assert.AreEqual(settings.Audio.SofaElevation, restored.Audio.SofaElevation);
        Assert.AreEqual(settings.Audio.SofaRotationStep, restored.Audio.SofaRotationStep);
        Assert.AreEqual(settings.Audio.SofaElevationStep, restored.Audio.SofaElevationStep);
    }

    [TestMethod]
    public void LegacySettingsLeaveSpatializationDisabled()
    {
        var settings = JsonSerializer.Deserialize("{\"audio\":{\"volume\":80}}", SettingsJsonContext.Default.Settings)!;
        settings.ValidateStored();
        Assert.IsFalse(settings.Audio.SofaEnabled);
        Assert.AreEqual(string.Empty, settings.Audio.SofaFile);
        Assert.AreEqual(5d, settings.Audio.SofaRotationStep);
        Assert.AreEqual(5d, settings.Audio.SofaElevationStep);
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

    [TestMethod]
    [DataRow(0d)]
    [DataRow(0.99d)]
    [DataRow(90.01d)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    public void InvalidStepsAreRejected(double step)
    {
        var settings = new Settings();
        settings.Audio.SofaRotationStep = step;
        Assert.ThrowsExactly<JsonException>(() => settings.ValidateStored());
        settings.Audio.SofaRotationStep = 5;
        settings.Audio.SofaElevationStep = step;
        Assert.ThrowsExactly<JsonException>(() => settings.ValidateStored());
    }

    [TestMethod]
    [DataRow(1d)]
    [DataRow(2.5d)]
    [DataRow(90d)]
    public void ValidStepsAreAccepted(double step)
    {
        var settings = new Settings();
        settings.Audio.SofaRotationStep = step;
        settings.Audio.SofaElevationStep = step;
        settings.ValidateStored();
    }

    [TestMethod]
    public void RuntimeChangesDoNotAlterOrPersistDefaults()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new Settings();
        settings.Audio.SofaFile = workspace.File("test.sofa");
        settings.Audio.SofaRotation = 45;
        settings.Audio.SofaElevation = -10;
        var saved = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
        Assert.IsTrue(player.ConfigureSofa(settings.Audio));
        Assert.IsTrue(player.CanToggleSofa);
        Assert.IsFalse(player.IsSofaEnabled);
        Assert.AreEqual(45d, player.SofaRotation);
        Assert.AreEqual(-10d, player.SofaElevation);
        Assert.IsTrue(player.SetSofaEnabled(true));
        Assert.IsTrue(player.ChangeSofaRotation(-5));
        Assert.IsTrue(player.ChangeSofaElevation(5));
        Assert.AreEqual(40d, player.SofaRotation);
        Assert.AreEqual(-5d, player.SofaElevation);
        var graph = engine.SofaGraph;
        Assert.IsTrue(player.OpenFiles(["one", "two"]));
        Assert.IsTrue(player.Next(false));
        Assert.AreEqual(graph, engine.SofaGraph);
        Assert.AreEqual(saved, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings));
        Assert.IsTrue(player.SetSofaEnabled(false));
        Assert.IsTrue(player.SetSofaEnabled(true));
        Assert.AreEqual(40d, player.SofaRotation);
        Assert.IsTrue(player.ConfigureSofa(settings.Audio));
        Assert.IsFalse(player.IsSofaEnabled);
        Assert.AreEqual(45d, player.SofaRotation);
        Assert.AreEqual(-10d, player.SofaElevation);
    }

    [TestMethod]
    [DataRow(0d, -5d, 355d)]
    [DataRow(355d, 5d, 0d)]
    [DataRow(-90d, -5d, 265d)]
    [DataRow(-360d, -5d, 355d)]
    [DataRow(360d, 5d, 5d)]
    [DataRow(359d, 2.5d, 1.5d)]
    public void RuntimeRotationWraps(double initial, double delta, double expected)
    {
        using var workspace = new Workspace();
        using var player = new Player(new FakeEngine());
        Assert.IsTrue(player.ConfigureSofa(new AudioSettings
            { SofaEnabled = true, SofaFile = workspace.File("test.sofa"), SofaRotation = initial }));
        Assert.IsTrue(player.ChangeSofaRotation(delta));
        Assert.AreEqual(expected, player.SofaRotation);
    }

    [TestMethod]
    [DataRow(88d, 5d, 90d)]
    [DataRow(-88d, -5d, -90d)]
    [DataRow(90d, 5d, 90d)]
    [DataRow(0d, -2.5d, -2.5d)]
    public void RuntimeElevationClampsAtThePoles(double initial, double delta, double expected)
    {
        using var workspace = new Workspace();
        using var player = new Player(new FakeEngine());
        Assert.IsTrue(player.ConfigureSofa(new AudioSettings
            { SofaEnabled = true, SofaFile = workspace.File("test.sofa"), SofaElevation = initial }));
        Assert.IsTrue(player.ChangeSofaElevation(delta));
        Assert.AreEqual(expected, player.SofaElevation);
    }

    [TestMethod]
    [DataRow(90d, 90d)]
    [DataRow(-90d, -90d)]
    public void ElevationEndpointsArePreserved(double stored, double expected)
    {
        var settings = new Settings();
        settings.Audio.SofaElevation = stored;
        settings.ValidateStored();
        settings.NormalizeValues();
        Assert.AreEqual(expected, settings.Audio.SofaElevation);
    }

    [TestMethod]
    public void FailedRuntimeChangeKeepsTheLastSuccessfulState()
    {
        using var workspace = new Workspace();
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        Assert.IsTrue(player.ConfigureSofa(new AudioSettings
            { SofaEnabled = true, SofaFile = workspace.File("test.sofa"), SofaRotation = 30, SofaElevation = 10 }));
        var graph = engine.SofaGraph;
        var notifications = 0;
        player.StateChanged += () => notifications++;
        engine.RejectSofa = true;
        Assert.IsFalse(player.ChangeSofaRotation(5));
        Assert.IsFalse(player.ChangeSofaElevation(5));
        Assert.AreEqual(30d, player.SofaRotation);
        Assert.AreEqual(10d, player.SofaElevation);
        Assert.IsTrue(player.IsSofaEnabled);
        Assert.AreEqual(graph, engine.SofaGraph);
        Assert.AreEqual(0, notifications);
        Assert.IsTrue(player.SetSofaEnabled(false));
        Assert.AreEqual(1, notifications);
        Assert.IsFalse(player.SetSofaEnabled(true));
        Assert.IsFalse(player.IsSofaEnabled);
        Assert.AreEqual(string.Empty, engine.SofaGraph);
    }

    [TestMethod]
    public void UnconfiguredOrDisabledSpatializationCannotBeAdjusted()
    {
        using var player = new Player(new FakeEngine());
        Assert.IsFalse(player.CanToggleSofa);
        Assert.IsFalse(player.SetSofaEnabled(true));
        Assert.IsFalse(player.ChangeSofaRotation(5));
        Assert.IsFalse(player.ChangeSofaElevation(5));
        Assert.IsTrue(player.ConfigureSofa(new AudioSettings { SofaFile = "missing.sofa" }));
        Assert.IsTrue(player.CanToggleSofa);
        Assert.IsTrue(player.SetSofaEnabled(true));
        Assert.IsTrue(player.ChangeSofaRotation(5));
        Assert.IsTrue(player.ChangeSofaElevation(5));
    }
    [TestMethod]
    public void RuntimeDirectionsDoNotRebuildTheFilter()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        Assert.IsTrue(player.ConfigureSofa(new AudioSettings { SofaEnabled = true, SofaFile = "not-checked.any-extension" }));
        var graph = engine.SofaGraph;
        for (var i = 0; i < 10; i++)
        {
            Assert.IsTrue(player.ChangeSofaRotation(5));
            Assert.IsTrue(player.ChangeSofaElevation(10));
        }
        Assert.AreEqual(1, engine.SofaConfigurations);
        Assert.AreEqual(20, engine.SofaDirectionChanges);
        Assert.AreEqual(-50d, engine.SofaRotation);
        Assert.AreEqual(90d, engine.SofaElevation);
        Assert.AreEqual(graph, engine.SofaGraph);
        Assert.IsTrue(player.OpenFiles(["one", "two"]));
        Assert.IsTrue(player.Next(false));
        Assert.AreEqual(1, engine.SofaConfigurations);
    }

    [TestMethod]
    public void BackgroundFailureCanDisableTheRequestedSpatialization()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        player.AudioFilterFailed += player.DisableFailedFilter;
        player.ConfigureSofa(new AudioSettings { SofaEnabled = true, SofaFile = "missing.sofa" });
        engine.FailFilter(AudioFilter.Spatialization);
        Assert.IsFalse(player.IsSofaEnabled);
        Assert.AreEqual(string.Empty, engine.SofaGraph);
    }

}
