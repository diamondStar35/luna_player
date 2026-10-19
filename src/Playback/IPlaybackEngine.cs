using LunaPlayer.Equalizer;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Playback;

internal enum PlaybackEndReason
{
    EndOfFile,
    Error,
}

internal readonly record struct AudioDevice(string Name, string Description);

/// <param name="Id">mpv's own track id, the value written to <c>aid</c> to select it.</param>
/// <param name="Title">The track's own title from the file, or null when it carries none.</param>
/// <param name="Language">The raw language code (such as "eng"), or null when the track names none.
/// Turned into a readable name for the user at the point it is shown.</param>
/// <param name="Channels">How many channels the track carries, or 0 when mpv has not said.</param>
/// <param name="Selected">Whether this is the track currently playing.</param>
internal readonly record struct AudioTrack(int Id, string? Title, string? Language, int Channels, bool Selected);

/// <param name="Id">mpv's own track id, the value written to <c>sid</c> to select it.</param>
/// <param name="Title">The track's own title from the file, or null when it carries none.</param>
/// <param name="Language">The raw language code (such as "eng"), or null when the track names none.
/// Turned into a readable name for the user at the point it is shown.</param>
/// <param name="Selected">Whether this is the subtitle track currently being read.</param>
internal readonly record struct SubtitleTrack(int Id, string? Title, string? Language, bool Selected);

internal interface IPlaybackEngine : IDisposable
{
    event Action<PlaybackEndReason>? Ended;

    /// <summary>Raised when a one-shot selection reaches its boundary. This is deliberately separate from
    /// <see cref="Ended"/> so a selection boundary can never enter the file-advance path.</summary>
    event Action<double>? SelectionEnded;

    /// <summary>Raised after an asynchronous seek into or out of a selection has completed.</summary>
    event Action? SelectionSeekCompleted;

    /// <summary>Raised when <see cref="HasVideo"/> may have changed. mpv learns a file's video parameters
    /// after the load returns, so the answer is not known the moment a file opens.</summary>
    event Action? VideoAvailabilityChanged;

    /// <summary>Raised when the set of tracks may have changed - a file's tracks are learned after the load
    /// returns, so <see cref="GetAudioTracks"/> is not settled the moment a file opens.</summary>
    event Action? AudioTracksChanged;

    /// <summary>Raised when the file's subtitle tracks may have changed, a moment after a load - the companion
    /// of <see cref="AudioTracksChanged"/> for <see cref="GetSubtitleTracks"/>.</summary>
    event Action? SubtitleTracksChanged;

    /// <summary>Raised with each subtitle line as it appears on screen, so it can be spoken. Only fires while a
    /// subtitle track is selected; the payload is the line's text, never empty.</summary>
    event Action<string>? SubtitleTextChanged;

    /// <param name="audioFile">A separate stream carrying the sound, played alongside
    /// <paramref name="path"/>. Null for anything that carries its own sound, which is everything but a
    /// YouTube video above 360p.</param>
    bool Load(string path, double? startPosition = null, bool paused = false, string? audioFile = null, bool noVideo = false);
    void Stop();
    bool TogglePause();
    void Play();
    void Pause();
    bool IsPaused { get; }
    bool SelectionRangeCompleted { get; }
    double? Duration { get; }
    double? Elapsed { get; }
    double? Remaining { get; }
    void SeekRelative(double seconds);
    void SeekAbsolute(double seconds);
    bool SetLoopStart(double seconds);
    bool SetLoopEnd(double seconds);
    bool ClearLoop();
    /// <summary>Constrains playback to one range. Repeating uses mpv's A-B loop; a one-shot range uses
    /// mpv's own end boundary so it stops at the requested media timestamp rather than on a UI timer.</summary>
    bool SetSelectionRange(double start, double end, bool repeat);
    /// <summary>Constrains playback to one range, seeks precisely to its start, then plays it.</summary>
    bool PlaySelectionRange(double start, double end, bool repeat);
    /// <summary>Removes a completed selection boundary, seeks to its end, and optionally resumes playback
    /// after mpv has left its end-of-file state.</summary>
    bool ExitSelectionRange(double end, bool play);
    /// <summary>Removes a selection range. When restoration is deferred, the selection's safe keep-open
    /// setting remains until playback restarts, preventing its artificial end from advancing the file.</summary>
    bool ClearSelectionRange(bool restoreEndBehavior = true);
    double SetVolume(double volume);
    double Volume { get; }
    double SetSpeed(double speed);
    double Speed { get; }
    double SetPitch(double semitones);
    double Pitch { get; }
    double SetPan(double pan);
    double Pan { get; }
    IReadOnlyList<AudioDevice> GetAudioDevices();
    string CurrentAudioDevice { get; }
    bool SetAudioDevice(string name);

    /// <summary>The audio tracks the current file carries, empty when nothing is open. Not settled the
    /// moment a file opens - see <see cref="AudioTracksChanged"/>.</summary>
    IReadOnlyList<AudioTrack> GetAudioTracks();

    /// <summary>Switches the playing audio track to the one with this mpv track id.</summary>
    bool SetAudioTrack(int id);

    /// <summary>The subtitle tracks the current file carries, empty when nothing is open. Not settled the
    /// moment a file opens - see <see cref="SubtitleTracksChanged"/>.</summary>
    IReadOnlyList<SubtitleTrack> GetSubtitleTracks();

    /// <summary>Selects the subtitle track with this mpv track id, so its lines are announced as they come.</summary>
    bool SetSubtitleTrack(int id);

    /// <summary>Turns subtitle reading off, selecting no subtitle track.</summary>
    bool DisableSubtitles();

    /// <summary>Loads a subtitle from a local path or an http(s) URL (mpv reads both through ffmpeg) and
    /// selects it, so it joins the file's <see cref="GetSubtitleTracks"/> exactly as an embedded one does.
    /// <paramref name="title"/> and <paramref name="language"/> label the track when given.</summary>
    bool AddSubtitle(string source, string? title, string? language);
    bool SetNormalization(bool enabled);
    bool SetMono(bool enabled);
    bool SetSilenceRemoval(bool enabled, string graph);

    /// <summary>Puts a preset's curve into the equalizer, or flattens it when <paramref name="preset"/>
    /// is null.</summary>
    /// <remarks>
    /// Off flattens rather than removing the filter. The filter is built once and stays in the chain for
    /// the life of the engine, because taking a filter out of a running graph is audible and switching
    /// the equalizer off should not be.
    /// </remarks>
    bool SetEqualizer(Preset? preset);

    void SetEndBehavior(EndBehavior behavior);

    /// <summary>Turns mpv's own system-media integration on or off: the media keys it answers and the
    /// transport controls it publishes to the OS. The equivalent of mpv's <c>--media-controls</c>.</summary>
    void SetMediaControls(bool enabled);

    /// <summary>The title the media declares for itself, from tags or a stream's metadata. Null when the
    /// media carries none, in which case callers fall back to the file name.</summary>
    string? MediaTitle { get; }

    /// <summary>Whether a real picture is playing: a decoded video track that is neither a still image nor
    /// cover art. False for audio and for audio-only tracks in a video container. Not known synchronously
    /// after a load - see <see cref="VideoAvailabilityChanged"/>.</summary>
    bool HasVideo { get; }
}
