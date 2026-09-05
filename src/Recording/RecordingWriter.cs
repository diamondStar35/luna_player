using NAudio.Wave;

namespace LunaPlayer.Recording;

/// <summary>Writes what comes out of the bridge to a file, on a thread of its own.</summary>
///
/// <remarks>
/// Two shapes behind one door. A wave file is written by a loop here, because there is nothing to encode
/// and the format is the bytes as they arrive. Everything else is handed to Media Foundation, whose
/// encoders take a source and pull on it until it runs dry - so the call does not return until the
/// recording ends, and has to have a thread to itself.
///
/// Both are given thirty-two bit float, which is what the bridge produces and what every one of these
/// encoders accepts - measured, for MP3, AAC and FLAC alike. A wave file written from it is a float wave
/// file, so a sample above full scale is still there when it is read back rather than flattened on the
/// way in. The encoders convert to integer inside Media Foundation and clip there, which is a property
/// of those formats rather than something this could avoid by converting first: encoding the same signal
/// from float and from sixteen bit produced identical peaks.
///
/// Either way the thread runs until the bridge says the recording is over, and <see cref="Wait"/> is how
/// the caller knows the file is closed and complete.
/// </remarks>
internal sealed class RecordingWriter : IDisposable
{
    private readonly Thread _thread;
    private readonly CaptureBridge _bridge;
    private Exception? _failure;
    private bool _disposed;

    private RecordingWriter(CaptureBridge bridge, Action write)
    {
        _bridge = bridge;
        _thread = new Thread(() =>
        {
            try
            {
                write();
            }
            catch (Exception failure)
            {
                // Kept rather than thrown. This is not the UI thread and nothing here can report to the
                // user; the caller asks for it when it stops the recording.
                _failure = failure;
            }
        })
        {
            // Above normal, because falling behind means a gap in the file rather than a slow window. Not
            // higher: the capture threads are the ones that must not be starved.
            IsBackground = true,
            Name = "Recording writer",
            Priority = ThreadPriority.AboveNormal,
        };
    }

    /// <summary>Starts writing <paramref name="bridge"/> to <paramref name="path"/>.</summary>
    internal static RecordingWriter Start(
        CaptureBridge bridge, string path, RecordingFormat format, int bitrate)
    {
        var writer = format is RecordingFormat.Wav
            ? new RecordingWriter(bridge, () => WriteWave(bridge, path))
            : new RecordingWriter(bridge, () => Encode(bridge, path, format, bitrate));
        writer._thread.Start();
        return writer;
    }

    /// <summary>Waits for the file to be closed, and reports anything that went wrong writing it.</summary>
    /// <remarks>
    /// The bridge must have been stopped first, or this waits for a recording that has not been told to
    /// end. The timeout is a backstop against an encoder that will not return: a recording that cannot be
    /// closed cleanly is still better handed back than hung on.
    /// </remarks>
    internal Exception? Wait(TimeSpan timeout)
    {
        _thread.Join(timeout);
        return _failure;
    }

    private static void WriteWave(CaptureBridge bridge, string path)
    {
        using var writer = new WaveFileWriter(path, bridge.WaveFormat);
        var buffer = new byte[bridge.WaveFormat.AverageBytesPerSecond / 10];
        int read;
        while ((read = bridge.Read(buffer)) > 0)
            writer.Write(buffer.AsSpan(0, read));
    }

    private static void Encode(CaptureBridge bridge, string path, RecordingFormat format, int bitrate)
    {
        MediaFoundation.Start();
        switch (format)
        {
            case RecordingFormat.Mp3:
                MediaFoundationEncoder.EncodeToMp3(bridge, path, bitrate);
                break;
            case RecordingFormat.Aac:
                MediaFoundationEncoder.EncodeToAac(bridge, path, bitrate);
                break;
            case RecordingFormat.Flac:
                // No bitrate: it is lossless, and the encoder has nothing to do with the figure.
                MediaFoundationEncoder.EncodeToFlac(bridge, path);
                break;
            default:
                throw new InvalidOperationException($"{format} is not an encoded format.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _bridge.Stop();
    }
}
