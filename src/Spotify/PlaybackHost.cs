using LunaPlayer.Configuration.Models;
using LunaPlayer.Playback;
using LunaPlayer.Application;

namespace LunaPlayer.Spotify;

/// <summary>Coordinates Spotify navigation and plays decrypted audio through Luna's mpv engine.</summary>
internal sealed class PlaybackHost : IDisposable
{
    private static readonly TimeSpan LoadWindow = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan BurstLoadDelay = TimeSpan.FromMilliseconds(1250);
    private readonly SpotifySettings _settings;
    private readonly Player? _player;
    private readonly IApplicationDispatcher? _dispatcher;
    private ManagedAudioSession? _session;
    private CancellationTokenSource? _loadCancellation;
    private readonly Action<string>? _sendOverride;
    private readonly SemaphoreSlim _prepareGate = new(1, 1);
    private readonly object _sessionLock = new();
    private int _sessionVersion;
    private volatile bool _disposed;
    private readonly object _positionLock = new();
    private readonly object _navigationLock = new();
    private readonly Queue<DateTime> _loadSentAt = new();
    private volatile bool _active;
    private bool _ownsPlayerSession;
    private List<Track> _queue = [];
    private List<Track> _sourceQueue = [];
    private int _index;
    private bool _shuffle;
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
    private int _scheduledLoadId;
    private bool _navigationStopSent;
    private Func<string?, CancellationToken, Task<TrackPage>>? _pageLoader;
    private string? _nextPage;
    private CancellationTokenSource? _pageCancellation;
    private bool _fetchingMore;
    private bool _pageFetchAfterEnded;
    private bool _deduplicateArtist;

    internal event Action<Exception>? CommandFailed;
    internal event Action<Track>? TrackChanged;
    internal event Action<Track>? Navigated;
    internal event Action<bool>? PlayingChanged;
    internal Func<bool>? BeforeTrackEnd { get; set; }

    internal PlaybackHost(SpotifySettings settings, Action<string>? sendOverride)
    {
        _settings = settings;
        _sendOverride = sendOverride;
    }

    internal PlaybackHost(SpotifySettings settings, Player player,
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
        if (_player is null || _ownsPlayerSession) return;
        _player.EnterSession();
        _ownsPlayerSession = true;
        _player.Stop();
    }

    internal bool CloseAllFiles()
    {
        if (!_ownsPlayerSession && !_active) return false;
        var ownedSession = _ownsPlayerSession;
        Stop(restoreSession: false);
        if (ownedSession) _player?.DiscardSpotifySession();
        return true;
    }
    internal string? CurrentTitle => _current is { } track ? track.Name + " - " + track.Artists : null;
    internal Track? CurrentTrack => _current;
    internal int CurrentLoadGeneration => Volatile.Read(ref _loadGeneration);
    internal IReadOnlyList<Track> CurrentQueue => _queue.ToArray();
    internal IReadOnlyList<Track> SourceQueue => _sourceQueue.ToArray();
    internal int CurrentIndex => _index;
    internal bool IsShuffleEnabled => _shuffle;
    internal bool ToggleShuffle()
    {
        lock (_navigationLock)
        {
            if (!_active) return false;
            var current = _current;
            _shuffle = !_shuffle;
            _queue = [.. _sourceQueue];
            if (_shuffle)
            {
                // Like Luna's local shuffle, randomize the entire order and then locate
                // the current track. Pinning it first prevents ordinary Previous navigation.
                for (var index = _queue.Count - 1; index > 0; index--)
                {
                    var other = Random.Shared.Next(index + 1);
                    (_queue[index], _queue[other]) = (_queue[other], _queue[index]);
                }
            }
            _index = _queue.FindIndex(track => ReferenceEquals(track, current));
            if (_index < 0) _index = 0;
            return _shuffle;
        }
    }
    internal Func<string?, CancellationToken, Task<TrackPage>>? PageLoader => _pageLoader;
    internal string? NextPage => _nextPage;
    internal bool DeduplicateArtist => _deduplicateArtist;
    internal double ResumePosition => _ended ? 0 : Math.Max(0, Elapsed ?? 0);
    internal string? CurrentLink => _current is { } track && IsTrack(track.Uri)
        ? "https://open.spotify.com/track/" + track.Uri[14..] : null;
    internal bool IsRepeatCurrentEnabled => _repeatCurrent;
    internal bool ToggleRepeatCurrent() => _repeatCurrent = !_repeatCurrent;
    internal double? Duration => IsLoadedCurrent
        ? _player!.Duration ?? _clock?.Duration : _clock?.Duration;
    internal double? Elapsed
    {
        get
        {
            if (IsLoadedCurrent
                && _player!.Elapsed is { } actual) return actual;
            if (_clock is not { } clock) return null;
            var elapsed = clock.Elapsed + (clock.Playing ? (DateTimeOffset.UtcNow - clock.ObservedAt).TotalSeconds : 0);
            return Math.Clamp(elapsed, 0, clock.Duration);
        }
    }
    internal double? Remaining => Duration is double d && Elapsed is double e ? Math.Max(0, d - e) : null;
    internal bool IsPlaying => !_ended && (IsLoadedCurrent
        ? _player!.IsPlaying : _clock?.Playing ?? false);
    private bool IsLoadedCurrent => _loadedUri == _current?.Uri && _loadedUri is not null
        && _player?.CurrentPath?.StartsWith("luna-spotify://", StringComparison.Ordinal) == true;
    internal string RequestedBitrate => _settings.AudioQuality?.ToUpperInvariant() switch
    {
        "NORMAL" => "96", "HIGH" => "160", _ => "320",
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
        if (_player is not null)
        {
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
            return;
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal void NotifyTrackPlaying(string uri)
    {
        lock (_navigationLock)
        {
            if (!_active || _current?.Uri != uri) return;
            if (_playRequested) SetPlaying(true);
            else { Send("pause"); SetPlaying(false); }
        }
    }

    internal void NotifyTrackEnded(string uri)
    {
        lock (_navigationLock)
            if (_active && _current?.Uri == uri) AdvanceAtEnd(uri);
    }

    internal Task PlayAsync(Track track, IReadOnlyList<Track>? queue,
        CancellationToken cancellationToken, double? startPosition = null,
        Func<string?, CancellationToken, Task<TrackPage>>? pageLoader = null,
        string? nextPage = null, bool deduplicateArtist = false, bool shuffle = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeginSession();
        lock (_navigationLock)
        {
            SavePosition();
            if (_active) Send("stop");
            _pageCancellation?.Cancel();
            _pageCancellation?.Dispose();
            _pageCancellation = null;
            _fetchingMore = false;
            _pageFetchAfterEnded = false;
            _pageLoader = pageLoader;
            _nextPage = nextPage;
            _deduplicateArtist = deduplicateArtist;
            _navigationStopSent = false;
            var tracks = queue?.Where(item => IsTrack(item.Uri)).ToList() ?? [];
            var selected = tracks.FindIndex(item => ReferenceEquals(item, track));
            if (selected < 0) selected = tracks.FindIndex(item => item.Uri == track.Uri);
            if (selected < 0) { tracks.Insert(0, track); selected = 0; }
            _queue = tracks;
            _sourceQueue = [.. tracks];
            _index = selected;
            _active = true;
            _shuffle = false;
            // The shared mpv engine retains Luna's volume and audio effects.
            SetCurrent(track, playing: false);
            if (shuffle) ToggleShuffle();
            cancellationToken.ThrowIfCancellationRequested();
            SendLoad(track, startPosition ?? SavedPosition(track));
        }
        return Task.CompletedTask;
    }

    internal bool Next() => Step(1);
    internal bool Previous() => Step(-1);
    internal bool GoToIndex(int index)
    {
        lock (_navigationLock)
            return _active && index >= 0 && index < _queue.Count
                && Step(index - _index);
    }

    private bool Step(int delta, bool savePosition = true)
    {
        lock (_navigationLock)
        {
            if (delta > 0 && _index == _queue.Count - 1 && _nextPage is not null)
            {
                _playRequested = true;
                FetchMore();
                return true;
            }
            if (!TryTarget(_index, delta, out var target)) return false;
            if (_fetchingMore)
            {
                _pageCancellation?.Cancel();
                _pageCancellation?.Dispose();
                _pageCancellation = null;
                _fetchingMore = false;
                _pageFetchAfterEnded = false;
            }
            if (savePosition) SavePosition();
            _index = target;
            var track = _queue[target];
            // Explicit navigation starts the selected file, matching ordinary local playback.
            SetCurrent(track, playing: false);
            Navigated?.Invoke(track);
            // Each key changes Luna's selection immediately. Only the final selection
            // after a short burst starts fetching audio, avoiding wasted key requests.
            if (!_navigationStopSent) { Send("stop"); _navigationStopSent = true; }
            _ = LoadAfterNavigationAsync(track, Volatile.Read(ref _loadGeneration));
            return true;
        }
    }

    private void FetchMore(bool afterTrackEnded = false, int pagesRemaining = 3)
    {
        if (_fetchingMore)
        {
            if (afterTrackEnded) _pageFetchAfterEnded = true;
            return;
        }
        if (_pageLoader is not { } loader || _nextPage is not { } cursor) return;
        _fetchingMore = true;
        _pageCancellation = new CancellationTokenSource();
        var cancellation = _pageCancellation;
        var generation = Volatile.Read(ref _loadGeneration);
        var current = _current;
        _ = Task.Run(async () =>
        {
            try
            {
                var page = await loader(cursor, cancellation.Token).ConfigureAwait(false);
                OnPlayerThread(() =>
                {
                    lock (_navigationLock)
                    {
                        if (!_active || cancellation.IsCancellationRequested
                            || Volatile.Read(ref _loadGeneration) != generation || _current != current) return;
                        List<Track> added;
                        if (_deduplicateArtist)
                        {
                            var known = new HashSet<string>(_queue.Select(track => track.ArtistSongKey), StringComparer.Ordinal);
                            added = page.Tracks.Where(track => known.Add(track.ArtistSongKey)).ToList();
                        }
                        else added = page.Tracks.ToList();
                        _sourceQueue.AddRange(added);
                        if (_shuffle)
                        {
                            // Newly discovered tracks join random positions in the full order,
                            // matching how Luna repairs its shuffle when a playlist grows.
                            foreach (var addedTrack in added)
                                _queue.Insert(Random.Shared.Next(_queue.Count + 1), addedTrack);
                            _index = _queue.FindIndex(track => ReferenceEquals(track, _current));
                        }
                        else _queue.AddRange(added);
                        _nextPage = page.Next == cursor ? null : page.Next;
                        var ended = afterTrackEnded || _pageFetchAfterEnded;
                        _fetchingMore = false;
                        _pageFetchAfterEnded = false;
                        _pageCancellation = null;
                        cancellation.Dispose();
                        if (_index + 1 < _queue.Count)
                        {
                            Step(1, savePosition: !ended);
                        }
                        else if (_nextPage is not null && pagesRemaining > 1)
                            FetchMore(ended, pagesRemaining - 1);
                        else if (ended) FinishCurrent();
                    }
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Stopping playback or selecting another track intentionally cancels this request.
            }
            catch (Exception error)
            {
                OnPlayerThread(() =>
                {
                    lock (_navigationLock)
                    {
                        if (!_active || cancellation.IsCancellationRequested
                            || Volatile.Read(ref _loadGeneration) != generation) return;
                        var ended = afterTrackEnded || _pageFetchAfterEnded;
                        _fetchingMore = false;
                        _pageFetchAfterEnded = false;
                        _pageCancellation = null;
                        cancellation.Dispose();
                        if (ended) FinishCurrent();
                        CommandFailed?.Invoke(error);
                    }
                });
            }
        });
    }

    private bool TryTarget(int index, int delta, out int target)
    {
        target = index + delta;
        if (_queue.Count == 0) return false;
        if (target >= 0 && target < _queue.Count) return true;
        if (_audioSettings?.WrapPlaylist != true || _queue.Count < 2) return false;
        target = (target + _queue.Count) % _queue.Count;
        return true;
    }

    private async Task LoadAfterNavigationAsync(Track track, int generation)
    {
        await Task.Delay(250).ConfigureAwait(false);
        OnPlayerThread(() =>
        {
            lock (_navigationLock)
            {
                if (!_active || Volatile.Read(ref _loadGeneration) != generation || _current?.Uri != track.Uri)
                    return;
                SendLoad(track, SavedPosition(track));
                if (!_playRequested) Send("pause");
            }
        });
    }

    private void AdvanceAtEnd(string endedUri)
    {
        lock (_navigationLock)
        {
            if (!_active || _current?.Uri != endedUri) return;
            // A natural track change invalidates skip targets calculated before it.
            AdvanceAtEndCore();
        }
    }

    private void AdvanceAtEndCore()
    {
        SavePosition(0);
        if (_repeatCurrent && _current is { } current)
        {
            SetCurrent(current, playing: false);
            SendLoad(current, 0);
            return;
        }
        if (_audioSettings?.EndBehavior == EndBehavior.Loop && _current is { } loopTrack)
        {
            SetCurrent(loopTrack, playing: false);
            SendLoad(loopTrack, 0);
            return;
        }
        if (_audioSettings?.EndBehavior == EndBehavior.None || _queue.Count == 0)
        {
            FinishCurrent();
            return;
        }
        var next = _index + 1;
        if (next >= _queue.Count)
        {
            if (_nextPage is not null)
            {
                SetPlaying(false);
                FetchMore(afterTrackEnded: true);
                return;
            }
            if (_audioSettings?.WrapPlaylist != true || _queue.Count < 2)
            {
                FinishCurrent();
                return;
            }
            next = 0;
        }
        _index = next;
        var track = _queue[_index];
        SetCurrent(track, playing: false);
        Navigated?.Invoke(track);
        SendLoad(track, SavedPosition(track));
    }

    private void FinishCurrent()
    {
        _playRequested = false;
        _ended = true;
        if (_clock is { } clock)
            _clock = clock with { Elapsed = clock.Duration, ObservedAt = DateTimeOffset.UtcNow };
        SetPlaying(false);
    }

    private void SendLoad(Track track, double seconds)
    {
        var now = DateTime.UtcNow;
        while (_loadSentAt.TryPeek(out var sent) && now - sent >= LoadWindow)
            _loadSentAt.Dequeue();
        var due = _loadSentAt.Count >= 2 ? now + BurstLoadDelay : now;
        var id = ++_scheduledLoadId;
        var wait = due - now;
        if (wait > TimeSpan.Zero)
        {
            var generation = Volatile.Read(ref _loadGeneration);
            _ = Task.Run(async () =>
            {
                await Task.Delay(wait).ConfigureAwait(false);
                OnPlayerThread(() =>
                {
                    lock (_navigationLock)
                    {
                        if (!_active || Volatile.Read(ref _loadGeneration) != generation
                            || _scheduledLoadId != id || _current?.Uri != track.Uri) return;
                        SendLoadNow(track, seconds);
                    }
                });
            });
            return;
        }
        SendLoadNow(track, seconds);
    }

    private void SendLoadNow(Track track, double seconds)
    {
        _navigationStopSent = false;
        var position = Math.Clamp(_pendingSeek ?? seconds, 0,
            track.DurationMs > 0 ? Math.Max(0, track.DurationMs / 1000.0 - 1) : double.MaxValue);
        if (_clock is { } clock)
            _clock = clock with { Elapsed = position, ObservedAt = DateTimeOffset.UtcNow };
        _currentLoadSent = true;
        _loadSentAt.Enqueue(DateTime.UtcNow);
        Send(position > 0 ? $"loadpos {track.Uri} {(long)Math.Round(position * 1000)}" : $"load {track.Uri}");
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
            Send("pause");
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
                SendLoad(track, 0);
                return;
            }
            _playRequested = true;
            Send("play");
            SetPlaying(true);
        }
    }

    internal void Seek(double seconds)
    {
        lock (_navigationLock)
        {
            if (Duration is not double duration || duration <= 0) return;
            var position = Math.Clamp(seconds, 0, duration);
            if (_clock is { } clock)
                _clock = clock with { Elapsed = position, Duration = duration, ObservedAt = DateTimeOffset.UtcNow };
            if (IsLoadedCurrent) Send($"seek {(long)Math.Round(position * 1000)}");
            else _pendingSeek = position;
        }
    }

    internal void SeekRelative(double delta)
    {
        lock (_navigationLock) { if (Elapsed is double e) Seek(e + delta); }
    }

    private void SetCurrent(Track track, bool playing)
    {
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

    private void Send(string command)
    {
        if (_sendOverride is { } sendOverride) { sendOverride(command); return; }
        if (_player is not null)
        {
            if (command.StartsWith("load ", StringComparison.Ordinal))
                LoadManaged(command[5..], 0);
            else if (command.StartsWith("loadpos ", StringComparison.Ordinal))
            {
                var parts = command[8..].Split(' ', 2);
                if (parts.Length == 2 && long.TryParse(parts[1], out var milliseconds))
                    LoadManaged(parts[0], milliseconds / 1000.0);
            }
            else if (command == "stop") _player.Stop();
            else if (command == "pause" && IsLoadedCurrent) _player.Pause();
            else if (command == "play" && IsLoadedCurrent) _player.Play();
            else if (command.StartsWith("seek ", StringComparison.Ordinal)
                && IsLoadedCurrent && long.TryParse(command.AsSpan(5), out var seekMilliseconds))
                _player.SeekAbsolute(seekMilliseconds / 1000.0);
            return;
        }
    }

    private void OnPlayerThread(Action action)
    {
        if (_dispatcher is null) action();
        else _dispatcher.Post(action);
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
            try
            {
                var session = _session ?? throw new InvalidOperationException();
                var audio = await session.GetAudioAsync(uri, int.Parse(RequestedBitrate),
                    token).ConfigureAwait(false);
                _dispatcher!.Post(() =>
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
                        if (!_player!.OpenSpotifyAudio(audio, CurrentTitle ?? string.Empty,
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
                _dispatcher!.Post(() =>
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
            _dispatcher!.Post(() =>
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
            _dispatcher!.Post(() =>
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
            _scheduledLoadId++;
            _navigationStopSent = false;
            _pageCancellation?.Cancel();
            _pageCancellation?.Dispose();
            _pageCancellation = null;
            _fetchingMore = false;
            _pageFetchAfterEnded = false;
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
        if (ownedSession && _player?.CurrentPath?.StartsWith("luna-spotify://", StringComparison.Ordinal) == true)
            _player.Stop();
        if (ownedSession && restoreSession) _player?.LeaveSession();
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
        if (_player is not null) _player.Ended -= HandlePlayerEnded;
        previous?.Dispose();
        // A canceled preparation still needs to release its managed gate on the worker thread.
    }

    private sealed record PlaybackClock(double Duration, double Elapsed, bool Playing, DateTimeOffset ObservedAt);
}
