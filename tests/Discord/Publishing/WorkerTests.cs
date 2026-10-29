using System.Collections.Concurrent;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;
using LunaPlayer.Discord.Publishing;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Discord.Publishing;

[TestClass, TestCategory("Unit")]
public sealed class WorkerTests
{
    private static readonly Timing Timing = new(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(40), TimeSpan.FromMinutes(15));
    private const string TestApplicationId = "123456789012345678";
    private static DiscordSettings Options() => new() { Enabled = true };

    private sealed class NoArtwork : IApplicationArtwork
    {
        public Task<string?> ReadAsync(string applicationId, CancellationToken token) => Task.FromResult<string?>(null);
    }

    private sealed class Host : IAsyncDisposable
    {
        private Publication _latest = new(false, null);
        private readonly Worker _worker;
        internal Host(Func<IRpcClient> factory, Timing timing, string? applicationId = null, SendBudget? budget = null)
            => _worker = new(_ => Volatile.Read(ref _latest), factory, timing, applicationId, new NoArtwork(), budget);
        internal void Update(DiscordSettings settings, string? activity)
        {
            Volatile.Write(ref _latest, new(settings.Enabled, activity, activity));
            _worker.Wake();
        }
        internal Task ShutdownAsync() => _worker.ShutdownAsync();
        public ValueTask DisposeAsync() => _worker.DisposeAsync();
    }

    private sealed class Client : IRpcClient
    {
        internal readonly ConcurrentQueue<string?> Activities = new();
        internal readonly ConcurrentQueue<string> Ids = new();
        internal bool Available = true;
        internal int Connections;
        internal int Disposals;
        internal TaskCompletionSource? HoldConnect;
        internal TaskCompletionSource? HoldSend;
        internal bool Accept = true;
        internal bool FailSend;
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
            if (FailSend) throw new IOException("Simulated IPC failure");
            if (activity is not null && HoldSend is not null) await HoldSend.Task.WaitAsync(token);
            return Accept;
        }
        public ValueTask DisposeAsync() { IsConnected = false; Interlocked.Increment(ref Disposals); return ValueTask.CompletedTask; }
    }

    [TestMethod]
    public async Task ProductionApplicationIdIsUsedWhenSharingIsEnabled()
    {
        var client = new Client();
        await using var service = new Host(() => client, Timing);
        service.Update(Options(), "playing");
        await NativeHost.Until(() => client.Activities.Contains("playing"));
        Assert.AreEqual("1565330415529762956", client.Ids.Single());
    }

    [TestMethod]
    public async Task DisabledDoesNotConnectAndEnablingPublishes()
    {
        var client = new Client();
        await using var service = new Host(() => client, Timing, TestApplicationId);
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
        await using var service = new Host(() => client, Timing, TestApplicationId);
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
        await using var service = new Host(() => client, Timing, TestApplicationId);
        service.Update(Options(), "first");
        await NativeHost.Until(() => client.Activities.Contains("first"));
        service.Update(new() { Enabled = true, ShowDetails = false }, "generic");
        await NativeHost.Until(() => client.Activities.Contains("generic"));
        client.IsConnected = false;
        await NativeHost.Until(() => client.Connections > 1);
        Assert.IsTrue(client.Ids.All(id => id == TestApplicationId));
    }

    [TestMethod]
    public async Task ChangesDuringHandshakeCannotPublishOldPrivateSnapshot()
    {
        var client = new Client { HoldConnect = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        await using var service = new Host(() => client, Timing, TestApplicationId);
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
        await using var service = new Host(() => client, Timing, TestApplicationId);
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
        var service = new Host(() => client, Timing, TestApplicationId);
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
        var service = new Host(() => client, Timing, TestApplicationId);
        service.Update(Options(), "playing");
        await NativeHost.Until(() => client.Activities.Contains("playing"));
        await service.ShutdownAsync();
        Assert.IsNull(client.Activities.Last());
        Assert.AreEqual(1, client.Disposals);
    }

    [TestMethod]
    public async Task ValidationRejectionDoesNotSpinAndNewStateIsNotHeldBehindRejectedPayload()
    {
        var client = new Client { Accept = false };
        await using var service = new Host(() => client, Timing with { Retry = TimeSpan.FromHours(1) });
        service.Update(Options(), "rejected");
        await NativeHost.Until(() => client.Activities.Contains("rejected"));
        await Task.Delay(70);
        Assert.AreEqual(1, client.Activities.Count);
        client.Accept = true;
        service.Update(Options(), "new state");
        await NativeHost.Until(() => client.Activities.Contains("new state"));
        Assert.AreEqual(1, client.Connections);
    }

    [TestMethod]
    public async Task TransportFailureReconnectsAndUsesTheLatestState()
    {
        var failed = new Client { FailSend = true };
        var connected = new Client();
        var connections = 0;
        await using var service = new Host(() => Interlocked.Increment(ref connections) == 1 ? failed : connected, Timing);
        service.Update(Options(), "old");
        await NativeHost.Until(() => failed.Disposals > 0);
        service.Update(Options(), "latest");
        await NativeHost.Until(() => connected.Activities.Contains("latest"));
        Assert.IsFalse(connected.Activities.Contains("old"));
    }

    [TestMethod]
    public async Task BudgetQueuesOnlyLatestPendingStateAndDoesNotDelayClearing()
    {
        var client = new Client();
        await using var service = new Host(() => client, Timing, budget: new SendBudget(1, TimeSpan.FromMilliseconds(120)));
        service.Update(Options(), "first");
        await NativeHost.Until(() => client.Activities.Contains("first"));
        service.Update(Options(), "second");
        service.Update(Options(), "third");
        await NativeHost.Until(() => client.Activities.Contains("third"));
        Assert.IsFalse(client.Activities.Contains("second"));
        service.Update(Options(), "pending");
        service.Update(new(), null);
        await NativeHost.Until(() => client.Disposals > 0);
        Assert.IsNull(client.Activities.Last());
        Assert.IsFalse(client.Activities.Contains("pending"));
    }
}
