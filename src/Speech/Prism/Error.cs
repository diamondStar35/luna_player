namespace PrismSharp
{
    /// <summary>
    /// Why a Prism operation failed.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>PrismError</c> from prism.h (v0.18.2). The numeric values are part of the ABI and
    /// will not change, though new codes may be added. Surfaced through
    /// <see cref="PrismException.Error"/>.
    /// </remarks>
    public enum Error
    {
        /// <summary>The operation completed without error.</summary>
        Ok = 0,

        /// <summary>
        /// The backend was not initialized - <see cref="Backend.Initialize"/> was never called, or it
        /// failed.
        /// </summary>
        NotInitialized = 1,

        /// <summary>An invalid parameter was passed.</summary>
        InvalidParam = 2,

        /// <summary>
        /// The selected backend does not support this operation. Check <see cref="Backend.Features"/>
        /// first to avoid it.
        /// </summary>
        NotImplemented = 3,

        /// <summary>The backend has no voices available.</summary>
        NoVoices = 4,

        /// <summary>The requested voice does not exist.</summary>
        VoiceNotFound = 5,

        /// <summary>Speech synthesis failed.</summary>
        SpeakFailure = 6,

        /// <summary>Memory allocation failed.</summary>
        MemoryFailure = 7,

        /// <summary>
        /// A value fell outside its valid range - a volume, rate or pitch that was negative, above
        /// <c>1.0</c>, NaN or infinite.
        /// </summary>
        RangeOutOfBounds = 8,

        /// <summary>An internal backend error occurred.</summary>
        Internal = 9,

        /// <summary>Something was stopped or paused while nothing was being said.</summary>
        NotSpeaking = 10,

        /// <summary>Speech was resumed while nothing was paused.</summary>
        NotPaused = 11,

        /// <summary>Speech was paused while already paused.</summary>
        AlreadyPaused = 12,

        /// <summary>A string parameter contained invalid UTF-8.</summary>
        InvalidUtf8 = 13,

        /// <summary>The operation is not valid in the backend's current state.</summary>
        InvalidOperation = 14,

        /// <summary>
        /// An already-initialized backend was initialized again. Expected from a cached backend, and
        /// not treated as a failure by <see cref="Backend.Initialize"/>.
        /// </summary>
        AlreadyInitialized = 15,

        /// <summary>The backend is not available on this system, or its service is not running.</summary>
        BackendNotAvailable = 16,

        /// <summary>An unspecified error occurred.</summary>
        Unknown = 17,

        /// <summary>
        /// The engine or voice uses an audio format Prism cannot convert, or reported nonsensical
        /// format parameters.
        /// </summary>
        InvalidAudioFormat = 18,

        /// <summary>
        /// The backend caps how many instances may exist at once, and another would exceed it.
        /// </summary>
        InternalBackendLimitExceeded = 19,

        /// <summary>
        /// A failure left the backend in an undefined state. Discard it and create a fresh one.
        /// </summary>
        BackendEnteredUndefinedState = 20,

        /// <summary>
        /// A plugin shared library could not be opened: no file at that path, not a loadable image,
        /// built for another architecture, or its initialization code failed.
        /// </summary>
        LibraryLoadFailed = 21,

        /// <summary>
        /// A plugin shared library opened but does not export the <c>prism_plugin_query</c> entry
        /// point.
        /// </summary>
        LibraryInvalid = 22,

        /// <summary>
        /// A plugin declined this host, or declared an ABI generation this build of Prism does not
        /// accept.
        /// </summary>
        IncompatibleAbi = 23,

        /// <summary>
        /// The number of error codes, for bounds checking. May grow in future Prism releases.
        /// </summary>
        Count = 24
    }
}
