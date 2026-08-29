using System.Runtime.InteropServices;

namespace PrismSharp.Native
{
    /// <summary>
    /// Direct bindings to prism.h (v0.18.2).
    /// </summary>
    /// <remarks>
    /// Declared with <see cref="LibraryImportAttribute"/> so the source generator emits the interop
    /// stub at compile time rather than the runtime building one, which is what keeps these calls
    /// NativeAOT-safe and free of marshalling overhead. Combined with the assembly-wide
    /// <c>DisableRuntimeMarshalling</c>, every signature here is blittable: C <c>bool</c> is a
    /// <see cref="byte"/>, C <c>size_t</c> is a <see cref="nuint"/>, and strings cross as UTF-8
    /// pointers.
    /// <para>
    /// A <c>const char*</c> coming back is returned as <c>byte*</c> rather than a marshalled
    /// <see cref="string"/> on purpose: Prism owns those buffers, and generated string marshalling
    /// would try to free them.
    /// </para>
    /// </remarks>
    internal static unsafe partial class Methods
    {
        private const string Library = "prism";

        #region Context management

        [LibraryImport(Library)]
        internal static partial PrismConfig prism_config_init();

        [LibraryImport(Library)]
        internal static partial PrismContextHandle* prism_init(PrismConfig* cfg);

        [LibraryImport(Library)]
        internal static partial void prism_shutdown(PrismContextHandle* ctx);

        [LibraryImport(Library)]
        internal static partial void prism_availability_poll_pause(PrismContextHandle* ctx);

        [LibraryImport(Library)]
        internal static partial void prism_availability_poll_resume(PrismContextHandle* ctx);

        [LibraryImport(Library)]
        internal static partial byte prism_availability_auto_power_supported();

        #endregion

        #region Registry

        [LibraryImport(Library)]
        internal static partial nuint prism_registry_count(PrismContextHandle* ctx);

        [LibraryImport(Library)]
        internal static partial ulong prism_registry_id_at(PrismContextHandle* ctx, nuint index);

        [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial ulong prism_registry_id(PrismContextHandle* ctx, string name);

        [LibraryImport(Library)]
        internal static partial byte* prism_registry_name(PrismContextHandle* ctx, ulong id);

        [LibraryImport(Library)]
        internal static partial int prism_registry_priority(PrismContextHandle* ctx, ulong id);

        [LibraryImport(Library)]
        internal static partial byte prism_registry_exists(PrismContextHandle* ctx, ulong id);

        [LibraryImport(Library)]
        internal static partial PrismBackendHandle* prism_registry_get(PrismContextHandle* ctx, ulong id);

        [LibraryImport(Library)]
        internal static partial PrismBackendHandle* prism_registry_create(PrismContextHandle* ctx, ulong id);

        [LibraryImport(Library)]
        internal static partial PrismBackendHandle* prism_registry_create_best(PrismContextHandle* ctx);

        [LibraryImport(Library)]
        internal static partial PrismBackendHandle* prism_registry_acquire(PrismContextHandle* ctx, ulong id);

        [LibraryImport(Library)]
        internal static partial PrismBackendHandle* prism_registry_acquire_best(PrismContextHandle* ctx);

        #endregion

        #region Registry builder

        [LibraryImport(Library)]
        internal static partial PrismRegistryBuilderHandle* prism_registry_builder_new();

        [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial Error prism_registry_builder_add_backend(
            PrismRegistryBuilderHandle* builder,
            string name,
            int priority,
            ulong features,
            BackendVTable* vtable,
            void* userdata,
            delegate* unmanaged[Cdecl]<void*, void> userdata_free,
            ulong* out_id);

        [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial Error prism_registry_builder_add_library(
            PrismRegistryBuilderHandle* builder, string path, int priority_override, nuint* out_count);

        [LibraryImport(Library)]
        internal static partial PrismRegistryHandle* prism_registry_freeze(PrismRegistryBuilderHandle* builder);

        [LibraryImport(Library)]
        internal static partial void prism_registry_builder_free(PrismRegistryBuilderHandle* builder);

        [LibraryImport(Library)]
        internal static partial PrismRegistryHandle* prism_registry_retain(PrismRegistryHandle* registry);

        [LibraryImport(Library)]
        internal static partial void prism_registry_release(PrismRegistryHandle* registry);

        #endregion

        #region Backends

        [LibraryImport(Library)]
        internal static partial void prism_backend_free(PrismBackendHandle* backend);

        [LibraryImport(Library)]
        internal static partial byte* prism_backend_name(PrismBackendHandle* backend);

        [LibraryImport(Library)]
        internal static partial ulong prism_backend_get_features(PrismBackendHandle* backend);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_initialize(PrismBackendHandle* backend);

        [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial Error prism_backend_speak(PrismBackendHandle* backend, string text, byte interrupt);

        [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial Error prism_backend_speak_to_memory(
            PrismBackendHandle* backend,
            string text,
            delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void> callback,
            void* userdata);

        [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial Error prism_backend_braille(PrismBackendHandle* backend, string text);

        [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial Error prism_backend_output(PrismBackendHandle* backend, string text, byte interrupt);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_stop(PrismBackendHandle* backend);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_pause(PrismBackendHandle* backend);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_resume(PrismBackendHandle* backend);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_is_speaking(PrismBackendHandle* backend, byte* out_speaking);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_set_volume(PrismBackendHandle* backend, float volume);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_get_volume(PrismBackendHandle* backend, float* out_volume);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_set_rate(PrismBackendHandle* backend, float rate);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_get_rate(PrismBackendHandle* backend, float* out_rate);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_set_pitch(PrismBackendHandle* backend, float pitch);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_get_pitch(PrismBackendHandle* backend, float* out_pitch);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_refresh_voices(PrismBackendHandle* backend);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_count_voices(PrismBackendHandle* backend, nuint* out_count);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_get_voice_name(PrismBackendHandle* backend, nuint voice_id, byte** out_name);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_get_voice_language(PrismBackendHandle* backend, nuint voice_id, byte** out_language);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_set_voice(PrismBackendHandle* backend, nuint voice_id);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_get_voice(PrismBackendHandle* backend, nuint* out_voice_id);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_get_channels(PrismBackendHandle* backend, nuint* out_channels);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_get_sample_rate(PrismBackendHandle* backend, nuint* out_sample_rate);

        [LibraryImport(Library)]
        internal static partial Error prism_backend_get_bit_depth(PrismBackendHandle* backend, nuint* out_bit_depth);

        #endregion

        #region Logging and utilities

        [LibraryImport(Library)]
        internal static partial PrismLogHandler prism_set_log_handler(PrismLogHandler handler);

        [LibraryImport(Library)]
        internal static partial LogLevel prism_set_log_level(LogLevel level);

        [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial void prism_log(LogLevel level, string source, string message);

        [LibraryImport(Library)]
        internal static partial void prism_log_flush();

        [LibraryImport(Library)]
        internal static partial void prism_log_shutdown();

        [LibraryImport(Library)]
        internal static partial byte* prism_error_string(Error error);

        [LibraryImport(Library)]
        internal static partial uint prism_version();

        [LibraryImport(Library)]
        internal static partial byte* prism_version_string();

        #endregion

        /// <summary>
        /// Reads a Prism-owned null-terminated UTF-8 string. The buffer belongs to Prism and is never
        /// freed here.
        /// </summary>
        internal static string? String(byte* value) => value == null ? null : Marshal.PtrToStringUTF8((nint)value);

        /// <summary>
        /// Narrows a <c>size_t</c>, saturating instead of throwing. A value this large is either the
        /// documented SIZE_MAX failure sentinel or a bug, and neither is worth an exception from
        /// inside an interop wrapper.
        /// </summary>
        internal static int ToInt32(nuint value) => value > int.MaxValue ? int.MaxValue : (int)value;
    }
}
