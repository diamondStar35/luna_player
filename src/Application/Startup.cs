using LunaPlayer.Accessibility;
using LunaPlayer.Bookmarks;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Iptv;
using LunaPlayer.Media;
using LunaPlayer.Playback;

namespace LunaPlayer.Application;

/// <summary>Creates the settings, native services, and managed data needed during application startup.
/// None of these methods owns windows or decides when startup results are attached to the UI.</summary>
internal static class Startup
{
    internal static StartupSettings LoadSettings()
    {
        var store = new SettingsStore(Paths.SettingsFile);
        var settings = store.LoadForStartup();
        return new StartupSettings(store, settings, store.LastError);
    }

    internal static async Task<StartupResources> CreateServicesAsync(nint nativeHandle, Settings settings)
    {
        var playerTask = Task.Factory.StartNew(
            () =>
            {
                // Audio is the only background service on the launch critical path. Give it a dedicated,
                // higher-priority thread so optional feature construction cannot hold it in the pool queue.
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                return CreatePlayer(nativeHandle, settings);
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default);
        var speechTask = Task.Run<ISpeechOutput>(() => new SpeechOutput(settings));
        var mediaControlsTask = Task.Run(() => new SystemMediaControls());
        var featuresTask = Task.Run(() => FeatureResources.Create(settings));
        try
        {
            await Task.WhenAll(speechTask, playerTask, mediaControlsTask, featuresTask).ConfigureAwait(false);
            return new StartupResources(
                speechTask.Result, playerTask.Result, mediaControlsTask.Result, featuresTask.Result);
        }
        catch
        {
            if (speechTask.Status == TaskStatus.RanToCompletion)
                speechTask.Result.Dispose();
            if (playerTask.Status == TaskStatus.RanToCompletion)
                playerTask.Result.Dispose();
            if (mediaControlsTask.Status == TaskStatus.RanToCompletion)
                mediaControlsTask.Result.Dispose();
            throw;
        }
    }

    internal static async Task<WindowResources> LoadWindowResourcesAsync(
        Settings settings,
        SettingsStore store)
    {
        var catalogTask = Task.Run(() => new Recording.AudioCatalog());
        var equalizerTask = Task.Run(() => new Equalizer.Library(
            settings, store, new Equalizer.PresetStore(Paths.EqualizerFile)));
        var recentsTask = Task.Run(() => new RecentsStore(Paths.RecentsFile));
        await Task.WhenAll(catalogTask, equalizerTask, recentsTask).ConfigureAwait(false);
        return new WindowResources(catalogTask.Result, equalizerTask.Result, recentsTask.Result);
    }

    /// <summary>Resolves launch paths before command wiring is ready. Folder walking is the expensive part,
    /// so it starts beside audio initialization instead of waiting behind every application feature.</summary>
    internal static PreparedLaunch PrepareLaunch(IReadOnlyList<string> arguments, Settings settings)
    {
        var convert = arguments.Any(path => path.Equals("--convert", StringComparison.OrdinalIgnoreCase));
        var paths = new List<string>();
        foreach (var argument in arguments)
        {
            if (argument.Equals("--convert", StringComparison.OrdinalIgnoreCase))
                continue;
            var value = argument.Trim().Trim('"');
            if (value.Length > 0 && Paths.TryAbsolute(value, out var absolute)
                && (File.Exists(absolute) || Directory.Exists(absolute)))
                paths.Add(absolute);
        }
        if (paths.Count == 0)
            return new PreparedLaunch(convert, [], null, null, null);

        if (convert)
        {
            var files = new List<string>();
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                    files.AddRange(MediaLibrary.CollectFiles(path, recursive: true));
                else
                    files.Add(path);
            }
            return new PreparedLaunch(true, paths, files, null, null);
        }

        var first = paths[0];
        if (Directory.Exists(first))
        {
            var recursive = settings.General.OpenFilesMode == OpenFilesMode.MainAndSubfolders;
            var files = MediaLibrary.CollectFiles(first, recursive);
            return new PreparedLaunch(false, paths, files, null, first);
        }
        if (File.Exists(first) && !MediaLibrary.IsPlaylist(first)
            && settings.General.OpenFilesMode != OpenFilesMode.FileOnly)
        {
            var folder = Path.GetDirectoryName(first);
            if (!string.IsNullOrEmpty(folder))
            {
                var recursive = settings.General.OpenFilesMode == OpenFilesMode.MainAndSubfolders;
                var files = MediaLibrary.CollectFiles(folder, recursive);
                return new PreparedLaunch(false, paths, files, first, null);
            }
        }
        return new PreparedLaunch(false, paths, null, null, null);
    }

    private static Player CreatePlayer(nint nativeHandle, Settings settings)
    {
        var engine = new Engine(nativeHandle, !settings.General.DisableMediaControls);
        try
        {
            var player = new Player(engine, new PositionStore(Paths.PositionsFile));
            ConfigurePlayer(player, settings);
            return player;
        }
        catch
        {
            engine.Dispose();
            throw;
        }
    }

    private static void ConfigurePlayer(Player player, Settings settings)
    {
        player.SetVolume(settings.Audio.Volume);
        settings.Audio.Pitch = player.SetPitch(settings.Audio.Pitch);
        player.SetSpeed(settings.Audio.Speed);
        player.TrackPositions(settings.Audio.SaveFilePositions);
        player.SetEndBehavior(settings.Audio.EndBehavior);
        player.ConfigurePlaybackEffects(settings.Audio);
        player.ConfigureSilence(settings.Silence);
        player.SetNormalization(settings.Audio.NormalizeAudio);
        player.SetMono(settings.Audio.MonoAudio);
        player.SetPan(settings.Audio.Pan);
        player.SetSilenceRemoval(settings.Silence.Enabled);
        settings.Audio.NormalizeAudio = player.IsNormalizationEnabled;
        settings.Audio.MonoAudio = player.IsMonoEnabled;
        settings.Silence.Enabled = player.IsSilenceRemovalEnabled;
        if (!string.IsNullOrWhiteSpace(settings.Audio.Device))
            player.SetAudioDevice(settings.Audio.Device);
    }
}

internal sealed record StartupSettings(SettingsStore Store, Settings Settings, string Error);

internal sealed record WindowResources(
    Recording.AudioCatalog Catalog,
    Equalizer.Library Equalizer,
    RecentsStore Recents);

internal sealed record PreparedLaunch(
    bool Convert,
    IReadOnlyList<string> Paths,
    IReadOnlyList<string>? Files,
    string? PreferredPath,
    string? SourceFolder);

/// <summary>Owns worker-created services until the application host transfers each one to its final owner.
/// </summary>
internal sealed class StartupResources(
    ISpeechOutput speech,
    Player player,
    SystemMediaControls mediaControls,
    FeatureResources features)
{
    private ISpeechOutput? _speech = speech;
    private Player? _player = player;
    private SystemMediaControls? _mediaControls = mediaControls;
    private FeatureResources? _features = features;

    internal ISpeechOutput TakeSpeech()
        => Interlocked.Exchange(ref _speech, null)
            ?? throw new InvalidOperationException("Speech startup resources were already transferred.");

    internal Player TakePlayer()
        => Interlocked.Exchange(ref _player, null)
            ?? throw new InvalidOperationException("Playback startup resources were already transferred.");

    internal SystemMediaControls TakeMediaControls()
        => Interlocked.Exchange(ref _mediaControls, null)
            ?? throw new InvalidOperationException("Media controls startup resources were already transferred.");

    internal FeatureResources TakeFeatures()
        => Interlocked.Exchange(ref _features, null)
            ?? throw new InvalidOperationException("Feature startup resources were already transferred.");

    internal void Dispose(bool disposeSpeech = true)
    {
        Interlocked.Exchange(ref _mediaControls, null)?.Dispose();
        Interlocked.Exchange(ref _player, null)?.Dispose();
        _ = Interlocked.Exchange(ref _features, null);
        var remainingSpeech = Interlocked.Exchange(ref _speech, null);
        if (disposeSpeech)
            remainingSpeech?.Dispose();
    }
}

/// <summary>Managed feature foundations that are safe to construct away from the wxWidgets thread.</summary>
internal sealed record FeatureResources(
    PlaybackSelection Selection,
    SelectionSlots Selections,
    BookmarkStore Bookmarks,
    FavoriteStore Favorites,
    IptvSourceStore IptvSources,
    Recording.RecordingSources RecordingSources,
    YouTube.Client.Resolver Resolver,
    YouTube.Client.Updater Updater,
    YouTube.Metadata.Client Metadata,
    YouTube.Backend YouTube,
    YouTube.Components.Installer Installer)
{
    internal static FeatureResources Create(Settings settings)
    {
        var runner = new YouTube.Client.Runner(settings);
        var channels = new YouTube.Client.Channels(runner);
        var metadata = new YouTube.Metadata.Client();
        return new FeatureResources(
            new PlaybackSelection(),
            new SelectionSlots(),
            new BookmarkStore(Paths.BookmarksFile),
            new FavoriteStore(Paths.FavoritesFile),
            new IptvSourceStore(Paths.IptvSourcesFile),
            new Recording.RecordingSources(settings.Recording),
            new YouTube.Client.Resolver(runner),
            new YouTube.Client.Updater(runner),
            metadata,
            new YouTube.Backend(metadata, channels),
            new YouTube.Components.Installer());
    }
}
