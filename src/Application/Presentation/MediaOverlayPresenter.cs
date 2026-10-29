using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.Playback;

namespace LunaPlayer.Application.Presentation;

/// <summary>Publishes what is playing to the Windows media overlay (SMTC) and keeps its scrubber moving.
/// </summary>
///
/// <remarks>
/// The position playing has reached is the one thing the overlay needs that nothing raises an event for:
/// mpv does not announce the passage of time, nor a seek, so a clock covers the gap while a file is open.
/// Overlay button presses arrive on a Windows Runtime thread, so they are posted back through the
/// dispatcher as ordinary action requests rather than run where they land.
/// </remarks>
internal sealed class MediaOverlayPresenter : IDisposable
{
    private readonly Player _player;
    private readonly Settings _settings;
    private readonly MediaTagsCache _tags;
    private readonly SystemMediaControls _mediaControls;
    private readonly Spotify.Playback.Sessions _spotify;
    private readonly Func<bool> _canGoNext;
    private readonly Func<bool> _canGoPrevious;

    internal MediaOverlayPresenter(
        Player player,
        Settings settings,
        IApplicationDispatcher dispatcher,
        MediaTagsCache tags,
        SystemMediaControls mediaControls,
        Spotify.Playback.Sessions spotify,
        Func<bool> canGoNext,
        Func<bool> canGoPrevious,
        Action<ActionId> onButton)
    {
        _player = player;
        _settings = settings;
        _tags = tags;
        _mediaControls = mediaControls;
        _spotify = spotify;
        _canGoNext = canGoNext;
        _canGoPrevious = canGoPrevious;
        _mediaControls.ButtonPressed += action => dispatcher.Post(() => onButton(action));
    }

    /// <summary>Brings the overlay into line with the general setting that governs it, read live so a change
    /// from preferences while the player runs takes effect.</summary>
    internal void ApplySetting()
        => _mediaControls.SetEnabled(!_settings.General.DisableMediaControls);

    /// <summary>Publishes the current state to the overlay.</summary>
    internal void Sync()
    {
        if (!_mediaControls.IsAvailable) return;
        var path = _player.CurrentPath;
        var hasMedia = !string.IsNullOrEmpty(path);
        var index = _player.CurrentIndex;
        var tags = _spotify.IsActive && _spotify.CurrentTrack is { } track
            ? new MediaTags(track.Name, track.Artists, string.Empty)
            : _tags.For(_player);
        _mediaControls.Update(new MediaControlsState(
            HasMedia: hasMedia,
            IsPlaying: _player.IsPlaying,
            // What the file calls itself, when it says; the name on disk is only a stand-in for that.
            Title: tags.Title.Length > 0 ? tags.Title : _player.CurrentDisplayName ?? string.Empty,
            Artist: tags.Artist,
            Album: tags.Album,
            Duration: hasMedia ? _player.Duration : null,
            Position: hasMedia ? _player.Elapsed : null,
            CanGoNext: hasMedia && (_player.InSession ? _canGoNext() : index >= 0 && index < _player.Count - 1),
            CanGoPrevious: hasMedia && (_player.InSession ? _canGoPrevious() : index > 0)));
    }

    internal bool RequiresTick => _mediaControls.IsAvailable && _player.IsLoaded;

    public void Dispose() => _mediaControls.Dispose();
}
