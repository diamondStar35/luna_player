using System;
using PrismSharp.Native;

namespace PrismSharp
{
    /// <summary>
    /// A frozen, immutable backend registry produced by <see cref="RegistryBuilder.Freeze"/>.
    /// </summary>
    /// <remarks>
    /// Bind a registry to a <see cref="Context"/> through <see cref="ContextOptions.Registry"/> to
    /// make custom and plugin backends visible to that context. A registry's contents are fixed when
    /// it is created; there is no way to add or remove a backend afterwards.
    /// <para>
    /// The handle is reference counted. A context binding takes its own reference, so disposing this
    /// object does not invalidate a context still using the registry: the registry is finalized once
    /// the last bound context has shut down and this reference has been released. Any number of
    /// contexts may share one registry, and they then share its backend cache.
    /// </para>
    /// </remarks>
    public sealed unsafe class Registry : IDisposable
    {
        private PrismRegistryHandle* _handle;

        internal Registry(PrismRegistryHandle* handle)
        {
            if (handle == null)
                throw new ArgumentException("Registry handle is null.", nameof(handle));

            _handle = handle;
        }

        internal PrismRegistryHandle* Handle => _handle;

        /// <summary>Whether this reference is still open.</summary>
        public bool IsOpen => _handle != null;

        /// <summary>
        /// Takes an additional reference to the same registry.
        /// </summary>
        /// <returns>An independently disposable handle to the same underlying registry.</returns>
        /// <exception cref="ObjectDisposedException">This reference has been disposed.</exception>
        public Registry Retain()
        {
            ObjectDisposedException.ThrowIf(_handle == null, this);
            return new Registry(Methods.prism_registry_retain(_handle));
        }

        /// <summary>
        /// Releases this reference.
        /// </summary>
        /// <remarks>
        /// The registry is finalized only when the last reference goes, at which point the
        /// userdata-free callbacks of any custom backends run on the calling thread.
        /// </remarks>
        public void Dispose()
        {
            if (_handle == null)
                return;

            var handle = _handle;
            _handle = null;
            Methods.prism_registry_release(handle);
        }
    }
}
