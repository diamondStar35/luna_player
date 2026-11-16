namespace LunaPlayer.Media.Discs;

internal static class Navigation
{
    internal static int? Target(int count, int current, int direction, bool wrap)
    {
        if (count < 2 || current < 0 || current >= count) return null;
        var index = current + direction;
        if (index >= 0 && index < count) return index;
        return wrap ? (index < 0 ? count - 1 : 0) : null;
    }
}
