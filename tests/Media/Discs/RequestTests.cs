using LunaPlayer.Application;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media.Discs;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Media.Discs;

[TestClass, TestCategory("Unit")]
public sealed class RequestTests
{
    [TestMethod]
    [DataRow("--dvd-video", 0)]
    [DataRow("--dvd-audio", 1)]
    public void DiscLaunchBypassesOrdinaryFolderCollection(string argument, int format)
    {
        using var workspace = new Workspace();
        workspace.File("ordinary.mp3");
        var launch = Resources.PrepareLaunch([argument, workspace.Root], new Settings());
        Assert.IsNotNull(launch.Disc);
        Assert.AreEqual((DiscFormat)format, launch.Disc.Format);
        Assert.AreEqual(workspace.Root, launch.Disc.Location);
        Assert.IsNull(launch.Files);
        Assert.IsEmpty(launch.Paths);
        Assert.IsFalse(launch.Convert);
    }

    [TestMethod]
    [DataRow("F:")]
    [DataRow("F:\\")]
    [DataRow("F:\\. ")]
    public void DriveLocationsResolveToTheRoot(string drive)
        => Assert.AreEqual(@"F:\", Request.FromArguments(["--dvd-video", drive])!.Location);

    [TestMethod]
    public void AnUnavailableDriveStillReachesTheDiscErrorFlow()
    {
        var launch = Resources.PrepareLaunch(["--DVD-AUDIO", @"Z:\"], new Settings());
        Assert.AreEqual(@"Z:\", launch.Disc!.Location);
        Assert.AreEqual(DiscFormat.Audio, launch.Disc.Format);
        Assert.IsNull(launch.Files);
    }

    [TestMethod]
    public void MalformedDiscRequestsNeverBecomeFolderOrConversionRequests()
    {
        string[][] requests = [
            ["--dvd-video"], ["--dvd-audio", ""], ["--dvd-video", "--dvd-audio"],
            ["--convert", "--dvd-audio", @"F:\"], ["--dvd-video", @"G:\", "unexpected"],
        ];
        foreach (var arguments in requests)
        {
            var launch = Resources.PrepareLaunch(arguments, new Settings());
            Assert.IsNotNull(launch.Disc);
            Assert.AreEqual(string.Empty, launch.Disc.Location);
            Assert.IsFalse(launch.Convert);
            Assert.IsNull(launch.Files);
        }
    }

    [TestMethod]
    public void OrdinaryFilesAndConversionKeepTheirExistingRouting()
    {
        using var workspace = new Workspace();
        var path = workspace.File("music.mp3");
        var settings = new Settings();
        settings.General.OpenFilesMode = OpenFilesMode.FileOnly;
        var file = Resources.PrepareLaunch([path], settings);
        Assert.IsNull(file.Disc);
        CollectionAssert.AreEqual(new[] { path }, file.Paths.ToArray());
        Assert.IsFalse(file.Convert);
        var conversion = Resources.PrepareLaunch(["--convert", workspace.Root], settings);
        Assert.IsNull(conversion.Disc);
        Assert.IsTrue(conversion.Convert);
        CollectionAssert.AreEqual(new[] { path }, conversion.Files!.ToArray());
    }
}
