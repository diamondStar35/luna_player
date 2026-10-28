using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
using LunaPlayer.Media;
namespace LunaPlayer.Playback;

internal sealed class Engine : IEngine
{
    private readonly PlaybackCoordinator _coordinator;
    internal Engine(nint windowHandle, IReadOnlyDictionary<string, object?>? options = null)
        => _coordinator = new PlaybackCoordinator(windowHandle, options);
    public void Dispose() => _coordinator.Dispose();
    public event Action<EndReason>? Ended { add => _coordinator.Ended += value; remove => _coordinator.Ended -= value; }
    public event Action<double>? SelectionEnded { add => _coordinator.SelectionEnded += value; remove => _coordinator.SelectionEnded -= value; }
    public event Action? SelectionSeekCompleted { add => _coordinator.SelectionSeekCompleted += value; remove => _coordinator.SelectionSeekCompleted -= value; }
    public event Action? VideoAvailabilityChanged { add => _coordinator.VideoAvailabilityChanged += value; remove => _coordinator.VideoAvailabilityChanged -= value; }
    public event Action? MetadataChanged { add => _coordinator.MetadataChanged += value; remove => _coordinator.MetadataChanged -= value; }
    public event Action? ChaptersChanged { add => _coordinator.ChaptersChanged += value; remove => _coordinator.ChaptersChanged -= value; }
    public IReadOnlyList<Chapter> GetChapters() => _coordinator.GetChapters();
    public void SetChapterStop(double? end) => _coordinator.SetChapterStop(end);
    public event Action? AudioTracksChanged { add => _coordinator.AudioTracksChanged += value; remove => _coordinator.AudioTracksChanged -= value; }
    public event Action? SubtitleTracksChanged { add => _coordinator.SubtitleTracksChanged += value; remove => _coordinator.SubtitleTracksChanged -= value; }
    public event Action<string>? SubtitleTextChanged { add => _coordinator.SubtitleTextChanged += value; remove => _coordinator.SubtitleTextChanged -= value; }
    public bool Load(string path, double? startPosition = null, bool paused = false, string? audioFile = null, bool noVideo = false, bool fadeTransition = false, PlaybackRange? range = null, string? itemId = null, double? chapterStop = null) => _coordinator.Load(path, startPosition, paused, audioFile, noVideo, fadeTransition, range, itemId, chapterStop);
    public bool QueueNext(string path, double? startPosition = null, string? audioFile = null, bool noVideo = false, PlaybackRange? range = null, string? itemId = null) => _coordinator.QueueNext(path, startPosition, audioFile, noVideo, range, itemId);
    public void ClearQueuedNext() => _coordinator.ClearQueuedNext();
    public QueuedAdvance? TakeQueuedAdvance() => _coordinator.TakeQueuedAdvance();
    public double? PositionFor(string path) => _coordinator.PositionFor(path);
    public void HoldAutomaticAdvance(bool hold) => _coordinator.HoldAutomaticAdvance(hold);
    public void ConfigurePlaybackEffects(AudioSettings settings) => _coordinator.ConfigurePlaybackEffects(settings);
    public void SetRepeatFile(bool enabled) => _coordinator.SetRepeatFile(enabled);
    public void Stop() => _coordinator.Stop();
    public bool TogglePause() => _coordinator.TogglePause();
    public void Play() => _coordinator.Play();
    public void Pause() => _coordinator.Pause();
    public bool IsPaused => _coordinator.IsPaused;
    public bool IsLoading => _coordinator.IsLoading;
    public bool EndReached => _coordinator.EndReached;
    public bool SelectionRangeCompleted => _coordinator.SelectionRangeCompleted;
    public double? Duration => _coordinator.Duration;
    public double? Elapsed => _coordinator.Elapsed;
    public double? Remaining => _coordinator.Remaining;
    public void SeekRelative(double seconds) => _coordinator.SeekRelative(seconds);
    public void SeekAbsolute(double seconds) => _coordinator.SeekAbsolute(seconds);
    public bool SetLoopStart(double seconds) => _coordinator.SetLoopStart(seconds);
    public bool SetLoopEnd(double seconds) => _coordinator.SetLoopEnd(seconds);
    public bool ClearLoop() => _coordinator.ClearLoop();
    public bool SetSelectionRange(double start, double end, bool repeat) => _coordinator.SetSelectionRange(start, end, repeat);
    public bool PlaySelectionRange(double start, double end, bool repeat) => _coordinator.PlaySelectionRange(start, end, repeat);
    public bool ExitSelectionRange(double end, bool play) => _coordinator.ExitSelectionRange(end, play);
    public bool ClearSelectionRange(bool restoreEndBehavior = true) => _coordinator.ClearSelectionRange(restoreEndBehavior);
    public double SetVolume(double volume) => _coordinator.SetVolume(volume);
    public double Volume => _coordinator.Volume;
    public double SetSpeed(double speed) => _coordinator.SetSpeed(speed);
    public double Speed => _coordinator.Speed;
    public double SetPitch(double semitones) => _coordinator.SetPitch(semitones);
    public double Pitch => _coordinator.Pitch;
    public double SetPan(double pan) => _coordinator.SetPan(pan);
    public double Pan => _coordinator.Pan;
    public IReadOnlyList<AudioDevice> GetAudioDevices() => _coordinator.GetAudioDevices();
    public string CurrentAudioDevice => _coordinator.CurrentAudioDevice;
    public bool SetAudioDevice(string name) => _coordinator.SetAudioDevice(name);
    public IReadOnlyList<AudioTrack> GetAudioTracks() => _coordinator.GetAudioTracks();
    public bool SetAudioTrack(int id) => _coordinator.SetAudioTrack(id);
    public IReadOnlyList<SubtitleTrack> GetSubtitleTracks() => _coordinator.GetSubtitleTracks();
    public bool SetSubtitleTrack(int id) => _coordinator.SetSubtitleTrack(id);
    public bool DisableSubtitles() => _coordinator.DisableSubtitles();
    public bool AddSubtitle(string source, string? title, string? language) => _coordinator.AddSubtitle(source, title, language);
    public bool SetNormalization(bool enabled) => _coordinator.SetNormalization(enabled);
    public bool SetMono(bool enabled) => _coordinator.SetMono(enabled);
    public bool SetSilenceRemoval(bool enabled, string graph) => _coordinator.SetSilenceRemoval(enabled, graph);
    public bool SetEqualizer(Preset? preset) => _coordinator.SetEqualizer(preset);
    public void SetEndBehavior(EndBehavior behavior) => _coordinator.SetEndBehavior(behavior);
    public string? MediaTitle => _coordinator.MediaTitle;
    public Metadata GetMetadata() => _coordinator.GetMetadata();
    public bool HasVideo => _coordinator.HasVideo;
    internal string RegisterSpotifyAudio(LunaPlayer.Spotify.Playback.AudioDecryptStream audio) => _coordinator.RegisterSpotifyAudio(audio);
    internal void ReleaseSpotifyAudio(string path) => _coordinator.ReleaseSpotifyAudio(path);
}
