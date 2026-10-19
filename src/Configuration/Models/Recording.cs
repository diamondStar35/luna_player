using System.Text.Json.Serialization;
using LunaPlayer.Recording;

namespace LunaPlayer.Configuration.Models;

/// <summary>How a recording is written, when nothing more particular has been asked for.</summary>
///
/// <remarks>
/// Recording shortcuts use these defaults when no sources are configured. The recording window works on
/// a session copy, so temporary changes are not persisted automatically.
/// </remarks>
internal sealed class RecordingSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter<RecordingFormat>))]
    public RecordingFormat Format { get; set; } = RecordingFormat.Wav;

    public int SampleRate { get; set; } = 44100;

    public int Channels { get; set; } = 2;

    /// <summary>Bits per second, for the formats that compress. Ignored by the rest.</summary>
    public int Bitrate { get; set; } = 192000;

    // Empty means the standard Luna Player recordings folder. Resolve it only when recording is used;
    // looking up Documents during settings construction needlessly delays ordinary startup.
    public string Folder { get; set; } = string.Empty;

    internal RecordingSettings Copy() => (RecordingSettings)MemberwiseClone();

    internal void Apply(RecordingSettings source)
    {
        Format = source.Format;
        SampleRate = source.SampleRate;
        Channels = source.Channels;
        Bitrate = source.Bitrate;
        Folder = source.Folder;
    }
}
