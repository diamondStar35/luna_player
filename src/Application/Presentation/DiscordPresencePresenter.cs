using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;
using LunaPlayer.Playback;
using LunaPlayer.YouTube;

namespace LunaPlayer.Application.Presentation;

/// <summary>Samples playback only on the application thread. Independent of Windows media controls,
/// using the shared presentation tick to follow seeks, chapters and live stream metadata.</summary>
internal sealed class DiscordPresencePresenter
{
    private readonly Player _player;
    private readonly Settings _settings;
    private readonly MediaTagsCache _tags;
    private readonly Spotify.Playback.Sessions _spotify;
    private readonly Func<YouTubeResult?> _currentYouTube;
    private readonly Func<Radio.Station?> _currentStation;
    private readonly PresenceService _service;
    private readonly PresenceText _text;
    private bool _stopped;

    internal DiscordPresencePresenter(Player player, Settings settings,
        MediaTagsCache tags, Spotify.Playback.Sessions spotify, Func<YouTubeResult?> currentYouTube, Func<Radio.Station?> currentStation)
    {
        _player = player;
        _settings = settings;
        _tags = tags;
        _spotify = spotify;
        _currentYouTube = currentYouTube;
        _currentStation = currentStation;
        _text = PresenceText.Localized();
        _service = new(_text);
    }

    internal void Sync()
    {
        if (_stopped) return;
        var enabled = _settings.Discord.Enabled;
        var media = enabled ? PresenceMediaReader.Read(_player, _tags, _spotify, _text.Untitled,
            _settings.Discord.ShowDetails, _settings.Discord.ShowDetails ? _currentYouTube() : null,
            _settings.Discord.ShowDetails ? _currentStation() : null) : null;
        _service.Update(_settings.Discord, media);
    }

    internal Task ShutdownAsync()
    {
        _stopped = true;
        return _service.ShutdownAsync();
    }
}
