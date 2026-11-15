using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;

namespace LunaPlayer.Application.Preferences;

internal static class Playback
{
    internal static bool Apply(Settings settings, Changes changes, Player player, Action<AudioFilter> failed)
    {
        var previousAudio = settings.Audio.Copy();
        var previousSilence = settings.Silence.Copy();
        if (!changes.ApplyTo(settings)) return false;
        if (changes.Contains("audio.saveFilePositions")) player.TrackPositions(settings.Audio.SaveFilePositions);
        if (changes.Contains("audio.audiobookMode")) player.SetAudiobookMode(settings.Audio.AudiobookMode);
        if (changes.Contains("audio.endBehavior")) player.SetEndBehavior(settings.Audio.EndBehavior);
        if (changes.Contains("audio.fadeIntervalMilliseconds", "audio.fadeSeeking", "audio.seekFadeIntervalMilliseconds",
            "audio.fadeFileTransitions", "audio.fadePauseResume", "audio.gaplessPlayback", "audio.crossfade",
            "audio.crossfadeMilliseconds", "audio.crossfadeCurve", "audio.crossfadeManualNavigation", "audio.wrapPlaylist"))
            player.ConfigurePlaybackEffects(settings.Audio);
        if (changes.Contains("silence.advanced", "silence.startPeriods", "silence.startDuration", "silence.threshold",
            "silence.stopPeriods", "silence.stopDuration", "silence.stopSilence", "silence.window", "silence.detection")
            && !player.ConfigureSilence(settings.Silence))
        {
            var enabled = settings.Silence.Enabled;
            settings.Silence.Apply(previousSilence);
            settings.Silence.Enabled = enabled;
            failed(AudioFilter.SilenceRemoval);
        }
        if (changes.Contains("audio.normalizeAudio"))
        {
            if (!player.SetNormalization(settings.Audio.NormalizeAudio)) failed(AudioFilter.Normalization);
            settings.Audio.NormalizeAudio = player.IsNormalizationEnabled;
        }
        if (changes.Contains("audio.monoAudio"))
        {
            if (!player.SetMono(settings.Audio.MonoAudio)) failed(AudioFilter.Mono);
            settings.Audio.MonoAudio = player.IsMonoEnabled;
        }
        if (changes.Contains("audio.sofaEnabled", "audio.sofaFile", "audio.sofaGain", "audio.sofaRotation", "audio.sofaElevation"))
        {
            var applied = changes.ReplacesAll ? player.ConfigureSofa(settings.Audio)
                : player.ApplySofaPreferences(previousAudio, settings.Audio);
            if (!applied)
            {
                settings.Audio.SofaEnabled = previousAudio.SofaEnabled;
                settings.Audio.SofaFile = previousAudio.SofaFile;
                settings.Audio.SofaGain = previousAudio.SofaGain;
                settings.Audio.SofaRotation = previousAudio.SofaRotation;
                settings.Audio.SofaElevation = previousAudio.SofaElevation;
                failed(AudioFilter.Spatialization);
            }
        }
        if (changes.Contains("audio.pitch")) settings.Audio.Pitch = player.SetPitch(settings.Audio.Pitch);
        if (changes.Contains("audio.pan")) settings.Audio.Pan = player.SetPan(settings.Audio.Pan);
        if (changes.Contains("silence.enabled"))
        {
            if (!player.SetSilenceRemoval(settings.Silence.Enabled)) failed(AudioFilter.SilenceRemoval);
            settings.Silence.Enabled = player.IsSilenceRemovalEnabled;
        }
        return true;
    }
}
