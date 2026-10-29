using System.Collections.Concurrent;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Discord;

[TestClass, TestCategory("Unit")]
public sealed class ServiceTests
{
    private static readonly PresenceTiming Timing = new(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(80),
        TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(40));
    private const string TestApplicationId = "123456789012345678";
    private static DiscordSettings Options() => new() { Enabled = true };

    private sealed class Client : IRpcClient
    {
        internal readonly ConcurrentQueue<string?> Activities = new();
        internal readonly ConcurrentQueue<string> Ids = new();
        internal bool Available = true;
        internal int Connections;
        internal int Disposals;
        internal TaskCompletionSource? HoldConnect;
        internal TaskCompletionSource? HoldSend;
        public bool IsConnected { get; set; }
        public async Task<bool> ConnectAsync(string id, CancellationToken token)
        {
            Ids.Enqueue(id); Interlocked.Increment(ref Connections);
            if (HoldConnect is not null) await HoldConnect.Task.WaitAsync(token);
            return IsConnected = Available;
        }
        public async Task<bool> SetActivityAsync(string? activity, CancellationToken token)
        {
            Activities.Enqueue(activity);
            if (activity is not null && HoldSend is not null) await HoldSend.Task.WaitAsync(token);
            return true;
        }
        public ValueTask DisposeAsync() { IsConnected = false; Interlocked.Increment(ref Disposals); return ValueTask.CompletedTask; }
    }

    [TestMethod]
    public async Task DisabledDoesNotConnectAndEnablingPublishes()
    {
        var client = new Client();
        await using var service = new PresenceService(() => client, Timing, TestApplicationId);
        service.Update(new(), "private");
        await Task.Delay(50);
        Assert.AreEqual(0, client.Connections);
        service.Update(Options(), "playing");
        await NativeHost.Until(() => client.Activities.Contains("playing"));
        service.Update(new(), null);
        await NativeHost.Until(() => client.Disposals > 0);
        Assert.IsNull(client.Activities.Last());
    }

    [TestMethod]
    public async Task AbsentDiscordAndBrokenConnectionsRetryLatestActivity()
    {
        var client = new Client { Available = false };
        await using var service = new PresenceService(() => client, Timing, TestApplicationId);
        service.Update(Options(), "first");
        await NativeHost.Until(() => client.Connections > 0);
        service.Update(Options(), "latest");
        client.Available = true;
        await NativeHost.Until(() => client.Activities.Contains("latest"));
        Assert.IsFalse(client.Activities.Contains("first"));
        client.IsConnected = false;
        await NativeHost.Until(() => client.Activities.Count(a => a == "latest") == 2);
    }

    [TestMethod]
    public async Task SamePublisherIdentityIsUsedAfterSettingsChangesAndReconnects()
    {
        var client = new Client();
        await using var service = new PresenceService(() => client, Timing, TestApplicationId);
        service.Update(Options(), "first");
        await NativeHost.Until(() => client.Activities.Contains("first"));
        service.Update(new() { Enabled = true, ShowDetails = false }, "generic");
        await NativeHost.Until(() => client.Activities.Contains("generic"));
        client.IsConnected = false;
        await NativeHost.Until(() => client.Connections > 1);
        Assert.IsTrue(client.Ids.All(id => id == TestApplicationId));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not-an-application-id")]
    public async Task MissingPublisherConfigurationNeverConnectsEvenWhenUserEnablesSharing(string id)
    {
        var client = new Client();
        await using var service = new PresenceService(() => client, Timing, id);
        service.Update(Options(), "private");
        await Task.Delay(100);
        Assert.AreEqual(0, client.Connections);
        Assert.IsTrue(client.Activities.IsEmpty);
    }

    [TestMethod]
    public async Task ChangesDuringHandshakeCannotPublishOldPrivateSnapshot()
    {
        var client = new Client { HoldConnect = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        await using var service = new PresenceService(() => client, Timing, TestApplicationId);
        service.Update(Options(), "private");
        await NativeHost.Until(() => client.Connections > 0);
        service.Update(new(), null);
        client.HoldConnect.SetResult();
        await NativeHost.Until(() => client.Disposals > 0);
        Assert.IsFalse(client.Activities.Contains("private"));
    }

    [TestMethod]
    public async Task ChangesDuringSendAreNotLostAndIdenticalSnapshotsAreCoalesced()
    {
        var client = new Client { HoldSend = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        await using var service = new PresenceService(() => client, Timing, TestApplicationId);
        service.Update(Options(), "first");
        await NativeHost.Until(() => client.Activities.Contains("first"));
        service.Update(Options(), "second");
        service.Update(Options(), "third");
        client.HoldSend.SetResult();
        await NativeHost.Until(() => client.Activities.Contains("third"));
        service.Update(Options(), "third");
        await Task.Delay(150);
        CollectionAssert.AreEqual(new[] { "first", "third" }, client.Activities.ToArray());
    }

    [TestMethod]
    public async Task ShutdownCancelsBlockedConnectAndIsIdempotent()
    {
        var client = new Client { HoldConnect = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var service = new PresenceService(() => client, Timing, TestApplicationId);
        service.Update(Options(), "playing");
        await NativeHost.Until(() => client.Connections > 0);
        await service.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(2));
        await service.ShutdownAsync();
        service.Update(Options(), "ignored");
        Assert.AreEqual(1, client.Disposals);
    }

    [TestMethod]
    public async Task ShutdownClearsAnEstablishedPresence()
    {
        var client = new Client();
        var service = new PresenceService(() => client, Timing, TestApplicationId);
        service.Update(Options(), "playing");
        await NativeHost.Until(() => client.Activities.Contains("playing"));
        await service.ShutdownAsync();
        Assert.IsNull(client.Activities.Last());
        Assert.AreEqual(1, client.Disposals);
    }
}
