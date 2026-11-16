using MpvNet;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    private sealed record NativeEntry(long Id, LoadRequest Request, long Generation);
    private sealed record NativeLoad(LoadRequest Request, long Generation, long QueueGeneration, bool Append);
    private readonly Dictionary<ulong, NativeLoad> _nativeLoads = [];
    private readonly Dictionary<long, NativeEntry> _nativeEntries = [];
    private ulong _nativeLoadSequence;
    private NativeEntry? _startedEntry;
    private NativeEntry? _queuedEntry;

    private ulong RegisterNativeLoad(LoadRequest request, bool append)
    {
        var token = ++_nativeLoadSequence;
        _nativeLoads[token] = new(request, _loadGeneration, _queueGeneration, append);
        return token;
    }

    private void HandleNativeLoadReply(MpvEvent reply)
    {
        lock (_loadSync)
        {
            if (!_nativeLoads.Remove(reply.ReplyUserData, out var load) || reply.Error < 0
                || load.Generation != _loadGeneration || load.Append && load.QueueGeneration != _queueGeneration
                || reply.Data is not IDictionary<string, object?> result
                || !result.TryGetValue("playlist_entry_id", out var value) || value is not long id || id <= 0)
                return;
            var entry = new NativeEntry(id, load.Request, load.Generation);
            foreach (var previous in _nativeEntries.Keys.Where(key => key != _startedEntry?.Id
                && key != _pendingAdvance?.Id && key != _loadingEntryId).ToArray())
                _nativeEntries.Remove(previous);
            _nativeEntries[entry.Id] = entry;
            if (load.Append)
                _queuedEntry = entry;
            else
                _loadingEntryId = entry.Id;
        }
    }

    private void HandleStartFile(MpvEvent start)
    {
        lock (_loadSync)
        {
            if (start.Data is not MpvStartFileEvent data)
                return;
            // The native core can finish another short file before callbacks run.
            // Bind lifecycle events to command results, not the current global path.
            _startedEntry = _nativeEntries.TryGetValue(data.PlaylistEntryId, out var entry)
                && entry.Generation == _loadGeneration ? entry : null;
            if (!_awaitingLoadRestart && _startedEntry is { } incoming && incoming.Id != _activeEntryId)
            {
                if (_pendingAdvance?.Id != incoming.Id)
                {
                    _pendingAdvance = incoming;
                    _pendingSourcePosition = _lastPosition;
                }
                if (_queuedEntry?.Id == incoming.Id)
                {
                    _queuedEntry = null;
                    _queuedNext = false;
                    _queuedSourceEntryId = null;
                    _nextRequest = null;
                }
                _fadeInOnRestart = false;
                CancelEndFade();
            }
        }
    }

    private void CompleteNativeAdvance(NativeEntry incoming)
    {
        var advance = incoming.Request;
        _pendingAdvance = null;
        _activeEntryId = incoming.Id;
        _activePath = advance.Path;
        ReleasePendingSpotifyAudio(advance.Path);
        _playbackRange = advance.Range;
        _chapterStopEnd = null;
        _chapterStopCompleted = false;
        _lastPosition = null;
        _completedAdvance = new QueuedAdvance(advance.Path, _pendingSourcePosition, advance.ItemId);
        Ended?.Invoke(EndReason.QueuedAdvance);
    }
}
