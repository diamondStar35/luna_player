namespace LunaPlayer.UI.Subsonic;

internal sealed record MoveResult(int[] Selected, int Focused);

internal static class Reordering
{
    internal static MoveResult? Move<T>(IList<T> entries, IEnumerable<int> selection, int direction, int focused)
    {
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
        var indices = selection.Where(index => index >= 0 && index < entries.Count).Distinct().ToArray();
        if (indices.Length == 0 || indices.Any(index => index + direction < 0 || index + direction >= entries.Count)) return null;
        foreach (var index in direction < 0 ? indices.Order() : indices.OrderDescending())
        {
            (entries[index], entries[index + direction]) = (entries[index + direction], entries[index]);
            // Keyboard focus can belong to an unselected row. Follow its actual swap as well.
            if (focused == index) focused += direction;
            else if (focused == index + direction) focused = index;
        }
        return new(indices.Select(index => index + direction).Order().ToArray(), focused);
    }
}
