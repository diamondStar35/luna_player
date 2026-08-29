using System;
using System.Collections.Generic;
using PrismSharp.Speech.Playback;

namespace PrismSharp.Speech.ScreenReaders
{
    /// <summary>
    /// A speech output device with the backend selection policy applied: it picks a backend, keeps
    /// it, and exposes speak, braille, voice and rate control over whichever one it chose.
    /// </summary>
    /// <remarks>
    /// This sits one level above <see cref="Backend"/>. Where <see cref="Context"/> hands out
    /// backends and leaves the choice to the caller, this decides - preferring a running screen
    /// reader, honouring an explicit preference, and falling back through the priority order when
    /// something is unavailable. Every operation reports failure by returning
    /// <see langword="false"/> rather than throwing, since speech failing is rarely worth breaking a
    /// caller over.
    /// <para>
    /// Instances are <b>not</b> safe to use from several threads: Prism backend handles are not
    /// thread-safe. Wrap one in a <see cref="ScreenReaderWorker"/>, which confines every call to a
    /// single thread.
    /// </para>
    /// </remarks>
    public interface IScreenReader
    {
        /// <summary>
        /// Raised when a backend becomes usable or stops being usable - a screen reader starting or
        /// quitting, for instance.
        /// </summary>
        /// <remarks>
        /// Raised on Prism's availability poll thread, which cannot scan again until the handler
        /// returns. A handler must not block and must not call back into this reader directly; hand
        /// the work to <see cref="ScreenReaderWorker.Post"/> instead.
        /// </remarks>
        event Action<SpeechBackendAvailability>? BackendAvailabilityChanged;

        /// <summary>
        /// Every registered backend that is usable right now, probed without initializing any of them.
        /// </summary>
        /// <remarks>Empty until <see cref="Initialize"/> has succeeded.</remarks>
        IReadOnlyList<BackendInfo> AvailableBackends { get; }

        /// <summary>The voices offered by the active backend, or empty when there is none.</summary>
        IReadOnlyList<VoiceInfo> AvailableVoices { get; }

        /// <summary>
        /// The backend to use in preference to the automatic choice, or <see langword="null"/> to let
        /// the policy decide.
        /// </summary>
        /// <remarks>Takes effect at the next <see cref="Initialize"/>.</remarks>
        ulong? PreferredBackendId { get; set; }

        /// <summary>
        /// The backend currently in use, or <see langword="null"/> when none is open or it could not
        /// be identified.
        /// </summary>
        ulong? ActiveBackendId { get; }

        /// <summary>
        /// The voice to select on the active backend, or <see langword="null"/> for its default.
        /// </summary>
        /// <remarks>Applied immediately when a backend is open, and again on each re-initialization.</remarks>
        int? PreferredVoiceIndex { get; set; }

        /// <summary>
        /// What the active backend supports, or <see cref="Features.None"/> when none is open.
        /// </summary>
        Features Capabilities { get; }

        /// <summary>
        /// The active backend's name, or <see langword="null"/> when none is open.
        /// </summary>
        string? ActiveBackendName { get; }

        /// <summary>
        /// Opens a Prism context and selects a backend, closing anything already open.
        /// </summary>
        /// <returns><see langword="true"/> when a usable backend was found.</returns>
        /// <remarks>
        /// Selection order: <see cref="PreferredBackendId"/> if set and usable; then SAPI if
        /// <see cref="PreferSAPI"/> asked for it; then registered backends in descending priority,
        /// skipping SAPI unless <see cref="TrySAPI"/> allowed it; then SAPI as a last resort; and
        /// finally whatever Prism itself considers best.
        /// </remarks>
        bool Initialize();

        /// <summary>Whether a context and backend are currently open.</summary>
        /// <returns><see langword="true"/> when ready to speak.</returns>
        bool IsLoaded();

        /// <summary>Speaks text, preferring speech output over combined output.</summary>
        /// <param name="text">The text to speak. Blank text is ignored.</param>
        /// <param name="interrupt">Whether to stop speech already in progress.</param>
        /// <returns><see langword="true"/> when the text was handed to a backend.</returns>
        /// <remarks>
        /// Routes through the bound <see cref="IPlayer"/> when the active backend can synthesize to
        /// memory and a player is bound, and falls back to combined output if plain speech is
        /// unsupported.
        /// </remarks>
        bool Speak(string text, bool interrupt = true);

        /// <summary>Whether speech is currently being produced, including through a bound player.</summary>
        /// <returns><see langword="true"/> while speaking.</returns>
        bool IsSpeaking();

        /// <summary>Stops speech and releases the backend and context.</summary>
        void Close();

        /// <summary>The active backend's volume, or <c>0</c> when unavailable.</summary>
        /// <returns>Normalized volume in <c>[0.0, 1.0]</c>.</returns>
        float GetVolume();

        /// <summary>Sets the active backend's volume, ignoring the request when unsupported.</summary>
        /// <param name="volume">Normalized volume in <c>[0.0, 1.0]</c>.</param>
        void SetVolume(float volume);

        /// <summary>The active backend's speech rate, or <c>0</c> when unavailable.</summary>
        /// <returns>Normalized rate in <c>[0.0, 1.0]</c>, where <c>0.5</c> is the default.</returns>
        float GetRate();

        /// <summary>Sets the active backend's speech rate, ignoring the request when unsupported.</summary>
        /// <param name="rate">Normalized rate in <c>[0.0, 1.0]</c>.</param>
        void SetRate(float rate);

        /// <summary>Whether the active backend can produce speech at all.</summary>
        /// <returns><see langword="true"/> when speech or combined output is supported.</returns>
        bool HasSpeech();

        /// <summary>
        /// Whether SAPI may be used as a fallback when no higher-priority backend works.
        /// </summary>
        /// <param name="trySapi">
        /// <see langword="true"/> to allow SAPI. When <see langword="false"/>, SAPI is skipped
        /// entirely - appropriate when the application produces its own speech and only wants a
        /// screen reader if one is running.
        /// </param>
        /// <remarks>Takes effect at the next <see cref="Initialize"/>.</remarks>
        void TrySAPI(bool trySapi);

        /// <summary>Whether SAPI should be chosen ahead of everything else.</summary>
        /// <param name="preferSapi"><see langword="true"/> to try SAPI first.</param>
        /// <remarks>Takes effect at the next <see cref="Initialize"/>.</remarks>
        void PreferSAPI(bool preferSapi);

        /// <summary>The name of the screen reader or engine in use.</summary>
        /// <returns>The backend's name, or <see langword="null"/> when none is open.</returns>
        string? DetectScreenReader();

        /// <summary>Sends text to speech and braille together, as the user has configured.</summary>
        /// <param name="text">The text to output. Blank text is ignored.</param>
        /// <param name="interrupt">Whether to stop output already in progress.</param>
        /// <returns><see langword="true"/> when the text was handed to a backend.</returns>
        /// <remarks>Falls back to <see cref="Speak"/> when combined output is unsupported.</remarks>
        bool Output(string text, bool interrupt = true);

        /// <summary>Whether the active backend can drive a braille display.</summary>
        /// <returns><see langword="true"/> when braille output is supported.</returns>
        bool HasBraille();

        /// <summary>Sends text to a braille display only.</summary>
        /// <param name="text">The text to braille. Blank text is ignored.</param>
        /// <returns><see langword="true"/> when the text was handed to a backend.</returns>
        bool Braille(string text);

        /// <summary>Stops speech in progress, including anything queued in a bound player.</summary>
        /// <returns><see langword="true"/> when something was stopped.</returns>
        bool Silence();

        /// <summary>
        /// Binds the audio sink used when a backend synthesizes to memory rather than speaking itself.
        /// </summary>
        /// <param name="player">The sink, or <see langword="null"/> to disable that path.</param>
        void BindPlayer(IPlayer? player);
    }
}
