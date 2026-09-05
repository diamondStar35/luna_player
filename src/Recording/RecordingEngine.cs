using System.Runtime.InteropServices;
using LunaPlayer.Configuration;
using NAudio.CoreAudioApi;
using NAudio.Extras;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LunaPlayer.Recording;

/// <summary>Runs a recording: opens every source, mixes them, and writes the result to a file.</summary>
///
/// <remarks>
/// The shape is NAudio's own. Each source is a <see cref="WasapiRecorder"/> that pushes buffers as they
/// arrive; each pushes into a <see cref="CaptureMixerInput"/>; one <see cref="RealtimeCaptureMixer"/>
/// paces the lot against the wall clock and zero-fills any input that has nothing to give. That last part
/// is not a nicety - a process that is silent produces no buffers at all, so without it a quiet minute
/// would not appear in the file and everything after it would be a minute early.
///
/// The mixer is used even for a single source, for that reason: the silence problem belongs to the
/// pipeline, not to a special case.
///
/// Nothing is started until everything is built, and anything that goes wrong after the writer thread
/// exists tears the whole lot down again. A writer left running with no one to stop it does not fail
/// quietly: it fills a wave file to its four gigabyte ceiling in under a minute.
///
/// This object outlives the recording window. It is owned by the application, so closing the window while
/// a recording runs does nothing to the recording.
/// </remarks>
internal sealed class RecordingEngine : IDisposable
{
    /// <summary>How long to wait for the file to close before giving up on it.</summary>
    private static readonly TimeSpan WriterTimeout = TimeSpan.FromSeconds(10);

    /// <summary>How long to wait at most for the last of the audio to be written.</summary>
    /// <remarks>
    /// A cap, not a duration. How much is left is asked of the mixer rather than guessed, because guessing
    /// is wrong in both directions: too little leaves the end of the recording in a buffer, and too much
    /// pads the file with the silence the mixer goes on producing after the capture has stopped.
    /// </remarks>
    private static readonly TimeSpan DrainLimit = TimeSpan.FromMilliseconds(500);

    /// <summary>The format every source is captured in: the one the recording is being made in.</summary>
    /// <remarks>
    /// Asked for rather than taken from the device, for three reasons. A process is captured through a
    /// virtual device that has no mix format of its own, so there is nothing to take. A source that
    /// arrives already in the recording's format leaves the mixer nothing to resample. And the sample
    /// rate the user chose is then the rate the device is actually opened at, rather than one chosen for
    /// it and converted afterwards. Shared mode does the conversion in the audio engine, through
    /// AutoConvertPcm.
    ///
    /// It was also, on the published NAudio 3.0.1 packages, the only way to record at all: their
    /// <c>WaveFormatExtensible</c> is a class deriving from <c>WaveFormat</c>, and NativeAOT ignores
    /// inherited fields when it marshals such a class, so <c>GetMixFormat</c> came back with its fields
    /// shifted and handing it back to <c>Initialize</c> was refused. That is fixed in the NAudio
    /// submodule this now builds against - see docs\issues - and the reasons above are why it stays.
    /// </remarks>
    private static WaveFormat CaptureFormat(RecordingOptions options) =>
        WaveFormat.CreateIeeeFloatWaveFormat(options.SampleRate, options.Channels);

    private readonly AudioCatalog _catalog;
    private readonly Lock _sync = new();
    private readonly List<WasapiRecorder> _recorders = [];
    private readonly List<CaptureMixerInput> _inputs = [];
    private CaptureBridge? _bridge;
    private RecordingWriter? _writer;
    private DateTime _started;
    private TimeSpan _paused;
    private DateTime _pausedAt;
    private bool _disposed;

    /// <param name="catalog">Asked what the encoders will take, so a combination that cannot be written
    /// is refused before anything is opened rather than when the file is closed.</param>
    internal RecordingEngine(AudioCatalog catalog) => _catalog = catalog;

    /// <summary>Raised whenever the recording starts, pauses, resumes or stops. Raised from whichever
    /// thread made the change, so a listener that touches windows must post.</summary>
    internal event Action? StateChanged;

    internal RecordingState State { get; private set; } = RecordingState.Idle;

    /// <summary>The file being written, or an empty string when nothing is being recorded.</summary>
    internal string CurrentPath { get; private set; } = string.Empty;

    /// <summary>How much audio has been recorded, which is not the same as how long ago it started: a
    /// paused stretch counts for neither.</summary>
    internal TimeSpan Elapsed
    {
        get
        {
            lock (_sync)
            {
                if (State is RecordingState.Idle)
                    return TimeSpan.Zero;
                var paused = State is RecordingState.Paused ? _paused + (DateTime.UtcNow - _pausedAt) : _paused;
                return DateTime.UtcNow - _started - paused;
            }
        }
    }

    /// <summary>Starts recording <paramref name="sources"/> into a new file.</summary>
    /// <param name="failures">The sources that would not open, by name. Recording still starts on the
    /// rest; only when none of them open does this refuse.</param>
    /// <remarks>Blocks while the devices are opened, so it belongs on a worker thread.</remarks>
    internal RecordingOutcome Start(
        RecordingOptions options,
        IReadOnlyList<RecordingSource> sources,
        out IReadOnlyList<string> failures)
    {
        failures = [];
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (State is not RecordingState.Idle)
                return RecordingOutcome.Failed(RecordingFailure.AlreadyRunning);
        }
        if (sources.Count == 0)
            return RecordingOutcome.Failed(RecordingFailure.NoSources);
        // Asked before a device is opened, because the encoder is not asked until the file is closed: a
        // combination it will not take starts happily, records for as long as the user likes, and fails
        // only when they stop - by which time the recording is gone. Better refused in the first second.
        if (!_catalog.Supports(options.Format, options.SampleRate, options.Channels))
            return RecordingOutcome.Failed(RecordingFailure.Unsupported);

        // Played and allowed to finish before a single source is opened. A loopback source records
        // whatever the speakers play, so a tone that overlapped the capture would be the first thing in
        // the file. The Python player orders it the same way, and for the same reason.
        RecordingTone.Play(RecordingTone.Started);

        var capture = CaptureFormat(options);
        var mixer = new RealtimeCaptureMixer(capture, null);
        var opened = new List<WasapiRecorder>();
        var inputs = new List<CaptureMixerInput>();
        var refused = new List<string>();
        foreach (var source in sources)
        {
            try
            {
                var recorder = Open(source, capture);
                var input = mixer.AddInput(recorder.WaveFormat, Gain(source.Volume), null);
                recorder.DataAvailable += (buffer, _, _, _) => input.AddSamples(buffer);
                opened.Add(recorder);
                inputs.Add(input);
            }
            catch (Exception failure) when (failure is not OutOfMemoryException)
            {
                // Named, not swallowed: a microphone that was unplugged is exactly the thing the user has
                // to be told about, and the other sources are still worth recording. The stack goes to the
                // log, because "could not be opened" does not say which call refused it.
                LunaPlayer.Application.CrashReport.Note(failure);
                refused.Add(source.Name.Length > 0 ? source.Name : failure.Message);
            }
        }
        failures = refused;
        if (opened.Count == 0)
        {
            return RecordingOutcome.Failed(RecordingFailure.NothingOpened);
        }

        string path;
        try
        {
            Directory.CreateDirectory(options.Folder);
            path = Paths.Unused(Path.Combine(options.Folder, Name(options.Format)));
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException)
        {
            Close(opened);
            return RecordingOutcome.Failed(RecordingFailure.Folder, failure.Message);
        }

        // The mixer's own format, carried through to the file. Nothing is quantised on the way: the
        // capture is float, the mixing is float, and what is written is float, so the only place a sample
        // can be lost is an encoder that will not take one - and none of them refuse.
        var bridge = new CaptureBridge(mixer, capture);
        RecordingWriter? writer = null;
        try
        {
            // This order is exact and both halves of it were paid for.
            //
            // The capture goes first, because opening the stream takes a measurable moment - some fifty
            // milliseconds - and the mixer paces from the instant it is started. Starting the clock first
            // means the recording is short by however long the device took to get going.
            //
            // The writer goes last, because the mixer must be started before anything reads from it. A
            // reader that gets there first is told a great deal of audio is already owed and takes it as
            // fast as it can be written, which fills a wave file to its four gigabyte ceiling in seconds.
            foreach (var recorder in opened)
                recorder.StartRecording();
            mixer.Start();
            bridge.Begin();
            writer = RecordingWriter.Start(bridge, path, options.Format, options.Bitrate);
        }
        catch (Exception failure) when (failure is not OutOfMemoryException)
        {
            // Logged as well as reported. What the user is shown is Windows' own sentence, which says what
            // was refused but not by which call - so the stack goes to the file for the next time somebody
            // has to work out why.
            LunaPlayer.Application.CrashReport.Note(failure);
            // Everything that was started gets stopped again, and the part-written file goes with it.
            // Leaving a writer running here is what turns a failed start into a file that fills the disk.
            bridge.Stop();
            writer?.Wait(WriterTimeout);
            writer?.Dispose();
            bridge.Dispose();
            Close(opened);
            Discard(path);
            return RecordingOutcome.Failed(RecordingFailure.Failed, failure.Message);
        }

        lock (_sync)
        {
            _bridge = bridge;
            _writer = writer;
            _recorders.AddRange(opened);
            _inputs.AddRange(inputs);
            _started = DateTime.UtcNow;
            _paused = TimeSpan.Zero;
            CurrentPath = path;
            State = RecordingState.Recording;
        }
        StateChanged?.Invoke();
        return RecordingOutcome.Started(path);
    }

    internal bool Pause()
    {
        lock (_sync)
        {
            if (State is not RecordingState.Recording || _bridge is null)
                return false;
            // The capture keeps running and the mixer keeps pacing; only the bridge stops handing audio
            // out. Stopping the capture instead would lose the moment resume was pressed.
            _bridge.Pause();
            _pausedAt = DateTime.UtcNow;
            State = RecordingState.Paused;
        }
        StateChanged?.Invoke();
        return true;
    }

    internal bool Resume()
    {
        lock (_sync)
        {
            if (State is not RecordingState.Paused || _bridge is null)
                return false;
            _bridge.Resume();
            _paused += DateTime.UtcNow - _pausedAt;
            State = RecordingState.Recording;
        }
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>Ends the recording and closes the file.</summary>
    /// <remarks>Waits for the writer, so it belongs on a worker thread: an encoder has a last block to
    /// flush and a header to go back and fix.</remarks>
    internal RecordingOutcome Stop()
    {
        List<WasapiRecorder> recorders;
        List<CaptureMixerInput> inputs;
        CaptureBridge? bridge;
        RecordingWriter? writer;
        string path;
        bool paused;
        lock (_sync)
        {
            if (State is RecordingState.Idle)
                return RecordingOutcome.Failed(RecordingFailure.NotRunning);
            paused = State is RecordingState.Paused;
            recorders = [.. _recorders];
            inputs = [.. _inputs];
            bridge = _bridge;
            writer = _writer;
            path = CurrentPath;
            _recorders.Clear();
            _inputs.Clear();
            _bridge = null;
            _writer = null;
            CurrentPath = string.Empty;
            State = RecordingState.Idle;
        }

        // Capture first, so nothing new arrives while the tail is written.
        Close(recorders);
        // Then wait for the writer to take what the mixer is still holding, and no longer. Nothing new
        // arrives now that the capture has stopped, so the buffered count only falls; when it reaches
        // nothing, the end of the recording has reached the file.
        // Not while paused: nothing is being taken out, so the count would not fall and the wait would be
        // the whole of the cap spent achieving nothing. A recording stopped from a pause has already had its
        // tail thrown away, which is what pausing means here.
        var deadline = DateTime.UtcNow + DrainLimit;
        while (!paused && DateTime.UtcNow < deadline && inputs.Exists(input => input.BufferedFrames > 0))
            Thread.Sleep(5);
        // Only now is the stream declared over, which is what lets the writer close the file.
        bridge?.Stop();
        // Played after the recording has been told to end, so it cannot be in it, but before the file has
        // finished being written - an encoder has a last block to flush, and the user should not be left
        // waiting for that before hearing that they stopped.
        RecordingTone.Play(RecordingTone.Stopped);
        var failure = writer?.Wait(WriterTimeout);
        writer?.Dispose();
        bridge?.Dispose();
        StateChanged?.Invoke();
        return failure is null
            ? RecordingOutcome.Started(path)
            : RecordingOutcome.Failed(RecordingFailure.Failed, failure.Message);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (State is not RecordingState.Idle)
            Stop();
    }

    /// <summary>Stops and lets go of a set of recorders, whatever state they are in.</summary>
    private static void Close(IReadOnlyList<WasapiRecorder> recorders)
    {
        foreach (var recorder in recorders)
        {
            try
            {
                recorder.StopRecording();
            }
            catch (Exception exception) when (exception is InvalidOperationException or COMException)
            {
                // A device taken away mid-recording refuses to stop. There is nothing left to do about it
                // and the file still has to be closed.
            }
            recorder.Dispose();
        }
    }

    /// <summary>Throws away a file a failed start left behind.</summary>
    private static void Discard(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // An empty file nobody asked for is untidy, not a failure worth reporting over the one that
            // actually stopped the recording.
        }
    }

    /// <summary>Opens one source as a recorder.</summary>
    /// <remarks>
    /// Nothing here asks for the recording's own format. A real device reports whatever rate and depth it
    /// runs at and the mixer converts; a process is captured at the one format its virtual device is
    /// documented to take. Asking a device for the format the file happens to want is how a perfectly good
    /// microphone comes to be refused.
    /// </remarks>
    private static WasapiRecorder Open(RecordingSource source, WaveFormat format)
    {
        if (source.Kind is RecordingSourceKind.Process)
        {
            if (!AudioCatalog.SupportsProcessCapture)
                throw new PlatformNotSupportedException("Process capture needs Windows 10 version 2004.");
            // The virtual device a process is captured through has no mix format of its own, so a format
            // is doubly required here - see CaptureFormat for the other reason.
            var mode = source.CaptureOthers
                ? ProcessLoopbackMode.ExcludeTargetProcessTree
                : ProcessLoopbackMode.IncludeTargetProcessTree;
            return new WasapiRecorderBuilder()
                .WithProcessLoopback((uint)source.ProcessId, mode)
                .WithFormat(format)
                .WithEventSync()
                .BuildAsync()
                .GetAwaiter()
                .GetResult();
        }

        var loopback = source.Kind is RecordingSourceKind.OutputLoopback;
        var flow = loopback ? DataFlow.Render : DataFlow.Capture;
        using var devices = new MMDeviceEnumerator();
        var device = source.DeviceId is string id
            ? devices.GetDevice(id)
            : devices.GetDefaultAudioEndpoint(flow, Role.Multimedia);
        var builder = new WasapiRecorderBuilder().WithDevice(device).WithFormat(format).WithEventSync();
        return (loopback ? builder.WithLoopbackCapture() : builder).Build();
    }

    /// <summary>The volume of one source, as something to put in the mixer's chain.</summary>
    /// <remarks>Null at full volume, so the ordinary case adds nothing to the path at all.</remarks>
    private static Func<ISampleProvider, ISampleProvider>? Gain(int volume)
    {
        if (volume == 100)
            return null;
        var scale = Math.Clamp(volume, 0, 100) / 100f;
        return source => new VolumeSampleProvider(source) { Volume = scale };
    }

    /// <summary>What a recording is called: the moment it started, as the Python player names them.
    /// </summary>
    private static string Name(RecordingFormat format)
        => $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.{AudioCatalog.Extension(format)}";
}
