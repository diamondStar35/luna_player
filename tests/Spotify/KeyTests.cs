using LunaPlayer.Spotify.Playback;

namespace LunaPlayer.Tests.Spotify;

[TestClass, TestCategory("Unit")]
public sealed class KeyTests
{
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    [TestMethod]
    public async Task SharedAudioKey()
    {
        using var lifetime = new CancellationTokenSource();
        using var navigation = new CancellationTokenSource();
        var response = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = 0; CancellationToken transport = default;
        using var keys = new AudioKeys((_, _, token) =>
        {
            requests++; transport = token;
            started.TrySetResult();
            return response.Task;
        }, lifetime.Token, minimumInterval: TimeSpan.Zero, retryDelay: TimeSpan.Zero);
        var track = new TrackId(1); var file = new byte[20];
        var first = keys.GetAsync(track, file, navigation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        navigation.Cancel();
        try { await first; throw new InvalidOperationException("Cancelled navigation kept waiting for a key."); }
        catch (OperationCanceledException) { }
        var second = keys.GetAsync(track, file, default);
        Check(requests == 1 && !transport.IsCancellationRequested, "Navigation cancelled or duplicated the transport request.");
        response.SetResult(Enumerable.Repeat((byte)7, 16).ToArray());
        var key = await second;
        Array.Clear(key);
        var cached = await keys.GetAsync(track, file, default);
        Check(requests == 1 && cached.All(value => value == 7), "Returned-key cleanup corrupted the cache or caused another request.");
    }

    [TestMethod]
    public async Task AudioKeyCleanup()
    {
        var owned = new List<byte[]>();
        var keys = new AudioKeys((_, _, _) =>
        {
            var key = Enumerable.Repeat((byte)9, 16).ToArray(); owned.Add(key);
            return Task.FromResult(key);
        }, default, minimumInterval: TimeSpan.Zero, retryDelay: TimeSpan.Zero);
        for (var n = 0; n < 65; n++) await keys.GetAsync(new TrackId((UInt128)n), new byte[20], default);
        Check(owned[0].All(value => value == 0), "Eviction retained an owned audio key.");
        keys.Dispose();
        Check(owned.All(key => key.All(value => value == 0)), "Disposal retained owned audio keys.");
    }

    [TestMethod]
    public async Task FailedAudioKey()
    {
        var requests = 0;
        using var keys = new AudioKeys((_, _, _) => ++requests == 1
            ? Task.FromException<byte[]>(new InvalidDataException())
            : Task.FromResult(new byte[16]), default, minimumInterval: TimeSpan.Zero, retryDelay: TimeSpan.Zero);
        try { await keys.GetAsync(new TrackId(1), new byte[20], default); throw new InvalidOperationException("A real key failure was hidden."); }
        catch (InvalidDataException) { }
        await keys.GetAsync(new TrackId(1), new byte[20], default);
        Check(requests == 2, "A failed key request was cached permanently.");
    }

    [TestMethod]
    public async Task AbandonedAudioKey()
    {
        var response = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<UInt128>();
        using var keys = new AudioKeys((track, _, _) =>
        {
            calls.Add(track.Value);
            return track.Value == 1 ? response.Task : Task.FromResult(new byte[16]);
        }, default, minimumInterval: TimeSpan.Zero, retryDelay: TimeSpan.Zero);
        var first = keys.GetAsync(new TrackId(1), new byte[20], default);
        using var navigation = new CancellationTokenSource();
        var abandoned = keys.GetAsync(new TrackId(2), new byte[20], navigation.Token);
        navigation.Cancel();
        try { await abandoned; throw new InvalidOperationException("Abandoned selection did not cancel."); }
        catch (OperationCanceledException) { }
        response.SetResult(new byte[16]); await first;
        await keys.GetAsync(new TrackId(3), new byte[20], default);
        Check(calls.SequenceEqual(new UInt128[] { 1, 3 }), "An abandoned queued selection still requested a key.");
    }

    [TestMethod]
    public async Task AudioKeyPriority()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<UInt128>();
        using var keys = new AudioKeys((track, _, _) =>
        {
            calls.Add(track.Value);
            if (track.Value != 1) return Task.FromResult(new byte[16]);
            started.SetResult();
            return response.Task;
        }, default, minimumInterval: TimeSpan.Zero);
        var first = keys.GetAsync(new TrackId(1), new byte[20], default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var background = keys.GetAsync(new TrackId(2), new byte[20], default, speculative: true);
        var promoted = keys.GetAsync(new TrackId(4), new byte[20], default, speculative: true);
        var foreground = keys.GetAsync(new TrackId(3), new byte[20], default);
        var shared = keys.GetAsync(new TrackId(4), new byte[20], default);
        response.SetResult(new byte[16]);
        await Task.WhenAll(first, background, promoted, foreground, shared).WaitAsync(TimeSpan.FromSeconds(2));
        Check(calls.SequenceEqual(new UInt128[] { 1, 4, 3, 2 }), "Foreground priority or shared-request promotion failed.");
    }

    [TestMethod]
    public async Task IndependentKeyRetry()
    {
        var refused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<UInt128>();
        using var keys = new AudioKeys((track, _, _) =>
        {
            calls.Add(track.Value);
            if (track.Value != 1) return Task.FromResult(new byte[16]);
            refused.TrySetResult();
            return Task.FromException<byte[]>(new AudioKeyUnavailableException(2));
        }, default, minimumInterval: TimeSpan.Zero, retryDelay: TimeSpan.FromHours(1));
        await keys.GetAsync(new TrackId(2), new byte[20], default);
        using var selection = new CancellationTokenSource();
        var retry = keys.GetAsync(new TrackId(1), new byte[20], selection.Token, speculative: true);
        await refused.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await keys.GetAsync(new TrackId(3), new byte[20], default).WaitAsync(TimeSpan.FromSeconds(1));
        await keys.GetAsync(new TrackId(2), new byte[20], default).WaitAsync(TimeSpan.FromSeconds(1));
        Check(!retry.IsCompleted, "A temporary refusal did not wait for its own retry time.");
        selection.Cancel();
        try { await retry; throw new InvalidOperationException("Abandoned retry did not cancel."); }
        catch (OperationCanceledException) { }
        await keys.GetAsync(new TrackId(4), new byte[20], default).WaitAsync(TimeSpan.FromSeconds(1));
        Check(calls.SequenceEqual(new UInt128[] { 2, 1, 3, 4 }), "A cooldown blocked unrelated work or retried an abandoned selection.");
    }

    [TestMethod]
    public async Task BoundedKeyRetry()
    {
        var calls = 0;
        var original = new AudioKeyUnavailableException(2);
        using var keys = new AudioKeys((_, _, _) =>
        {
            calls++;
            return Task.FromException<byte[]>(original);
        }, default, minimumInterval: TimeSpan.Zero, retryDelay: TimeSpan.Zero);
        try
        {
            await keys.GetAsync(new TrackId(1), new byte[20], default).WaitAsync(TimeSpan.FromSeconds(2));
            throw new InvalidOperationException("A genuine foreground refusal was hidden.");
        }
        catch (AudioKeyUnavailableException error) { Check(ReferenceEquals(error, original), "The original refusal identity was lost."); }
        Check(calls == 2, "Temporary refusals caused an unbounded retry loop.");
    }

    [TestMethod]
    public async Task TemporaryAudioKey()
    {
        var calls = 0;
        using var keys = new AudioKeys((_, _, _) => ++calls == 1
            ? Task.FromException<byte[]>(new AudioKeyUnavailableException(2))
            : Task.FromResult(new byte[16]), default, minimumInterval: TimeSpan.Zero, retryDelay: TimeSpan.Zero);
        await keys.GetAsync(new TrackId(1), new byte[20], default);
        Check(calls == 2, "A temporary refusal was treated as an unavailable track.");
    }
}
