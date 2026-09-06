using System.Text;

namespace LunaPlayer.Media;

/// <summary>Repairs legacy Arabic tags whose bytes were labelled as Western text.</summary>
internal static class LegacyMetadataEncoding
{
    private static readonly Encoding ArabicWindows = CreateArabicWindows();

    /// <summary>Decodes nominal Latin-1 text, correcting it when the bytes are clearly Arabic CP-1256.</summary>
    internal static string DecodeLatin1(ReadOnlySpan<byte> bytes)
        => RepairArabicMojibake(Encoding.Latin1.GetString(bytes));

    /// <summary>Undoes the Western mojibake returned by demuxers for incorrectly labelled Arabic tags.</summary>
    internal static string RepairArabicMojibake(string value)
    {
        if (value.Length == 0 || value.Any(IsArabic) || value.Any(character => character > byte.MaxValue))
            return value;

        var repaired = ArabicWindows.GetString(Encoding.Latin1.GetBytes(value));
        var letters = 0;
        var arabicLetters = 0;
        foreach (var character in repaired)
        {
            if (!char.IsLetter(character))
                continue;
            letters++;
            if (IsArabic(character))
                arabicLetters++;
        }

        // A single accented Western character can map to an Arabic letter in CP-1256 by coincidence.
        // Requiring multiple Arabic letters and a majority keeps ordinary Latin metadata unchanged.
        return arabicLetters >= 2 && arabicLetters * 2 >= letters ? repaired : value;
    }

    private static Encoding CreateArabicWindows()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1256);
    }

    private static bool IsArabic(char character)
        => character is (>= '\u0600' and <= '\u06ff')
            or (>= '\u0750' and <= '\u077f')
            or (>= '\u08a0' and <= '\u08ff')
            or (>= '\ufb50' and <= '\ufdff')
            or (>= '\ufe70' and <= '\ufeff');
}
