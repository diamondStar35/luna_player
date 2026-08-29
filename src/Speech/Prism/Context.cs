using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using PrismSharp.Native;

namespace PrismSharp
{
    /// <summary>
    /// Reports that a backend's runtime availability has changed.
    /// </summary>
    /// <param name="backendId">Identifier of the backend that changed. Stable; may be retained.</param>
    /// <param name="name">The backend's name, copied before this call returns.</param>
    /// <param name="available"><see langword="true"/> if the backend became available.</param>
    /// <remarks>
    /// Invoked on Prism's internal poll thread, not one owned by the application. It must synchronize
    /// any shared state it touches, must not block - the poll thread cannot scan again until it
    /// returns - and must not dispose the <see cref="Context"/> that owns the thread, which would
    /// deadlock against itself. Read-only registry queries and backend acquisition on the same
    /// context are safe to call from here.
    /// </remarks>
    public delegate void AvailabilityCallback(ulong backendId, string name, bool available);

    /// <summary>
    /// Optional configuration for a <see cref="Context"/>, mapping onto <c>PrismConfig</c>.
    /// </summary>
    public sealed class ContextOptions
    {
        /// <summary>
        /// A frozen registry carrying custom or plugin backends, or <see langword="null"/> for the
        /// global registry.
        /// </summary>
        /// <remarks>
        /// The context takes its own reference, so the caller keeps ownership of the registry it
        /// passes. Contexts bound to the same registry share its backend cache; contexts bound to
        /// different registries share nothing.
        /// </remarks>
        public Registry? Registry { get; set; }

        /// <summary>
        /// Invoked when a backend's runtime availability changes.
        /// </summary>
        /// <remarks>
        /// Supplying a callback starts Prism's internal poll thread. Leaving it
        /// <see langword="null"/> means no thread is created and the feature costs nothing. The first
        /// scan establishes a baseline silently, so a backend already available at construction
        /// produces no notification - query initial availability directly.
        /// </remarks>
        public AvailabilityCallback? AvailabilityChanged { get; set; }

        /// <summary>
        /// Base interval between availability scans, in milliseconds. Zero selects Prism's default of
        /// 1000. Ignored when <see cref="AvailabilityChanged"/> is <see langword="null"/>.
        /// </summary>
        public uint PollIntervalMs { get; set; }

        /// <summary>
        /// Consecutive agreeing samples required before a change is confirmed and reported. Zero
        /// selects the default of 2; one reports every observed change immediately, without
        /// debouncing. Ignored when <see cref="AvailabilityChanged"/> is <see langword="null"/>.
        /// </summary>
        public uint DebounceSamples { get; set; }

        /// <summary>
        /// Upper bound, in milliseconds, for adaptive backoff of the scan interval.
        /// </summary>
        /// <remarks>
        /// While availability is unchanging the interval doubles from <see cref="PollIntervalMs"/>
        /// toward this bound, returning to the base interval as soon as any change is observed. Zero,
        /// or any value not greater than the base interval, disables backoff.
        /// </remarks>
        public uint BackoffMaxMs { get; set; }

        /// <summary>
        /// Pause the poll thread automatically across operating-system suspend and resume.
        /// </summary>
        /// <remarks>
        /// Honoured only where <see cref="Context.AutoPowerManagementSupported"/> is
        /// <see langword="true"/>; elsewhere the application may drive pausing itself.
        /// </remarks>
        public bool AutoPowerManage { get; set; }
    }

    /// <summary>
    /// A Prism context: the entry point to the library and the handle through which backends are
    /// discovered and created.
    /// </summary>
    /// <remarks>
    /// Each context is independent, and several may exist at once. A context is bound at creation to
    /// the registry named by <see cref="ContextOptions.Registry"/>, or to the global registry.
    /// <para>
    /// Registry operations are thread-safe, and this class serializes them internally. Contexts do
    /// not own the backends they hand out: a backend obtained from a context stays valid after the
    /// context is disposed, though no new backends can be obtained from it afterwards.
    /// </para>
    /// </remarks>
    public sealed unsafe class Context : IDisposable
    {
        private readonly Lock _sync = new();

        // Rooted for the life of the context: Prism's poll thread dereferences this handle, and that
        // thread is only joined by prism_shutdown.
        private GCHandle _availabilityHandle;

        private PrismContextHandle* _handle;

        /// <summary>Creates a context bound to the global registry, with no availability polling.</summary>
        /// <exception cref="PrismException">Initialization failed.</exception>
        /// <exception cref="PlatformNotSupportedException">The loaded Prism library is too old.</exception>
        public Context()
            : this(null)
        {
        }

        /// <summary>Creates a context.</summary>
        /// <param name="options">Configuration, or <see langword="null"/> for the defaults.</param>
        /// <exception cref="PrismException">Initialization failed.</exception>
        /// <exception cref="PlatformNotSupportedException">
        /// The loaded Prism library is older than these bindings, or reports a configuration version
        /// they do not understand.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The supplied registry has been disposed.</exception>
        public Context(ContextOptions? options)
        {
            // The loaded library, not the headers these bindings were written against, decides the
            // PrismConfig layout and the error codes. Fail loudly rather than corrupt memory.
            PrismVersion.EnsureSupported();

            var config = Methods.prism_config_init();
            if (config.version > PrismConfig.CurrentVersion)
                throw new PlatformNotSupportedException(
                    $"Prism reported PrismConfig version {config.version}; these bindings describe {PrismConfig.CurrentVersion}.");

            if (options != null)
            {
                if (options.Registry != null)
                {
                    ObjectDisposedException.ThrowIf(!options.Registry.IsOpen, options.Registry);
                    config.registry = options.Registry.Handle;
                }

                if (options.AvailabilityChanged != null)
                {
                    _availabilityHandle = GCHandle.Alloc(options.AvailabilityChanged);
                    config.availability_callback = &OnAvailabilityChanged;
                    config.availability_userdata = (void*)GCHandle.ToIntPtr(_availabilityHandle);
                    config.availability_poll_interval_ms = options.PollIntervalMs;
                    config.availability_debounce_samples = options.DebounceSamples;
                    config.availability_backoff_max_ms = options.BackoffMaxMs;
                    config.availability_auto_power_manage = options.AutoPowerManage ? (byte)1 : (byte)0;
                }
            }

            _handle = Methods.prism_init(&config);
            if (_handle == null)
            {
                ReleaseAvailabilityHandle();
                throw new PrismException(Error.NotInitialized);
            }
        }

        /// <summary>
        /// Whether this build of Prism honours <see cref="ContextOptions.AutoPowerManage"/>.
        /// </summary>
        public static bool AutoPowerManagementSupported => Methods.prism_availability_auto_power_supported() != 0;

        /// <summary>
        /// Every backend registered in this context's registry, in descending priority order.
        /// </summary>
        /// <remarks>
        /// Registration says nothing about whether a backend can run right now. To test that, create
        /// one with <see cref="CreateUninitialized"/> and read
        /// <see cref="Backend.IsSupportedAtRuntime"/>. The set is fixed for the registry's lifetime,
        /// and an empty list means Prism was built with no backends for this platform.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public IReadOnlyList<BackendInfo> AvailableBackends
        {
            get
            {
                lock (_sync)
                {
                    ThrowIfClosed();

                    // size_t functions report failure as SIZE_MAX; treat that as "no backends".
                    var native = Methods.prism_registry_count(_handle);
                    var count = native == nuint.MaxValue ? 0 : Methods.ToInt32(native);

                    var backends = new List<BackendInfo>(count);
                    for (var i = 0; i < count; i++)
                    {
                        var id = Methods.prism_registry_id_at(_handle, (nuint)i);
                        if (id == Ids.Invalid)
                            continue;

                        // prism_registry_priority reports an unknown id as -1; never let that reach a sort.
                        var priority = Methods.prism_registry_priority(_handle, id);
                        backends.Add(new BackendInfo(
                            id,
                            Methods.String(Methods.prism_registry_name(_handle, id)) ?? string.Empty,
                            priority < 0 ? 0 : priority,
                            Methods.prism_registry_exists(_handle, id) != 0));
                    }

                    return backends;
                }
            }
        }

        /// <summary>
        /// Looks up a backend by its human-readable name.
        /// </summary>
        /// <param name="name">The name to find. Matching is exact and case-sensitive.</param>
        /// <returns>The backend's identifier, or <see cref="Ids.Invalid"/> if there is no such backend.</returns>
        /// <remarks>
        /// When the wanted backend is known at compile time, the <see cref="Ids"/> constants are
        /// cheaper, since they avoid the string comparison.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public ulong GetBackendId(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            lock (_sync)
            {
                ThrowIfClosed();
                return Methods.prism_registry_id(_handle, name);
            }
        }

        /// <summary>Whether a backend with the given identifier is registered.</summary>
        /// <param name="id">The identifier to test.</param>
        /// <returns><see langword="true"/> when the backend exists in this context's registry.</returns>
        /// <remarks>
        /// This checks registration only, not whether the backend can initialize. For the predefined
        /// <see cref="Ids"/> it effectively asks whether Prism was compiled with that backend for this
        /// platform.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public bool Exists(ulong id)
        {
            lock (_sync)
            {
                ThrowIfClosed();
                return Methods.prism_registry_exists(_handle, id) != 0;
            }
        }

        /// <summary>
        /// Acquires the shared instance for an identifier, creating one if the cache is empty, and
        /// initializes it.
        /// </summary>
        /// <param name="id">The backend to acquire.</param>
        /// <returns>An initialized backend. The caller must dispose it.</returns>
        /// <remarks>
        /// The returned handle shares ownership and state with every other holder, so a voice or rate
        /// set through one handle is visible through the rest. Prefer <see cref="Create"/> unless
        /// sharing state with other callers is specifically wanted.
        /// </remarks>
        /// <exception cref="PrismException">The backend could not be acquired or initialized.</exception>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public Backend Acquire(ulong id)
        {
            lock (_sync)
            {
                ThrowIfClosed();
                return Open(Methods.prism_registry_acquire(_handle, id), id, initialize: true);
            }
        }

        /// <summary>Creates an independent backend instance and initializes it.</summary>
        /// <param name="id">The backend to create.</param>
        /// <returns>An initialized backend with isolated state. The caller must dispose it.</returns>
        /// <exception cref="PrismException">The backend could not be created or initialized.</exception>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public Backend Create(ulong id)
        {
            lock (_sync)
            {
                ThrowIfClosed();
                return Open(Methods.prism_registry_create(_handle, id), id, initialize: true);
            }
        }

        /// <summary>
        /// Creates an independent backend instance <b>without</b> initializing it.
        /// </summary>
        /// <param name="id">The backend to create.</param>
        /// <returns>An uninitialized backend. The caller must dispose it.</returns>
        /// <remarks>
        /// Intended for discovery. <see cref="Backend.Features"/> and <see cref="Backend.Name"/> are
        /// valid on an uninitialized backend, so availability can be probed without paying for
        /// initialization - which is slow and, for some backends, has side effects such as prompting
        /// the user. Any other operation returns <see cref="Error.NotInitialized"/>.
        /// </remarks>
        /// <exception cref="PrismException">The backend could not be created.</exception>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public Backend CreateUninitialized(ulong id)
        {
            lock (_sync)
            {
                ThrowIfClosed();
                return Open(Methods.prism_registry_create(_handle, id), id, initialize: false);
            }
        }

        /// <summary>
        /// Acquires the highest-priority backend that initializes, reusing a cached instance when one
        /// exists.
        /// </summary>
        /// <returns>An already-initialized backend. The caller must dispose it.</returns>
        /// <remarks>
        /// Tries backends in descending priority order, so a running screen reader is preferred over a
        /// standalone engine. The result is already initialized and shares state with other holders;
        /// prefer <see cref="CreateBest"/> without a specific reason to share.
        /// </remarks>
        /// <exception cref="PrismException">No backend could be acquired.</exception>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public Backend AcquireBest()
        {
            lock (_sync)
            {
                ThrowIfClosed();
                return Open(Methods.prism_registry_acquire_best(_handle), Ids.Invalid, initialize: false);
            }
        }

        /// <summary>
        /// Creates a new instance of the highest-priority backend that initializes.
        /// </summary>
        /// <returns>An already-initialized backend with isolated state. The caller must dispose it.</returns>
        /// <remarks>
        /// The recommended way to obtain a backend with no particular preference: it prefers a running
        /// screen reader and falls back to a standalone engine. The result is already initialized.
        /// </remarks>
        /// <exception cref="PrismException">No backend could be created.</exception>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public Backend CreateBest()
        {
            lock (_sync)
            {
                ThrowIfClosed();
                return Open(Methods.prism_registry_create_best(_handle), Ids.Invalid, initialize: false);
            }
        }

        /// <summary>
        /// Returns the cached instance for an identifier if one is still alive, without creating one.
        /// </summary>
        /// <param name="id">The backend to look for.</param>
        /// <returns>
        /// A handle sharing ownership of the cached instance, or <see langword="null"/> when nothing is
        /// cached. A returned handle must still be disposed.
        /// </returns>
        /// <remarks>
        /// The cache holds weak references: once every handle to an instance has been disposed the
        /// backend is destroyed and this returns <see langword="null"/> until something acquires it
        /// again.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public Backend? TryGetCached(ulong id)
        {
            lock (_sync)
            {
                ThrowIfClosed();

                var handle = Methods.prism_registry_get(_handle, id);
                return handle == null ? null : new Backend(handle, id);
            }
        }

        /// <summary>
        /// Suspends the availability poll thread. No effect when polling was not configured.
        /// </summary>
        /// <remarks>While paused the thread performs no scans and consumes no processor time.</remarks>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public void PauseAvailabilityPolling()
        {
            lock (_sync)
            {
                ThrowIfClosed();
                Methods.prism_availability_poll_pause(_handle);
            }
        }

        /// <summary>
        /// Resumes the availability poll thread.
        /// </summary>
        /// <remarks>
        /// Resuming performs an immediate re-synchronizing scan rather than waiting for the next
        /// interval, and that scan is not debounced, so anything that changed while paused is reported
        /// at once. A change that occurred and reverted entirely while paused is not reported.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
        public void ResumeAvailabilityPolling()
        {
            lock (_sync)
            {
                ThrowIfClosed();
                Methods.prism_availability_poll_resume(_handle);
            }
        }

        /// <summary>
        /// Destroys the context and releases its resources, joining the availability poll thread.
        /// </summary>
        /// <remarks>
        /// Backends obtained from this context remain valid afterwards, but no new ones can be
        /// obtained. No other thread may be using the context while this runs.
        /// </remarks>
        public void Dispose()
        {
            lock (_sync)
            {
                if (_handle == null)
                    return;

                var handle = _handle;
                _handle = null;

                // Joins the poll thread, so no callback can be in flight once this returns.
                Methods.prism_shutdown(handle);
                ReleaseAvailabilityHandle();
            }
        }

        private static Backend Open(PrismBackendHandle* handle, ulong requestedId, bool initialize)
        {
            if (handle == null)
                throw new PrismException(Error.BackendNotAvailable);

            var backend = new Backend(handle, requestedId);
            if (!initialize)
                return backend;

            try
            {
                backend.Initialize();
            }
            catch
            {
                backend.Dispose();
                throw;
            }

            return backend;
        }

        private void ReleaseAvailabilityHandle()
        {
            if (_availabilityHandle.IsAllocated)
                _availabilityHandle.Free();
        }

        private void ThrowIfClosed()
        {
            ObjectDisposedException.ThrowIf(_handle == null, this);
        }

        // Runs on Prism's poll thread. An exception escaping into native code is undefined behaviour.
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnAvailabilityChanged(void* userdata, ulong backendId, byte* name, byte available)
        {
            try
            {
                if (userdata == null)
                    return;

                var handle = GCHandle.FromIntPtr((nint)userdata);
                if (!handle.IsAllocated || handle.Target is not AvailabilityCallback callback)
                    return;

                // The name points into the registry and is valid only for this call, so copy it.
                callback(backendId, Methods.String(name) ?? string.Empty, available != 0);
            }
            catch
            {
            }
        }
    }
}
