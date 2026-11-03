using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Spotify;
using LunaPlayer.Spotify.Playback;
using LunaPlayer.Tests.Infrastructure;
using MpvNet;

namespace LunaPlayer.Tests.Spotify;

[TestClass, DoNotParallelize, TestCategory("Integration")]
public sealed partial class CrossfadeTests
{
    [TestInitialize]
    public void Initialize()
    {
        NativeHost.Initialize();
        // Keep these tests independent of wx's application loop and current UI language.
        var cache = (ConcurrentDictionary<string, string>)typeof(Text).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        cache["You can return to Luna Player now."] = "You can return to Luna Player now.";
        cache["{name} - {artists}"] = "{name} - {artists}";
    }

    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    static object? Field(object owner, string name) => owner switch
    {
        Engine engine when name == "_coordinator" => FixtureFields.Read(typeof(Engine), engine, name),
        Engine engine => FixtureFields.Read(typeof(PlaybackCoordinator), Field(engine, "_coordinator")!, name),
        Sessions sessions => FixtureFields.Read(typeof(Sessions), sessions, name),
        _ => throw new InvalidOperationException("Unsupported test fixture owner."),
    };

    static MPV Mpv(Engine engine) => (MPV)FixtureFields.Read(typeof(MpvSession), Field(engine, "_session")!, "_mpv")!;

    static Engine NewEngine() => NativeHost.Create();

    static Track Track(int n) => new("Track " + n, "Artist", "spotify:track:" + n.ToString("D22"), 12000);

    static AudioDecryptStream Audio()
    {
        using var wav = new MemoryStream();
        using var writer = new BinaryWriter(wav);
        const int samples = 48000 * 12;
        writer.Write("RIFF"u8); writer.Write(36 + samples * 2); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(48000);
        writer.Write(96000); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(samples * 2);
        for (var i = 0; i < samples; i++) writer.Write((short)(Math.Sin(i * 2 * Math.PI * 440 / 48000) * 2000));
        var clear = wav.ToArray();
        var encrypted = new byte[clear.Length];
        using (var cipher = new AudioDecryptStream(new MemoryStream(clear), new byte[16])) cipher.ReadExactly(encrypted);
        return new AudioDecryptStream(new MemoryStream(encrypted), new byte[16]);
    }

    static async Task Until(Dispatcher dispatcher, Func<bool> condition, int seconds = 10)
    {
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(seconds))
        {
            dispatcher.Drain();
            if (condition()) return;
            await Task.Delay(10);
        }
        throw new TimeoutException("Playback condition did not become true.");
    }

    static AudioSettings Options(bool manual = false) => new() { Crossfade = true, CrossfadeMilliseconds = 500, CrossfadeManualNavigation = manual };

    static void Configure(Player player, Sessions sessions, AudioSettings options, AudioSettings? audio = null)
    {
        audio ??= options;
        player.ConfigurePlaybackEffects(audio);
        player.SetEndBehavior(audio.EndBehavior);
        sessions.ConfigurePlayback(audio, _ => null, (_, _) => { });
    }
}

internal static class FixtureFields
{
    // Explicit type annotations retain the fixture fields in the native test runner.
    internal static object? Read([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)] Type type, object owner, string name)
        => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner);
}

sealed class Dispatcher : IApplicationDispatcher
{
    private readonly ConcurrentQueue<Action> _posts = new();
    public void Post(Action action) => _posts.Enqueue(action);
    public IDisposable Repeat(TimeSpan interval, Action action) => throw new NotSupportedException();
    public void Drain() { while (_posts.TryDequeue(out var action)) action(); }
}

sealed class HeldRange : HttpMessageHandler
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        const int length = 1024 * 1024;
        var range = request.Headers.Range!.Ranges.Single();
        var start = range.From!.Value; var end = Math.Min(range.To!.Value, length - 1);
        if (start == 131072 && end - start + 1 == 65536)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
        }
        var body = new ByteArrayContent(new byte[checked((int)(end - start + 1))]);
        body.Headers.ContentRange = new ContentRangeHeaderValue(start, end, length);
        return new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = body };
    }
}
