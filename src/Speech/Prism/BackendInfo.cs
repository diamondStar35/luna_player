namespace PrismSharp
{
    /// <summary>A backend as it appears in the registry, before any instance is created.</summary>
    public readonly struct BackendInfo
    {
        /// <summary>Creates a registry entry descriptor.</summary>
        /// <param name="id">Stable backend identifier.</param>
        /// <param name="name">Human-readable name.</param>
        /// <param name="priority">Selection priority.</param>
        /// <param name="isSupported">Whether the backend is registered on this platform.</param>
        public BackendInfo(ulong id, string name, int priority, bool isSupported)
        {
            Id = id;
            Name = name;
            Priority = priority;
            IsSupported = isSupported;
        }

        /// <summary>
        /// The backend's stable identifier, matching an <see cref="Ids"/> constant for a built-in
        /// backend. Safe to persist and reuse across runs.
        /// </summary>
        public ulong Id { get; }

        /// <summary>
        /// The backend's human-readable name, such as <c>"NVDA"</c>, <c>"SAPI"</c> or
        /// <c>"Speech Dispatcher"</c>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Selection priority; higher wins. Screen readers rank above standalone engines, on the
        /// assumption that a user running one wants speech routed through it. Treat the ordering as
        /// meaningful but the specific numbers as subject to change.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Whether the backend is registered - that is, compiled into this build of Prism and
        /// applicable to this platform. It says nothing about whether the backend can run right now;
        /// for that, create one and read <see cref="Backend.IsSupportedAtRuntime"/>.
        /// </summary>
        public bool IsSupported { get; }
    }
}
