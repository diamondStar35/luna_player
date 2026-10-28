using System.Globalization;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    private void HandleMetadataChange(string _, object? __)
    {
        if (!_disposed) MetadataChanged?.Invoke();
    }

    private void HandleVideoParamsChange(string _, object? __)
    {
        if (_disposed)
            return;
        var hasVideo = ComputeHasVideo();
        if (hasVideo == _hasVideo)
            return;
        _hasVideo = hasVideo;
        VideoAvailabilityChanged?.Invoke();
    }

    private void HandleTrackListChange(string _, object? __)
    {
        if (_disposed)
            return;
        AudioTracksChanged?.Invoke();
        SubtitleTracksChanged?.Invoke();
    }

    private void HandleSubTextChange(string _, object? value)
    {
        if (_disposed)
            return;
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            // The cue has cleared. Forget it so the very same line is read again if it comes back later.
            _lastSubText = null;
            return;
        }
        if (text == _lastSubText)
            return;
        _lastSubText = text;
        SubtitleTextChanged?.Invoke(text);
    }

    // A decoded video track with real dimensions that is neither a still image nor album art.
    private bool ComputeHasVideo()
    {
        if ((_session.ReadDouble("video-params/w") ?? 0) <= 0)
            return false;
        if (_session.ReadBoolean("current-tracks/video/image") == true)
            return false;
        return _session.ReadBoolean("current-tracks/video/albumart") != true;
    }
}
