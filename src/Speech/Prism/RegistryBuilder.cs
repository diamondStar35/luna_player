using System;
using PrismSharp.Native;

namespace PrismSharp
{
    /// <summary>
    /// Builds a <see cref="Registry"/> containing the compiled-in backends plus any custom or plugin
    /// backends added here.
    /// </summary>
    /// <remarks>
    /// A builder is a transient configuration object and is <b>not</b> thread-safe; if one is
    /// reachable from more than one thread, the application must synchronize it externally.
    /// </remarks>
    public sealed unsafe class RegistryBuilder : IDisposable
    {
        /// <summary>
        /// Priority override meaning "honour whatever priorities the plugin declares".
        /// </summary>
        public const int UsePluginPriority = -1;

        private PrismRegistryBuilderHandle* _handle;
        private bool _frozen;

        /// <summary>Creates an empty builder.</summary>
        /// <exception cref="PrismException">Allocation failed.</exception>
        public RegistryBuilder()
        {
            _handle = Methods.prism_registry_builder_new();
            if (_handle == null)
                throw new PrismException(Error.MemoryFailure);
        }

        /// <summary>
        /// Loads a plugin shared library and adds every backend it exports.
        /// </summary>
        /// <param name="path">
        /// Path to a shared library exporting <c>prism_plugin_query</c>. Must be non-empty and free of
        /// embedded nulls.
        /// </param>
        /// <param name="priorityOverride">
        /// A non-negative priority applied to every backend the library supplies, or
        /// <see cref="UsePluginPriority"/> to keep the priorities the plugin declares.
        /// </param>
        /// <returns>The number of backends added.</returns>
        /// <remarks>
        /// Loading is atomic: if any part of it fails, no backend from the library is added and the
        /// library is unloaded before this returns, so a partially loaded plugin is never observable.
        /// A plugin's backends are added to this builder only and never appear in the global registry.
        /// </remarks>
        /// <exception cref="PrismException">
        /// Loading failed - typically <see cref="Error.LibraryLoadFailed"/> when the file cannot be
        /// opened, <see cref="Error.LibraryInvalid"/> when it exports no plugin entry point, or
        /// <see cref="Error.IncompatibleAbi"/> when its declared ABI generation is not accepted.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The builder has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The builder has already been frozen.</exception>
        public int AddLibrary(string path, int priorityOverride = UsePluginPriority)
        {
            EnsureUsable();

            if (string.IsNullOrEmpty(path) || path.Contains('\0'))
                throw new PrismException(Error.InvalidParam);
            ArgumentOutOfRangeException.ThrowIfLessThan(priorityOverride, UsePluginPriority);

            nuint count;
            var error = Methods.prism_registry_builder_add_library(_handle, path, priorityOverride, &count);
            if (error != Error.Ok)
                throw new PrismException(error);

            return Methods.ToInt32(count);
        }

        /// <summary>
        /// Registers a backend implemented in managed code.
        /// </summary>
        /// <param name="name">
        /// The backend's name, as reported by <see cref="Context.AvailableBackends"/> and matched by
        /// <see cref="Context.GetBackendId"/>. Must be unique within the builder and valid UTF-8.
        /// </param>
        /// <param name="priority">
        /// Selection priority; higher wins, and the <c>Best</c> methods try backends in descending
        /// order. Must be non-negative. Built-in screen readers sit around 100, with standalone TTS
        /// engines just below.
        /// </param>
        /// <param name="factory">
        /// Produces one instance per backend Prism creates. Must be thread-safe and must not throw. It
        /// is also invoked once immediately - and that instance disposed - to read
        /// <see cref="IPrismBackend.DeclaredFeatures"/>, so it must tolerate producing an instance
        /// that is never initialized or used.
        /// </param>
        /// <returns>The identifier Prism assigned to the backend.</returns>
        /// <exception cref="PrismException">
        /// Registration failed. <see cref="Error.InvalidParam"/> most often means the declared feature
        /// bits do not correspond to the operations the backend actually overrides;
        /// <see cref="Error.InvalidOperation"/> means the name is already taken.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The builder has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The builder has already been frozen.</exception>
        public ulong AddBackend(string name, int priority, Func<IPrismBackend> factory)
        {
            EnsureUsable();
            return CustomBackend.Register(_handle, name, priority, factory);
        }

        /// <summary>
        /// Produces the immutable registry.
        /// </summary>
        /// <returns>The frozen registry, which the caller owns and must dispose.</returns>
        /// <remarks>The builder is spent afterwards but still needs disposing.</remarks>
        /// <exception cref="PrismException">Freezing failed.</exception>
        /// <exception cref="ObjectDisposedException">The builder has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The builder has already been frozen.</exception>
        public Registry Freeze()
        {
            EnsureUsable();

            var registry = Methods.prism_registry_freeze(_handle);
            _frozen = true;
            if (registry == null)
                throw new PrismException(Error.MemoryFailure);

            return new Registry(registry);
        }

        /// <summary>Releases the builder. Required even after <see cref="Freeze"/>.</summary>
        public void Dispose()
        {
            if (_handle == null)
                return;

            var handle = _handle;
            _handle = null;
            Methods.prism_registry_builder_free(handle);
        }

        private void EnsureUsable()
        {
            ObjectDisposedException.ThrowIf(_handle == null, this);
            if (_frozen)
                throw new InvalidOperationException("This builder has already been frozen.");
        }
    }
}
