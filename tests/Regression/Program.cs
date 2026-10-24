using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using LunaPlayer.Application;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Spotify;
using LunaPlayer.UI;

await Run("bounded read-ahead and arbitrary seeks", BufferWindow);
await Run("demand requests preempt slow read-ahead", DemandPriority);
await Run("track cancellation aborts demand and speculative requests", Cancellation);
await Run("range validation and CDN fallback", RangeValidation);
await Run("AES-CTR decryption after unaligned seeks", Decryption);
await Run("close-all discards the suspended playlist", CloseAll);
await Run("stream shutdown preserves remembered local files", RememberStreams);
await Run("successful completion wins a cancellation race", CompletionRace);
Console.WriteLine("All 8 regression groups passed.");

static async Task Run(string name, Func<Task> test)
{
    await test().WaitAsync(TimeSpan.FromSeconds(30));
    Console.WriteLine("PASS: " + name);
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static async Task Until(Func<bool> condition)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    while (!condition()) await Task.Delay(10, timeout.Token);
}

static async Task BufferWindow()
{
    using var handler = new Ranges();
    using var http = new HttpClient(handler);
    using var stream = await BufferedAudioStream.OpenAsync(http, ["https://audio.test/a"], default);
    Check(handler.Requests.Count == 1, "Opening must only fetch the first requested block.");
    Check(stream.ReadByte() == Ranges.Value(0), "Initial read returned incorrect bytes.");
    await Until(() => handler.Requests.Any(r => r.End == 4 * 1024 * 1024 - 1));
    await Task.Delay(50);
    Check(handler.Requests.All(r => r.End < 4 * 1024 * 1024), "Read-ahead exceeded four MiB.");
    handler.BlockSpeculation = true;
    var buffer = new byte[91];
    for (var index = 0; index < 350; index++)
    {
        var position = ((long)index * 79 % 700) * 65536 + 123;
        stream.Position = position;
        stream.ReadExactly(buffer);
        Check(buffer.Select((value, offset) => value == Ranges.Value(position + offset)).All(x => x),
            "A seek returned bytes from the wrong position.");
    }
    var blocks = (System.Collections.IDictionary)typeof(BufferedAudioStream)
        .GetField("_blocks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(stream)!;
    Check(blocks.Count <= 320, "Retained blocks exceeded twenty MiB.");
    var requestCount = handler.Requests.Count;
    var recentPosition = stream.Position - buffer.Length;
    stream.Position = recentPosition;
    stream.ReadExactly(buffer);
    Check(!handler.Requests.Skip(requestCount).Any(r => r.Start == recentPosition / 65536 * 65536),
        "A recent backward seek unexpectedly refetched its block.");
    stream.Position = 123;
    stream.ReadExactly(buffer);
    Check(handler.Requests.Count(r => r.Start == 0) == 2, "An evicted block was not fetched on demand.");
    stream.Position = stream.Length - 3;
    Check(stream.Read(buffer) == 3 && stream.Read(buffer) == 0, "End of stream handling failed.");
    Check(stream.Read([], 0, 0) == 0, "An empty read must return zero.");
}

static async Task DemandPriority()
{
    using var handler = new Ranges { BlockSpeculation = true };
    using var http = new HttpClient(handler);
    using var stream = await BufferedAudioStream.OpenAsync(http, ["https://audio.test/a"], default);
    stream.ReadByte();
    await handler.SpeculationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    stream.Position = 30 * 1024 * 1024 + 123;
    var value = await Task.Run(stream.ReadByte).WaitAsync(TimeSpan.FromSeconds(2));
    Check(value == Ranges.Value(30 * 1024 * 1024 + 123), "Seek waited for speculative work.");
    await handler.SpeculationCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));
}

static async Task Cancellation()
{
    using var handler = new Ranges { BlockSpeculation = true };
    using var http = new HttpClient(handler);
    using var source = new CancellationTokenSource();
    using var stream = await BufferedAudioStream.OpenAsync(http, ["https://audio.test/a"], source.Token);
    stream.ReadByte();
    await handler.SpeculationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    source.Cancel();
    await handler.SpeculationCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));
    var blocks = (System.Collections.IDictionary)typeof(BufferedAudioStream)
        .GetField("_blocks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(stream)!;
    Check(blocks.Count == 0, "Changing tracks retained the old track's audio blocks.");
    try { stream.ReadByte(); throw new InvalidOperationException("A canceled stream kept playing."); }
    catch (OperationCanceledException) { }

    using var demandHandler = new Ranges { BlockDemandAt = 30 * 1024 * 1024 };
    using var demandHttp = new HttpClient(demandHandler);
    using var demandStream = await BufferedAudioStream.OpenAsync(demandHttp, ["https://audio.test/a"], default);
    demandStream.Position = 30 * 1024 * 1024;
    var reading = Task.Run(demandStream.ReadByte);
    await demandHandler.DemandStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    demandStream.Cancel();
    try { await reading.WaitAsync(TimeSpan.FromSeconds(2)); throw new InvalidOperationException("Demand was not canceled."); }
    catch (OperationCanceledException) { }
}

static async Task RangeValidation()
{
    using var handler = new Ranges();
    using var http = new HttpClient(handler);
    using var stream = await BufferedAudioStream.OpenAsync(http,
        ["https://bad.test/a", "https://audio.test/a"], default);
    Check(stream.ReadByte() == Ranges.Value(0), "A valid CDN alternative was not used.");
    Check(handler.Requests.Count >= 2, "An invalid range response was accepted.");
    try
    {
        using var invalid = await BufferedAudioStream.OpenAsync(http, ["https://bad.test/a"], default);
        throw new InvalidOperationException("An invalid CDN response was accepted.");
    }
    catch (IOException) { }
}

static Task Decryption()
{
    // NIST SP 800-38A F.5.1 AES-128 CTR known-answer vector, independent of the implementation.
    var key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
    var iv = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
    var encrypted = Convert.FromHexString(
        "874d6191b620e3261bef6864990db6ce" +
        "9806f66b7970fdff8617187bb9fffdff" +
        "5ae4df3edbd5d35e5b4f09020db03eab" +
        "1e031dda2fbe03d1792170a0f3009cee");
    var plain = Convert.FromHexString("6bc1bee22e409f96e93d7e117393172aae2d8a571e03ac9c9eb76fac45af8e5130c81c46a35ce411e5fbc1191a0a52eff69f2445df4f9b17ad2b417be66c3710");
    using var audio = new AudioDecryptStream(new MemoryStream(encrypted), key, iv, 0);
    var data = new byte[plain.Length];
    audio.ReadExactly(data);
    Check(data.SequenceEqual(plain), "The CTR reference vector failed.");
    audio.Position = 13;
    audio.ReadExactly(data.AsSpan(0, 33));
    Check(data.AsSpan(0, 33).SequenceEqual(plain.AsSpan(13, 33)), "Unaligned CTR seek failed.");
    using var body = new AudioDecryptStream(new MemoryStream(encrypted), key, iv, 17);
    Check(body.ReadByte() == plain[17] && body.Length == 47, "The Spotify body offset was ignored.");
    CryptographicOperations.ZeroMemory(key);
    return Task.CompletedTask;
}

static Task CloseAll()
{
    var engine = Proxy<IEngine>(method => method.Name == "Load" ? true : Default(method.ReturnType));
    using var player = new Player(engine);
    Check(player.OpenFiles(["https://local.test/one", "https://local.test/two"]), "Could not prepare playlist.");
    player.EnterSession();
    Check(player.PlaySessionStream("https://audio.test/track", "track", null, null), "Could not start session.");
    Check(player.CloseAll(), "Close-all failed during a session.");
    player.LeaveSession();
    Check(player.Count == 0 && player.CurrentPath is null && !player.InSession,
        "Close-all allowed the suspended playlist to return.");
    player.OpenFile("https://local.test/one");
    player.EnterSession();
    Check(player.CloseAll() && !player.InSession && player.Count == 0,
        "Close-all failed before the first session track loaded.");
    return Task.CompletedTask;
}

static Task RememberStreams()
{
    var settings = new Settings();
    settings.General.RememberLastPosition = true;
    settings.Playback.LastFile = "remembered.flac";
    settings.Playback.LastPosition = 123;
    foreach (var path in new[] { "https://youtube.test/video", "https://iptv.test/channel", "https://hls.test/live.m3u8", "https://stream.test/music", "luna-spotify://track" })
    {
        SessionState.Remember(settings, path, 4, null, 0);
        Check(settings.Playback.LastFile == "remembered.flac" && settings.Playback.LastPosition == 123,
            "A non-local stream replaced the remembered local session.");
    }
    var track = new Track("Track", "Artist", "spotify:track:0000000000000000000001", 120000);
    SessionState.Remember(settings, "luna-spotify://track", null, track, 45);
    Check(settings.Playback.LastSpotifyUri == track.Uri && settings.Playback.LastSpotifyPosition == 45,
        "Spotify resume information was not saved.");
    var local = typeof(Player).Assembly.Location;
    SessionState.Remember(settings, local, 22, null, 0);
    Check(settings.Playback.LastFile == local && settings.Playback.LastPosition == 22
        && settings.Playback.LastSpotifyUri.Length == 0, "Local playback restoration failed.");
    settings.General.RememberLastPosition = false;
    SessionState.Remember(settings, local, 40, track, 80);
    Check(settings.Playback.LastPosition == 22 && settings.Playback.LastSpotifyUri.Length == 0,
        "Disabled session remembering changed the saved state.");
    return Task.CompletedTask;
}

static async Task CompletionRace()
{
    foreach (var cancelTask in new[] { false, true })
    {
        var progress = new Progress();
        var dialogs = Proxy<IDialogService>(_ => progress);
        var view = Proxy<IMainView>(_ => dialogs);
        var dispatcher = new Dispatcher();
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var finished = new ManualResetEventSlim();
        var completed = false;
        BackgroundProgress.Start(view, dispatcher, new ProgressPrompt("test", "test", _ => "test"),
            (_, token) =>
            {
                started.Set();
                release.Wait();
                try { if (cancelTask) token.ThrowIfCancellationRequested(); return 42; }
                finally { finished.Set(); }
            }, value => completed = value == 42);
        Check(started.Wait(TimeSpan.FromSeconds(5)), "Work did not start.");
        progress.Cancelled = true;
        dispatcher.Tick!();
        release.Set();
        Check(finished.Wait(TimeSpan.FromSeconds(5)), "Work did not finish.");
        await Until(() =>
        {
            dispatcher.Tick!();
            dispatcher.Drain();
            return progress.Disposed;
        });
        Check(completed == !cancelTask, "Progress changed successful/canceled task completion semantics.");
    }
}

static object? Default(Type type) => type == typeof(void) || !type.IsValueType ? null : Activator.CreateInstance(type);
static T Proxy<T>(Func<MethodInfo, object?> invoke) where T : class
{
    var proxy = DispatchProxy.Create<T, Stub>();
    ((Stub)(object)proxy).Call = invoke;
    return proxy;
}

public class Stub : DispatchProxy
{
    public Func<MethodInfo, object?> Call { get; set; } = null!;
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Call(method!);
}

sealed class Progress : IProgressView
{
    public bool Cancelled { get; set; }
    public bool Disposed { get; private set; }
    public void Update(int percent, string message) { }
    public void Pulse() { }
    public void Dispose() => Disposed = true;
}

sealed class Dispatcher : IApplicationDispatcher
{
    private readonly ConcurrentQueue<Action> _posts = new();
    public Action? Tick { get; private set; }
    public void Post(Action action) => _posts.Enqueue(action);
    public IDisposable Repeat(TimeSpan interval, Action action) { Tick = action; return new Progress(); }
    public void Drain() { while (_posts.TryDequeue(out var action)) action(); }
}

sealed class Ranges : HttpMessageHandler
{
    public ConcurrentQueue<(long Start, long End)> Requests { get; } = new();
    public volatile bool BlockSpeculation;
    public long BlockDemandAt = -1;
    public TaskCompletionSource SpeculationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource SpeculationCancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource DemandStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public static byte Value(long position) => (byte)((position * 31 + 17) % 251);
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        const long length = 50 * 1024 * 1024 + 3;
        var range = request.Headers.Range!.Ranges.Single();
        var start = range.From!.Value;
        var end = Math.Min(length - 1, range.To!.Value);
        Requests.Enqueue((start, end));
        if (request.RequestUri!.Host == "bad.test") return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
        if (BlockSpeculation && end - start + 1 > 65536)
        {
            SpeculationStarted.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, token); }
            finally { SpeculationCancelled.TrySetResult(); }
        }
        if (start == BlockDemandAt)
        {
            DemandStarted.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
        }
        token.ThrowIfCancellationRequested();
        var data = new byte[checked((int)(end - start + 1))];
        for (var index = 0; index < data.Length; index++) data[index] = Value(start + index);
        var content = new ByteArrayContent(data);
        content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, length);
        return new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = content };
    }
}
