using LunaPlayer.Media;
using LunaPlayer.Playlist;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    private IMediaResolver? _mediaResolver;
    private Action<Action>? _postMedia;
    private CancellationTokenSource? _mediaLoad;
    private CancellationTokenSource? _mediaNext;
    private MediaInput? _currentInput;
    private string? _inputIdentity;
    private bool _resolvingMedia, _pendingMediaPaused;
    internal bool IsResolvingMedia => _resolvingMedia;
    private readonly Dictionary<string, MediaTags> _catalogTags = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MediaInput> _preparedInputs = new(StringComparer.Ordinal);
    internal event Action<Exception>? MediaResolutionFailed;
    internal event Action? SamplingTick;
    internal void SampleMedia() => SamplingTick?.Invoke();
    internal MediaTags? CatalogTags => CurrentPath is { } path && _catalogTags.TryGetValue(path, out var tags) ? tags : null;
    internal void SetMediaResolver(IMediaResolver resolver, Action<Action> post) { _mediaResolver = resolver; _postMedia = post; }

    internal int InsertMedia(IReadOnlyList<(string Address, MediaTags Tags)> items, bool play, double? position = null)
    {
        if (items.Count == 0) return 0;
        _mediaForward = true;
        LeaveSession(); LeaveDisc();
        var requested = items.DistinctBy(item => item.Address, StringComparer.Ordinal).ToArray();
        var existing = _playlist.Sources.Select(item => item.Path).ToHashSet(StringComparer.Ordinal);
        var logical = SnapshotSaved().ToList();
        var added = _playlist.Insert(requested.Where(item => !existing.Contains(item.Address)).Select(item => item.Address),
            afterCurrent: false, SourceElapsed ?? 0);
        foreach (var item in requested) { _catalogTags[item.Address] = item.Tags; _playlist.SetTitle(item.Address, item.Tags.Title); }
        logical.AddRange(added.Select(item => SavedEntry.FromPath(item.Path) with { Id = item.SourceId, Title = _playlist.GetTitle(item.Path) }));
        _playlist.ReplaceSaved(logical);
        var target = _playlist.Items.FirstOrDefault(item => item.Path == requested[0].Address);
        if (play && target is not null)
        {
            SavePosition(); _playlist.GoToItem(target.Id);
            LoadCurrent(position, paused: false, fadeTransition: true);
        }
        else { PrepareNext(); StateChanged?.Invoke(); }
        return added.Count;
    }

    internal void RemoveServerMedia(Guid server)
    {
        var paths = Items.Where(x => x.ServerMedia?.ServerId == server).Select(x => x.Path).Distinct().ToArray();
        if (paths.Length == 0) return;
        var active = Subsonic.Playback.Reference.Parse(CurrentPath)?.ServerId == server;
        if (active) StopEngine();
        foreach (var path in paths) ForgetSavedPath(path);
        _playlist.RemovePaths(paths);
        foreach (var path in paths) _catalogTags.Remove(path);
        CurrentChanged?.Invoke(); StateChanged?.Invoke();
        if (!active) PrepareNext();
    }

    private bool BeginMediaLoad(string identity, double? position, bool paused, bool fade)
    {
        if (_mediaResolver?.Handles(identity) != true) return false;
        var resolveFrom = _mediaBoundaries.ResolveFrom(position);
        _mediaLoad?.Cancel(); _mediaLoad?.Dispose();
        _mediaNext?.Cancel();
        _mediaLoad = new();
        _resolvingMedia = true; _pendingMediaPaused = paused;
        _requestedSourcePosition = position ?? 0;
        var token = _mediaLoad.Token;
        var forward = _mediaForward;
        var item = _playlist.CurrentItem!;
        _inputIdentity = identity; _currentInput = null;
        _loaded = false;
        _engine.ClearQueuedNext();
        if (_preparedInputs.Remove(identity, out var prepared)
            && (prepared.SeekByReopening ? Math.Abs(prepared.Offset - (resolveFrom ?? 0)) < 0.001
                : prepared.Offset <= (resolveFrom ?? 0)))
        {
            CompleteMediaLoad(identity, item.Id, prepared, position, fade);
            return true;
        }
        _ = Task.Run(async () =>
        {
            try
            {
                var input = await _mediaResolver.ResolveAsync(identity, resolveFrom, token).ConfigureAwait(false);
                _postMedia!(() =>
                {
                    if (_disposed || token.IsCancellationRequested || CurrentItemId != item.Id) return;
                    CompleteMediaLoad(identity, item.Id, input, position, fade);
                });
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error) { _postMedia!(() => { if (!_disposed && !token.IsCancellationRequested && CurrentItemId == item.Id)
                MediaLoadFailed(identity, error, forward); }); }
        }, token);
        CurrentChanged?.Invoke(); StateChanged?.Invoke();
        return true;
    }

    private void CompleteMediaLoad(string identity, string itemId, MediaInput input, double? position, bool fade)
    {
        _resolvingMedia = false;
        _currentInput = input; _inputIdentity = identity; _catalogTags[identity] = input.Tags;
        _playlist.SetTitle(identity, input.Tags.Title);
        _loaded = _engine.Load(input.Address, position is { } target ? Math.Max(0, target - input.Offset) : null, _pendingMediaPaused,
            noVideo: true, fadeTransition: fade, itemId: itemId, headers: input.Headers,
            boundaries: _mediaBoundaries.RelativeTo(input.Offset));
        _engine.SetNormalization(_normalizationEnabled); _engine.SetMono(_monoEnabled);
        _engine.SetSilenceRemoval(_silenceEnabled, AudioFilters.SilenceGraph(_silence));
        PrepareNext(); CurrentChanged?.Invoke(); StateChanged?.Invoke();
    }

    private void PrepareRemoteNext(PlaylistItem next, double? start)
    {
        _mediaNext?.Cancel(); _mediaNext?.Dispose(); _mediaNext = new();
        var token = _mediaNext.Token;
        var current = CurrentItemId;
        _ = Task.Run(async () =>
        {
            try
            {
                var input = await _mediaResolver!.ResolveAsync(next.Path, start, token).ConfigureAwait(false);
                _postMedia!(() =>
                {
                    if (_disposed || token.IsCancellationRequested || CurrentItemId != current || _playlist.PeekNext(_wrapPlaylist)?.Id != next.Id) return;
                    _catalogTags[next.Path] = input.Tags;
                    _preparedInputs.Clear();
                    _preparedInputs[next.Path] = input;
                    _engine.QueueNext(input.Address, input.Offset > 0 ? 0 : start, noVideo: true, itemId: next.Id, headers: input.Headers);
                });
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error)
            {
                if (MissingMedia(error)) _postMedia!(() => { if (!_disposed && !token.IsCancellationRequested)
                    { MarkUnavailable(next.Path); _engine.ClearQueuedNext(); } });
                // Transient prefetch failures are retried when the item is actually selected.
            }
        }, token);
    }

    internal void ApplyServerQueue(Guid server, Subsonic.Models.PlayQueue queue, bool play = false)
    {
        if (play) { _mediaForward = true; SavePosition(); LeaveSession(); LeaveDisc(); }
        var active = (_loaded || _resolvingMedia) && Subsonic.Playback.Reference.Parse(CurrentPath) is { Kind: Subsonic.Playback.MediaKind.Song } reference && reference.ServerId == server;
        var stopped = !_loaded && !_resolvingMedia;
        var currentItem = CurrentItemId;
        var wasPaused = !play && IsPaused;
        var position = SourceElapsed ?? 0;
        var addresses = queue.Entries.Select(x => new Subsonic.Playback.Reference(server, x.Id).Address).ToArray();
        foreach (var song in queue.Entries)
        {
            var address = new Subsonic.Playback.Reference(server, song.Id).Address;
            _catalogTags[address] = new(song.Title, song.DisplayArtist.Length > 0 ? song.DisplayArtist : song.Artist, song.Album);
            _playlist.SetTitle(address, song.Title);
        }
        _playlist.ReconcileServer(server, addresses, position);
        // Reuse an open original stream's decoder and buffered data. Resuming the engine
        // directly avoids Play's restart-at-end branch while the saved seek is queued.
        if (active || play)
        {
            var index = queue.CurrentIndex ?? Array.FindIndex(queue.Entries, x => x.Id == queue.Current);
            var target = index >= 0 && index < addresses.Length ? addresses[index] : null;
            if (target is null) StopEngine();
            else
            {
                var occurrence = Items.Where(x => x.Path == target).Skip(queue.Entries.Take(index).Count(x => x.Id == queue.Entries[index].Id)).FirstOrDefault();
                if (occurrence is not null) _playlist.GoToItem(occurrence.Id);
                if (stopped || _resolvingMedia || CurrentPath != _inputIdentity || CurrentItemId != currentItem
                    || play && (_currentInput?.SeekByReopening == true || !_mediaBoundaries.IsEmpty || _chapterStopHeld))
                    LoadCurrent(queue.Position / 1000, wasPaused);
                else
                {
                    if (play || Math.Abs(position - queue.Position / 1000) > 1) SeekAbsolute(queue.Position / 1000);
                    if (play) { _engine.Play(); StateChanged?.Invoke(); }
                }
            }
        }
        PrepareNext(); CurrentChanged?.Invoke(); StateChanged?.Invoke();
    }

    private void CancelMediaLoad()
    {
        _mediaLoad?.Cancel(); _mediaLoad?.Dispose(); _mediaLoad = null;
        _mediaNext?.Cancel(); _mediaNext?.Dispose(); _mediaNext = null;
        _inputIdentity = null; _currentInput = null; _resolvingMedia = false;
        _preparedInputs.Clear();
        Artwork = null;
        ClearMediaBoundaries();
    }
}
