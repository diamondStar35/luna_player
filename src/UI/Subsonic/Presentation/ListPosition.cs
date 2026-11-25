using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.UI.Subsonic;

/// <summary>Keeps keyboard focus and selection attached to items when a result view is reordered.</summary>
internal sealed record ListPosition(ListPosition.Key[] Selected, ListPosition.Key? Focused, ListPosition.Key? Top)
{
    internal readonly record struct Key(RowKind Kind, string Id, int? Occurrence)
    {
        internal static Key Of(Row row) => new(row.Kind, row.Id, row.PlaylistIndex);
    }

    internal static bool SameRows(Row[] before, Row[] after)
        => before.Length == after.Length && before.Select(Key.Of).SequenceEqual(after.Select(Key.Of));

    internal static ListPosition Capture(Row[] rows, IEnumerable<long> selected, long focused, long top)
    {
        Key? At(long index) => index >= 0 && index < rows.Length ? Key.Of(rows[(int)index]) : null;
        return new(selected.Select(At).OfType<Key>().ToArray(), At(focused), At(top));
    }

    internal (int[] Selected, int Focused, int Top) Restore(Row[] rows)
    {
        var selected = Selected.ToHashSet();
        var indices = rows.Select((row, index) => (Key: Key.Of(row), Index: index)).ToArray();
        int Find(Key? key) => key is null ? -1 : Array.FindIndex(rows, row => Key.Of(row) == key);
        return (indices.Where(item => selected.Contains(item.Key)).Select(item => item.Index).ToArray(), Find(Focused), Find(Top));
    }
}
