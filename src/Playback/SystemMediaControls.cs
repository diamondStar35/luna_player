using System.Runtime.InteropServices;
using LunaPlayer.Actions;
using Windows.Media;

// This file's MediaPlayer is the Windows Runtime one, not the player in this namespace.
using SessionPlayer = Windows.Media.Playback.MediaPlayer;

namespace LunaPlayer.Playback;

/// <summary>What the Windows media overlay should be showing right now.</summary>
internal readonly record struct MediaControlsState(
    bool HasMedia,
    bool IsPlaying,
    string Title,
    string Artist,
    double? Duration,
    double? Position,
    bool CanGoNext,
    bool CanGoPrevious);

/// <summary>Publishes what is playing to the Windows System Media Transport Controls - the overlay the OS
/// shows for the volume keys and the lock screen - and turns its buttons back into player actions.</summary>
///
/// <remarks>
/// A Windows Runtime media player is created purely to obtain a transport-controls session; it never plays
/// anything, and its own command manager is switched off so it cannot answer the buttons itself. Every
/// button press is reported through <see cref="ButtonPressed"/>, which is raised on a Windows Runtime
/// thread - the subscriber is responsible for moving that onto the UI thread.
///
/// The whole class is optional: if the session cannot be created, <see cref="IsAvailable"/> stays false and
/// every method does nothing, so playback works exactly as before on a system without it.
/// </remarks>
internal sealed class SystemMediaControls : IDisposable
{
    private readonly SessionPlayer? _session;
    private readonly SystemMediaTransportControls? _controls;
    private string _lastTitle = string.Empty;
    private string _lastArtist = string.Empty;
    private bool _disposed;

    internal SystemMediaControls()
    {
        try
        {
            _session = new SessionPlayer();
            _session.CommandManager.IsEnabled = false;
            _controls = _session.SystemMediaTransportControls;
            _controls.IsEnabled = true;
            EnableButtons(true);
            _controls.ButtonPressed += OnButtonPressed;
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException
            or PlatformNotSupportedException or TypeLoadException or NotSupportedException)
        {
            // No transport-controls session on this system; the player simply goes without the overlay.
            Release();
            _session = null;
            _controls = null;
        }
    }

    /// <summary>A media button was pressed. Raised on a Windows Runtime thread.</summary>
    internal event Action<ActionId>? ButtonPressed;

    internal bool IsAvailable => _controls is not null && !_disposed;

    /// <summary>Pushes the current playback state to the overlay. Cheap enough to call on a timer: the
    /// display is only rewritten when the text actually changes.</summary>
    internal void Update(in MediaControlsState state)
    {
        if (_controls is not { } controls || _disposed) return;
        try
        {
            var hasMedia = state.HasMedia;
            controls.IsPlayEnabled = hasMedia;
            controls.IsPauseEnabled = hasMedia;
            controls.IsStopEnabled = hasMedia;
            controls.IsFastForwardEnabled = hasMedia;
            controls.IsRewindEnabled = hasMedia;
            controls.IsNextEnabled = hasMedia && state.CanGoNext;
            controls.IsPreviousEnabled = hasMedia && state.CanGoPrevious;

            if (!hasMedia)
            {
                controls.PlaybackStatus = MediaPlaybackStatus.Closed;
                ClearDisplay(controls);
                return;
            }

            controls.PlaybackStatus = state.IsPlaying ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
            UpdateDisplay(controls, state.Title, state.Artist);
            UpdateTimeline(controls, state.Duration, state.Position);
        }
        catch (COMException)
        {
            // The session can go away underneath us - when the shell restarts, say. Losing the overlay is
            // not worth interrupting playback for.
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Release();
    }

    private void Release()
    {
        try
        {
            if (_controls is not null)
            {
                _controls.ButtonPressed -= OnButtonPressed;
                _controls.IsEnabled = false;
            }
            _session?.Dispose();
        }
        catch (COMException)
        {
        }
    }

    private void EnableButtons(bool enabled)
    {
        if (_controls is not { } controls) return;
        controls.IsPlayEnabled = enabled;
        controls.IsPauseEnabled = enabled;
        controls.IsStopEnabled = enabled;
        controls.IsNextEnabled = enabled;
        controls.IsPreviousEnabled = enabled;
        controls.IsFastForwardEnabled = enabled;
        controls.IsRewindEnabled = enabled;
    }

    private void OnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        var action = args.Button switch
        {
            SystemMediaTransportControlsButton.Play or SystemMediaTransportControlsButton.Pause => ActionId.PlayPause,
            SystemMediaTransportControlsButton.Next => ActionId.NextTrack,
            SystemMediaTransportControlsButton.Previous => ActionId.PreviousTrack,
            SystemMediaTransportControlsButton.FastForward => ActionId.SeekForward,
            SystemMediaTransportControlsButton.Rewind => ActionId.SeekBackward,
            _ => (ActionId?)null,
        };
        if (action is ActionId id) ButtonPressed?.Invoke(id);
    }

    private void UpdateDisplay(SystemMediaTransportControls controls, string title, string artist)
    {
        if (title == _lastTitle && artist == _lastArtist) return;
        _lastTitle = title;
        _lastArtist = artist;
        var updater = controls.DisplayUpdater;
        updater.Type = MediaPlaybackType.Music;
        updater.MusicProperties.Title = title;
        updater.MusicProperties.Artist = artist;
        updater.Update();
    }

    private void ClearDisplay(SystemMediaTransportControls controls)
    {
        if (_lastTitle.Length == 0 && _lastArtist.Length == 0) return;
        _lastTitle = string.Empty;
        _lastArtist = string.Empty;
        var updater = controls.DisplayUpdater;
        updater.Type = MediaPlaybackType.Unknown;
        updater.MusicProperties.Title = string.Empty;
        updater.MusicProperties.Artist = string.Empty;
        updater.Update();
    }

    private static void UpdateTimeline(SystemMediaTransportControls controls, double? duration, double? position)
    {
        if (duration is not > 0) return;
        var total = TimeSpan.FromSeconds(duration.Value);
        var elapsed = TimeSpan.FromSeconds(Math.Clamp(position ?? 0, 0, duration.Value));
        controls.UpdateTimelineProperties(new SystemMediaTransportControlsTimelineProperties
        {
            StartTime = TimeSpan.Zero,
            MinSeekTime = TimeSpan.Zero,
            Position = elapsed,
            MaxSeekTime = total,
            EndTime = total,
        });
    }
}
