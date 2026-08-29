using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PrismSharp.Native;

namespace PrismSharp
{
    /// <summary>
    /// A speech backend implemented in managed code and registered through
    /// <see cref="RegistryBuilder.AddBackend"/>.
    /// </summary>
    /// <remarks>
    /// Derive from <see cref="PrismBackendBase"/> rather than implementing this directly: every
    /// member there already reports <see cref="Error.NotImplemented"/>, so an implementation
    /// overrides only what it supports.
    /// <para>
    /// Prism never invokes two members of the same instance concurrently, so an implementation needs
    /// no locking for its own state; distinct instances may run concurrently and must synchronize
    /// anything they share. No member may throw - an exception escaping into native code is
    /// undefined behaviour. The adapter catches everything and reports
    /// <see cref="Error.Internal"/> as a backstop, but that is not a contract to rely on.
    /// </para>
    /// </remarks>
    public interface IPrismBackend : IDisposable
    {
        /// <summary>
        /// The operations this backend implements.
        /// </summary>
        /// <remarks>
        /// Must correspond exactly to the members actually overridden: Prism fills a vtable slot only
        /// when its bit is declared and rejects a registration whose slots and bits disagree.
        /// <see cref="Features.SupportedAtRuntime"/> is ignored here - Prism derives it from
        /// <see cref="IsSupported"/>.
        /// </remarks>
        Features DeclaredFeatures { get; }

        /// <summary>Whether the underlying engine is available right now.</summary>
        /// <returns><see langword="true"/> when the backend can be used.</returns>
        /// <remarks>
        /// May be called from Prism's availability poll thread, so it must not assume any particular
        /// thread, COM apartment, or platform main context, and must not block.
        /// </remarks>
        bool IsSupported();

        /// <summary>Brings the backend up. Called once before any other operation.</summary>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error Initialize();

        /// <summary>Speaks text aloud.</summary>
        /// <param name="text">The text to speak.</param>
        /// <param name="interrupt">Whether to stop speech already in progress.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error Speak(string text, bool interrupt);

        /// <summary>Synthesizes text and hands the audio to <paramref name="emit"/>.</summary>
        /// <param name="text">The text to synthesize.</param>
        /// <param name="emit">
        /// Receives interleaved 32-bit float samples in <c>[-1.0, 1.0]</c>, the channel count, and the
        /// sample rate. May be called once or many times before returning.
        /// </param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error SpeakToMemory(string text, Action<float[], int, int> emit);

        /// <summary>Sends text to a braille display.</summary>
        /// <param name="text">The text to braille.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error Braille(string text);

        /// <summary>Sends text to speech and braille together.</summary>
        /// <param name="text">The text to output.</param>
        /// <param name="interrupt">Whether to stop output already in progress.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error Output(string text, bool interrupt);

        /// <summary>Stops speech and discards anything queued.</summary>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error Stop();

        /// <summary>Pauses speech.</summary>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error Pause();

        /// <summary>Resumes paused speech.</summary>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error Resume();

        /// <summary>Reports whether speech is in progress.</summary>
        /// <param name="speaking">Set to <see langword="true"/> when speaking.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error IsSpeaking(out bool speaking);

        /// <summary>Sets the volume.</summary>
        /// <param name="volume">Normalized volume in <c>[0.0, 1.0]</c>. Prism validates the range first.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error SetVolume(float volume);

        /// <summary>Reads the volume.</summary>
        /// <param name="volume">Receives the normalized volume.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error GetVolume(out float volume);

        /// <summary>Sets the speech rate.</summary>
        /// <param name="rate">Normalized rate in <c>[0.0, 1.0]</c>, where <c>0.5</c> is the default.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error SetRate(float rate);

        /// <summary>Reads the speech rate.</summary>
        /// <param name="rate">Receives the normalized rate.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error GetRate(out float rate);

        /// <summary>Sets the pitch.</summary>
        /// <param name="pitch">Normalized pitch in <c>[0.0, 1.0]</c>, where <c>0.5</c> is the default.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error SetPitch(float pitch);

        /// <summary>Reads the pitch.</summary>
        /// <param name="pitch">Receives the normalized pitch.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error GetPitch(out float pitch);

        /// <summary>Re-enumerates the available voices.</summary>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error RefreshVoices();

        /// <summary>Reports how many voices are available.</summary>
        /// <param name="count">Receives the voice count.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error CountVoices(out int count);

        /// <summary>Reads a voice's display name.</summary>
        /// <param name="voiceIndex">Zero-based voice index.</param>
        /// <param name="name">Receives the name.</param>
        /// <returns><see cref="Error.Ok"/>, or <see cref="Error.VoiceNotFound"/> for a bad index.</returns>
        /// <remarks>
        /// The adapter caches the UTF-8 copy it hands to Prism for the life of the instance, since
        /// Prism borrows the pointer rather than copying it.
        /// </remarks>
        Error GetVoiceName(int voiceIndex, out string? name);

        /// <summary>Reads a voice's language tag, such as <c>"en-GB"</c>.</summary>
        /// <param name="voiceIndex">Zero-based voice index.</param>
        /// <param name="language">Receives the language tag.</param>
        /// <returns><see cref="Error.Ok"/>, or <see cref="Error.VoiceNotFound"/> for a bad index.</returns>
        Error GetVoiceLanguage(int voiceIndex, out string? language);

        /// <summary>Selects a voice.</summary>
        /// <param name="voiceIndex">Zero-based voice index.</param>
        /// <returns><see cref="Error.Ok"/>, or <see cref="Error.VoiceNotFound"/> for a bad index.</returns>
        Error SetVoice(int voiceIndex);

        /// <summary>Reports the selected voice.</summary>
        /// <param name="voiceIndex">Receives the zero-based voice index.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error GetVoice(out int voiceIndex);

        /// <summary>Reports the channel count of the backend's audio.</summary>
        /// <param name="channels">Receives the channel count.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error GetChannels(out int channels);

        /// <summary>Reports the sample rate in Hz.</summary>
        /// <param name="sampleRate">Receives the sample rate.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error GetSampleRate(out int sampleRate);

        /// <summary>Reports the backend's native bit depth.</summary>
        /// <param name="bitDepth">Receives the bit depth.</param>
        /// <returns><see cref="Error.Ok"/> on success.</returns>
        Error GetBitDepth(out int bitDepth);
    }

    /// <summary>
    /// Base class for a managed backend: declares nothing and implements nothing.
    /// </summary>
    /// <remarks>
    /// Override the operations you support and set <see cref="DeclaredFeatures"/> to match. The two
    /// must agree exactly - Prism rejects a registration where a declared bit has no corresponding
    /// vtable slot or vice versa.
    /// </remarks>
    public abstract class PrismBackendBase : IPrismBackend
    {
        /// <inheritdoc />
        public virtual Features DeclaredFeatures => Features.None;

        /// <inheritdoc />
        public virtual bool IsSupported() => true;

        /// <inheritdoc />
        public virtual Error Initialize() => Error.Ok;

        /// <inheritdoc />
        public virtual Error Speak(string text, bool interrupt) => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error SpeakToMemory(string text, Action<float[], int, int> emit) => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error Braille(string text) => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error Output(string text, bool interrupt) => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error Stop() => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error Pause() => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error Resume() => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error IsSpeaking(out bool speaking)
        {
            speaking = false;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error SetVolume(float volume) => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error GetVolume(out float volume)
        {
            volume = 0f;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error SetRate(float rate) => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error GetRate(out float rate)
        {
            rate = 0f;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error SetPitch(float pitch) => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error GetPitch(out float pitch)
        {
            pitch = 0f;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error RefreshVoices() => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error CountVoices(out int count)
        {
            count = 0;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error GetVoiceName(int voiceIndex, out string? name)
        {
            name = null;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error GetVoiceLanguage(int voiceIndex, out string? language)
        {
            language = null;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error SetVoice(int voiceIndex) => Error.NotImplemented;

        /// <inheritdoc />
        public virtual Error GetVoice(out int voiceIndex)
        {
            voiceIndex = 0;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error GetChannels(out int channels)
        {
            channels = 0;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error GetSampleRate(out int sampleRate)
        {
            sampleRate = 0;
            return Error.NotImplemented;
        }

        /// <inheritdoc />
        public virtual Error GetBitDepth(out int bitDepth)
        {
            bitDepth = 0;
            return Error.NotImplemented;
        }

        /// <summary>Releases whatever the backend holds. The default does nothing.</summary>
        public virtual void Dispose()
        {
        }
    }

    /// <summary>
    /// Bridges an <see cref="IPrismBackend"/> factory to a native <c>PrismBackendVTable</c>.
    /// </summary>
    /// <remarks>
    /// Every slot is filled from an <c>UnmanagedCallersOnly</c> static method, so the vtable holds
    /// plain code addresses: nothing has to be kept alive to stop a delegate being collected, and the
    /// whole path is NativeAOT-safe.
    /// <para>
    /// Ownership follows the manual. The vtable is copied during registration, so the buffer here is
    /// freed immediately afterwards. The userdata handle, in contrast, transfers to Prism the moment
    /// registration is called, and Prism invokes the free callback exactly once whether or not
    /// registration succeeded - so nothing here frees it.
    /// </para>
    /// </remarks>
    internal static unsafe class CustomBackend
    {
        /// <summary>Per-instance state: the managed backend plus the string buffers Prism borrows.</summary>
        private sealed class Instance(IPrismBackend backend)
        {
            public IPrismBackend Backend { get; } = backend;

            /// <summary>
            /// Voice name and language buffers. Prism takes the pointer without copying, so these live
            /// until the instance is destroyed.
            /// </summary>
            public Dictionary<(bool Language, int Index), IntPtr> Strings { get; } = [];
        }

        public static ulong Register(
            PrismRegistryBuilderHandle* builder, string name, int priority, Func<IPrismBackend> factory)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentOutOfRangeException.ThrowIfNegative(priority);
            ArgumentNullException.ThrowIfNull(factory);

            // Bit 0 is derived by Prism from is_supported, so never claim it here.
            var probe = factory();
            var declared = probe.DeclaredFeatures & ~Features.SupportedAtRuntime;
            probe.Dispose();

            // A slot must be non-null exactly when its feature bit is declared; Prism rejects any
            // mismatch outright. create, destroy, is_supported and initialize have no feature bit.
            var vtable = new BackendVTable
            {
                size = (nuint)sizeof(BackendVTable),
                create = &OnCreate,
                destroy = &OnDestroy,
                is_supported = &OnIsSupported,
                initialize = &OnInitialize,
                speak = Has(declared, Features.Speak) ? &OnSpeak : null,
                speak_to_memory = Has(declared, Features.SpeakToMemory) ? &OnSpeakToMemory : null,
                braille = Has(declared, Features.Braille) ? &OnBraille : null,
                output = Has(declared, Features.Output) ? &OnOutput : null,
                stop = Has(declared, Features.Stop) ? &OnStop : null,
                pause = Has(declared, Features.Pause) ? &OnPause : null,
                resume = Has(declared, Features.Resume) ? &OnResume : null,
                is_speaking = Has(declared, Features.IsSpeaking) ? &OnIsSpeaking : null,
                set_volume = Has(declared, Features.SetVolume) ? &OnSetVolume : null,
                get_volume = Has(declared, Features.GetVolume) ? &OnGetVolume : null,
                set_rate = Has(declared, Features.SetRate) ? &OnSetRate : null,
                get_rate = Has(declared, Features.GetRate) ? &OnGetRate : null,
                set_pitch = Has(declared, Features.SetPitch) ? &OnSetPitch : null,
                get_pitch = Has(declared, Features.GetPitch) ? &OnGetPitch : null,
                refresh_voices = Has(declared, Features.RefreshVoices) ? &OnRefreshVoices : null,
                count_voices = Has(declared, Features.CountVoices) ? &OnCountVoices : null,
                get_voice_name = Has(declared, Features.GetVoiceName) ? &OnGetVoiceName : null,
                get_voice_language = Has(declared, Features.GetVoiceLanguage) ? &OnGetVoiceLanguage : null,
                set_voice = Has(declared, Features.SetVoice) ? &OnSetVoice : null,
                get_voice = Has(declared, Features.GetVoice) ? &OnGetVoice : null,
                get_channels = Has(declared, Features.GetChannels) ? &OnGetChannels : null,
                get_sample_rate = Has(declared, Features.GetSampleRate) ? &OnGetSampleRate : null,
                get_bit_depth = Has(declared, Features.GetBitDepth) ? &OnGetBitDepth : null,
            };

            // Ownership of this handle passes to Prism the moment add_backend is called.
            var factoryHandle = GCHandle.Alloc(factory);

            ulong id;
            var error = Methods.prism_registry_builder_add_backend(
                builder,
                name,
                priority,
                (ulong)declared,
                &vtable,
                (void*)GCHandle.ToIntPtr(factoryHandle),
                &OnUserDataFree,
                &id);

            if (error != Error.Ok)
                throw new PrismException(error);

            return id;
        }

        private static bool Has(Features declared, Features feature) => (declared & feature) == feature;

        // ---- Lifecycle ----

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void* OnCreate(void* userdata)
        {
            try
            {
                if (userdata == null)
                    return null;

                var handle = GCHandle.FromIntPtr((nint)userdata);
                if (!handle.IsAllocated || handle.Target is not Func<IPrismBackend> factory)
                    return null;

                var backend = factory();
                return backend == null ? null : (void*)GCHandle.ToIntPtr(GCHandle.Alloc(new Instance(backend)));
            }
            catch
            {
                return null;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnDestroy(void* instance)
        {
            try
            {
                if (instance == null)
                    return;

                var handle = GCHandle.FromIntPtr((nint)instance);
                if (!handle.IsAllocated)
                    return;

                if (handle.Target is Instance state)
                {
                    foreach (var buffer in state.Strings.Values)
                        Marshal.FreeHGlobal(buffer);

                    state.Strings.Clear();
                    state.Backend.Dispose();
                }

                handle.Free();
            }
            catch
            {
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnUserDataFree(void* userdata)
        {
            try
            {
                if (userdata == null)
                    return;

                var handle = GCHandle.FromIntPtr((nint)userdata);
                if (handle.IsAllocated)
                    handle.Free();
            }
            catch
            {
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static byte OnIsSupported(void* instance)
        {
            try
            {
                var state = Resolve(instance);
                return state != null && state.Backend.IsSupported() ? (byte)1 : (byte)0;
            }
            catch
            {
                return 0;
            }
        }

        // ---- Speech ----

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnInitialize(void* instance) => Invoke(instance, static b => b.Initialize());

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnSpeak(void* instance, byte* text, byte interrupt) =>
            Invoke(instance, b => b.Speak(Text(text), interrupt != 0));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnBraille(void* instance, byte* text) =>
            Invoke(instance, b => b.Braille(Text(text)));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnOutput(void* instance, byte* text, byte interrupt) =>
            Invoke(instance, b => b.Output(Text(text), interrupt != 0));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnStop(void* instance) => Invoke(instance, static b => b.Stop());

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnPause(void* instance) => Invoke(instance, static b => b.Pause());

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnResume(void* instance) => Invoke(instance, static b => b.Resume());

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnIsSpeaking(void* instance, byte* outSpeaking)
        {
            try
            {
                var state = Resolve(instance);
                if (state == null || outSpeaking == null)
                    return Error.InvalidParam;

                var error = state.Backend.IsSpeaking(out var speaking);
                *outSpeaking = speaking ? (byte)1 : (byte)0;
                return error;
            }
            catch
            {
                return Error.Internal;
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnSpeakToMemory(
            void* instance,
            byte* text,
            delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> sink,
            void* sinkUserData)
        {
            try
            {
                var state = Resolve(instance);
                if (state == null || sink == null)
                    return Error.InvalidParam;

                return state.Backend.SpeakToMemory(Text(text), (samples, channels, sampleRate) =>
                {
                    if (samples == null || samples.Length == 0)
                        return;

                    // Prism's consumer copies within the call, so the pin need only span it.
                    fixed (float* buffer = samples)
                        sink(sinkUserData, buffer, (nuint)samples.Length, (nuint)channels, (nuint)sampleRate);
                });
            }
            catch
            {
                return Error.Internal;
            }
        }

        // ---- Voice parameters ----

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnSetVolume(void* instance, float value) => Invoke(instance, b => b.SetVolume(value));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnSetRate(void* instance, float value) => Invoke(instance, b => b.SetRate(value));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnSetPitch(void* instance, float value) => Invoke(instance, b => b.SetPitch(value));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnGetVolume(void* instance, float* value) =>
            Float(instance, value, static (IPrismBackend b, out float v) => b.GetVolume(out v));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnGetRate(void* instance, float* value) =>
            Float(instance, value, static (IPrismBackend b, out float v) => b.GetRate(out v));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnGetPitch(void* instance, float* value) =>
            Float(instance, value, static (IPrismBackend b, out float v) => b.GetPitch(out v));

        // ---- Voices and audio format ----

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnRefreshVoices(void* instance) => Invoke(instance, static b => b.RefreshVoices());

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnCountVoices(void* instance, nuint* value) =>
            Size(instance, value, static (IPrismBackend b, out int v) => b.CountVoices(out v));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnGetVoice(void* instance, nuint* value) =>
            Size(instance, value, static (IPrismBackend b, out int v) => b.GetVoice(out v));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnGetChannels(void* instance, nuint* value) =>
            Size(instance, value, static (IPrismBackend b, out int v) => b.GetChannels(out v));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnGetSampleRate(void* instance, nuint* value) =>
            Size(instance, value, static (IPrismBackend b, out int v) => b.GetSampleRate(out v));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnGetBitDepth(void* instance, nuint* value) =>
            Size(instance, value, static (IPrismBackend b, out int v) => b.GetBitDepth(out v));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnSetVoice(void* instance, nuint voiceId) =>
            Invoke(instance, b => b.SetVoice(Methods.ToInt32(voiceId)));

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnGetVoiceName(void* instance, nuint voiceId, byte** value) =>
            VoiceString(instance, voiceId, value, language: false);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static Error OnGetVoiceLanguage(void* instance, nuint voiceId, byte** value) =>
            VoiceString(instance, voiceId, value, language: true);

        // ---- Shared helpers ----

        private delegate Error OutFloat(IPrismBackend backend, out float value);

        private delegate Error OutInt(IPrismBackend backend, out int value);

        private static Error Float(void* instance, float* target, OutFloat read)
        {
            try
            {
                var state = Resolve(instance);
                if (state == null || target == null)
                    return Error.InvalidParam;

                var error = read(state.Backend, out var value);
                if (error == Error.Ok)
                    *target = value;

                return error;
            }
            catch
            {
                return Error.Internal;
            }
        }

        private static Error Size(void* instance, nuint* target, OutInt read)
        {
            try
            {
                var state = Resolve(instance);
                if (state == null || target == null)
                    return Error.InvalidParam;

                var error = read(state.Backend, out var value);
                if (error == Error.Ok && value >= 0)
                    *target = (nuint)value;

                return error;
            }
            catch
            {
                return Error.Internal;
            }
        }

        private static Error VoiceString(void* instance, nuint voiceId, byte** target, bool language)
        {
            try
            {
                var state = Resolve(instance);
                if (state == null || target == null)
                    return Error.InvalidParam;

                var index = Methods.ToInt32(voiceId);
                var key = (language, index);
                if (state.Strings.TryGetValue(key, out var cached))
                {
                    *target = (byte*)cached;
                    return Error.Ok;
                }

                string? text;
                var error = language
                    ? state.Backend.GetVoiceLanguage(index, out text)
                    : state.Backend.GetVoiceName(index, out text);

                if (error != Error.Ok)
                    return error;

                // Prism borrows this pointer rather than copying, so it must outlive the call. The
                // buffer is released when the instance is destroyed.
                var utf8 = Utf8(text ?? string.Empty);
                state.Strings[key] = utf8;
                *target = (byte*)utf8;
                return Error.Ok;
            }
            catch
            {
                return Error.Internal;
            }
        }

        private static IntPtr Utf8(string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var buffer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            Marshal.WriteByte(buffer, bytes.Length, 0);
            return buffer;
        }

        private static Error Invoke(void* instance, Func<IPrismBackend, Error> action)
        {
            try
            {
                var state = Resolve(instance);
                return state == null ? Error.InvalidParam : action(state.Backend);
            }
            catch
            {
                return Error.Internal;
            }
        }

        private static Instance? Resolve(void* instance)
        {
            if (instance == null)
                return null;

            var handle = GCHandle.FromIntPtr((nint)instance);
            return handle.IsAllocated ? handle.Target as Instance : null;
        }

        private static string Text(byte* value) => Methods.String(value) ?? string.Empty;
    }
}
