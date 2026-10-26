using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
using LunaPlayer.Media;

namespace LunaPlayer.Playback;

internal interface IEngine : IDisposable
{
    event Action<EndReason>? Ended;

    /// <summary>Raised when a one-shot selection reaches its boundary. This is deliberately separate from
    /// <see cref="Ended"/> so a selection boundary can never enter the file-advance path.</summary>
    event Action<double>? SelectionEnded;

    /// <summary>Raised after an asynchronous seek into or out of a selection has completed.</summary>
    event Action? SelectionSeekCompleted;

    /// <summary>Raised when <see cref="HasVideo"/> may have changed. mpv learns a file's video parameters
    /// after the load returns, so the answer is not known the moment a file opens.</summary>
    event Action? VideoAvailabilityChanged;

    /// <summary>Raised when the loaded media's metadata changes, including a new live radio programme.</summary>
    event Action? MetadataChanged;

    event Action? ChaptersChanged;
    IReadOnlyList<Chapter> GetChapters();
    void SetChapterStop(double? end);

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
    bool Load(string path, double? startPosition = null, bool paused = false, string? audioFile = null,
        bool noVideo = false, bool fadeTransition = false, PlaybackRange? range = null, string? itemId = null, double? chapterStop = null);
    /// <summary>Appends the next item to mpv's internal playlist so its audio can start without waiting for
    /// the application to react to end-of-file. Returns false when the item cannot be queued safely.</summary>
    bool QueueNext(string path, double? startPosition = null, string? audioFile = null, bool noVideo = false, PlaybackRange? range = null, string? itemId = null);
    void ClearQueuedNext();
    QueuedAdvance? TakeQueuedAdvance();
    double? PositionFor(string path);
    void HoldAutomaticAdvance(bool hold);
    void ConfigurePlaybackEffects(AudioSettings settings);
    void SetRepeatFile(bool enabled);
    void Stop();
    bool TogglePause();
    void Play();
    void Pause();
    bool IsPaused { get; }
    bool IsLoading { get; }
    bool EndReached { get; }
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

    /// <summary>Switches the playing audio track to the one with this mpv track ID.</summary>
    bool SetAudioTrack(int id);

    /// <summary>The subtitle tracks the current file carries, empty when nothing is open. Not settled the
    /// moment a file opens - see <see cref="SubtitleTracksChanged"/>.</summary>
    IReadOnlyList<SubtitleTrack> GetSubtitleTracks();

    /// <summary>Selects the subtitle track with this mpv track ID, so its lines are announced as they come.</summary>
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

    /// <summary>Puts a preset's curve into the equalizer, or removes the equalizer when
    /// <paramref name="preset"/> is null.</summary>
    bool SetEqualizer(Preset? preset);

    void SetEndBehavior(EndBehavior behavior);

    /// <summary>The title selected by mpv from file or stream metadata, falling back to its file name.</summary>
    string? MediaTitle { get; }

    /// <summary>The current title and metadata from mpv. Live radio can update these during playback.</summary>
    Metadata GetMetadata();

    /// <summary>Whether a real picture is playing: a decoded video track that is neither a still image nor
    /// cover art. False for audio and for audio-only tracks in a video container. Not known synchronously
    /// after a load - see <see cref="VideoAvailabilityChanged"/>.</summary>
    bool HasVideo { get; }
}
