namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    private void OnAudioFilterFailed(AudioFilter filter) => AudioFilterFailed?.Invoke(filter);

    // Reconcile the requested state on the UI thread after a background filter failure.
    internal void DisableFailedFilter(AudioFilter filter)
    {
        switch (filter)
        {
            case AudioFilter.Normalization: SetNormalization(false); break;
            case AudioFilter.Mono: SetMono(false); break;
            case AudioFilter.Spatialization: SetSofaEnabled(false); break;
            case AudioFilter.SilenceRemoval: SetSilenceRemoval(false); break;
            case AudioFilter.Equalizer: SetEqualizer(null); break;
            case AudioFilter.Pitch: SetPitch(0); break;
            case AudioFilter.Pan: SetPan(0); break;
        }
        RefreshState();
    }
}
