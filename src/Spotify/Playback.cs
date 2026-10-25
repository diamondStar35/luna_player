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
    private string _authorizationMessage = string.Empty;
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
    internal event Action? QueueEmpty;
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
        // Translators: Confirmation displayed in the browser after returning from Spotify authorization.
        _authorizationMessage = Tr("You can return to Luna Player now.");
        _player.BeginSpotifySession();
        _ownsPlayerSession = true;
        _player.Stop();
    }

    internal bool CloseAllFiles()
    {
        if (!_ownsPlayerSession && !_active) return false;
        Stop(restoreSession: false);
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
    internal bool IsPlaying => !_ended && _playRequested && (IsLoadedCurrent
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

    internal void ReloadQuality()
    {
        lock (_navigationLock)
        {
            if (!_active || _current is not { } track) return;
            var position = Elapsed ?? 0;
            var playing = _playRequested && !_ended;
            var ended = _ended;
            SavePosition();
            CancelPageRead();
            _player.Stop();
            SetCurrent(track, playing: false);
            _playRequested = playing;
            _ended = ended;
            LoadTrack(track, position);
        }
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
                var signedIn = await PlaybackOAuth.SignInAsync(_authorizationMessage, cancellationToken).ConfigureAwait(false);
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

    private void ClearCurrent()
    {
        CancelNavigationDelay();
        // Invalidate the old track before any asynchronous completion can use its stream.
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;
        Interlocked.Increment(ref _loadGeneration);
        _playRequested = false;
        _ended = false;
        _currentLoadSent = false;
        _loadedUri = null;
        _pendingSeek = null;
        _current = null;
        _clock = null;
    }

    private void SetCurrent(Track track, bool playing)
    {
        ClearCurrent();
        _playRequested = true;
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

    private bool OnPlayerThread(Action action)
    {
        if (_disposed) return false;
        try { _dispatcher.Post(action); return true; }
        catch (InvalidOperationException) when (_disposed)
        {
            // The host can be released while a worker is posting its final canceled result.
            return false;
        }
    }

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
            AudioDecryptStream? pendingAudio = null;
            CancellationTokenRegistration pendingRegistration = default;
            try
            {
                var session = _session ?? throw new InvalidOperationException();
                pendingAudio = await session.GetAudioAsync(uri, RequestedBitrate, token).ConfigureAwait(false);
                var audio = pendingAudio;
                // Cancellation must release audio even if wx discards an already-posted callback at exit.
                pendingRegistration = token.UnsafeRegister(static state => ((AudioDecryptStream)state!).Dispose(), audio);
                var registration = pendingRegistration;
                if (OnPlayerThread(() =>
                {
                    registration.Dispose();
                    if (token.IsCancellationRequested || !_active || !_player.InSpotifySession
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
                        // The player owns successfully opened streams; failed opens dispose their audio.
                        CommandFailed?.Invoke(error);
                    }
                }))
                {
                    pendingAudio = null;
                    pendingRegistration = default;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Stopping playback or selecting another track intentionally cancels this request.
            }
            catch (Exception error)
            {
                OnPlayerThread(() =>
                {
                    if (!token.IsCancellationRequested && _active
                        && generation == CurrentLoadGeneration) CommandFailed?.Invoke(error);
                });
            }
            finally
            {
                pendingRegistration.Dispose();
                pendingAudio?.Dispose();
            }
        }, token);
    }

    private void HandlePlayerEnded(EndReason reason)
    {
        if (!_active || !IsLoadedCurrent || _current is not { } track) return;
        var generation = CurrentLoadGeneration;
        if (reason == EndReason.EndOfFile)
            OnPlayerThread(() =>
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
            OnPlayerThread(() =>
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
        lock (_navigationLock)
        {
            SavePosition();
            ClearCurrent();
            _active = false;
            _repeatCurrent = false;
            _navigationStopSent = false;
            CancelPageRead();
            _pageCursors.Clear();
            _pageLoader = null;
            _nextPage = null;
            _deduplicateArtist = false;
            _queue = [];
            _sourceQueue = [];
            _index = -1;
            _shuffle = false;
        }
        // A replacement source may already have acquired the player. Never stop or restore over it.
        if (!ownedSession || !_player.InSpotifySession) return;
        _player.Stop();
        if (restoreSession) _player.LeaveSession();
        else _player.DiscardSpotifySession();
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
