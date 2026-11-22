using LunaPlayer.Subsonic.Browsing;

namespace LunaPlayer.UI.Subsonic;

internal static class Ratings
{
    internal static bool CanRate(IReadOnlyList<Row> rows) => rows.Count > 0
        && rows.All(row => row.Available && row.Kind is RowKind.Song or RowKind.Bookmark);

    // Null means a mixed selection. Zero is an actual choice that removes the server rating.
    internal static int? Common(IEnumerable<Row> rows)
    {
        var values = rows.Select(row => row.Rating is >= 1 and <= 5 ? row.Rating.Value : 0).Distinct().Take(2).ToArray();
        return values.Length == 1 ? values[0] : null;
    }
}
