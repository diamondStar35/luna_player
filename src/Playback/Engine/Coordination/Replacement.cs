namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    private const int DeclickMilliseconds = 5;
    private Replacement? _replacement;

    private sealed record Replacement(LoadRequest Request, bool Paused, long Generation);

    private bool BeginReplacement(LoadRequest request, bool paused, long generation)
    {
        _replacement = new(request, paused, generation);
        _manualRequest = request;
        return _session.TryDo(mpv =>
        {
            if (_disposed || generation != Volatile.Read(ref _loadGeneration)) return;
            mpv.FadePause(DeclickMilliseconds / 1000.0);
            // A paused/idle output can finish immediately without a new pause notification.
            if (mpv.GetProperty("pause") is true)
                CompleteReplacement(generation);
        }, _ => CompleteReplacement(generation));
    }

    private void CompleteReplacement(long generation)
    {
        lock (_loadSync)
        {
            if (_disposed || _replacement is not { } replacement || replacement.Generation != generation)
                return;
            var request = _manualRequest ?? replacement.Request;
            var paused = _loadingPaused ?? replacement.Paused;
            _replacement = null;
            _manualRequest = null;
            LoadCore(request, paused);
        }
    }

    private void FinishReplacement(long generation, int milliseconds)
        => _session.TryDo(mpv =>
        {
            if (_disposed || generation != Volatile.Read(ref _loadGeneration)) return;
            // Native work is serialized here, after the destination decoder is ready and before
            // the output envelope rises. No filter setup holds the playback or UI lock.
            if (!_audio.CompleteEqualizerLoad()) AudioFilterFailed?.Invoke(AudioFilter.Equalizer);
            bool paused;
            lock (_loadSync)
            {
                if (_disposed || generation != _loadGeneration) return;
                paused = _loadingPaused ?? _session.ReadBoolean("pause") ?? false;
                _loadingPaused = null;
            }
            // A seek or user pause issued while opening retains ownership of its envelope.
            if (mpv.GetProperty("audio-fade/transport-active") is true) return;
            if (paused)
                mpv.SetProperty("pause", true);
            else
            {
                mpv.FadePlay(milliseconds / 1000.0);
                _replacementMuted = false;
            }
        });
}
