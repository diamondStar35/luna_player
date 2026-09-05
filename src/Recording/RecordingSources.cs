namespace LunaPlayer.Recording;

/// <summary>The sources the user has set up, and the settings the recording window is working with.
/// </summary>
///
/// <remarks>
/// Owned by the application rather than by the window, so closing the window changes nothing: the sources
/// are still there when it is opened again, and a recording started from it carries on regardless.
///
/// Nothing here is written to the settings file. A process id means nothing by the next launch, and a
/// list where half the entries survived and half did not would be worse than starting empty. What does
/// persist is <see cref="Configuration.RecordingSettings"/>, which this is seeded from and which the
/// recording shortcuts fall back to when no sources have been set up.
/// </remarks>
internal sealed class RecordingSources
{
    private readonly List<RecordingSource> _sources = [];

    /// <param name="defaults">The saved settings, copied so the window can be changed for one session
    /// without rewriting what the player starts with next time.</param>
    internal RecordingSources(Configuration.RecordingSettings defaults) => Options = new RecordingOptions(
        defaults.Format, defaults.SampleRate, defaults.Channels, defaults.Bitrate, defaults.Folder);

    internal IReadOnlyList<RecordingSource> All => _sources;

    internal int Count => _sources.Count;

    /// <summary>How the recording window is set to write, for this session.</summary>
    internal RecordingOptions Options { get; set; }

    internal RecordingSource? Find(string id)
        => _sources.Find(source => string.Equals(source.Id, id, StringComparison.Ordinal));

    internal void Add(RecordingSource source) => _sources.Add(source);

    /// <summary>Replaces a source with an edited copy of itself, keeping its place in the list.</summary>
    internal bool Update(RecordingSource source)
    {
        var index = _sources.FindIndex(existing =>
            string.Equals(existing.Id, source.Id, StringComparison.Ordinal));
        if (index < 0)
            return false;
        _sources[index] = source;
        return true;
    }

    internal bool Remove(string id)
        => _sources.RemoveAll(source => string.Equals(source.Id, id, StringComparison.Ordinal)) > 0;

    /// <summary>Turns a failure the engine reported into the sentence the user reads.</summary>
    /// <remarks>
    /// Here rather than in the engine because <c>Tr</c> may only be called on the thread that owns the
    /// windows, and the engine runs on a worker. The raw detail follows the sentence when there is one, as
    /// a diagnostic rather than in place of one.
    /// </remarks>
    internal static string Describe(RecordingFailure failure, string detail)
    {
        var message = failure switch
        {
            // Translators: Shown when recording was asked for and a recording is already running.
            RecordingFailure.AlreadyRunning => Tr("A recording is already running."),
            // Translators: Shown when pausing or stopping was asked for and nothing is being recorded.
            RecordingFailure.NotRunning => Tr("Nothing is being recorded."),
            // Translators: Shown when recording was asked for with nothing to record from.
            RecordingFailure.NoSources => Tr("There is nothing to record from."),
            // Translators: Shown when every source failed to open, so there was nothing left to record.
            RecordingFailure.NothingOpened => Tr("None of the recording sources could be opened."),
            // Translators: Shown when the folder recordings are saved into could not be used.
            RecordingFailure.Folder => Tr("Could not write to the recordings folder."),
            // Translators: Shown when the chosen file format cannot be written at the chosen sample rate
            // and channel count - MP3 at 96000 Hz, for instance, which no encoder on Windows will do.
            RecordingFailure.Unsupported =>
                Tr("This format cannot be recorded at the chosen sample rate and channel count."),
            // Translators: Shown when recording went wrong in a way the player cannot explain more precisely.
            _ => Tr("Could not record."),
        };
        return detail.Length == 0
            ? message
            // Translators: Adds the technical reason under a message about recording. {message} is that
            // message and {reason} is the reason, which is not translated.
            : TrFormat("{message}\n{reason}", message, detail);
    }

    /// <summary>Whether a source is complete enough to record from, and what is wrong when it is not.
    /// </summary>
    /// <remarks>
    /// Here rather than in the dialog, so the window and whatever records cannot come to disagree about
    /// what counts as a usable source.
    /// </remarks>
    internal static bool Validate(RecordingSource source, out string error)
    {
        if (source.Name.Trim().Length == 0)
        {
            // Translators: Shown when a recording source was left without a name.
            error = Tr("Give this source a name.");
            return false;
        }
        if (source.Kind is RecordingSourceKind.Process && source.ProcessId <= 0)
        {
            // Translators: Shown when a recording source is set to capture a program but none was chosen.
            error = Tr("Choose a program to capture.");
            return false;
        }
        error = string.Empty;
        return true;
    }

    /// <summary>How a source reads in the list: what it is called, what kind it is, and how loud.
    /// </summary>
    /// <remarks>
    /// One sentence per row rather than columns, because a screen reader reads a row and the user should
    /// then know everything about that source without going hunting for the rest of it.
    /// </remarks>
    internal static string Describe(RecordingSource source)
    {
        var kind = source.Kind switch
        {
            // Translators: How a microphone or line-in source is described in the recording sources list.
            RecordingSourceKind.InputDevice => Tr("input device"),
            // Translators: How a source that captures everything a speaker plays is described in the list.
            RecordingSourceKind.OutputLoopback => Tr("system output"),
            // Translators: How a source that captures one program is described in the list. {program} is
            // the program's name and {pid} the number Windows knows it by.
            _ => source.CaptureOthers
                // Translators: How a source that captures everything except one program is described.
                ? TrFormat("everything except {program}; PID: {pid}", source.ProcessName, source.ProcessId)
                : TrFormat("program {program}; PID: {pid}", source.ProcessName, source.ProcessId),
        };
        // Translators: One row of the recording sources list. {name} is what the user called the source,
        // {kind} says what it captures and {volume} is a percentage.
        return TrFormat("{name} - {kind} - {volume}%", source.Name, kind, source.Volume);
    }
}
