using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Tests.Configuration;

[TestClass, TestCategory("Unit")]
public sealed class ChangesTests
{
    [TestMethod]
    public void UneditedDraftHasNoChanges()
    {
        var settings = new Settings();
        var changes = new Changes(settings, settings.Copy());
        Assert.IsFalse(changes.Any);
        Assert.IsFalse(changes.Contains("audio", "general", "shortcuts"));
        Assert.IsFalse(changes.ApplyTo(settings));
    }

    [TestMethod]
    public void RevertingAnEditDoesNotCountAsAChange()
    {
        var settings = new Settings();
        var edited = settings.Copy();
        edited.Audio.SofaEnabled = true;
        edited.Audio.SofaEnabled = false;
        Assert.IsFalse(new Changes(settings, edited).Any);
    }

    [TestMethod]
    public void ChangesAreSnapshotsOfTheDraft()
    {
        var before = new Settings();
        var edited = before.Copy();
        edited.Audio.VolumeStep = 7;
        var changes = new Changes(before, edited);
        before.Audio.VolumeStep = 3;
        edited.Audio.VolumeStep = 9;
        var current = new Settings();
        Assert.IsTrue(changes.ApplyTo(current));
        Assert.AreEqual(7, current.Audio.VolumeStep);
        Assert.IsTrue(changes.Contains("audio", "audio.volumeStep"));
        Assert.IsFalse(changes.Contains("audio.sofaEnabled", "general"));
    }

    [TestMethod]
    public void ApplyingOnePreferencePreservesValuesChangedSinceTheDialogOpened()
    {
        var settings = new Settings();
        var edited = settings.Copy();
        edited.General.CheckUpdatesOnStartup = false;
        var changes = new Changes(settings, edited);
        var audio = settings.Audio;
        settings.Audio.Volume = 42;
        settings.Playback.LastPosition = 17;
        settings.Spotify.ClientId = "new credentials";
        Assert.IsTrue(changes.ApplyTo(settings));
        Assert.IsFalse(settings.General.CheckUpdatesOnStartup);
        Assert.AreEqual(42d, settings.Audio.Volume);
        Assert.AreEqual(17d, settings.Playback.LastPosition);
        Assert.AreEqual("new credentials", settings.Spotify.ClientId);
        Assert.AreSame(audio, settings.Audio);
    }

    [TestMethod]
    public void ShortcutEditsMergeAdditionsAndRemovalsWithoutReplacingOtherBindings()
    {
        var settings = new Settings();
        settings.Shortcuts.Primary[ActionId.OpenFile] = new("f", ShortcutModifiers.Control);
        var edited = settings.Copy();
        edited.Shortcuts.Primary.Remove(ActionId.OpenFile);
        edited.Shortcuts.Primary[ActionId.OpenPreferences] = new("p", ShortcutModifiers.Alt);
        var changes = new Changes(settings, edited);
        settings.Shortcuts.Secondary[ActionId.OpenFile] = new("o", ShortcutModifiers.Alt);
        Assert.IsTrue(changes.ApplyTo(settings));
        Assert.IsFalse(settings.Shortcuts.Primary.ContainsKey(ActionId.OpenFile));
        Assert.AreEqual(new Shortcut("p", ShortcutModifiers.Alt), settings.Shortcuts.Primary[ActionId.OpenPreferences]);
        Assert.AreEqual(new Shortcut("o", ShortcutModifiers.Alt), settings.Shortcuts.Secondary[ActionId.OpenFile]);
        Assert.IsTrue(changes.Contains("shortcuts.primary"));
        Assert.IsFalse(changes.Contains("shortcuts.secondary", "shortcuts.global"));
    }

    [TestMethod]
    public void DisablingSecondaryShortcutsDoesNotReplaceBindings()
    {
        var settings = new Settings();
        var edited = settings.Copy();
        edited.Shortcuts.DisableSecondary = true;
        var changes = new Changes(settings, edited);
        settings.Shortcuts.Secondary[ActionId.PlayPause] = new("p", ShortcutModifiers.Alt);

        Assert.IsTrue(changes.Contains("shortcuts.disableSecondary"));
        Assert.IsFalse(changes.Contains("shortcuts.primary", "shortcuts.secondary", "shortcuts.global"));
        Assert.IsTrue(changes.ApplyTo(settings));
        Assert.IsTrue(settings.Shortcuts.DisableSecondary);
        Assert.AreEqual(new Shortcut("p", ShortcutModifiers.Alt), settings.Shortcuts.Secondary[ActionId.PlayPause]);
    }

    [TestMethod]
    public void CollectionOrderDoesNotCountAsAnEdit()
    {
        var before = new Settings();
        before.Equalizer.Overrides["first"] = [new() { Slot = 0, Gain = 2 }];
        before.Equalizer.Overrides["second"] = [new() { Slot = 1, Gain = 3 }];
        var after = before.Copy();
        var first = after.Equalizer.Overrides["first"];
        after.Equalizer.Overrides.Remove("first");
        after.Equalizer.Overrides["first"] = first;
        Assert.IsFalse(new Changes(before, after).Any);
    }

    [TestMethod]
    public void NestedCollectionsSupportReplacementRemovalAndNullValues()
    {
        var settings = new Settings();
        settings.Equalizer.Overrides["first"] = [new() { Slot = 0, Frequency = 100, Gain = 2 }];
        settings.Equalizer.Overrides["second"] = [new() { Slot = 1, Gain = 3 }];
        var edited = settings.Copy();
        edited.Equalizer.Overrides["first"][0].Frequency = null;
        edited.Equalizer.Overrides.Remove("second");
        var changes = new Changes(settings, edited);
        settings.Equalizer.Overrides["third"] = [new() { Slot = 2, Gain = 4 }];
        Assert.IsTrue(changes.ApplyTo(settings));
        Assert.IsNull(settings.Equalizer.Overrides["first"][0].Frequency);
        Assert.AreEqual(2d, settings.Equalizer.Overrides["first"][0].Gain);
        Assert.IsFalse(settings.Equalizer.Overrides.ContainsKey("second"));
        Assert.AreEqual(4d, settings.Equalizer.Overrides["third"][0].Gain);
    }

    [TestMethod]
    public void ExplicitReplacementIncludesUnchangedPreferences()
    {
        var settings = new Settings();
        var replacement = settings.Copy();
        var changes = new Changes(settings, replacement, replaceAll: true);
        settings.Audio.Volume = 42;
        Assert.IsTrue(changes.Any);
        Assert.IsTrue(changes.Contains("audio.sofaEnabled"));
        Assert.IsTrue(changes.ApplyTo(settings));
        Assert.AreEqual(100d, settings.Audio.Volume);
    }
}
