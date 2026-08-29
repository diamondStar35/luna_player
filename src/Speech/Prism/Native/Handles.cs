using System.Runtime.InteropServices;

namespace PrismSharp.Native
{
    /// <summary>Opaque <c>PrismContext</c>. Only ever used behind a pointer.</summary>
    internal struct PrismContextHandle;

    /// <summary>Opaque <c>PrismBackend</c>. Only ever used behind a pointer.</summary>
    internal struct PrismBackendHandle;

    /// <summary>Opaque <c>PrismRegistry</c>. Only ever used behind a pointer.</summary>
    internal struct PrismRegistryHandle;

    /// <summary>Opaque <c>PrismRegistryBuilder</c>. Only ever used behind a pointer.</summary>
    internal struct PrismRegistryBuilderHandle;

    /// <summary>
    /// Mirrors <c>PrismConfig</c> from prism.h (v0.18.2). The layout is ABI: the struct is returned
    /// by value from <c>prism_config_init</c> and read through a pointer by <c>prism_init</c>, so
    /// every field must be present and correctly aligned even when unused.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PrismConfig
    {
        /// <summary>PRISM_CONFIG_VERSION. Prism rejects a configuration newer than itself.</summary>
        public const byte CurrentVersion = 3;

        /// <summary>Written by <c>prism_config_init</c>; the manual says it MUST NOT be modified.</summary>
        public byte version;

        /// <summary>A registry from <c>prism_registry_freeze</c>, or null for the global registry.</summary>
        public PrismRegistryHandle* registry;

        /// <summary>An availability callback, or null to run no poll thread at all.</summary>
        public delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte, void> availability_callback;

        public void* availability_userdata;
        public uint availability_poll_interval_ms;
        public uint availability_debounce_samples;
        public uint availability_backoff_max_ms;
        public byte availability_auto_power_manage;
    }

    /// <summary>Mirrors <c>PrismLogHandler</c>. Passed and returned by value.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PrismLogHandler
    {
        public delegate* unmanaged[Cdecl]<void*, LogLevel, byte*, byte*, void> fn;
        public void* userdata;
    }
}
