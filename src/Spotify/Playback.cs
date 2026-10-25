using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Application;

namespace LunaPlayer.Spotify;

/// <summary>Coordinates Spotify navigation and plays decrypted audio through Luna's mpv engine.</summary>
internal sealed partial class Playback : IDisposable
{
    private readonly SpotifySettings _settings;
    private readonly Player _player;
    private readonly IApplicationDispatcher _dispatcher;
    private ManagedAudioSession? _session;
    private CancellationTokenSource? _loadCancellation;
    private readonly SemaphoreSlim _prepareGate = new(1, 1);
    private readonly object _sessionLock = new();
    private int _sessionVersion;
    private volatile bool _disposed;
    private readonly object _positionLock = new();
    private volatile bool _active;
    private bool _ownsPlayerSession;
    private PlaybackClock? _clock;
    private volatile Track? _current;
    private volatile string? _loadedUri;
    private double? _pendingSeek;
    private int _loadGeneration;
    private volatile bool _playRequested;
    private bool _repeatCurrent;
    private bool _ended;
    private bool _currentLoadSent;
    private AudioSettings? _audioSettings;
    private Func<string, double?>? _readPosition;
    private Action<string, double>? _writePosition;

    internal event Action<Exception>? CommandFailed;
    internal event Action<Track>? TrackChanged;
    internal event Action<Track>? Navigated;
    internal event Action<bool>? PlayingChanged;
    internal Func<bool>? BeforeTrackEnd { get; set; }

    internal Playback(SpotifySettings settings, Player player,
        IApplicationDispatcher dispatcher)
    {
        _settings = settings;
        _player = player;
        _dispatcher = dispatcher;
        player.Ended += HandlePlayerEnded;
    }

    /// <summary>True once a Spotify track is the active source, until it is stopped or replaced. Luna gates all
    /// Spotify-specific transport/window behaviour on this.</summary>
    internal bool IsActive => _active;
    internal bool HasSession => _ownsPlayerSession;

    internal void BeginSession()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_ownsPlayerSession) return;
        _player.EnterSession();
        _ownsPlayerSession = true;
        _player.Stop();
    }

    internal bool CloseAllFiles()
    {
        if (!_ownsPlayerSession && !_active) return false;
        var ownedSession = _ownsPlayerSession;
        Stop(restoreSession: false);
        if (ownedSession) _player.DiscardSpotifySession();
        return true;
    }
    internal string? CurrentTitle => _current is { } track ? track.Name + " - " + track.Artists : null;
    internal Track? CurrentTrack => _current;
    internal int CurrentLoadGeneration => Volatile.Read(ref _loadGeneration);
    internal string? CurrentLink => _current is { } track && IsTrack(track.Uri)
        ? "https://open.spotify.com/track/" + track.Uri[14..] : null;
    internal bool IsRepeatCurrentEnabled => _repeatCurrent;
    internal bool ToggleRepeatCurrent() => _repeatCurrent = !_repeatCurrent;
    internal double? Duration => IsLoadedCurrent
        ? _player.Duration ?? _clock?.Duration : _clock?.Duration;
    internal double? Elapsed
    {
        get
        {
            if (IsLoadedCurrent
                && _player.Elapsed is { } actual) return actual;
            if (_clock is not { } clock) return null;
            var elapsed = clock.Elapsed + (clock.Playing ? (DateTimeOffset.UtcNow - clock.ObservedAt).TotalSeconds : 0);
            return Math.Clamp(elapsed, 0, clock.Duration);
        }
    }
    internal double? Remaining => Duration is double d && Elapsed is double e ? Math.Max(0, d - e) : null;
    internal bool IsPlaying => !_ended && (IsLoadedCurrent
        ? _player.IsPlaying : _clock?.Playing ?? false);
    private bool IsLoadedCurrent => _loadedUri == _current?.Uri && _loadedUri is not null
        && _player.CurrentPath?.StartsWith("luna-spotify://", StringComparison.Ordinal) == true;
    private int RequestedBitrate => _settings.AudioQuality?.ToUpperInvariant() switch
    {
        "NORMAL" => 96, "HIGH" => 160, _ => 320,
    };

    internal void ConfigurePlayback(AudioSettings audioSettings,
        Func<string, double?> readPosition, Action<string, double> writePosition)
    {
        _audioSettings = audioSettings;
        _readPosition = readPosition;
        _writePosition = writePosition;
    }

    internal async Task PrepareAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _prepareGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        ManagedAudioSession? prepared = null;
        try
        {
            int version;
            lock (_sessionLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_session is not null) return;
                version = _sessionVersion;
            }
            var credentials = PlaybackCredentialStore.Load() ?? PlaybackCredentialStore.LoadLegacy();
            if (credentials is not null)
            {
                try
                {
                    prepared = await ManagedAudioSession.ConnectAsync(credentials.Value,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (PlaybackAuthenticationException)
                {
                    PlaybackCredentialStore.Delete();
                }
            }
            if (prepared is null)
            {
                var signedIn = await PlaybackOAuth.SignInAsync(cancellationToken).ConfigureAwait(false);
                prepared = await ManagedAudioSession.ConnectAsync(signedIn, cancellationToken)
                    .ConfigureAwait(false);
            }
            lock (_sessionLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (version != _sessionVersion) throw new OperationCanceledException(cancellationToken);
                // A late authorization must not reinstall a signed-out session or its credentials.
                prepared.SaveCredentials();
                _session = prepared;
                prepared = null;
            }
        }
        finally { prepared?.Dispose(); _prepareGate.Release(); }
    }

    private void NotifyTrackPlaying(string uri)
    {
        lock (_navigationLock)
        {
            if (!_active || _current?.Uri != uri) return;
            if (_playRequested) SetPlaying(true);
            else { PauseLoaded(); SetPlaying(false); }
        }
    }

    private void NotifyTrackEnded(string uri)
    {
        lock (_navigationLock)
            if (_active && _current?.Uri == uri) AdvanceAtEnd(uri);
    }

    private void LoadTrack(Track track, double seconds)
    {
        _navigationStopSent = false;
        var position = Math.Clamp(_pendingSeek ?? seconds, 0,
            track.DurationMs > 0 ? Math.Max(0, track.DurationMs / 1000.0 - 1) : double.MaxValue);
        if (_clock is { } clock)
            _clock = clock with { Elapsed = position, ObservedAt = DateTimeOffset.UtcNow };
        _currentLoadSent = true;
        LoadManaged(track.Uri, position);
    }

    private double SavedPosition(Track track)
    {
        if (_audioSettings?.SaveFilePositions != true || _readPosition is null || !IsTrack(track.Uri)) return 0;
        double seconds;
        lock (_positionLock)
            seconds = _readPosition("https://open.spotify.com/track/" + track.Uri[14..]) ?? 0;
        return double.IsFinite(seconds) ? Math.Max(0, seconds) : 0;
    }

    private void SavePosition(double? overrideSeconds = null)
    {
        if (!_currentLoadSent) return;
        if (_ended && overrideSeconds is null) return;
        var elapsed = overrideSeconds ?? Elapsed;
        if (_audioSettings?.SaveFilePositions != true || _writePosition is null
            || CurrentLink is not { } link || elapsed is null) return;
        lock (_positionLock) _writePosition(link, elapsed.Value);
    }

    /// <summary>Toggles play/pause and returns whether it is now playing.</summary>
    internal bool TogglePause()
    {
        lock (_navigationLock)
        {
            var playing = !_playRequested;
            if (playing) Resume(); else Pause();
            return playing;
        }
    }

    internal void Pause()
    {
        lock (_navigationLock)
        {
            _playRequested = false;
            PauseLoaded();
            SetPlaying(false);
        }
    }
    internal void Resume()
    {
        lock (_navigationLock)
        {
            if (_ended && _current is { } track)
            {
                SetCurrent(track, playing: false);
                LoadTrack(track, 0);
                return;
            }
            _playRequested = true;
            PlayLoaded();
            SetPlaying(true);
        }
    }

    internal void Seek(double seconds)
    {
        lock (_navigationLock)
        {
            if (Duration is not double duration || duration <= 0) return;
            var position = Math.Clamp(seconds, 0, duration);
            // A completed track can be sought and played again without restarting at zero.
            if (position < duration) _ended = false;
            if (_clock is { } clock)
                _clock = clock with { Elapsed = position, Duration = duration, ObservedAt = DateTimeOffset.UtcNow };
            if (IsLoadedCurrent) _player.SeekAbsolute(position);
            else _pendingSeek = position;
        }
    }

    internal void SeekRelative(double delta)
    {
        lock (_navigationLock) { if (Elapsed is double e) Seek(e + delta); }
    }

    private void SetCurrent(Track track, bool playing)
    {
        CancelNavigationDelay();
        // Selection changes immediately, before the navigation debounce starts the next load.
        // Cancel metadata, key, demand and read-ahead requests for the previous track now.
        _loadCancellation?.Cancel();
        Interlocked.Increment(ref _loadGeneration);
        _playRequested = true;
        _ended = false;
        _currentLoadSent = false;
        _loadedUri = null;
        _pendingSeek = null;
        _current = track;
        _clock = track.DurationMs > 0
            ? new PlaybackClock(track.DurationMs / 1000.0, 0, playing, DateTimeOffset.UtcNow) : null;
        TrackChanged?.Invoke(track);
        PlayingChanged?.Invoke(playing);
    }

    private void SetPlaying(bool playing)
    {
        if (_clock is { } clock)
        {
            var elapsed = Elapsed ?? clock.Elapsed;
            _clock = clock with { Elapsed = elapsed, Playing = playing, ObservedAt = DateTimeOffset.UtcNow };
        }
        PlayingChanged?.Invoke(playing);
    }

    private void PauseLoaded()
    {
        if (IsLoadedCurrent) _player.Pause();
    }

    private void PlayLoaded()
    {
        if (IsLoadedCurrent) _player.Play();
    }

    private void OnPlayerThread(Action action) => _dispatcher.Post(action);

    private void LoadManaged(string uri, double position)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;
        var token = cancellation.Token;
        var generation = CurrentLoadGeneration;
        _ = Task.Run(async () =>
        {
            try
            {
                var session = _session ?? throw new InvalidOperationException();
                var audio = await session.GetAudioAsync(uri, RequestedBitrate,
                    token).ConfigureAwait(false);
                _dispatcher.Post(() =>
                {
                    if (token.IsCancellationRequested || !_active
                        || generation != CurrentLoadGeneration || _current?.Uri != uri)
                    {
                        audio.Dispose();
                        return;
                    }
                    try
                    {
                        var startPosition = _pendingSeek ?? position;
                        if (!_player.OpenSpotifyAudio(audio, CurrentTitle ?? string.Empty,
                            "https://open.spotify.com/track/" + uri[14..], startPosition))
                        {
                            CommandFailed?.Invoke(new InvalidDataException());
                            return;
                        }
                        _loadedUri = uri;
                        _pendingSeek = null;
                        if (!_playRequested) _player.Pause();
                        NotifyTrackPlaying(uri);
                    }
                    catch (Exception error)
                    {
                        // This action runs later on the UI thread, outside the worker's try/catch.
                        // Optional Spotify stream failures must restore the session, not escape it.
                        CommandFailed?.Invoke(error);
                    }
                });
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Stopping playback or selecting another track intentionally cancels this request.
            }
            catch (Exception error)
            {
                _dispatcher.Post(() =>
                {
                    if (!token.IsCancellationRequested && _active
                        && generation == CurrentLoadGeneration) CommandFailed?.Invoke(error);
                });
            }
        }, token);
    }

    private void HandlePlayerEnded(EndReason reason)
    {
        if (!_active || !IsLoadedCurrent || _current is not { } track) return;
        var generation = CurrentLoadGeneration;
        if (reason == EndReason.EndOfFile)
            _dispatcher.Post(() =>
            {
                if (generation != CurrentLoadGeneration) return;
                if (BeforeTrackEnd?.Invoke() == true)
                {
                    if (_active) FinishCurrent();
                    return;
                }
                NotifyTrackEnded(track.Uri);
            });
        else if (reason == EndReason.Error)
            _dispatcher.Post(() =>
            {
                if (generation == CurrentLoadGeneration)
                    CommandFailed?.Invoke(new InvalidDataException());
            });
    }

    internal static bool IsTrack(string uri) => TrackId.TryParse(uri, out _);

    internal void Stop(bool restoreSession = true)
    {
        var ownedSession = _ownsPlayerSession;
        _ownsPlayerSession = false;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;
        lock (_navigationLock)
        {
            SavePosition();
            Interlocked.Increment(ref _loadGeneration);
            _active = false;
            _playRequested = false;
            _ended = false;
            _currentLoadSent = false;
            _loadedUri = null;
            _pendingSeek = null;
            _repeatCurrent = false;
            _navigationStopSent = false;
            _pageCancellation?.Cancel();
            _pageCancellation?.Dispose();
            _pageCancellation = null;
            _fetchingMore = false;
            _pageFetchAfterEnded = false;
            CancelNavigationDelay();
            _pageCursors.Clear();
            _pageLoader = null;
            _nextPage = null;
            _deduplicateArtist = false;
            _queue = [];
            _sourceQueue = [];
            _index = 0;
            _shuffle = false;
            _clock = null;
            _current = null;
        }
        if (ownedSession && _player.CurrentPath?.StartsWith("luna-spotify://", StringComparison.Ordinal) == true)
            _player.Stop();
        if (ownedSession && restoreSession) _player.LeaveSession();
    }

    internal void SignOut()
    {
        Stop();
        ManagedAudioSession? previous;
        lock (_sessionLock)
        {
            _sessionVersion++;
            previous = _session;
            _session = null;
            try { PlaybackCredentialStore.Delete(); }
            finally { previous?.Dispose(); }
        }
    }

    public void Dispose()
    {
        ManagedAudioSession? previous;
        lock (_sessionLock)
        {
            if (_disposed) return;
            _disposed = true;
            _sessionVersion++;
            previous = _session;
            _session = null;
        }
        Stop(restoreSession: false);
        _player.Ended -= HandlePlayerEnded;
        previous?.Dispose();
        // A canceled preparation still needs to release its managed gate on the worker thread.
    }

    private sealed record PlaybackClock(double Duration, double Elapsed, bool Playing, DateTimeOffset ObservedAt);
}
