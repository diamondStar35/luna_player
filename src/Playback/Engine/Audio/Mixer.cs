using System.Globalization;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Playback;

internal sealed partial class AudioProcessor
{
    public double SetVolume(double volume)
    {
        _volume = Precision.Normalize(Math.Clamp(volume, 0, AudioSettings.MaximumVolume));
        // The output mixer applies volume after buffered effects, so a volume
        // change does not wait for normalization's analysis window.
        ApplyVolume();
        return _volume;
    }

    public double Volume => _volume;

    public double SetSpeed(double speed)
    {
        var value = Precision.Normalize(Math.Clamp(
            speed, AudioSettings.MinimumSpeed, AudioSettings.MaximumSpeed));
        _session.SetPropertySafely("speed", value);
        _speed = value;
        return value;
    }

    public double Speed => _speed;

    public double SetPitch(double semitones)
    {
        var value = Precision.Normalize(Math.Clamp(
            semitones, AudioSettings.MinimumPitch, AudioSettings.MaximumPitch));
        _pitch = value;
        _session.QueueFilter(AudioFilter.Pitch, () => ApplyPitch(value));
        return value;
    }

    private bool ApplyPitch(double value)
    {
        // mpv places tempo compensation after the user effects. A user scaletempo2
        // before lavfi passes speed-adjusted timestamps that lavfi cannot preserve.
        var scale = Math.Pow(2, value / 12);
        return _session.TryDoNow(mpv => mpv.SetProperty("pitch", scale));
    }

    public double Pitch => _pitch;

    public double SetPan(double pan)
    {
        var value = Precision.Normalize(Math.Clamp(pan, -100, 100));
        _pan = value;
        _session.QueueFilter(AudioFilter.Pan, () => ApplyPan(value));
        return value;
    }

    private bool ApplyPan(double value)
    {
        if (_panFilterActive)
            _session.RemoveFilter("@audiopan");
        _panFilterActive = false;
        if (value == 0)
        {
            return true;
        }

        var balance = Precision.Normalize(value / 100).ToString("0.###", CultureInfo.InvariantCulture);
        _panFilterActive = _session.AddFilter(
            $"@audiopan:lavfi=[aformat=channel_layouts=stereo,stereotools=balance_out={balance}]");
        return _panFilterActive;
    }

    public double Pan => _pan;
}
