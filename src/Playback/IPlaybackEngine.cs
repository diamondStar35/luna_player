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

    bool Load(string path, double? startPosition = null, bool paused = false);
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
}
