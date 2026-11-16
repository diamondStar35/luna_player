using System.Text;
using LunaPlayer.Configuration;
using LunaPlayer.Media.Discs;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Media.Discs;

[TestClass, TestCategory("Unit")]
public sealed class CatalogTests
{
    [TestMethod]
    public void AutomaticOpeningPrefersAudioOnADualFormatDisc()
    {
        using var workspace = new Workspace();
        workspace.File("VIDEO_TS/VIDEO_TS.IFO", "DVDVIDEO-VMG");
        workspace.File("AUDIO_TS/AUDIO_TS.IFO", "DVDAUDIO-AMG");
        Assert.AreEqual(DiscFormat.Audio, Catalog.Detect(workspace.Root));
        Assert.AreEqual(DiscFormat.Audio, Catalog.Detect(Path.Combine(workspace.Root, "AUDIO_TS")));
    }

    [TestMethod]
    public void AutomaticOpeningUsesVideoWhenNoValidAudioAreaExists()
    {
        using var workspace = new Workspace();
        workspace.File("VIDEO_TS/VIDEO_TS.IFO", "DVDVIDEO-VMG");
        Directory.CreateDirectory(Path.Combine(workspace.Root, "AUDIO_TS"));
        Assert.AreEqual(DiscFormat.Video, Catalog.Detect(workspace.Root));
        workspace.File("AUDIO_TS/AUDIO_TS.IFO", "invalid header");
        Assert.AreEqual(DiscFormat.Video, Catalog.Detect(workspace.Root));
    }

    [TestMethod]
    public void AutomaticOpeningRejectsAnOrdinaryFolder()
    {
        using var workspace = new Workspace();
        Assert.AreEqual(Failure.InvalidDisc, Assert.Throws<DiscException>(() => Catalog.Detect(workspace.Root)).Failure);
    }

    [TestMethod]
    [DataRow(0, "VIDEO_TS", "DVDVIDEO-VMG")]
    [DataRow(1, "AUDIO_TS", "DVDAUDIO-AMG")]
    public void ResolvesDiscRootAndItsContentDirectory(int format, string zone, string header)
    {
        using var workspace = new Workspace();
        var directory = Path.Combine(workspace.Root, zone);
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, zone + ".IFO"), Encoding.ASCII.GetBytes(header));
        Assert.AreEqual(directory, Catalog.Resolve(workspace.Root, (DiscFormat)format));
        Assert.AreEqual(directory, Catalog.Resolve(directory, (DiscFormat)format));
    }

    [TestMethod]
    public void VideoDiscWithEmptyAudioZoneReportsMissingAudioTitles()
    {
        using var workspace = new Workspace();
        Directory.CreateDirectory(Path.Combine(workspace.Root, "VIDEO_TS"));
        Directory.CreateDirectory(Path.Combine(workspace.Root, "AUDIO_TS"));
        File.WriteAllText(Path.Combine(workspace.Root, "VIDEO_TS", "VIDEO_TS.IFO"), "DVDVIDEO-VMG");
        var error = Assert.Throws<DiscException>(() => Catalog.Resolve(workspace.Root, DiscFormat.Audio));
        Assert.AreEqual(Failure.NoAudioTitles, error.Failure);
    }

    [TestMethod]
    public void RejectsOrdinaryFoldersAndInvalidManagerHeaders()
    {
        using var workspace = new Workspace();
        Assert.AreEqual(Failure.InvalidDisc, Assert.Throws<DiscException>(() => Catalog.Resolve(workspace.Root, DiscFormat.Video)).Failure);
        var directory = Path.Combine(workspace.Root, "VIDEO_TS");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "VIDEO_TS.IFO"), "ordinary file contents");
        Assert.AreEqual(Failure.InvalidDisc, Assert.Throws<DiscException>(() => Catalog.Resolve(workspace.Root, DiscFormat.Video)).Failure);
    }

    [TestMethod]
    public void MenuEntryIsExcludedWithoutChangingNativeTitleIndices()
    {
        object[] entries = [
            new Dictionary<string, object?> { ["id"] = 0L, ["title"] = "title: 1" },
            new Dictionary<string, object?> { ["id"] = 3L, ["title"] = "title: 4" },
            new Dictionary<string, object?> { ["id"] = 4L, ["title"] = "Disc Menu" },
        ];
        CollectionAssert.AreEqual(new[] { new Title(0), new Title(3) }, Catalog.ReadTitles(entries).ToArray());
    }

    [TestMethod]
    public void DiscAddressKeepsItsTitleAndDirectoryAcrossPathNormalization()
    {
        var path = Addressing.Build(DiscFormat.Audio, @"F:\AUDIO_TS", 0);
        Assert.AreEqual("dvda://0/F:/AUDIO_TS", path);
        Assert.AreEqual(path, Paths.Absolute(path));
        Assert.AreEqual(path.ToLowerInvariant(), Paths.Key(path));
    }
}
