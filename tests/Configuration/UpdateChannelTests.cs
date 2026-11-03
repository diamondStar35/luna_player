using System.Text.Json;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Update;

namespace LunaPlayer.Tests.Configuration;

[TestClass, TestCategory("Unit")]
public sealed class UpdateChannelTests
{
    private sealed class ReleaseServer(string manifest) : HttpMessageHandler
    {
        internal List<string> Requests { get; } = [];
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(manifest),
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(Send(request, cancellationToken));
    }

    [TestMethod]
    public void NightlyFetchAndPackageCheckUseTheNightlyRelease()
    {
        var server = new ReleaseServer("""{"version":"0.7.2","build_id":"123.2","changes":["A fix"]}""");
        using var service = new AppUpdateService(server);
        var update = service.Fetch(CancellationToken.None, UpdateChannel.Nightly);
        Assert.AreEqual("123.2", update.BuildId);
        Assert.AreEqual("A fix", update.Changes);
        Assert.IsTrue(service.PackageExists(update, CancellationToken.None));
        Assert.HasCount(3, server.Requests);
        Assert.IsTrue(server.Requests.All(path => path.Contains("/releases/download/nightly/", StringComparison.Ordinal)));
        Assert.IsTrue(server.Requests[0].EndsWith("/release-metadata.json", StringComparison.Ordinal));
        Assert.IsTrue(server.Requests[2].EndsWith("/SHA256SUMS.txt", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NightlyWithoutBuildIdentityIsRejected()
    {
        using var service = new AppUpdateService(new ReleaseServer("""{"version":"0.7.2"}"""));
        Assert.Throws<InvalidDataException>(() => service.Fetch(CancellationToken.None, UpdateChannel.Nightly));
    }

    [TestMethod]
    public void ChannelDefaultsToStableAndNightlySurvivesCopyApplyAndStorage()
    {
        var defaults = JsonSerializer.Deserialize("{}", SettingsJsonContext.Default.Settings)!;
        Assert.AreEqual(UpdateChannel.Stable, defaults.General.UpdateChannel);
        defaults.General.UpdateChannel = UpdateChannel.Nightly;
        var applied = new Settings();
        applied.General.Apply(defaults.Copy().General);
        var json = JsonSerializer.Serialize(applied, SettingsJsonContext.Default.Settings);
        var loaded = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings)!;
        Assert.AreEqual(UpdateChannel.Nightly, loaded.General.UpdateChannel);
    }

    [TestMethod]
    [DataRow(false, "", true)]
    [DataRow(true, "123.1", false)]
    [DataRow(true, "122.1", true)]
    [DataRow(true, "123.2", true)]
    public void NightlyChecksUseBuildIdentityEvenAtTheSameVersion(bool nightly, string installedBuild, bool expected)
    {
        var available = new AppUpdateInfo("0.7.2", "changes", UpdateChannel.Nightly, "123.1");
        Assert.AreEqual(expected, AppUpdateService.IsUpdateAvailable(available, "0.7.2", nightly, installedBuild));
        Assert.AreEqual("nightly", available.Tag);
    }

    [TestMethod]
    [DataRow("0.7.1", false, true)]
    [DataRow("0.7.2", false, false)]
    [DataRow("0.8.0", false, false)]
    [DataRow("0.7.2", true, true)]
    [DataRow("0.8.0", true, false)]
    public void StableChecksCompareVersionsAndAllowReturningFromAnEqualNightly(string current, bool nightly, bool expected)
    {
        var available = new AppUpdateInfo("0.7.2", "changes");
        Assert.AreEqual(expected, AppUpdateService.IsUpdateAvailable(available, current, nightly, "123.1"));
        Assert.AreEqual("v0.7.2", available.Tag);
    }
}
