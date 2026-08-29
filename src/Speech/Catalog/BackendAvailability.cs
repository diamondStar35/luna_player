namespace PrismSharp.Speech
{
    /// <summary>
    /// A change in whether a speech backend can be used right now - a screen reader starting or
    /// quitting, for instance.
    /// </summary>
    /// <remarks>
    /// Reported by Prism's background availability enumeration. Only transitions are reported: the
    /// first scan establishes a baseline silently, so initial availability must be queried directly.
    /// </remarks>
    public readonly struct SpeechBackendAvailability
    {
        /// <summary>Creates an availability change record.</summary>
        /// <param name="id">The backend's identifier.</param>
        /// <param name="name">The backend's name.</param>
        /// <param name="available">Whether it became available.</param>
        public SpeechBackendAvailability(ulong id, string name, bool available)
        {
            Id = id;
            Name = name;
            Available = available;
        }

        /// <summary>The backend's stable identifier, matching an <see cref="Ids"/> constant for a built-in backend.</summary>
        public ulong Id { get; }

        /// <summary>The backend's human-readable name.</summary>
        public string Name { get; }

        /// <summary>
        /// <see langword="true"/> when the backend became available, <see langword="false"/> when it
        /// went away. A backend in use that becomes unavailable should be discarded and replaced.
        /// </summary>
        public bool Available { get; }
    }
}
