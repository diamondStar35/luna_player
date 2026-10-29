using LunaPlayer.Playback;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Equalizer;
using LunaPlayer.Media;
namespace LunaPlayer.Tests.Infrastructure;
internal sealed class FakeEngine : IEngine
{
    private readonly Dictionary<string, Delegate?> _events = [];
    internal List<string> Loads { get; } = [];
    internal int Stops { get; private set; }
    internal bool Paused { get; private set; }
    internal double Position { get; private set; }
    internal double Loudness { get; private set; } = 100;
    internal double Rate { get; private set; } = 1;
    public void Dispose() { }
    internal void End(EndReason reason) => ((Action<EndReason>?)_events.GetValueOrDefault("Ended"))?.Invoke(reason);
    public event Action<EndReason>? Ended { add => _events["Ended"] = Delegate.Combine(_events.GetValueOrDefault("Ended"), value); remove => _events["Ended"] = Delegate.Remove(_events.GetValueOrDefault("Ended"), value); }
    public event Action<double>? SelectionEnded { add => _events["SelectionEnded"] = Delegate.Combine(_events.GetValueOrDefault("SelectionEnded"), value); remove => _events["SelectionEnded"] = Delegate.Remove(_events.GetValueOrDefault("SelectionEnded"), value); }
    public event Action? SelectionSeekCompleted { add => _events["SelectionSeekCompleted"] = Delegate.Combine(_events.GetValueOrDefault("SelectionSeekCompleted"), value); remove => _events["SelectionSeekCompleted"] = Delegate.Remove(_events.GetValueOrDefault("SelectionSeekCompleted"), value); }
    public event Action? VideoAvailabilityChanged { add => _events["VideoAvailabilityChanged"] = Delegate.Combine(_events.GetValueOrDefault("VideoAvailabilityChanged"), value); remove => _events["VideoAvailabilityChanged"] = Delegate.Remove(_events.GetValueOrDefault("VideoAvailabilityChanged"), value); }
    public event Action? MetadataChanged { add => _events["MetadataChanged"] = Delegate.Combine(_events.GetValueOrDefault("MetadataChanged"), value); remove => _events["MetadataChanged"] = Delegate.Remove(_events.GetValueOrDefault("MetadataChanged"), value); }
    public event Action? ChaptersChanged { add => _events["ChaptersChanged"] = Delegate.Combine(_events.GetValueOrDefault("ChaptersChanged"), value); remove => _events["ChaptersChanged"] = Delegate.Remove(_events.GetValueOrDefault("ChaptersChanged"), value); }
    public IReadOnlyList<Chapter> GetChapters() => [];
    public void SetChapterStop(double? end) {}
    public event Action? AudioTracksChanged { add => _events["AudioTracksChanged"] = Delegate.Combine(_events.GetValueOrDefault("AudioTracksChanged"), value); remove => _events["AudioTracksChanged"] = Delegate.Remove(_events.GetValueOrDefault("AudioTracksChanged"), value); }
    public event Action? SubtitleTracksChanged { add => _events["SubtitleTracksChanged"] = Delegate.Combine(_events.GetValueOrDefault("SubtitleTracksChanged"), value); remove => _events["SubtitleTracksChanged"] = Delegate.Remove(_events.GetValueOrDefault("SubtitleTracksChanged"), value); }
    public event Action<string>? SubtitleTextChanged { add => _events["SubtitleTextChanged"] = Delegate.Combine(_events.GetValueOrDefault("SubtitleTextChanged"), value); remove => _events["SubtitleTextChanged"] = Delegate.Remove(_events.GetValueOrDefault("SubtitleTextChanged"), value); }
    public bool Load(string path, double? startPosition = null, bool paused = false, string? audioFile = null, bool noVideo = false, bool fadeTransition = false, PlaybackRange? range = null, string? itemId = null, double? chapterStop = null) { Loads.Add(path); Position = startPosition ?? 0; Paused = paused; return true; }
    public bool QueueNext(string path, double? startPosition = null, string? audioFile = null, bool noVideo = false, PlaybackRange? range = null, string? itemId = null) => true;
    public void ClearQueuedNext() {}
    public QueuedAdvance? TakeQueuedAdvance() => default;
    public double? PositionFor(string path) { return Position; }
    public void HoldAutomaticAdvance(bool hold) {}
    public void ConfigurePlaybackEffects(AudioSettings settings) {}
    public void SetRepeatFile(bool enabled) {}
    public void Stop() { Stops++; }
    public bool TogglePause() { Paused = !Paused; return !Paused; }
    public void Play() { Paused = false; }
    public void Pause() { Paused = true; }
    public bool IsPaused => Paused;
    public bool IsLoading => default;
    public bool EndReached => default;
    public bool SelectionRangeCompleted => default;
    public double? Duration => 10d;
    public double? Elapsed => Position;
    public double? Remaining => 10d - Position;
    public void SeekRelative(double seconds) { Position += seconds; }
    public void SeekAbsolute(double seconds) { Position = seconds; }
    public bool SetLoopStart(double seconds) => true;
    public bool SetLoopEnd(double seconds) => true;
    public bool ClearLoop() => true;
    public bool SetSelectionRange(double start, double end, bool repeat) => true;
    public bool PlaySelectionRange(double start, double end, bool repeat) => true;
    public bool ExitSelectionRange(double end, bool play) => true;
    public bool ClearSelectionRange(bool restoreEndBehavior = true) => true;
    public double SetVolume(double volume) { return Loudness = Math.Clamp(volume, 0, 2000); }
    public double Volume => Loudness;
    public double SetSpeed(double speed) { return Rate = Math.Clamp(speed, 0.5, 4); }
    public double Speed => Rate;
    public double SetPitch(double semitones) => default;
    public double Pitch => default;
    public double SetPan(double pan) => default;
    public double Pan => default;
    public IReadOnlyList<AudioDevice> GetAudioDevices() => [];
    public string CurrentAudioDevice => "auto";
    public bool SetAudioDevice(string name) => true;
    public IReadOnlyList<AudioTrack> GetAudioTracks() => [];
    public bool SetAudioTrack(int id) => true;
    public IReadOnlyList<SubtitleTrack> GetSubtitleTracks() => [];
    public bool SetSubtitleTrack(int id) => true;
    public bool DisableSubtitles() => true;
    public bool AddSubtitle(string source, string? title, string? language) => true;
    public bool SetNormalization(bool enabled) => true;
    public bool SetMono(bool enabled) => true;
    public bool SetSilenceRemoval(bool enabled, string graph) => true;
    public bool SetEqualizer(Preset? preset) => true;
    public void SetEndBehavior(EndBehavior behavior) {}
    public string? MediaTitle => default;
    internal Metadata CurrentMetadata { get; set; } = Metadata.None;
    public Metadata GetMetadata() => CurrentMetadata;
    public bool HasVideo => default;
}
