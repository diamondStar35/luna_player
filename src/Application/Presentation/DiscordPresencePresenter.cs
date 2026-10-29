using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;
using LunaPlayer.Playback;
using LunaPlayer.YouTube;

namespace LunaPlayer.Application.Presentation;

/// <summary>Samples playback only on the application thread. Independent of Windows media controls,
/// with a clock to follow seeks, loops, chapter boundaries and live stream metadata.</summary>
internal sealed class DiscordPresencePresenter
{
    private readonly Player _player;
    private readonly Settings _settings;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly MediaTagsCache _tags;
    private readonly Spotify.Playback.Sessions _spotify;
    private readonly Func<YouTubeResult?> _currentYouTube;
    private readonly PresenceService _service = new();
    private readonly ActivityBuilder _builder = new(DateTimeOffset.UtcNow);
    private readonly PresenceText _text;
    private IDisposable? _clock;
    private bool _stopped;

    internal DiscordPresencePresenter(Player player, Settings settings, IApplicationDispatcher dispatcher,
        MediaTagsCache tags, Spotify.Playback.Sessions spotify, Func<YouTubeResult?> currentYouTube)
    {
        _player = player;
        _settings = settings;
        _dispatcher = dispatcher;
        _tags = tags;
        _spotify = spotify;
        _currentYouTube = currentYouTube;
        _text = new(
            // Translators: Discord activity when Luna has no media loaded.
            Tr("Idle"),
            // Translators: Discord activity state while playback is running.
            Tr("Playing"),
            // Translators: Discord activity state while playback is paused.
            Tr("Paused"),
            // Translators: Discord activity when media details are hidden.
            Tr("Using Luna Player"),
            // Translators: Discord fallback when the current media has no safe display title.
            Tr("Untitled media"),
            // Translators: Discord button opening the public page of the current video.
            Tr("Watch online"),
            // Translators: Discord button opening the public page of the current audio.
            Tr("Listen online"),
            // Translators: Discord button opening Luna Player's releases page.
            Tr("Download Luna Player"));
    }

    internal void Sync()
    {
        if (_stopped) return;
        var enabled = _settings.Discord.Enabled && PublisherApplication.IsConfigured;
        if (enabled && _clock is null) _clock = _dispatcher.Repeat(TimeSpan.FromSeconds(1), Sync);
        else if (!enabled) { _clock?.Dispose(); _clock = null; }
        var media = enabled ? PresenceMediaReader.Read(_player, _tags, _spotify, _text.Untitled,
            _settings.Discord.ShowDetails, _settings.Discord.ShowDetails ? _currentYouTube() : null) : null;
        _service.Update(_settings.Discord, _builder.Build(_settings.Discord, media, _text, DateTimeOffset.UtcNow));
    }

    internal Task ShutdownAsync()
    {
        _stopped = true;
        _clock?.Dispose();
        _clock = null;
        return _service.ShutdownAsync();
    }
}

/// <summary>Maps native playback state to a shareable snapshot. In particular, a retained playlist path
/// after Stop is not playing media, and an input URL is never used as a fallback title.</summary>
internal static class PresenceMediaReader
{
    internal static PresenceMedia? Read(Player player, MediaTagsCache tagsCache, Spotify.Playback.Sessions spotify,
        string untitled, bool details, YouTubeResult? youTube = null)
    {
        if (!player.IsLoaded && !(spotify.IsActive && spotify.CurrentTrack is not null)) return null;
        var track = spotify.IsActive ? spotify.CurrentTrack : null;
        var media = new PresenceMedia(track?.Uri ?? player.CurrentItemId ?? string.Empty,
            string.Empty, string.Empty, null,
            spotify.IsActive ? spotify.IsPlaying : player.IsPlaying,
            !spotify.IsActive && player.HasVideo,
            spotify.IsActive ? spotify.Elapsed : player.Elapsed,
            spotify.IsActive ? spotify.Duration : player.Duration, player.Speed);
        if (!details) return media;
        if (track is not null)
            return media with { Title = track.Name, Artist = track.Artists, PublicUrl = spotify.CurrentLink };
        var tags = tagsCache.For(player);
        var live = player.CurrentMetadata.Display;
        var title = tags.Title.Length > 0 ? tags.Title : player.CurrentTitle ?? live.Title;
        if (string.IsNullOrWhiteSpace(title)) title = player.CurrentDisplayName ?? string.Empty;
        var artist = tags.Artist.Length > 0 ? tags.Artist : live.Artist;
        if (youTube is { } video && video.Url == player.CurrentSource)
        {
            if (!string.IsNullOrWhiteSpace(video.Title)) title = video.Title;
            if (!string.IsNullOrWhiteSpace(video.Author)) artist = video.Author;
        }
        // mpv sometimes supplies its input address as the title. Do not expose raw URLs or full paths.
        if (Uri.TryCreate(title, UriKind.Absolute, out _) || title.StartsWith(@"\\", StringComparison.Ordinal))
            title = untitled;
        return media with
        {
            Title = title,
            Artist = artist,
            PublicUrl = player.CurrentSource ?? player.CurrentPath,
        };
    }
}
