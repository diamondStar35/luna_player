using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Playback;

internal sealed partial class AudioProcessor
{
    public double SetVolume(double volume)
    {
        _volume = Precision.Normalize(Math.Clamp(volume, 0, AudioSettings.MaximumVolume));
        // Volume is mpv's continuously adjustable software mixer. Changing a live lavfi gain filter,
        // even through af-command, can make buffered filters such as dynaudnorm audibly pump or drop out.
        ApplyVolume();
        return _volume;
    }

    public double Volume => _volume;

    public double SetSpeed(double speed)
    {
        var value = Precision.Normalize(Math.Clamp(
            speed, AudioSettings.MinimumSpeed, AudioSettings.MaximumSpeed));
        _session.SetPropertySafely("speed", value);
        return value;
    }

    public double Speed => _session.ReadDouble("speed") is double speed
        ? Precision.Normalize(Math.Clamp(
            speed, AudioSettings.MinimumSpeed, AudioSettings.MaximumSpeed))
        : 1;

    public double SetPitch(double semitones)
    {
        var value = Precision.Normalize(Math.Clamp(
            semitones, AudioSettings.MinimumPitch, AudioSettings.MaximumPitch));
        if (value == 0 && !_pitchFilterActive)
        {
            _pitch = 0;
            return _pitch;
        }

        // Keep scaletempo2 in the graph once pitch is used. Letting mpv insert and remove it as pitch
        // crosses zero changes the audio chain, while Luna's pitch and speed ranges fit its supported range.
        if (!_pitchFilterActive)
            _pitchFilterActive = _session.AddFilter("@audiopitch:scaletempo2");
        if (!_pitchFilterActive)
            return _pitch;

        // Semitones are logarithmic: twelve semitones double the frequency and twelve negative
        // semitones halve it. Recompute the derived multiplier from the normalized semitone state so
        // repeated changes cannot accumulate floating-point drift.
        var scale = Math.Pow(2, value / 12);
        if (_session.TrySetProperty("pitch", scale))
            _pitch = value;
        return _pitch;
    }

    public double Pitch => _pitch;

    /// <remarks>
    /// Rebuilt rather than adjusted through <c>af-command</c>. The command reaches the running filter but
    /// not the string mpv built it from, and mpv builds the lavfi graph out of that string again whenever
    /// the audio chain is reinitialized - a seek or a pause is enough - at which point the balance goes
    /// back to whatever the string still says. Keeping the value nowhere but in the string is the only
    /// arrangement that survives that, and it is what the equalizer does for the same reason.
    /// </remarks>
    public double SetPan(double pan)
    {
        var value = Precision.Normalize(Math.Clamp(pan, -100, 100));
        if (_panFilterActive)
            _session.RemoveFilter("@audiopan");
        _panFilterActive = false;
        if (value == 0)
        {
            _pan = 0;
            return _pan;
        }

        var balance = Precision.Normalize(value / 100).ToString("0.###", CultureInfo.InvariantCulture);
        _panFilterActive = _session.AddFilter(
            $"@audiopan:lavfi=[aformat=channel_layouts=stereo,stereotools=balance_out={balance}]");
        if (_panFilterActive)
            _pan = value;
        return _pan;
    }

    public double Pan => _pan;
}
