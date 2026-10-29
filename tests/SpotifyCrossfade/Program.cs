using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Spotify;
using LunaPlayer.Spotify.Playback;
using MpvNet;

namespace LunaPlayer.Tests.SpotifyCrossfade;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "src/LunaPlayer.csproj")))
            root = Directory.GetParent(root)?.FullName ?? throw new DirectoryNotFoundException("Luna repository root not found.");
        NativeLibrary.SetDllImportResolver(typeof(MPV).Assembly, (name, _, _) =>
            name == "mpv" ? NativeLibrary.Load(Path.Combine(root, "third-party/mpv.dll")) : 0);
        var cache = (ConcurrentDictionary<string, string>)typeof(Text).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        cache["You can return to Luna Player now."] = "You can return to Luna Player now.";
        cache["{name} - {artists}"] = "{name} - {artists}";

        if (args.Contains("--live"))
        {
            var credentials = CredentialStore.Load() ?? throw new InvalidOperationException("No saved Spotify playback sign-in is available.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(55));
            using var connection = await Connection.ConnectAsync(credentials, timeout.Token);
            if (args.Contains("--probe-sequence"))
            {
                foreach (var id in new[] { "0296EMdteBuK0TLMgNJ9y4", "34rRFl0bz9PocxWuO2ca5J", "1GkBG6kv0tDEDALIG7NR2K", "4uLU6hMCjMI75M1A2tKUQC" })
                {
                    try
                    {
                        using var audio = await connection.GetAudioAsync("spotify:track:" + id, 320, timeout.Token);
                        audio.ReadExactly(new byte[4]);
                        Console.WriteLine("Sequence OK " + id);
                    }
                    catch (Exception error) { Console.WriteLine("Sequence FAILED " + id + " " + error.GetType().Name); Environment.ExitCode = 1; }
                }
                return;
            }
            if (args.Contains("--probe"))
            {
                var id = args.FirstOrDefault(value => value.Length == 22 && value.All(char.IsAsciiLetterOrDigit)) ?? "1GkBG6kv0tDEDALIG7NR2K";
                try
                {
                    using var audio = await connection.GetAudioAsync("spotify:track:" + id, 320, timeout.Token);
                    var header = new byte[4]; audio.ReadExactly(header);
                    Console.WriteLine("PASS fresh-connection load of the stress-test failure");
                }
                catch (Exception error)
                {
                    Environment.ExitCode = 1;
                    Console.WriteLine("Fresh connection: " + error.GetType().Name);
                    for (var cause = error; cause is not null; cause = cause.InnerException)
                        if (cause is AudioKeyUnavailableException denied) Console.WriteLine("Key response code: " + denied.Code);
                }
                var saved = JsonSerializer.Deserialize(File.ReadAllBytes(Paths.SettingsFile), SettingsJsonContext.Default.Settings)!;
                using var auth = new Auth(); using var http = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/tracks/" + id + "?market=from_token");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await auth.GetAccessTokenAsync(saved.Spotify.ClientId, timeout.Token));
                using var response = await http.SendAsync(request, timeout.Token);
                Console.WriteLine("Catalog status: " + (int)response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
                    foreach (var field in new[] { "id", "is_playable", "restrictions", "linked_from" })
                        if (body.RootElement.TryGetProperty(field, out var value)) Console.WriteLine(field + ": " + value.GetRawText());
                }
                return;
            }
            if (args.Contains("--stress"))
            {
                var saved = JsonSerializer.Deserialize(File.ReadAllBytes(Paths.SettingsFile), SettingsJsonContext.Default.Settings)!;
                using var auth = new Auth();
                var catalog = new Catalog(() => auth, saved.Spotify);
                var page = await catalog.SearchAsync("music", filter: SearchFilter.Songs, token: timeout.Token);
                var regional = await catalog.SearchAsync("Amr Diab", filter: SearchFilter.Songs, token: timeout.Token);
                var tracks = page.Items.Concat(regional.Items).Select(item => item.Track).OfType<Track>().DistinctBy(track => track.Uri).ToArray();
                Check(tracks.Length >= 4, "Live stress test needs at least four catalog tracks.");
                using var engine = NewEngine(); using var player = new Player(engine);
                var dispatcher = new Dispatcher(); var options = Options(manual: true); options.CrossfadeMilliseconds = 3000;
                using var sessions = new Sessions(saved.Spotify, player, dispatcher,
                    (uri, bitrate, token) => connection.GetAudioAsync(uri, bitrate, token),
                    (uri, bitrate, token) => connection.GetAudioAsync(uri, bitrate, token, speculative: true));
                Configure(player, sessions, options);
                var errors = new List<Exception>(); sessions.CommandFailed += errors.Add;
                engine.Ended += reason => { var generation = sessions.CurrentLoadGeneration; dispatcher.Post(() => sessions.HandleEnded(reason, generation)); };
                sessions.Play(tracks[0], tracks, timeout.Token);
                await Until(dispatcher, () => errors.Count > 0 || engine.Duration is > 0 && !engine.IsLoading, seconds: 65);
                for (var index = 1; index < tracks.Length; index++)
                {
                    Check(errors.Count == 0, "Live stress failure: " + string.Join(",", errors.Select(error => error.GetType().Name)));
                    sessions.Next();
                    var selectedAt = System.Diagnostics.Stopwatch.StartNew();
                    if (index % 4 == 0)
                    {
                        await Task.Delay(35); dispatcher.Drain();
                    }
                    else
                    {
                        await Until(dispatcher, () => errors.Count > 0 || player.CurrentSource == sessions.CurrentLink && !engine.IsLoading, seconds: 65);
                        await Task.Delay(index % 2 == 0 ? 250 : 800); dispatcher.Drain();
                    }
                    Console.WriteLine("Live stress selection " + index + "/" + (tracks.Length - 1));
                    Console.WriteLine("Selection observed after " + selectedAt.ElapsedMilliseconds + "ms");
                }
                await Until(dispatcher, () => errors.Count > 0 || player.CurrentSource == sessions.CurrentLink && !engine.IsLoading, seconds: 65);
                Check(errors.Count == 0 && sessions.CurrentIndex == tracks.Length - 1, "Live stress navigation failed.");
                await Task.Delay(1500); dispatcher.Drain();
                Check(errors.Count == 0 && sessions.IsPlaying, "A late failure interrupted healthy live playback.");
                Console.WriteLine("PASS varied-track live stress: " + tracks.Length + " tracks");
                return;
            }
            if (args.Contains("--double"))
            {
                await DoubleNext((uri, bitrate, token) => connection.GetAudioAsync(uri, bitrate, token),
                    [new Track("Live 1", "", "spotify:track:4uLU6hMCjMI75M1A2tKUQC", 0),
                     new Track("Live 2", "", "spotify:track:11dFghVXANMlKmJXsNCbNl", 0),
                     new Track("Live 3", "", "spotify:track:4uLU6hMCjMI75M1A2tKUQC", 0),
                     new Track("Live 4", "", "spotify:track:11dFghVXANMlKmJXsNCbNl", 0)]);
                Console.WriteLine("PASS live double Next");
                return;
            }
            foreach (var id in new[] { "4uLU6hMCjMI75M1A2tKUQC", "11dFghVXANMlKmJXsNCbNl" })
            {
                try
                {
                    using var audio = await connection.GetAudioAsync("spotify:track:" + id, 320, timeout.Token);
                    var header = new byte[4]; audio.ReadExactly(header);
                    Check(header.AsSpan().SequenceEqual("OggS"u8) || header[0] == 0xff || header.AsSpan().StartsWith("ID3"u8), "Unexpected decrypted audio header.");
                    Console.WriteLine("PASS live Spotify audio header");
                }
                catch (Exception error)
                {
                    Console.WriteLine("Live Spotify load failed: " + error.GetType().Name);
                    Console.WriteLine(error.StackTrace);
                    Environment.ExitCode = 1;
                }
            }
            if (Environment.ExitCode == 0)
            {
                using var engine = NewEngine(); using var player = new Player(engine);
                var dispatcher = new Dispatcher(); var options = Options(manual: true);
                using var sessions = new Sessions(new SpotifySettings(), player, dispatcher,
                    (uri, bitrate, token) => connection.GetAudioAsync(uri, bitrate, token),
                    (uri, bitrate, token) => connection.GetAudioAsync(uri, bitrate, token, speculative: true));
                Configure(player, sessions, options);
                var errors = new ConcurrentQueue<Exception>();
                sessions.CommandFailed += errors.Enqueue;
                engine.Ended += reason => { var generation = sessions.CurrentLoadGeneration; dispatcher.Post(() => sessions.HandleEnded(reason, generation)); };
                var tracks = new[] {
                    new Track("Live track 1", "", "spotify:track:4uLU6hMCjMI75M1A2tKUQC", 0),
                    new Track("Live track 2", "", "spotify:track:11dFghVXANMlKmJXsNCbNl", 0) };
                sessions.Play(tracks[0], tracks, timeout.Token);
                await Until(dispatcher, () => errors.Count > 0 || Field(sessions, "_successorUrl") is string && !engine.IsLoading);
                Check(errors.IsEmpty, "Initial live playback failed.");
                sessions.Next();
                await Until(dispatcher, () => errors.Count > 0 || !engine.IsLoading && engine.Duration is > 0);
                Check(errors.IsEmpty && sessions.CurrentIndex == 1, "Prepared live navigation failed.");
                Console.WriteLine("PASS live Spotify prepared crossfade");
                sessions.Previous();
                await Until(dispatcher, () => errors.Count > 0 || player.CurrentSource == sessions.CurrentLink && !engine.IsLoading && engine.Duration is > 0);
                Check(errors.IsEmpty && sessions.CurrentIndex == 0, "Uncached live navigation failed.");
                Console.WriteLine("PASS live Spotify uncached Previous");
            }
            return;
        }

        if (args.Contains("--double"))
        {
            await Run("two Next commands during an overlap", () => DoubleNext());
            return;
        }

        await Run("local settings also control Spotify", SharedSettings);
        await Run("registrations coexist and release individually", Registrations);
        await Run("Spotify streams can reopen after prefetch cancellation", Reopen);
        await Run("equal-power native advance is adopted exactly once", () => Automatic(CrossfadeCurve.EqualPower));
        await Run("linear native advance is adopted exactly once", () => Automatic(CrossfadeCurve.Linear));
        await Run("manual navigation retains outgoing audio and paused state", Manual);
        await Run("prepared manual Next starts immediately without another fetch", FastNext);
        await Run("two Next commands during an overlap", () => DoubleNext());
        await Run("cancelling one decoder aborts only its own HTTP read", ReaderCancellation);
        await Run("speculative page reads do not navigate", Paging);
        await Run("late prepared audio is disposed after source exit", StalePreparation);
        await Run("repeat, stop-at-end, and sleep holds prevent queueing", AdvanceGuards);
        await Run("failed speculation leaves current playback running", FailedPreparation);
        await Run("cancelled navigation shares the pending audio key", SharedAudioKey);
        await Run("audio-key cache is bounded and clears owned keys", AudioKeyCleanup);
        await Run("failed audio-key requests remain retryable", FailedAudioKey);
        await Run("canonical track IDs and denied audio versions fall back correctly", TransportAudit.Alternatives);
        await Run("CDN retries recover transient failures and respect cancellation", TransportAudit.RangeRetry);
        await Run("early-expired Spotify tokens refresh without signing in again", TransportAudit.TokenRefresh);
        await Run("decoder recovery is bounded and preserves pause state", DecoderRecovery);
        await Run("a late decoder error cannot end the replacement entry", StaleDecoderError);
        await Run("an error queued before Stop cannot restart Spotify", StaleControllerError);
        await Run("a late error dialog cannot interrupt a successful retry", LateFailureDialog);
        await Run("failure dialogs require exact exception, generation, attempt and revision", FailureOwnership);
        await Run("abandoned queued tracks do not request audio keys", AbandonedAudioKey);
        await Run("temporary key refusal backs off without trying extra formats", TemporaryAudioKey);
        await Run("foreground keys overtake queued prefetch and promote shared requests", AudioKeyPriority);
        await Run("a refused key cannot block healthy or cached selections", IndependentKeyRetry);
        await Run("repeated temporary refusals finish with the original error", BoundedKeyRetry);
        await Run("unchanged successor preparation is retained", StableSuccessor);
        await Run("metadata is shared without sharing decoder ownership", TransportAudit.SharedMetadata);
        await Run("connection preserves temporary refusal without quality fallback", TransportAudit.TemporaryRefusal);
        Console.WriteLine("All Spotify crossfade checks passed.");

        static async Task Run(string name, Func<Task> test)
        {
            await test();
            Console.WriteLine("PASS " + name);
        }
        static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
        static object? Field(object owner, string name)
        {
            var type = owner.GetType();
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return field is not null ? field.GetValue(owner) : owner is Engine
                ? Field(Field(owner, "_coordinator")!, name) : throw new MissingFieldException(type.Name, name);
        }
        static MPV Mpv(Engine engine) => ((MpvSession)Field(engine, "_session")!).Native;
        static Engine NewEngine()
        {
            var engine = new Engine(0);
            Mpv(engine).SetProperty("ao", "null");
            Mpv(engine).SetProperty("vo", "null");
            return engine;
        }
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
        static Task SharedSettings()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var audio = Options();
            player.ConfigurePlaybackEffects(audio);
            player.BeginSpotifySession();
            Check((bool)Field(engine, "_crossfade")! && (int)Field(engine, "_crossfadeMilliseconds")! == 500,
                "Spotify did not use local crossfade settings.");
            audio.Crossfade = false; player.ConfigurePlaybackEffects(audio);
            Check(!(bool)Field(engine, "_crossfade")!, "Disabling shared crossfade did not disable Spotify.");
            audio.Crossfade = true; audio.CrossfadeMilliseconds = 2222; player.ConfigurePlaybackEffects(audio);
            player.LeaveSession();
            Check((bool)Field(engine, "_crossfade")! && (int)Field(engine, "_crossfadeMilliseconds")! == 2222,
                "Leaving Spotify changed shared settings.");
            var settings = new Settings { Audio = audio };
            var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.Settings);
            var restored = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.Settings)!;
            Check(restored.Audio.Crossfade && restored.Audio.CrossfadeMilliseconds == 2222, "Shared settings did not round-trip.");
            Check(!json.Split("\"spotify\"")[1].Contains("crossfade", StringComparison.OrdinalIgnoreCase), "Spotify-specific crossfade settings remain.");
            var older = JsonSerializer.Deserialize("{\"audio\":{\"crossfade\":true,\"crossfadeMilliseconds\":2222},\"spotify\":{\"crossfade\":false,\"crossfadeMilliseconds\":9999}}", SettingsJsonContext.Default.Settings)!;
            Check(older.Audio.Crossfade && older.Audio.CrossfadeMilliseconds == 2222, "Obsolete Spotify fields overrode the shared controls.");
            return Task.CompletedTask;
        }
        static Task Registrations()
        {
            using var engine = NewEngine();
            var first = Audio(); var second = Audio();
            var firstUrl = engine.RegisterSpotifyAudio(first);
            var secondUrl = engine.RegisterSpotifyAudio(second);
            Check(first.CanRead && second.CanRead, "New registration disposed another pending stream.");
            engine.ReleaseSpotifyAudio(firstUrl);
            Check(!first.CanRead && second.CanRead, "Release affected another stream.");
            engine.ReleaseSpotifyAudio(secondUrl);
            Check(!second.CanRead, "Unclaimed stream was retained.");
            var abandoned = Audio(); engine.RegisterSpotifyAudio(abandoned);
            engine.Stop();
            Check(!abandoned.CanRead, "Stop retained an unopened Spotify stream.");
            return Task.CompletedTask;
        }
        static Task Reopen()
        {
            using var engine = NewEngine();
            var audio = Audio(); var url = engine.RegisterSpotifyAudio(audio);
            IMpvStream Open() => (IMpvStream)typeof(PlaybackCoordinator).GetMethod("OpenSpotifyStream", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Field(engine, "_coordinator"), [url])!;
            using var first = Open();
            using var second = Open();
            var a = new byte[128]; var b = new byte[128];
            first.Read(a, 0, a.Length); second.Read(b, 0, b.Length);
            Check(a.SequenceEqual(b), "Reopened readers do not have independent positions.");
            first.Cancel();
            using var third = Open();
            third.Read(b, 0, b.Length);
            Check(a.SequenceEqual(b), "Prefetch cancellation invalidated a later open.");
            engine.ReleaseSpotifyAudio(url);
            Check(audio.CanRead, "Registration release disposed an active decoder.");
            first.Dispose(); second.Dispose(); third.Dispose();
            Check(!audio.CanRead, "Final decoder close retained retired audio.");
            return Task.CompletedTask;
        }
        static async Task Automatic(CrossfadeCurve curve)
        {
            using var engine = NewEngine();
            using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options();
            options.CrossfadeCurve = curve;
            var opens = new ConcurrentDictionary<string, int>();
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (uri, _, _) =>
            {
                opens.AddOrUpdate(uri, 1, (_, count) => count + 1);
                return Task.FromResult(Audio());
            });
            Configure(player, sessions, options);
            engine.Ended += reason => { var generation = sessions.CurrentLoadGeneration; dispatcher.Post(() => sessions.HandleEnded(reason, generation)); };
            var tracks = new[] { Track(1), Track(2) };
            sessions.CommandFailed += error => Console.WriteLine(error);
            sessions.Play(tracks[0], tracks, default);
            try { await Until(dispatcher, () => Field(sessions, "_successorUrl") is string && engine.Duration is > 0); }
            catch { Console.WriteLine($"path={player.CurrentPath} duration={engine.Duration} source={player.CurrentSource} loadedUri={Field(sessions, "_loadedUri")} crossfade={Field(engine, "_crossfade")} next={Field(sessions, "_successor")} loading={engine.IsLoading}"); throw; }
            await Until(dispatcher, () => (bool)Field(engine, "_queuedNext")!);
            engine.SeekAbsolute(11);
            await Until(dispatcher, () => sessions.CurrentIndex == 1);
            Check(player.CurrentSource == sessions.CurrentLink, "Player and Spotify session disagree after transition.");
            Check(opens[tracks[1].Uri] == 1, "Queued transition reopened incoming audio.");
            Check(((System.Collections.IDictionary)Field(engine, "_spotifyAudio")!).Count == 1,
                "Automatic advancement retained outgoing registrations.");
            sessions.HandleEnded(EndReason.QueuedAdvance, sessions.CurrentLoadGeneration);
            Check(sessions.CurrentIndex == 1 && opens[tracks[1].Uri] == 1, "Duplicate transition changed the queue.");
        }
        static async Task Manual()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options(manual: true);
            CancellationToken outgoing = default;
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (uri, _, token) =>
            {
                if (uri == Track(1).Uri) outgoing = token;
                return Task.FromResult(Audio());
            });
            Configure(player, sessions, options);
            var tracks = new[] { Track(1), Track(2), Track(3), Track(4) };
            sessions.Play(tracks[0], tracks, default);
            await Until(dispatcher, () => engine.Duration is > 0 && player.IsPlaying);
            var oldPath = player.CurrentPath;
            sessions.Next();
            sessions.Next();
            Check(!outgoing.IsCancellationRequested, "Navigation cancelled outgoing audio before mpv released it.");
            Check(player.CurrentPath == oldPath && player.IsPlaying, "Manual navigation stopped outgoing audio before preparation.");
            await Until(dispatcher, () => player.CurrentSource == sessions.CurrentLink && player.CurrentPath != oldPath);
            await Until(dispatcher, () => !engine.IsLoading);
            sessions.Pause();
            sessions.Next();
            await Until(dispatcher, () => player.CurrentSource == sessions.CurrentLink);
            Check(player.IsPaused && !sessions.IsPlaying, "Paused navigation started playback.");
        }
        static async Task FastNext()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options(manual: true);
            var tracks = new[] { Track(1), Track(2) };
            var fetches = 0; var errors = 0;
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) =>
            {
                Interlocked.Increment(ref fetches);
                return Task.FromResult(Audio());
            });
            sessions.CommandFailed += _ => errors++;
            Configure(player, sessions, options);
            sessions.Play(tracks[0], tracks, default);
            await Until(dispatcher, () => Field(sessions, "_successorUrl") is string && !engine.IsLoading);
            Check(fetches == 2, "Preparation fetched more than one successor.");
            sessions.Next();
            Check(player.CurrentSource == sessions.CurrentLink && sessions.CurrentIndex == 1,
                "Prepared navigation was delayed instead of opening immediately.");
            await Until(dispatcher, () => !engine.IsLoading && engine.Duration is > 0);
            Check(fetches == 2 && errors == 0, "Prepared navigation fetched again or failed to reopen prefetched audio.");
        }
        static async Task DoubleNext(Func<string, int, CancellationToken, Task<AudioDecryptStream>>? openAudio = null, Track[]? liveTracks = null)
        {
            foreach (var delay in new[] { 0, 20, 80, 200, 500 })
            {
                using var engine = NewEngine(); using var player = new Player(engine);
                var dispatcher = new Dispatcher(); var options = Options(manual: true);
                options.CrossfadeMilliseconds = 3000;
                using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, openAudio ?? ((_, _, _) => Task.FromResult(Audio())));
                Configure(player, sessions, options);
                var errors = new ConcurrentQueue<Exception>();
                sessions.CommandFailed += errors.Enqueue;
                engine.Ended += reason => { var generation = sessions.CurrentLoadGeneration; dispatcher.Post(() => sessions.HandleEnded(reason, generation)); };
                var tracks = liveTracks ?? new[] { Track(1), Track(2), Track(3), Track(4) };
                sessions.Play(tracks[0], tracks, default);
                await Until(dispatcher, () => errors.Count > 0 || Field(sessions, "_successorUrl") is string && !engine.IsLoading);
                Check(errors.IsEmpty, "Initial load before double Next failed at " + delay + "ms: " + string.Join("; ", errors.Select(error => error.GetType().Name + " " + error.StackTrace)));
                sessions.Next();
                await Task.Delay(delay);
                dispatcher.Drain();
                sessions.Next();
                await Until(dispatcher, () => errors.Count > 0 || sessions.CurrentIndex == 2 && player.CurrentSource == sessions.CurrentLink && !engine.IsLoading);
                Check(errors.IsEmpty, "Double Next failed at " + delay + "ms: " + string.Join("; ", errors.Select(error => error.GetType().Name + " " + error.StackTrace)));
                await Task.Delay(150); dispatcher.Drain();
                Check(errors.IsEmpty, "Late decoder error after double Next at " + delay + "ms.");
                if (liveTracks is not null) Console.WriteLine("PASS live double Next at " + delay + "ms");
            }
        }
        static async Task ReaderCancellation()
        {
            using var engine = NewEngine();
            using var handler = new HeldRange(); using var http = new HttpClient(handler);
            var buffered = await BufferedAudioStream.OpenAsync(http, ["https://audio.test/track"], default);
            var audio = new AudioDecryptStream(buffered, new byte[16]);
            var url = engine.RegisterSpotifyAudio(audio);
            IMpvStream Open() => (IMpvStream)typeof(PlaybackCoordinator).GetMethod("OpenSpotifyStream", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Field(engine, "_coordinator"), [url])!;
            using var first = Open(); using var second = Open();
            first.Seek(131072);
            var reading = Task.Run(() => first.Read(new byte[128], 0, 128));
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            first.Cancel();
            try { await reading.WaitAsync(TimeSpan.FromSeconds(2)); throw new InvalidOperationException("Cancelled read returned successfully."); }
            catch (OperationCanceledException) { }
            Check(second.Read(new byte[128], 0, 128) == 128, "One decoder cancelled the entire backing stream.");
            engine.ReleaseSpotifyAudio(url);
        }
        static async Task Paging()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options();
            var page = new TaskCompletionSource<TrackPage>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
            Configure(player, sessions, options);
            var first = Track(1); var second = Track(2);
            sessions.Play(first, [first], default, pageLoader: (_, _) => page.Task, nextPage: "next");
            await Until(dispatcher, () => sessions.CurrentTrack == first && engine.Duration is > 0);
            page.SetResult(new TrackPage([second], null));
            await Until(dispatcher, () => sessions.CurrentQueue.Count == 2);
            Check(sessions.CurrentTrack == first && sessions.CurrentIndex == 0, "Speculative page loading navigated.");
        }
        static async Task StalePreparation()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options();
            var pending = new TaskCompletionSource<AudioDecryptStream>(TaskCreationOptions.RunContinuationsAsynchronously);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => { started.TrySetResult(); return pending.Task; });
            Configure(player, sessions, options);
            sessions.Play(Track(1), null, default);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            sessions.Stop();
            var audio = Audio(); pending.SetResult(audio);
            await Until(dispatcher, () => !audio.CanRead);
            Check(!player.InSpotifySession, "Stale result reentered Spotify.");
        }
        static async Task AdvanceGuards()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options();
            var audio = Options(); audio.EndBehavior = EndBehavior.None;
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
            Configure(player, sessions, options, audio);
            var tracks = new[] { Track(1), Track(2) };
            sessions.Play(tracks[0], tracks, default);
            await Until(dispatcher, () => engine.Duration is > 0);
            Check(Field(sessions, "_successorUrl") is null, "Stop-at-end prepared an advancing successor.");
            audio.EndBehavior = EndBehavior.Advance; player.SetEndBehavior(EndBehavior.Advance);
            await Until(dispatcher, () => Field(sessions, "_successorUrl") is string);
            sessions.ToggleRepeatCurrent();
            Check(Field(sessions, "_successorUrl") is null && !(bool)Field(engine, "_queuedNext")!, "Repeat retained a successor.");
            player.HoldAutomaticAdvance(true);
            sessions.ToggleRepeatCurrent();
            await Task.Delay(100); dispatcher.Drain();
            Check(!(bool)Field(engine, "_queuedNext")!, "Sleep hold allowed an advancing successor.");
            player.HoldAutomaticAdvance(false);
            await Until(dispatcher, () => Field(sessions, "_successorUrl") is string);
        }
        static async Task FailedPreparation()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options();
            var first = Track(1); var second = Track(2);
            var attempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (uri, _, _) =>
            {
                if (uri == first.Uri) return Task.FromResult(Audio());
                attempted.TrySetResult();
                throw new IOException("Simulated speculative failure.");
            });
            var errors = 0; sessions.CommandFailed += _ => errors++;
            Configure(player, sessions, options);
            sessions.Play(first, [first, second], default);
            await Until(dispatcher, () => attempted.Task.IsCompleted);
            await Until(dispatcher, () => engine.Duration is > 0);
            Check(errors == 0 && sessions.CurrentTrack == first && player.IsPlaying, "Speculative failure interrupted current playback.");
        }
        static async Task DecoderRecovery()
        {
            foreach (var permanent in new[] { false, true })
            {
                using var engine = NewEngine(); using var player = new Player(engine);
                var dispatcher = new Dispatcher(); var options = Options(); options.Crossfade = false;
                var opens = 0; var errors = new List<Exception>();
                using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) =>
                {
                    opens++;
                    return Task.FromResult(permanent || opens == 1
                        ? new AudioDecryptStream(new MemoryStream(new byte[2048]), new byte[16]) : Audio());
                });
                Configure(player, sessions, options);
                sessions.CommandFailed += errors.Add;
                engine.Ended += reason => { var generation = sessions.CurrentLoadGeneration; dispatcher.Post(() => sessions.HandleEnded(reason, generation)); };
                sessions.Play(Track(1), null, default);
                sessions.Pause();
                await Until(dispatcher, () => errors.Count > 0 || opens == 2 && !engine.IsLoading && engine.Duration is > 0);
                Check(opens == 2, "Decoder recovery did not retry exactly once.");
                Check(permanent ? errors.Count == 1 && errors[0] is PlaybackDecodeException : errors.Count == 0 && player.IsPaused,
                    "Decoder recovery lost pause state or hid a permanent failure.");
            }
        }
        static Task StaleDecoderError()
        {
            using var engine = NewEngine(); var errors = 0;
            engine.Ended += reason => { if (reason == EndReason.Error) errors++; };
            void Set(string name, object value) => typeof(PlaybackCoordinator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Field(engine, "_coordinator"), value);
            Set("_awaitingLoadRestart", true); Set("_loadingEntryId", 42L); Set("_loadingSourceEntryId", 1L);
            void Error(long entry) => typeof(PlaybackCoordinator).GetMethod("HandleEndFile", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Field(engine, "_coordinator"),
                [new MpvEvent { EventId = MpvEventId.EndFile, Data = new MpvEndFileEvent { Reason = MpvEndFileReason.Error, PlaylistEntryId = entry } }]);
            Error(41);
            Check(errors == 0, "An outgoing entry's late error was attributed to its replacement.");
            Error(42);
            Check(errors == 1, "A genuine replacement failure was ignored.");
            return Task.CompletedTask;
        }
        static async Task StaleControllerError()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options(); options.Crossfade = false;
            var opens = 0;
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => { opens++; return Task.FromResult(Audio()); });
            Configure(player, sessions, options);
            sessions.Play(Track(1), null, default);
            await Until(dispatcher, () => !engine.IsLoading && engine.Duration is > 0);
            var controller = (ApplicationController)RuntimeHelpers.GetUninitializedObject(typeof(ApplicationController));
            void Set(string name, object value) => typeof(ApplicationController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(controller, value);
            Set("_spotify", sessions); Set("_player", player); Set("_dispatcher", dispatcher);
            typeof(ApplicationController).GetMethod("OnPlaybackEnded", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(controller, [EndReason.Error]);
            player.Stop();
            dispatcher.Drain();
            Check(opens == 1 && !(bool)Field(sessions, "_playbackRecoveryAttempted")!, "A stale error restarted stopped playback.");
        }
        static async Task LateFailureDialog()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options(); options.Crossfade = false;
            var opens = 0; var reported = new List<Exception>();
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) =>
                ++opens == 1 ? Task.FromException<AudioDecryptStream>(new TrackUnavailableException()) : Task.FromResult(Audio()));
            Configure(player, sessions, options);
            sessions.CommandFailed += reported.Add;
            var track = Track(1);
            sessions.Play(track, null, default);
            await Until(dispatcher, () => reported.Count == 1);
            var generation = sessions.CurrentLoadGeneration;
            typeof(Sessions).GetMethod("LoadManaged", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sessions, [track.Uri, 0.0]);
            await Until(dispatcher, () => !engine.IsLoading && engine.Duration is > 0);
            Check(sessions.CurrentLoadGeneration == generation && player.IsPlaying, "Retry test did not retain the selected track.");
            Check(!sessions.TakeCurrentFailure(reported[0]), "A failure from the previous attempt can still stop successful playback.");
            var current = new TrackUnavailableException();
            typeof(Sessions).GetMethod("ReportFailure", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sessions, [current]);
            Check(sessions.TakeCurrentFailure(current) && !sessions.TakeCurrentFailure(current), "A genuine failure was hidden or presented twice.");
        }
        static async Task FailureOwnership()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher(); var options = Options(); options.Crossfade = false;
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) => Task.FromResult(Audio()));
            Configure(player, sessions, options);
            sessions.Play(Track(1), null, default);
            await Until(dispatcher, () => !engine.IsLoading && engine.Duration is > 0);
            var report = typeof(Sessions).GetMethod("ReportFailure", BindingFlags.NonPublic | BindingFlags.Instance)!;
            Exception Report()
            {
                var error = new TrackUnavailableException(); report.Invoke(sessions, [error]); return error;
            }
            void Change(string name, object value) => typeof(Sessions).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(sessions, value);
            var identity = Report();
            Check(!sessions.TakeCurrentFailure(new TrackUnavailableException()) && sessions.TakeCurrentFailure(identity)
                && !sessions.TakeCurrentFailure(identity), "Exception identity or single consumption was not enforced.");
            var attempt = Report();
            Change("_loadAttempt", (int)Field(sessions, "_loadAttempt")! + 1);
            Check(!sessions.TakeCurrentFailure(attempt), "A previous load attempt still owns the dialog.");
            var generation = Report();
            Change("_loadGeneration", sessions.CurrentLoadGeneration + 1);
            Check(!sessions.TakeCurrentFailure(generation), "A previous selection generation still owns the dialog.");
            var track = Report(); var current = sessions.CurrentTrack!;
            Change("_current", Track(2));
            Check(!sessions.TakeCurrentFailure(track), "A different track still owns the dialog.");
            Change("_current", current);
            var revision = Report(); player.Stop();
            Check(!sessions.TakeCurrentFailure(revision), "A previous native playback revision still owns the dialog.");
        }
        static async Task SharedAudioKey()
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
        static async Task AudioKeyCleanup()
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
        static async Task FailedAudioKey()
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
        static async Task AbandonedAudioKey()
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
        static async Task AudioKeyPriority()
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
        static async Task IndependentKeyRetry()
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
        static async Task BoundedKeyRetry()
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
        static async Task StableSuccessor()
        {
            using var engine = NewEngine(); using var player = new Player(engine);
            var dispatcher = new Dispatcher();
            var pending = new TaskCompletionSource<AudioDecryptStream>(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0;
            using var sessions = new Sessions(new SpotifySettings(), player, dispatcher, (_, _, _) =>
                Interlocked.Increment(ref calls) == 1 ? Task.FromResult(Audio()) : pending.Task);
            Configure(player, sessions, Options());
            var tracks = new[] { Track(1), Track(2) };
            sessions.Play(tracks[0], tracks, default);
            await Until(dispatcher, () => Volatile.Read(ref calls) == 2);
            var refresh = typeof(Sessions).GetMethod("RefreshSuccessor", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var cancellation = Field(sessions, "_successorCancellation");
            refresh.Invoke(sessions, null); refresh.Invoke(sessions, null);
            Check(ReferenceEquals(cancellation, Field(sessions, "_successorCancellation")) && calls == 2,
                "Refreshing an unchanged successor restarted its preparation.");
            pending.SetResult(Audio());
            await Until(dispatcher, () => Field(sessions, "_successorUrl") is string);
            var url = Field(sessions, "_successorUrl");
            refresh.Invoke(sessions, null);
            Check(Equals(url, Field(sessions, "_successorUrl")) && calls == 2, "A prepared successor was discarded unchanged.");
        }
        static async Task TemporaryAudioKey()
        {
            var calls = 0;
            using var keys = new AudioKeys((_, _, _) => ++calls == 1
                ? Task.FromException<byte[]>(new AudioKeyUnavailableException(2))
                : Task.FromResult(new byte[16]), default, minimumInterval: TimeSpan.Zero, retryDelay: TimeSpan.Zero);
            await keys.GetAsync(new TrackId(1), new byte[20], default);
            Check(calls == 2, "A temporary refusal was treated as an unavailable track.");
        }
    }
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
