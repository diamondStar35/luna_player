namespace LunaPlayer.Media;

/// <summary>Compares text naturally, treating each run of ASCII digits as one integer.</summary>
internal sealed class NaturalStringComparer : IComparer<string>
{
    internal static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        if (ReferenceEquals(left, right))
            return 0;
        if (left is null)
            return -1;
        if (right is null)
            return 1;

        var leftAt = 0;
        var rightAt = 0;
        while (leftAt < left.Length && rightAt < right.Length)
        {
            var leftDigit = IsDigit(left[leftAt]);
            var rightDigit = IsDigit(right[rightAt]);
            if (leftDigit && rightDigit)
            {
                var leftEnd = EndOfRun(left, leftAt, digits: true);
                var rightEnd = EndOfRun(right, rightAt, digits: true);
                var leftSignificant = SkipZeroes(left, leftAt, leftEnd);
                var rightSignificant = SkipZeroes(right, rightAt, rightEnd);
                var leftLength = leftEnd - leftSignificant;
                var rightLength = rightEnd - rightSignificant;
                if (leftLength != rightLength)
                    return leftLength.CompareTo(rightLength);
                for (var offset = 0; offset < leftLength; offset++)
                {
                    var difference = left[leftSignificant + offset].CompareTo(right[rightSignificant + offset]);
                    if (difference != 0)
                        return difference;
                }

                // Numerically equal names have a stable order: 2 comes before 02, then 002.
                var runLength = (leftEnd - leftAt).CompareTo(rightEnd - rightAt);
                if (runLength != 0)
                    return runLength;
                leftAt = leftEnd;
                rightAt = rightEnd;
                continue;
            }

            if (leftDigit != rightDigit)
            {
                var difference = char.ToUpperInvariant(left[leftAt])
                    .CompareTo(char.ToUpperInvariant(right[rightAt]));
                if (difference != 0)
                    return difference;
            }

            var leftTextEnd = EndOfRun(left, leftAt, digits: false);
            var rightTextEnd = EndOfRun(right, rightAt, digits: false);
            var textDifference = left.AsSpan(leftAt, leftTextEnd - leftAt).CompareTo(
                right.AsSpan(rightAt, rightTextEnd - rightAt), StringComparison.OrdinalIgnoreCase);
            if (textDifference != 0)
                return textDifference;
            leftAt = leftTextEnd;
            rightAt = rightTextEnd;
        }
        return (left.Length - leftAt).CompareTo(right.Length - rightAt);
    }

    private static int EndOfRun(string value, int at, bool digits)
    {
        while (at < value.Length && IsDigit(value[at]) == digits)
            at++;
        return at;
    }

    private static int SkipZeroes(string value, int at, int end)
    {
        while (at < end && value[at] == '0')
            at++;
        return at;
    }

    private static bool IsDigit(char character) => character is >= '0' and <= '9';
}
