using LunaPlayer.Actions;
using LunaPlayer.Configuration;
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
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly MediaTagsCache _tags;
    private readonly SystemMediaControls _mediaControls = new();
    private IDisposable? _clock;

    internal MediaOverlayPresenter(
        MediaPlayer player,
        PlayerSettings settings,
        IApplicationDispatcher dispatcher,
        MediaTagsCache tags,
        Action<ActionId> onButton)
    {
        _player = player;
        _settings = settings;
        _dispatcher = dispatcher;
        _tags = tags;
        _mediaControls.ButtonPressed += action => _dispatcher.Post(() => onButton(action));
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
        var tags = _tags.For(path);
        _mediaControls.Update(new MediaControlsState(
            HasMedia: hasMedia,
            IsPlaying: _player.IsPlaying,
            // What the file calls itself, when it says; the name on disk is only a stand-in for that.
            Title: tags.Title.Length > 0 ? tags.Title : _player.CurrentDisplayName ?? string.Empty,
            Artist: tags.Artist,
            Album: tags.Album,
            Duration: hasMedia ? _player.Duration : null,
            Position: hasMedia ? _player.Elapsed : null,
            CanGoNext: hasMedia && index >= 0 && index < _player.Count - 1,
            CanGoPrevious: hasMedia && index > 0));
    }

    /// <summary>Runs the clock only while a file is open - paused counts, because a paused file can still be
    /// seeked - so the scrubber keeps up with moves nothing else reports.</summary>
    internal void UpdateClock()
    {
        var wanted = _mediaControls.IsAvailable && _player.IsLoaded;
        if (wanted == (_clock is not null))
            return;
        if (wanted)
            _clock = _dispatcher.Repeat(TimeSpan.FromSeconds(1), Sync);
        else
            StopClock();
    }

    internal void StopClock()
    {
        _clock?.Dispose();
        _clock = null;
    }

    public void Dispose()
    {
        StopClock();
        _mediaControls.Dispose();
    }
}
