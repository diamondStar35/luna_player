using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PrismSharp.Native;

namespace PrismSharp
{
    /// <summary>
    /// A single speech backend instance: one screen reader or text-to-speech engine, obtained from a
    /// <see cref="Context"/>.
    /// </summary>
    /// <remarks>
    /// Prism does not make backend instances thread-safe, so this class serializes every call it
    /// makes. That serialization is per-object, and handles obtained from
    /// <see cref="Context.Acquire"/> may share one underlying instance, so callers sharing a cached
    /// backend across threads must still coordinate between their own handles.
    /// <para>
    /// Voice parameters - volume, rate and pitch - are normalized to <c>[0.0, 1.0]</c>, with
    /// <c>0.5</c> the backend's default for rate and pitch. Changes take effect on subsequent
    /// speech; whether they affect speech already in progress is backend-dependent.
    /// </para>
    /// </remarks>
    public sealed unsafe class Backend : IDisposable
    {
        private readonly Lock _sync = new();
        private PrismBackendHandle* _handle;

        internal Backend(PrismBackendHandle* handle, ulong requestedId)
        {
            _handle = handle;
            RequestedId = requestedId;
        }

        /// <summary>
        /// The identifier this backend was requested by, or <see cref="Ids.Invalid"/> when it came
        /// from <see cref="Context.CreateBest"/> or <see cref="Context.AcquireBest"/>.
        /// </summary>
        public ulong RequestedId { get; }

        /// <summary>The backend's human-readable name, such as <c>"NVDA"</c> or <c>"SAPI"</c>.</summary>
        /// <remarks>Readable regardless of initialization state.</remarks>
        /// <exception cref="ObjectDisposedException">The backend has been disposed.</exception>
        public string Name
        {
            get
            {
                lock (_sync)
                {
                    ThrowIfClosed();
                    return Methods.String(Methods.prism_backend_name(_handle)) ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// The operations this backend implements, plus whether its underlying engine is available
        /// right now.
        /// </summary>
        /// <remarks>
        /// Readable regardless of initialization state. An operation whose bit is clear returns
        /// <see cref="Error.NotImplemented"/>. Reading this may perform lightweight probes - COM class
        /// factory lookups, D-Bus name checks, process enumeration - so cache the result rather than
        /// polling it, and use <see cref="Context"/>'s availability callback to learn about changes.
        /// </remarks>
        public Features Features
        {
            get
            {
                lock (_sync)
                    return _handle == null ? Features.None : (Features)Methods.prism_backend_get_features(_handle);
            }
        }

        /// <summary>
        /// Whether the underlying engine or service is available at this moment.
        /// </summary>
        /// <remarks>
        /// Advisory only: <see cref="Initialize"/> may still fail after a positive result. This is the
        /// supported way to test a backend before paying for initialization.
        /// </remarks>
        public bool IsSupportedAtRuntime => Supports(Features.SupportedAtRuntime);

        /// <summary>
        /// Speech volume in <c>[0.0, 1.0]</c>, or <see langword="null"/> when the backend does not
        /// report one.
        /// </summary>
        /// <remarks>
        /// Independent of the system volume: setting <c>1.0</c> does not override system or other
        /// applications' volume, and <c>0.0</c> still synthesizes, just silently. Screen reader
        /// backends do not support volume control. Setting is ignored when unsupported.
        /// </remarks>
        /// <exception cref="PrismException">
        /// Setting failed - <see cref="Error.RangeOutOfBounds"/> when the value is negative, above
        /// <c>1.0</c>, NaN or infinite.
        /// </exception>
        public float? Volume
        {
            get => TryGetFloat(Features.GetVolume, &Methods.prism_backend_get_volume);
            set => SetFloat(Features.SetVolume, &Methods.prism_backend_set_volume, value);
        }

        /// <summary>
        /// Speech rate in <c>[0.0, 1.0]</c> - <c>0.0</c> slowest, <c>1.0</c> fastest, <c>0.5</c> the
        /// backend default - or <see langword="null"/> when the backend does not report one.
        /// </summary>
        /// <remarks>
        /// The normalized range maps onto each backend's native range by a piecewise linear transform
        /// that preserves the midpoint, so <c>0.5</c> is always the engine's own default. Rate changes
        /// tempo but not pitch. Screen reader backends do not support this.
        /// </remarks>
        /// <exception cref="PrismException">Setting failed, typically <see cref="Error.RangeOutOfBounds"/>.</exception>
        public float? Rate
        {
            get => TryGetFloat(Features.GetRate, &Methods.prism_backend_get_rate);
            set => SetFloat(Features.SetRate, &Methods.prism_backend_set_rate, value);
        }

        /// <summary>
        /// Voice pitch in <c>[0.0, 1.0]</c>, or <see langword="null"/> when the backend does not
        /// report one.
        /// </summary>
        /// <remarks>
        /// Lower is deeper. Not every engine allows runtime adjustment: SAPI does, AVSpeech does
        /// within a limited range, and screen reader backends do not.
        /// </remarks>
        /// <exception cref="PrismException">Setting failed, typically <see cref="Error.RangeOutOfBounds"/>.</exception>
        public float? Pitch
        {
            get => TryGetFloat(Features.GetPitch, &Methods.prism_backend_get_pitch);
            set => SetFloat(Features.SetPitch, &Methods.prism_backend_set_pitch, value);
        }

        /// <summary>
        /// Whether the backend is currently speaking. <see langword="false"/> when it cannot report.
        /// </summary>
        public bool IsSpeaking
        {
            get
            {
                lock (_sync)
                {
                    if (!SupportsLocked(Features.IsSpeaking))
                        return false;

                    byte speaking;
                    return Methods.prism_backend_is_speaking(_handle, &speaking) == Error.Ok && speaking != 0;
                }
            }
        }

        /// <summary>
        /// Index of the selected voice, or <see langword="null"/> when the backend does not report
        /// one. Setting is ignored when voice selection is unsupported.
        /// </summary>
        /// <exception cref="PrismException">
        /// Setting failed - <see cref="Error.VoiceNotFound"/> for an index outside the voice list.
        /// </exception>
        public int? CurrentVoiceIndex
        {
            get
            {
                lock (_sync)
                {
                    if (!SupportsLocked(Features.GetVoice))
                        return null;

                    nuint index;
                    return Methods.prism_backend_get_voice(_handle, &index) == Error.Ok ? Methods.ToInt32(index) : null;
                }
            }
            set
            {
                lock (_sync)
                {
                    if (!value.HasValue || !SupportsLocked(Features.SetVoice))
                        return;

                    ThrowIfError(Methods.prism_backend_set_voice(_handle, (nuint)value.Value));
                }
            }
        }

        /// <summary>
        /// The voices this backend offers, in index order. Empty when voices are unsupported.
        /// </summary>
        /// <remarks>
        /// Refreshes the backend's voice list first where that is supported, so newly installed
        /// voices appear. Indices are what <see cref="CurrentVoiceIndex"/> expects.
        /// </remarks>
        /// <exception cref="PrismException">Refreshing the voice list failed.</exception>
        public IReadOnlyList<VoiceInfo> Voices
        {
            get
            {
                lock (_sync)
                {
                    if (_handle == null)
                        return [];

                    if (SupportsLocked(Features.RefreshVoices))
                        ThrowIfError(Methods.prism_backend_refresh_voices(_handle));

                    nuint nativeCount;
                    if (!SupportsLocked(Features.CountVoices) ||
                        Methods.prism_backend_count_voices(_handle, &nativeCount) != Error.Ok)
                        return [];

                    var count = Methods.ToInt32(nativeCount);
                    var hasName = SupportsLocked(Features.GetVoiceName);
                    var hasLanguage = SupportsLocked(Features.GetVoiceLanguage);

                    var voices = new List<VoiceInfo>(count);
                    for (var i = 0; i < count; i++)
                    {
                        var name = string.Empty;
                        var language = string.Empty;

                        if (hasName)
                        {
                            byte* value;
                            if (Methods.prism_backend_get_voice_name(_handle, (nuint)i, &value) == Error.Ok)
                                name = Methods.String(value) ?? string.Empty;
                        }

                        if (hasLanguage)
                        {
                            byte* value;
                            if (Methods.prism_backend_get_voice_language(_handle, (nuint)i, &value) == Error.Ok)
                                language = Methods.String(value) ?? string.Empty;
                        }

                        voices.Add(new VoiceInfo(i, name, language));
                    }

                    return voices;
                }
            }
        }

        /// <summary>
        /// Channel count of the backend's audio, or <see langword="null"/> when it does not report one.
        /// </summary>
        public int? Channels => TryGetSize(Features.GetChannels, &Methods.prism_backend_get_channels);

        /// <summary>
        /// Sample rate in Hz, or <see langword="null"/> when the backend does not report one.
        /// </summary>
        public int? SampleRate => TryGetSize(Features.GetSampleRate, &Methods.prism_backend_get_sample_rate);

        /// <summary>
        /// The backend's native bit depth, or <see langword="null"/> when it does not report one.
        /// </summary>
        /// <remarks>
        /// Describes the engine's own format. Samples delivered by <see cref="SpeakToMemory"/> are
        /// always 32-bit float regardless of this value.
        /// </remarks>
        public int? BitDepth => TryGetSize(Features.GetBitDepth, &Methods.prism_backend_get_bit_depth);

        /// <summary>
        /// Initializes the backend, bringing up whatever engine or service it wraps.
        /// </summary>
        /// <remarks>
        /// Required before any operation other than <see cref="Name"/> and <see cref="Features"/> on a
        /// backend from <see cref="Context.CreateUninitialized"/>. A backend from the cache may
        /// already be initialized, which is not treated as an error. Backends from
        /// <see cref="Context.CreateBest"/> and <see cref="Context.AcquireBest"/> arrive initialized
        /// and should not be passed here.
        /// </remarks>
        /// <exception cref="PrismException">
        /// Initialization failed - <see cref="Error.BackendNotAvailable"/> when the underlying service
        /// is not running, or <see cref="Error.InternalBackendLimitExceeded"/> when the engine caps
        /// concurrent instances.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The backend has been disposed.</exception>
        public void Initialize()
        {
            lock (_sync)
            {
                ThrowIfClosed();

                var error = Methods.prism_backend_initialize(_handle);
                if (error == Error.AlreadyInitialized)
                    return;

                ThrowIfError(error);
            }
        }

        /// <summary>Speaks text aloud.</summary>
        /// <param name="text">The text to speak.</param>
        /// <param name="interrupt">
        /// Whether to stop speech already in progress. When <see langword="false"/>, the text is
        /// queued behind it.
        /// </param>
        /// <remarks>
        /// Some backends accept SSML here; check <see cref="Features.SpeakSsml"/> before relying on it.
        /// </remarks>
        /// <exception cref="PrismException">
        /// Speaking failed - <see cref="Error.NotImplemented"/> when the backend has no speech output,
        /// or <see cref="Error.InvalidUtf8"/> for malformed text.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The backend has been disposed.</exception>
        public void Speak(string text, bool interrupt)
        {
            ArgumentNullException.ThrowIfNull(text);

            lock (_sync)
            {
                ThrowIfClosed();
                ThrowIfError(Methods.prism_backend_speak(_handle, text, interrupt ? (byte)1 : (byte)0));
            }
        }

        /// <summary>
        /// Synthesizes text to memory and streams the resulting audio instead of playing it.
        /// </summary>
        /// <param name="text">The text to synthesize.</param>
        /// <returns>
        /// A lazy sequence of audio chunks, each carrying its own sample format. A chunk's samples are
        /// valid only until the enumeration advances, so copy anything that must outlive it.
        /// Enumerating drives the synthesis; abandoning the sequence stops it.
        /// </returns>
        /// <remarks>
        /// Synthesis runs on a worker thread and chunks cross through a bounded queue, so a slow
        /// consumer applies back-pressure rather than letting audio buffer without limit. Buffers come
        /// from the shared array pool and are returned as the sequence advances.
        /// <para>
        /// Do not read <see cref="Channels"/>, <see cref="SampleRate"/> or any other member of this
        /// backend while enumerating: synthesis holds the backend's lock until it completes, so such a
        /// call would deadlock. Each <see cref="AudioChunk"/> carries the format it needs.
        /// </para>
        /// <para>
        /// Not every backend supports this; check <see cref="Features.SpeakToMemory"/> first. Some
        /// trim leading and trailing silence - see <see cref="Features.SilenceTrimmingOnSpeakToMemory"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="PrismException">Synthesis failed.</exception>
        /// <exception cref="ObjectDisposedException">The backend has been disposed.</exception>
        public IEnumerable<AudioChunk> SpeakToMemory(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            lock (_sync)
                ThrowIfClosed();

            // Bounded so a slow consumer applies back-pressure to synthesis rather than letting the
            // queue grow without limit.
            var chunks = new BlockingCollection<(float[] Buffer, int Count, int Channels, int SampleRate)>(boundedCapacity: 64);
            var state = new MemorySpeechState(chunks);
            var stateHandle = GCHandle.Alloc(state);

            _ = Task.Run(() =>
            {
                try
                {
                    Error error;
                    lock (_sync)
                    {
                        error = _handle == null
                            ? Error.InvalidOperation
                            : Methods.prism_backend_speak_to_memory(
                                _handle, text, &OnAudio, (void*)GCHandle.ToIntPtr(stateHandle));
                    }

                    if (error != Error.Ok)
                        state.Failure = new PrismException(error);
                }
                catch (Exception ex)
                {
                    state.Failure = ex;
                }
                finally
                {
                    chunks.CompleteAdding();
                    stateHandle.Free();
                }
            });

            return Drain(chunks, state);
        }

        /// <summary>Sends text to a connected braille display.</summary>
        /// <param name="text">The text to braille.</param>
        /// <remarks>Supported only by screen reader backends; check <see cref="Features.Braille"/>.</remarks>
        /// <exception cref="PrismException">Failed, typically <see cref="Error.NotImplemented"/>.</exception>
        /// <exception cref="ObjectDisposedException">The backend has been disposed.</exception>
        public void Braille(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            lock (_sync)
            {
                ThrowIfClosed();
                ThrowIfError(Methods.prism_backend_braille(_handle, text));
            }
        }

        /// <summary>
        /// Sends text to both speech and braille, however the backend routes it.
        /// </summary>
        /// <param name="text">The text to output.</param>
        /// <param name="interrupt">Whether to stop output already in progress.</param>
        /// <remarks>
        /// The natural call for screen reader backends, which decide for themselves how the user has
        /// configured output. Check <see cref="Features.Output"/>.
        /// </remarks>
        /// <exception cref="PrismException">Failed, typically <see cref="Error.NotImplemented"/>.</exception>
        /// <exception cref="ObjectDisposedException">The backend has been disposed.</exception>
        public void Output(string text, bool interrupt)
        {
            ArgumentNullException.ThrowIfNull(text);

            lock (_sync)
            {
                ThrowIfClosed();
                ThrowIfError(Methods.prism_backend_output(_handle, text, interrupt ? (byte)1 : (byte)0));
            }
        }

        /// <summary>Stops speech in progress and discards anything queued.</summary>
        /// <exception cref="PrismException">
        /// Failed - some backends report <see cref="Error.NotSpeaking"/> when nothing was being said.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The backend has been disposed.</exception>
        public void Stop()
        {
            lock (_sync)
            {
                ThrowIfClosed();
                ThrowIfError(Methods.prism_backend_stop(_handle));
            }
        }

        /// <summary>Pauses speech, to be continued by <see cref="Resume"/>.</summary>
        /// <exception cref="PrismException">
        /// Failed - <see cref="Error.NotSpeaking"/> when nothing is being said, or
        /// <see cref="Error.AlreadyPaused"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The backend has been disposed.</exception>
        public void Pause()
        {
            lock (_sync)
            {
                ThrowIfClosed();
                ThrowIfError(Methods.prism_backend_pause(_handle));
            }
        }

        /// <summary>Resumes speech paused by <see cref="Pause"/>.</summary>
        /// <exception cref="PrismException">Failed - <see cref="Error.NotPaused"/> when nothing was paused.</exception>
        /// <exception cref="ObjectDisposedException">The backend has been disposed.</exception>
        public void Resume()
        {
            lock (_sync)
            {
                ThrowIfClosed();
                ThrowIfError(Methods.prism_backend_resume(_handle));
            }
        }

        /// <summary>Whether every bit in <paramref name="feature"/> is present in <see cref="Features"/>.</summary>
        /// <param name="feature">One or more feature bits to test.</param>
        /// <returns><see langword="true"/> when all of them are supported.</returns>
        public bool Supports(Features feature) => (Features & feature) == feature;

        /// <summary>
        /// Stops speech where possible, then releases this handle.
        /// </summary>
        /// <remarks>
        /// For a backend from <see cref="Context.Create"/> or <see cref="Context.CreateBest"/> this
        /// destroys the instance. For one from <see cref="Context.Acquire"/> or
        /// <see cref="Context.AcquireBest"/> it drops a reference, and the instance survives until the
        /// last handle is disposed. Prism does not promise that releasing a backend stops speech, so
        /// this stops first where the backend supports it.
        /// </remarks>
        public void Dispose()
        {
            lock (_sync)
            {
                if (_handle == null)
                    return;

                var handle = _handle;
                _handle = null;

                if (((Features)Methods.prism_backend_get_features(handle) & Features.Stop) == Features.Stop)
                {
                    try
                    {
                        Methods.prism_backend_stop(handle);
                    }
                    catch
                    {
                    }
                }

                Methods.prism_backend_free(handle);
            }
        }

        private static IEnumerable<AudioChunk> Drain(
            BlockingCollection<(float[] Buffer, int Count, int Channels, int SampleRate)> chunks, MemorySpeechState state)
        {
            try
            {
                foreach (var (buffer, count, channels, sampleRate) in chunks.GetConsumingEnumerable())
                {
                    try
                    {
                        yield return new AudioChunk(new ReadOnlyMemory<float>(buffer, 0, count), channels, sampleRate);
                    }
                    finally
                    {
                        ArrayPool<float>.Shared.Return(buffer);
                    }
                }

                if (state.Failure != null)
                    throw state.Failure;
            }
            finally
            {
                // A consumer that stops early would otherwise leave the audio callback blocked on a
                // full queue forever, holding the backend lock with it.
                state.Abandoned.Cancel();
            }
        }

        private float? TryGetFloat(Features feature, delegate*<PrismBackendHandle*, float*, Error> get)
        {
            lock (_sync)
            {
                if (!SupportsLocked(feature))
                    return null;

                float value;
                return get(_handle, &value) == Error.Ok ? value : null;
            }
        }

        private void SetFloat(Features feature, delegate*<PrismBackendHandle*, float, Error> set, float? value)
        {
            lock (_sync)
            {
                if (!value.HasValue || !SupportsLocked(feature))
                    return;

                ThrowIfError(set(_handle, value.Value));
            }
        }

        private int? TryGetSize(Features feature, delegate*<PrismBackendHandle*, nuint*, Error> get)
        {
            lock (_sync)
            {
                if (!SupportsLocked(feature))
                    return null;

                nuint value;
                return get(_handle, &value) == Error.Ok ? Methods.ToInt32(value) : null;
            }
        }

        private bool SupportsLocked(Features feature)
        {
            if (_handle == null)
                return false;

            var features = (Features)Methods.prism_backend_get_features(_handle);
            return (features & feature) == feature;
        }

        private void ThrowIfClosed()
        {
            ObjectDisposedException.ThrowIf(_handle == null, this);
        }

        private static void ThrowIfError(Error error)
        {
            if (error != Error.Ok)
                throw new PrismException(error);
        }

        private sealed class MemorySpeechState(BlockingCollection<(float[] Buffer, int Count, int Channels, int SampleRate)> chunks)
        {
            public BlockingCollection<(float[] Buffer, int Count, int Channels, int SampleRate)> Chunks { get; } = chunks;

            /// <summary>Signalled when the consumer stops enumerating before synthesis finishes.</summary>
            public CancellationTokenSource Abandoned { get; } = new();

            public Exception? Failure { get; set; }
        }

        // Runs on whichever thread the backend synthesizes on, which need not be the caller's. An
        // exception escaping into native code is undefined behaviour.
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnAudio(void* userdata, float* samples, nuint sampleCount, nuint channels, nuint sampleRate)
        {
            try
            {
                if (userdata == null || samples == null || sampleCount == 0)
                    return;

                var handle = GCHandle.FromIntPtr((nint)userdata);
                if (!handle.IsAllocated || handle.Target is not MemorySpeechState state)
                    return;

                // The sample buffer is valid only for this call, so copy it out before returning.
                var count = Methods.ToInt32(sampleCount);
                var buffer = ArrayPool<float>.Shared.Rent(count);
                new ReadOnlySpan<float>(samples, count).CopyTo(buffer);

                var chunk = (buffer, count, Methods.ToInt32(channels), Methods.ToInt32(sampleRate));
                if (!state.Chunks.TryAdd(chunk, Timeout.Infinite, state.Abandoned.Token))
                    ArrayPool<float>.Shared.Return(buffer);
            }
            catch
            {
            }
        }
    }
}
