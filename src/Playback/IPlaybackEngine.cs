using LunaPlayer.Configuration;

namespace LunaPlayer.Playback;

internal enum PlaybackEndReason
{
    EndOfFile,
    Error,
}

internal readonly record struct AudioDevice(string Name, string Description);

internal interface IPlaybackEngine : IDisposable
{
    event Action<PlaybackEndReason>? Ended;

    /// <param name="audioFile">A separate stream carrying the sound, played alongside
    /// <paramref name="path"/>. Null for anything that carries its own sound, which is everything but a
    /// YouTube video above 360p.</param>
    bool Load(string path, double? startPosition = null, bool paused = false, string? audioFile = null);
    void Stop();
    bool TogglePause();
    void Play();
    void Pause();
    bool IsPaused { get; }
    double? Duration { get; }
    double? Elapsed { get; }
    double? Remaining { get; }
    void SeekRelative(double seconds);
    void SeekAbsolute(double seconds);
    bool SetLoopStart(double seconds);
    bool SetLoopEnd(double seconds);
    bool ClearLoop();
    double SetVolume(double volume);
    double Volume { get; }
    double SetSpeed(double speed);
    double Speed { get; }
    IReadOnlyList<AudioDevice> GetAudioDevices();
    string CurrentAudioDevice { get; }
    bool SetAudioDevice(string name);
    bool SetNormalization(bool enabled);
    bool SetMono(bool enabled);
    bool SetSilenceRemoval(bool enabled, string graph);
    void SetEndBehavior(EndBehavior behavior);

    /// <summary>The title the media declares for itself, from tags or a stream's metadata. Null when the
    /// media carries none, in which case callers fall back to the file name.</summary>
    string? MediaTitle { get; }
}
