namespace LunaPlayer.Discord;

/// <summary>Keeps timestamp anchors stable across one-second samples, without freezing seeks,
/// speed changes or new items. Both endpoints are needed for Discord's elapsed/total media bar.</summary>
internal sealed class Timeline(DateTimeOffset started)
{
    private readonly long _sessionStart = started.ToUnixTimeSeconds();
    private string? _identity;
    private long? _start;
    private long? _end;
    private double _speed;

    internal (long? Start, long? End) Sample(PresenceMedia? media, DateTimeOffset now)
    {
        long? start = null, end = null;
        if (media is null) start = _sessionStart;
        else if (media.Playing)
        {
            var speed = double.IsFinite(media.Speed) && media.Speed > 0 ? Math.Clamp(media.Speed, 0.1, 100) : 1;
            var position = Seconds(media.Position);
            var duration = Seconds(media.Duration);
            // Discord's displayed elapsed and total values must use media time, as Luna does.
            // Dividing by speed turns those values into wall time and makes the two players disagree.
            start = Math.Max(0, now.ToUnixTimeSeconds() - (long)position);
            if (duration > position) end = start + (long)duration;
            if (_identity == media.Identity && _speed == speed && _start is long previous)
            {
                if (media.Position is null)
                {
                    start = previous;
                    end = duration > 0 ? previous + (long)duration : null;
                }
                else if (Math.Abs(start.Value - previous) <= 1 && SameEnd(end, _end))
                {
                    start = _start;
                    end = _end;
                }
            }
            _speed = speed;
        }
        _identity = media?.Identity;
        _start = start;
        _end = end;
        return (start, end);
    }

    private static double Seconds(double? value)
        => value is double v && double.IsFinite(v) ? Math.Clamp(v, 0, 315360000) : 0;

    private static bool SameEnd(long? left, long? right)
        => left is long l && right is long r ? Math.Abs(l - r) <= 1 : left == right;
}
