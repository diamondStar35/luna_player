namespace LunaPlayer.Recording;

/// <summary>Where a recording source takes its sound from.</summary>
internal enum RecordingSourceKind
{
    /// <summary>A microphone, a line in, or anything else Windows lists as a capture device.</summary>
    InputDevice,
    /// <summary>Everything played through one output device, captured as it leaves.</summary>
    OutputLoopback,
    /// <summary>One program's sound, taken without touching the device it plays through.</summary>
    Process,
}

/// <summary>What a recording is written as.</summary>
/// <remarks>
/// Everything but <see cref="Wav"/> goes through Media Foundation, using the encoders Windows already
/// ships. Ogg is deliberately absent: Windows has no Vorbis or Opus encoder, only decoders.
/// </remarks>
internal enum RecordingFormat
{
    Wav,
    Mp3,
    Aac,
    Flac,
}

/// <summary>What the recorder is doing.</summary>
internal enum RecordingState
{
    Idle,
    Recording,
    Paused,
}

/// <summary>One configured source: where the sound comes from and how loud it should be.</summary>
///
/// <remarks>
/// Mutable and identified by <see cref="Id"/> rather than by position, so the window can hand one back
/// after an edit without the list having to be the same length it was. These live in memory for as long
/// as the player runs and are not written to the settings file - a process id is meaningless by the next
/// launch, and half a list that survives is worse than none.
/// </remarks>
internal sealed class RecordingSource
{
    internal string Id { get; init; } = Guid.NewGuid().ToString("n");

    /// <summary>What the user called it. Shown in the list and used when a source has to be named in a
    /// message.</summary>
    internal string Name { get; set; } = string.Empty;

    internal RecordingSourceKind Kind { get; set; }

    /// <summary>Which device, or null for whichever is the default at the moment recording starts.</summary>
    /// <remarks>
    /// Null is not the same as storing today's default device: somebody who chose "default microphone"
    /// means the one that is default when they press record, which may be a headset plugged in since.
    /// </remarks>
    internal string? DeviceId { get; set; }

    /// <summary>The program to capture, for <see cref="RecordingSourceKind.Process"/>.</summary>
    internal int ProcessId { get; set; }

    /// <summary>What that program is called, kept so the list and any failure message can name it after
    /// the program has gone.</summary>
    internal string ProcessName { get; set; } = string.Empty;

    /// <summary>Whether to capture everything except this program rather than the program itself.</summary>
    /// <remarks>
    /// Windows offers exactly two process modes and both act on the program <em>and its children</em>:
    /// capture that tree, or capture everything else. There is no "this program without its children",
    /// which is why this is worded as it is rather than as a switch about child processes.
    /// </remarks>
    internal bool CaptureOthers { get; set; }

    /// <summary>How loud this source is in the mix, as a percentage. 100 leaves it alone.</summary>
    internal int Volume { get; set; } = 100;

    internal RecordingSource Copy() => (RecordingSource)MemberwiseClone();
}

/// <summary>How a recording is to be written.</summary>
/// <param name="Bitrate">Bits per second. Ignored by the formats that do not compress.</param>
/// <param name="Folder">Where the file goes. The name is made from the clock when recording starts.</param>
internal readonly record struct RecordingOptions(
    RecordingFormat Format,
    int SampleRate,
    int Channels,
    int Bitrate,
    string Folder);

/// <summary>Why a recording did not start, or did not stop cleanly.</summary>
///
/// <remarks>
/// A code rather than a message. The engine runs on a worker thread and <c>Tr</c> may only be called on
/// the thread that owns the windows, so the words are chosen where the failure is reported. Building the
/// sentence in the engine would mean every failure path calling <c>Tr</c> from the wrong thread - and a
/// failure path that throws while reporting a failure reports nothing at all.
/// </remarks>
internal enum RecordingFailure
{
    None,
    /// <summary>A recording is already running.</summary>
    AlreadyRunning,
    /// <summary>Nothing is being recorded, so there is nothing to pause or stop.</summary>
    NotRunning,
    /// <summary>There was nothing to record from.</summary>
    NoSources,
    /// <summary>Every source refused to open.</summary>
    NothingOpened,
    /// <summary>The folder could not be made, or the file could not be created in it.</summary>
    Folder,
    /// <summary>The chosen format cannot be written at the chosen rate and channel count.</summary>
    Unsupported,
    /// <summary>Something else went wrong; the detail says what.</summary>
    Failed,
}

/// <summary>Whether a recording started, and why it did not.</summary>
/// <param name="Path">The file being written, when one is.</param>
/// <param name="Detail">Whatever Windows said, untranslated. Shown after the sentence the user reads, as a
/// diagnostic rather than in place of one.</param>
internal readonly record struct RecordingOutcome(
    bool Success, string Path = "", RecordingFailure Failure = RecordingFailure.None, string Detail = "")
{
    internal static RecordingOutcome Started(string path) => new(true, path);

    internal static RecordingOutcome Failed(RecordingFailure failure, string detail = "")
        => new(false, string.Empty, failure, detail);
}
