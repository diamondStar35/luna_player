using LunaPlayer.Media;
namespace LunaPlayer.UI.Conversion;
internal static class RequestBuilder
{
    internal static ConversionRequest Create(IReadOnlyList<string> files, string extension, bool preserve,
        string folder, ConversionSettings settings, int threads, bool deleteOriginals, string label)
    {
        var jobs = new List<ConversionJob>(files.Count);
        foreach (var source in files)
        {
            var directory = preserve ? Path.GetDirectoryName(source) ?? folder : folder;
            jobs.Add(new ConversionJob(source, Path.Combine(directory, Path.GetFileNameWithoutExtension(source) + extension)));
        }
        return new ConversionRequest(jobs, settings, threads, deleteOriginals, label);
    }

    // The ffmpeg encoder that writes each format. Named rather than left to ffmpeg's own default for the
    // container, so the same choice writes the same thing however the file is named, and so the bundled build's
    // libmp3lame, libvorbis and libopus are used rather than any weaker encoder that shares their container.
    internal static string CodecFor(string extension) => extension switch
    {
        ".mp3" => "libmp3lame",
        ".aac" or ".m4a" => "aac",
        ".ac3" => "ac3",
        ".aiff" => "pcm_s16be",
        ".wav" => "pcm_s16le",
        ".flac" => "flac",
        ".ogg" or ".oga" => "libvorbis",
        ".opus" => "libopus",
        ".wma" => "wmav2",
        ".mka" => "libvorbis",
        _ => "libmp3lame",
    };

}
