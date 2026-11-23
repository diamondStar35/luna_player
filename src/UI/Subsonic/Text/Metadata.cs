namespace LunaPlayer.UI.Subsonic;

internal static partial class Words
{
    internal static string TrackTitle =>
        // Translators: First column of a music-server track list; the song's metadata title.
        Tr("Title");
    internal static string Quality =>
        // Translators: Music-server track column describing the original file's audio format.
        Tr("Quality");
    internal static string SortSampleRate =>
        // Translators: Sort-by choice for the source file's audio sample rate in Music servers.
        Tr("Sample rate");
    internal static string SourceBitrate =>
        // Translators: Sort-by choice for the original file's bitrate in Music servers.
        Tr("Bitrate");
    internal static string SortBitDepth =>
        // Translators: Sort-by choice for the source file's audio bit depth in Music servers.
        Tr("Bit depth");
    internal static string RandomAlbums =>
        // Translators: Browser category that fetches a fresh random selection of albums from a music server.
        Tr("Random albums");
    internal static string Published =>
        // Translators: Publication date column in the music-server podcast episode list.
        Tr("Published");
    internal static string UnavailableItem =>
        // Translators: Item status when a music-server track is missing or not downloaded yet.
        Tr("Unavailable");
    internal static string NotRated =>
        // Translators: Radio button and track-list value for a song with no server rating.
        Tr("Not rated");
    internal static string Stars(int count) => count == 1
        // Translators: One-star radio button and rating column in Music servers.
        ? Tr("1 star")
        // Translators: Song rating in Music servers. {count} is a number from 2 to 5.
        : TrFormat("{count} stars", count);
    internal static string ChooseRating =>
        // Translators: Validation message when saving a rating dialog with no radio button selected.
        Tr("Choose a rating before saving.");
    internal static string MixedRatings =>
        // Translators: Explains why none of the radio buttons is initially selected for multiple songs.
        Tr("The selected songs have different ratings. Choose a rating to apply to all of them.");
    internal static string PartialFeedback(int count) =>
        // Translators: A batch rating or favorite operation partially failed. {count} is how many items failed.
        TrFormat("Could not update {count} items. Successful changes have been kept.", count);
    internal static string Kilobits(int value) =>
        // Translators: Bitrate in the music-server Quality column or currently playing stream details. {value} is kilobits per second.
        TrFormat("{value} kbps", value);
    internal static string SampleRate(double value) =>
        // Translators: Sample rate in the music-server Quality column or currently playing stream details. {value} is kilohertz.
        TrFormat("{value} kHz", value);
    internal static string BitDepth(int value) =>
        // Translators: Bit depth in the music-server Quality column or currently playing stream details. {value} is bits per sample.
        TrFormat("{value} bit", value);
    internal static string Channels(int value) =>
        // Translators: Channel count in the music-server Quality column or currently playing stream details. {value} is the count.
        TrFormat("{value} channels", value);
}
