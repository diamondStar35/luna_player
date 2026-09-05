using System.Buffers.Binary;
using NAudio.Extras;
using NAudio.Wave;

namespace LunaPlayer.Recording;

/// <summary>Stands between the mixer and whatever is writing the file, and is the one thing that can hold
/// the recording still.</summary>
///
/// <remarks>
/// Two things about the mixer decide the shape of this, and getting either wrong is expensive.
///
/// It is read through <see cref="RealtimeCaptureMixer.Read"/>, never through its <c>Output</c> provider.
/// Read is the one that paces against the wall clock; Output is the raw mixing provider behind it and will
/// hand out silence as fast as it is asked - measured at some three hundred times real time, which fills a
/// wave file to its four gigabyte ceiling in about twenty seconds.
///
/// And the way Read paces is by <em>returning nothing</em>: in a tight loop better than nine reads in ten
/// come back empty, because no more audio is due yet. So an empty read here means "ask again in a moment",
/// not "the recording has ended". Passing it on would finalise the file within milliseconds of starting,
/// since every writer treats a read of nothing as the stream running dry. Zero is returned from here for
/// one reason only: <see cref="Stop"/> has been called.
///
/// Pause works the same way round. The mixer paces against the clock whether anyone is listening or not,
/// so pausing cannot mean "stop feeding it" - that would leave the paused stretch in the file as silence
/// and make the recording longer than what was recorded. It means "stop taking audio out", so a read waits
/// instead of returning and the audio that arrives meanwhile ages out of the mixer's buffers unheard.
/// </remarks>
internal sealed class CaptureBridge : IWaveProvider, IDisposable
{
    /// <summary>How long to wait before asking the mixer again when it says nothing is due yet.</summary>
    private static readonly TimeSpan Idle = TimeSpan.FromMilliseconds(5);

    /// <summary>How long a paused read waits before looking again. Short enough that resuming is
    /// immediate, long enough not to spin.</summary>
    private static readonly TimeSpan PausePoll = TimeSpan.FromMilliseconds(20);

    /// <summary>How far ahead of the clock the recording may get before this stops handing audio out.
    /// </summary>
    /// <remarks>
    /// A backstop, not a mechanism: the mixer's own pacing keeps this within a fraction of a second and
    /// nothing legitimate comes near the margin. It is here because the cost of pacing going wrong is a
    /// file that fills the disk in seconds, and the cost of the guard is one comparison per read.
    /// </remarks>
    private static readonly TimeSpan Margin = TimeSpan.FromSeconds(10);

    /// <summary>Four, and not a choice: the whole pipeline is thirty-two bit float.</summary>
    private const int BytesPerSample = 4;

    private readonly RealtimeCaptureMixer _mixer;
    private readonly ManualResetEventSlim _running = new(initialState: true);
    private readonly Lock _sync = new();
    private float[] _samples = [];
    private DateTime _started = DateTime.UtcNow;
    private TimeSpan _paused;
    private DateTime _pausedAt;
    private bool _stopped;
    private bool _drain;
    private bool _disposed;

    /// <param name="mixer">The mixer itself, not its output provider. The difference is the whole of the
    /// remarks above.</param>
    /// <param name="format">The mixer's own format, passed through unchanged. Thirty-two bit float, the
    /// same as every stage before it.</param>
    internal CaptureBridge(RealtimeCaptureMixer mixer, WaveFormat format)
    {
        _mixer = mixer;
        WaveFormat = format;
    }

    public WaveFormat WaveFormat { get; }

    /// <summary>How many bytes have been taken, which is how long the recording actually is.</summary>
    /// <remarks>Counted here rather than from the clock, so a paused stretch is not counted and the figure
    /// matches the file exactly.</remarks>
    internal long BytesRead { get; private set; }

    /// <summary>Starts the clock this measures itself against, so it and the mixer agree about when the
    /// recording began.</summary>
    internal void Begin() => _started = DateTime.UtcNow;

    internal void Pause()
    {
        lock (_sync)
        {
            if (_running.IsSet)
                _pausedAt = DateTime.UtcNow;
        }
        _running.Reset();
    }

    internal void Resume()
    {
        lock (_sync)
        {
            if (!_running.IsSet)
            {
                _paused += DateTime.UtcNow - _pausedAt;
                // The mixer paces against its own clock and does not know the recording was paused, so by
                // now it believes every second of the pause is still owed and will hand it all over the
                // moment it is asked. Left alone that puts the paused stretch into the file after all,
                // just later - so it is read out and thrown away before recording resumes.
                _drain = true;
            }
        }
        _running.Set();
    }

    /// <summary>Ends the recording. The next read returns nothing, which is how the writer knows to close
    /// the file.</summary>
    internal void Stop()
    {
        lock (_sync)
            _stopped = true;
        // Released as well as stopped, or a read waiting for a resume that will never come would never see
        // that the recording has ended.
        _running.Set();
    }

    public int Read(Span<byte> buffer)
    {
        // Rounded down to whole frames, not merely whole samples. A writer that asked for an odd number of
        // samples in stereo would take a left without its right, and every frame after it in the file would
        // have the two channels the wrong way round.
        var frame = WaveFormat.Channels;
        var wanted = buffer.Length / BytesPerSample / frame * frame;
        if (wanted == 0)
            return 0;
        if (_samples.Length < wanted)
            _samples = new float[wanted];
        while (true)
        {
            lock (_sync)
            {
                if (_stopped)
                    return 0;
            }
            // False means still paused. Nothing is taken from the mixer while it is, so what arrives
            // meanwhile ages out of its buffers rather than reaching the file.
            if (!_running.Wait(PausePoll))
                continue;
            if (Drain())
                continue;
            if (Ahead())
            {
                Thread.Sleep(Idle);
                continue;
            }
            var read = _mixer.Read(_samples, 0, wanted);
            if (read == 0)
            {
                // Not the end - the mixer simply has nothing due yet, which is the ordinary case.
                Thread.Sleep(Idle);
                continue;
            }
            for (var index = 0; index < read; index++)
            {
                // Written through as they are. Nothing is clamped here: a sample above full scale is real
                // and the only place it has to be given up is a container that cannot hold it, which is
                // the writer's business rather than this one's. Clamping early is exactly what loses the
                // loud parts of a hot microphone.
                //
                // A NaN is the one value that is not passed on. It cannot be encoded, it survives every
                // arithmetic operation downstream, and in a file it is a click that no edit will remove.
                var value = _samples[index];
                BinaryPrimitives.WriteSingleLittleEndian(
                    buffer[(index * BytesPerSample)..], float.IsNaN(value) ? 0f : value);
            }
            BytesRead += read * BytesPerSample;
            return read * BytesPerSample;
        }
    }

    /// <summary>Throws away everything the mixer accumulated while the recording was paused.</summary>
    /// <returns>True when there was a backlog to drop, so the caller starts its read again.</returns>
    /// <remarks>
    /// Done on the reading thread rather than in <see cref="Resume"/>, which is called from the thread that
    /// owns the windows and must not be left waiting on a mixer. Reading until it gives back nothing is
    /// what "caught up" means to it, so that is the test.
    /// </remarks>
    private bool Drain()
    {
        lock (_sync)
        {
            if (!_drain)
                return false;
            _drain = false;
        }
        while (_mixer.Read(_samples, 0, _samples.Length) > 0)
        {
            lock (_sync)
            {
                if (_stopped)
                    return true;
            }
        }
        return true;
    }

    /// <summary>Whether more audio has been taken than the clock can account for.</summary>
    private bool Ahead()
    {
        TimeSpan paused;
        lock (_sync)
            paused = _running.IsSet ? _paused : _paused + (DateTime.UtcNow - _pausedAt);
        var recorded = TimeSpan.FromSeconds((double)BytesRead / WaveFormat.AverageBytesPerSecond);
        return recorded > DateTime.UtcNow - _started - paused + Margin;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
        _running.Dispose();
    }
}
