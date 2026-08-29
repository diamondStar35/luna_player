using System.Runtime.InteropServices;

namespace PrismSharp.Native
{
    /// <summary>
    /// Mirrors <c>PrismBackendVTable</c> from prism.h. Member order and count are ABI.
    /// </summary>
    /// <remarks>
    /// Prism copies this struct during <c>prism_registry_builder_add_backend</c> but keeps the
    /// function pointers, which must stay valid for as long as any registry or instance refers to
    /// them. Because every slot here is filled from an <c>UnmanagedCallersOnly</c> method, the
    /// pointers are ordinary static code addresses with no delegate to keep alive and nothing for a
    /// trimmer or AOT compiler to remove.
    /// <para>
    /// A slot must be non-null exactly when its corresponding feature bit is declared; Prism
    /// validates this and rejects a mismatch with <c>PRISM_ERROR_INVALID_PARAM</c>.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct BackendVTable
    {
        /// <summary>Size of this struct. Prism rejects a vtable declaring zero.</summary>
        public nuint size;

        public delegate* unmanaged[Cdecl]<void*, void*> create;
        public delegate* unmanaged[Cdecl]<void*, void> destroy;
        public delegate* unmanaged[Cdecl]<void*, byte> is_supported;
        public delegate* unmanaged[Cdecl]<void*, Error> initialize;
        public delegate* unmanaged[Cdecl]<void*, byte*, byte, Error> speak;
        public delegate* unmanaged[Cdecl]<void*, byte*, delegate* unmanaged[Cdecl]<void*, float*, nuint, nuint, nuint, void>, void*, Error> speak_to_memory;
        public delegate* unmanaged[Cdecl]<void*, byte*, Error> braille;
        public delegate* unmanaged[Cdecl]<void*, byte*, byte, Error> output;
        public delegate* unmanaged[Cdecl]<void*, Error> stop;
        public delegate* unmanaged[Cdecl]<void*, Error> pause;
        public delegate* unmanaged[Cdecl]<void*, Error> resume;
        public delegate* unmanaged[Cdecl]<void*, byte*, Error> is_speaking;
        public delegate* unmanaged[Cdecl]<void*, float, Error> set_volume;
        public delegate* unmanaged[Cdecl]<void*, float*, Error> get_volume;
        public delegate* unmanaged[Cdecl]<void*, float, Error> set_rate;
        public delegate* unmanaged[Cdecl]<void*, float*, Error> get_rate;
        public delegate* unmanaged[Cdecl]<void*, float, Error> set_pitch;
        public delegate* unmanaged[Cdecl]<void*, float*, Error> get_pitch;
        public delegate* unmanaged[Cdecl]<void*, Error> refresh_voices;
        public delegate* unmanaged[Cdecl]<void*, nuint*, Error> count_voices;
        public delegate* unmanaged[Cdecl]<void*, nuint, byte**, Error> get_voice_name;
        public delegate* unmanaged[Cdecl]<void*, nuint, byte**, Error> get_voice_language;
        public delegate* unmanaged[Cdecl]<void*, nuint, Error> set_voice;
        public delegate* unmanaged[Cdecl]<void*, nuint*, Error> get_voice;
        public delegate* unmanaged[Cdecl]<void*, nuint*, Error> get_channels;
        public delegate* unmanaged[Cdecl]<void*, nuint*, Error> get_sample_rate;
        public delegate* unmanaged[Cdecl]<void*, nuint*, Error> get_bit_depth;
    }
}
