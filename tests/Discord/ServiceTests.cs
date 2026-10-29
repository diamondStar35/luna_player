using System.Collections.Concurrent;
using System.Text.Json;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;
using LunaPlayer.Discord.Publishing;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Discord;

[TestClass, TestCategory("Unit")]
public sealed class ServiceTests
{
    private static readonly Timing Timing = new(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(30), TimeSpan.FromMilliseconds(100));
    private const string Icon = "https://cdn.discordapp.com/app-icons/1565330415529762956/930f1667326a41fa0865bd709fa45f0c.png?size=256";

    private sealed class Client : IRpcClient
    {
        internal readonly ConcurrentQueue<string?> Activities = new();
        internal TaskCompletionSource? HoldConnect;
        internal readonly TaskCompletionSource Connecting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsConnected { get; private set; }
        public async Task<bool> ConnectAsync(string id, CancellationToken token)
        {
            Connecting.TrySetResult();
            if (HoldConnect is not null) await HoldConnect.Task.WaitAsync(token);
            return IsConnected = true;
        }
        public Task<bool> SetActivityAsync(string? activity, CancellationToken token)
        {
            Activities.Enqueue(activity);
            return Task.FromResult(true);
        }
        public ValueTask DisposeAsync() { IsConnected = false; return ValueTask.CompletedTask; }
    }

    private sealed class Artwork : IApplicationArtwork
    {
        internal TaskCompletionSource<string?>? Hold;
        internal int Calls;
        public async Task<string?> ReadAsync(string id, CancellationToken token)
        {
            Interlocked.Increment(ref Calls);
            return Hold is null ? null : await Hold.Task.WaitAsync(token);
        }
    }

    private static PresenceMedia Media(bool playing = true)
        => new("song", "Song title", "Artist", null, playing, false, 30, 180, 1);

    private static string? Heading(string? json)
    {
        if (json is null) return null;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("details").GetString();
    }

    [TestMethod]
    public async Task PauseAndResumeWakeTheWorkerWithoutWaitingForItsPeriodicTick()
    {
        var client = new Client();
        var options = new DiscordSettings { Enabled = true };
        // A long maintenance interval makes missing event-driven wakes observable.
        var timing = Timing with { Tick = TimeSpan.FromHours(1) };
        await using var service = new PresenceService(Examples.English, () => client, timing, new Artwork());
        service.Update(options, Media());
        await NativeHost.Until(() => client.Activities.Any(json => Heading(json) == "Luna Player"));
        service.Update(options, Media(playing: false));
        await NativeHost.Until(() => client.Activities.Any(json => Heading(json) == "[Paused]: Luna Player"));
        service.Update(options, Media());
        await NativeHost.Until(() => client.Activities.Count(json => Heading(json) == "Luna Player") == 2);
    }

    [TestMethod]
    public async Task MutableSettingsAreCapturedBeforeTheHandshakeCompletes()
    {
        var client = new Client { HoldConnect = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var settings = new DiscordSettings { Enabled = true };
        await using var service = new PresenceService(Examples.English, () => client, Timing, new Artwork());
        service.Update(settings, Media());
        await client.Connecting.Task.WaitAsync(TimeSpan.FromSeconds(2));
        settings.ShowDetails = false;
        settings.Enabled = false;
        client.HoldConnect.SetResult();
        await NativeHost.Until(() => client.Activities.Any(json => json?.Contains("Song title", StringComparison.Ordinal) == true));
        service.Update(settings, null);
        await NativeHost.Until(() => client.Activities.Any(json => json is null));
    }

    [TestMethod]
    public async Task SlowArtworkCannotDelayPlaybackUpdatesAndCompletionKeepsTheLatestState()
    {
        var client = new Client();
        var artwork = new Artwork { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var options = new DiscordSettings { Enabled = true };
        await using var service = new PresenceService(Examples.English, () => client, Timing, artwork);
        service.Update(options, Media());
        await NativeHost.Until(() => client.Activities.Any(json => Heading(json) == "Luna Player"));
        service.Update(options, Media(false));
        await NativeHost.Until(() => client.Activities.Any(json => Heading(json) == "[Paused]: Luna Player"));
        artwork.Hold.SetResult(Icon);
        await NativeHost.Until(() => client.Activities.Any(json => json?.Contains(Icon, StringComparison.Ordinal) == true));
        var latest = client.Activities.Last(json => json is not null)!;
        Assert.AreEqual("[Paused]: Luna Player", Heading(latest));
        Assert.IsTrue(latest.Contains("Song title", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ShutdownCancelsArtworkAndRepeatedIdenticalSamplesDoNotResend()
    {
        var client = new Client();
        var artwork = new Artwork { Hold = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var options = new DiscordSettings { Enabled = true };
        var service = new PresenceService(Examples.English, () => client, Timing, artwork);
        service.Update(options, Media(false));
        await NativeHost.Until(() => client.Activities.Count == 1 && artwork.Calls > 0);
        for (var i = 0; i < 5; i++) service.Update(options, Media(false));
        await Task.Delay(60);
        Assert.AreEqual(1, client.Activities.Count);
        await service.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(2));
        await service.ShutdownAsync();
        Assert.IsNull(client.Activities.Last());
    }
}
