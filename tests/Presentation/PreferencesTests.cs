using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Tests.Infrastructure;
using PlaybackPreferences = LunaPlayer.Application.Preferences.Playback;

namespace LunaPlayer.Tests.Presentation;

[TestClass, TestCategory("Unit")]
public sealed class PreferencesTests
{
    [TestMethod]
    public void UnrelatedPreferenceRetainsTemporarySpatialization()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new Settings();
        settings.Audio.SofaFile = "headphones.sofa";
        Assert.IsTrue(player.ConfigureSofa(settings.Audio));
        Assert.IsTrue(player.SetSofaEnabled(true));
        Assert.IsTrue(player.ChangeSofaRotation(30));
        Assert.IsTrue(player.ChangeSofaElevation(15));
        var configurations = engine.SofaConfigurations;
        var directions = engine.SofaDirectionChanges;
        var edited = settings.Copy();
        edited.General.CheckUpdatesOnStartup = false;

        Assert.IsTrue(PlaybackPreferences.Apply(settings, new Changes(settings, edited), player,
            filter => Assert.Fail("Unexpected filter failure: " + filter)));

        Assert.IsTrue(player.IsSofaEnabled);
        Assert.AreEqual(30d, player.SofaRotation);
        Assert.AreEqual(15d, player.SofaElevation);
        Assert.AreEqual(configurations, engine.SofaConfigurations);
        Assert.AreEqual(directions, engine.SofaDirectionChanges);
        Assert.IsFalse(settings.Audio.SofaEnabled);
        Assert.IsFalse(settings.General.CheckUpdatesOnStartup);
    }

    [TestMethod]
    public void UneditedPreferencesDoNotApplyAnyAudioSettings()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = TemporarySpatialization(player);
        engine.AudioUpdates.Clear();
        Assert.IsFalse(Apply(settings, settings.Copy(), player));
        Assert.IsEmpty(engine.AudioUpdates);
        Assert.IsTrue(player.IsSofaEnabled);
        Assert.AreEqual(30d, player.SofaRotation);
    }

    [TestMethod]
    public void ChangingAnAudioStepDoesNotReapplyAudioEffects()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = TemporarySpatialization(player);
        var edited = settings.Copy();
        edited.Audio.VolumeStep = 7;
        engine.AudioUpdates.Clear();
        Assert.IsTrue(Apply(settings, edited, player));
        Assert.IsEmpty(engine.AudioUpdates);
        Assert.AreEqual(7, settings.Audio.VolumeStep);
        Assert.IsTrue(player.IsSofaEnabled);
    }

    [TestMethod]
    public void SpatializationStepChangesDoNotTouchTheFilterOrDirections()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = TemporarySpatialization(player);
        var edited = settings.Copy();
        edited.Audio.SofaRotationStep = 20;
        edited.Audio.SofaElevationStep = 10;
        engine.AudioUpdates.Clear();
        Assert.IsTrue(Apply(settings, edited, player));
        Assert.IsEmpty(engine.AudioUpdates);
        Assert.AreEqual(20d, settings.Audio.SofaRotationStep);
        Assert.AreEqual(10d, settings.Audio.SofaElevationStep);
        Assert.IsFalse(settings.Audio.SofaEnabled);
        Assert.IsTrue(player.IsSofaEnabled);
        Assert.AreEqual(30d, player.SofaRotation);
        Assert.AreEqual(15d, player.SofaElevation);
    }

    [TestMethod]
    [DataRow("gain")]
    [DataRow("file")]
    [DataRow("rotation")]
    [DataRow("elevation")]
    [DataRow("enabled")]
    public void EditingOneSpatializationDefaultRetainsOtherTemporaryValues(string field)
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = TemporarySpatialization(player);
        var edited = settings.Copy();
        switch (field)
        {
            case "gain": edited.Audio.SofaGain = -2; break;
            case "file": edited.Audio.SofaFile = "other-headphones.sofa"; break;
            case "rotation": edited.Audio.SofaRotation = 90; break;
            case "elevation": edited.Audio.SofaElevation = -20; break;
            case "enabled": edited.Audio.SofaEnabled = true; break;
        }
        Assert.IsTrue(Apply(settings, edited, player));
        Assert.IsTrue(player.IsSofaEnabled);
        Assert.AreEqual(field == "rotation" ? 90d : 30d, player.SofaRotation);
        Assert.AreEqual(field == "elevation" ? -20d : 15d, player.SofaElevation);
        Assert.AreEqual(field == "enabled", settings.Audio.SofaEnabled);
        Assert.AreEqual(field == "rotation" ? 90d : 0d, settings.Audio.SofaRotation);
        Assert.AreEqual(field == "elevation" ? -20d : 0d, settings.Audio.SofaElevation);
    }

    [TestMethod]
    public void RemovingTheSofaPathDisablesTemporarySpatialization()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = TemporarySpatialization(player);
        var edited = settings.Copy();
        edited.Audio.SofaFile = string.Empty;
        Assert.IsTrue(Apply(settings, edited, player));
        Assert.IsFalse(player.CanToggleSofa);
        Assert.IsFalse(player.IsSofaEnabled);
        Assert.AreEqual(string.Empty, engine.SofaGraph);
    }

    [TestMethod]
    public void FailedSpatializationEditKeepsIndependentChanges()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = TemporarySpatialization(player);
        var edited = settings.Copy();
        edited.Audio.SofaGain = -2;
        edited.Audio.SofaRotationStep = 20;
        edited.General.CheckUpdatesOnStartup = false;
        engine.RejectSofa = true;
        var failures = new List<AudioFilter>();
        Assert.IsTrue(PlaybackPreferences.Apply(settings, new Changes(settings, edited), player, failures.Add));
        CollectionAssert.AreEqual(new[] { AudioFilter.Spatialization }, failures);
        Assert.AreEqual(0d, settings.Audio.SofaGain);
        Assert.AreEqual(20d, settings.Audio.SofaRotationStep);
        Assert.IsFalse(settings.General.CheckUpdatesOnStartup);
        Assert.IsTrue(player.IsSofaEnabled);
        Assert.AreEqual(30d, player.SofaRotation);
    }

    [TestMethod]
    public void ExplicitReplacementRestoresDefaultsEvenWhenPreferencesHaveNotChanged()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = TemporarySpatialization(player);
        Assert.IsTrue(PlaybackPreferences.Apply(settings, new Changes(settings, settings.Copy(), replaceAll: true), player,
            filter => Assert.Fail("Unexpected filter failure: " + filter)));
        Assert.IsFalse(player.IsSofaEnabled);
        Assert.AreEqual(0d, player.SofaRotation);
        Assert.AreEqual(0d, player.SofaElevation);
    }

    [TestMethod]
    public void UnrelatedChangesKeepTemporaryNormalizationMonoSilencePitchAndPan()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new Settings();
        Assert.IsTrue(player.SetNormalization(false));
        Assert.IsTrue(player.SetMono(true));
        Assert.IsTrue(player.SetSilenceRemoval(true));
        player.SetPitch(3);
        player.SetPan(20);
        var edited = settings.Copy();
        edited.General.CheckUpdatesOnStartup = false;
        engine.AudioUpdates.Clear();
        Assert.IsTrue(Apply(settings, edited, player));
        Assert.IsEmpty(engine.AudioUpdates);
        Assert.IsFalse(player.IsNormalizationEnabled);
        Assert.IsTrue(player.IsMonoEnabled);
        Assert.IsTrue(player.IsSilenceRemovalEnabled);
        Assert.AreEqual(3d, player.Pitch);
        Assert.AreEqual(20d, player.Pan);
        Assert.IsTrue(settings.Audio.NormalizeAudio);
        Assert.IsFalse(settings.Audio.MonoAudio);
        Assert.IsFalse(settings.Silence.Enabled);
        Assert.AreEqual(0d, settings.Audio.Pitch);
        Assert.AreEqual(0d, settings.Audio.Pan);
    }

    [TestMethod]
    public void ChangingNormalizationOnlyAppliesNormalization()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new Settings();
        Assert.IsTrue(player.SetNormalization(true));
        Assert.IsTrue(player.SetMono(true));
        var edited = settings.Copy();
        edited.Audio.NormalizeAudio = false;
        engine.AudioUpdates.Clear();
        Assert.IsTrue(Apply(settings, edited, player));
        CollectionAssert.AreEqual(new[] { nameof(FakeEngine.SetNormalization) }, engine.AudioUpdates);
        Assert.IsFalse(player.IsNormalizationEnabled);
        Assert.IsTrue(player.IsMonoEnabled);
        Assert.IsFalse(settings.Audio.NormalizeAudio);
        Assert.IsFalse(settings.Audio.MonoAudio);
    }

    [TestMethod]
    public void ChangingSilenceParametersPreservesItsTemporaryEnabledState()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new Settings();
        Assert.IsTrue(player.ConfigureSilence(settings.Silence));
        Assert.IsTrue(player.SetSilenceRemoval(true));
        var edited = settings.Copy();
        edited.Silence.Threshold = -20;
        engine.AudioUpdates.Clear();
        Assert.IsTrue(Apply(settings, edited, player));
        CollectionAssert.AreEqual(new[] { nameof(FakeEngine.SetSilenceRemoval) }, engine.AudioUpdates);
        Assert.AreEqual(-20d, settings.Silence.Threshold);
        Assert.IsFalse(settings.Silence.Enabled);
        Assert.IsTrue(player.IsSilenceRemovalEnabled);
    }

    [TestMethod]
    public void ChangingCrossfadeOnlyReconfiguresPlaybackEffects()
    {
        using var engine = new FakeEngine();
        using var player = new Player(engine);
        var settings = new Settings();
        var edited = settings.Copy();
        edited.Audio.Crossfade = true;
        engine.AudioUpdates.Clear();
        Assert.IsTrue(Apply(settings, edited, player));
        CollectionAssert.AreEqual(new[] { nameof(FakeEngine.ConfigurePlaybackEffects) }, engine.AudioUpdates);
        Assert.IsTrue(settings.Audio.Crossfade);
    }

    private static Settings TemporarySpatialization(Player player)
    {
        var settings = new Settings();
        settings.Audio.SofaFile = "headphones.sofa";
        Assert.IsTrue(player.ConfigureSofa(settings.Audio));
        Assert.IsTrue(player.SetSofaEnabled(true));
        Assert.IsTrue(player.ChangeSofaRotation(30));
        Assert.IsTrue(player.ChangeSofaElevation(15));
        return settings;
    }

    private static bool Apply(Settings settings, Settings edited, Player player)
        => PlaybackPreferences.Apply(settings, new Changes(settings, edited), player,
            filter => Assert.Fail("Unexpected filter failure: " + filter));
}
