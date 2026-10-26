using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    internal bool OpenFile(string path, double? startPosition = null)
    {
        LeaveSession();
        SavePosition();
        return _playlist.OpenFile(path, startPosition) && LoadCurrent();
    }

    internal bool OpenFiles(IEnumerable<string> files, string? preferredPath = null, double? startPosition = null)
    {
        LeaveSession();
        SavePosition();
        return _playlist.OpenFiles(files, preferredPath, startPosition) && LoadCurrent();
    }

    /// <summary>Replaces the opened-files list with the entries supplied by an M3U playlist, retaining
    /// names declared by EXTINF lines.</summary>
    internal bool OpenPlaylist(IReadOnlyList<M3uEntry> entries)
    {
        LeaveSession();
        SavePosition();
        if (!_playlist.OpenFiles(entries.Select(static entry => entry.Location)))
            return false;
        foreach (var entry in entries)
            _playlist.SetTitle(entry.Location, entry.Title);
        return LoadCurrent();
    }

    /// <summary>Opens a network stream. Unlike a file it is appended to the playlist rather than replacing
    /// it.</summary>
    /// <param name="title">What to call the entry. A resolved stream address says nothing a user would
    /// recognise, so the name comes from whoever resolved it.</param>
    /// <param name="source">The address the stream was resolved from, which outlives the stream itself.
    /// </param>
    /// <param name="audioFile">A second stream carrying the sound, when the first carries only picture.
    /// </param>
    internal bool OpenStream(
        string url, string? title = null, string? source = null, string? audioFile = null, bool audioOnly = false)
    {
        // A plain stream, or a single video named by a link, belongs to the playlist the user is working
        // in. Only a list of YouTube videos gets a temporary session playlist of its own.
        LeaveSession();
        SavePosition();
        if (!_playlist.Append(url, jump: true))
            return false;
        Describe(url, title, source, audioFile, audioOnly);
        return LoadCurrent();
    }

    /// <summary>The address whatever is playing was resolved from, or null when its path is its address.
    /// </summary>
    internal string? CurrentSource => _playlist.CurrentSource;

    private void Describe(string url, string? title, string? source, string? audioFile, bool audioOnly = false)
    {
        _playlist.SetTitle(url, title);
        _playlist.SetSource(url, source, audioFile, audioOnly);
    }

    /// <summary>Queues a resolved session stream as the preloaded successor without changing what Luna says
    /// is current. Split video/audio streams are rejected by the engine and keep the ordinary YouTube path.</summary>
    internal bool QueueSessionStream(string expectedCurrentSource, string url, string? title, string? source,
        string? audioFile, bool audioOnly)
    {
        if ((!_crossfade && _gaplessPlayback == GaplessPlaybackMode.Disabled) || !_loaded || _holdAutomaticAdvance
            || !string.Equals(CurrentSource, expectedCurrentSource, StringComparison.Ordinal))
            return false;
        if (_engine.QueueNext(url, audioFile: audioFile, noVideo: audioOnly))
        {
            _queuedSessionStream = new PendingSessionStream(
                expectedCurrentSource, url, title, source, audioFile, audioOnly);
            return true;
        }
        _queuedSessionStream = null;
        return false;
    }

    private sealed record PendingSessionStream(
        string CurrentSource, string Url, string? Title, string? Source, string? AudioFile, bool AudioOnly);

    internal bool OpenFolder(string folderPath)
    {
        var files = MediaLibrary.CollectFiles(folderPath);
        return files.Count > 0 && OpenFiles(files, _playlist.CurrentPath);
    }

    internal bool OpenFileWithFolder(string path, bool recursive = false, double? startPosition = null)
    {
        var folder = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(folder))
            return false;
        var files = MediaLibrary.CollectFiles(folder, recursive);
        return files.Count > 0 && OpenFiles(files, path, startPosition);
    }

    internal bool Next(bool wrap)
    {
        SavePosition();
        return _playlist.MoveNext(wrap) && LoadCurrent(fadeTransition: true);
    }

    internal bool Previous(bool wrap)
    {
        SavePosition();
        return _playlist.MovePrevious(wrap) && LoadCurrent(fadeTransition: true);
    }

    internal bool First()
    {
        SavePosition();
        return _playlist.GoToFirst() && LoadCurrent(fadeTransition: true);
    }

    internal bool Last()
    {
        SavePosition();
        return _playlist.GoToLast() && LoadCurrent(fadeTransition: true);
    }

    internal bool GoToIndex(int index)
    {
        SavePosition();
        return _playlist.GoToIndex(index) && LoadCurrent(fadeTransition: true);
    }

    internal bool GoToItem(Playlist.PlaylistItem item)
    {
        var target = Items.FirstOrDefault(candidate => candidate.Id == item.Id)
            ?? Items.FirstOrDefault(candidate => candidate.SourceId == item.SourceId);
        if (target is null) return false;
        SavePosition();
        return _playlist.GoToItem(target.Id) && LoadCurrent(fadeTransition: true);
    }

    internal bool Reload() => LoadCurrent();

    /// <summary>Reloads the current file from the very beginning, ignoring any remembered position. This is
    /// what repeating and looping need: the file has just finished, so its saved position is the end.</summary>
    internal bool RestartCurrent() => LoadCurrent(0, paused: false);

    internal void SetEndBehavior(EndBehavior behavior)
    {
        _endBehavior = behavior;
        _engine.SetEndBehavior(behavior);
        PrepareNext();
    }

    internal void ConfigurePlaybackEffects(AudioSettings settings)
    {
        _gaplessPlayback = settings.GaplessPlayback;
        _crossfade = settings.Crossfade;
        _wrapPlaylist = settings.WrapPlaylist;
        if (!_crossfade && _gaplessPlayback == GaplessPlaybackMode.Disabled)
            _queuedSessionStream = null;
        _engine.ConfigurePlaybackEffects(settings);
        PrepareNext();
        NextPreparationRequested?.Invoke();
    }

    private bool LoadCurrent(bool fadeTransition = false)
        => LoadCurrent(_playlist.TakePendingStart(), paused: false, fadeTransition);

    private bool LoadCurrent(double? startPosition, bool paused, bool fadeTransition = false)
    {
        Interlocked.Increment(ref _playbackRevision);
        _queuedSessionStream = null;
        var path = _playlist.CurrentPath;
        if (path is null)
            return false;
        if (!startPosition.HasValue && _playlist.CurrentItem?.Chapter is Chapter chapter)
            startPosition = chapter.Start;
        if (!startPosition.HasValue && _trackPositions && _positions is not null && File.Exists(path))
            startPosition = _positions.Get(path);
        // A replacement load supplies its own end-behaviour options. Restoring them on the outgoing file
        // can turn a completed selection's artificial EOF into another playlist advance before loadfile.
        _engine.ClearSelectionRange(restoreEndBehavior: false);
        _engine.ClearLoop();
        if (_playlist.CurrentItem?.Chapter is Chapter bounds && startPosition is double requested)
            startPosition = Math.Clamp(requested, bounds.Start, bounds.End);
        _chapterStopHeld = false;
        _requestedSourcePosition = startPosition ?? 0;
        var item = _playlist.CurrentItem!;
        var stop = _stopAtChapterEnd ? Media.Chapters.At(_playlist.ChaptersFor(path), _requestedSourcePosition)?.End : null;
        _armedChapterEnd = stop;
        if (!_engine.Load(path, startPosition, paused, _playlist.GetAudioFile(path),
                _playlist.GetAudioOnly(path), fadeTransition, RangeFor(item), item.Id, stop))
            return false;
        _engine.SetNormalization(_normalizationEnabled);
        _engine.SetMono(_monoEnabled);
        _engine.SetSilenceRemoval(_silenceEnabled, AudioFilters.SilenceGraph(_silence));
        _loaded = true;
        PrepareNext();
        CurrentChanged?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>Moves Luna's playlist to the item mpv has already started. Called on the UI thread after
    /// the engine reports its native queued transition.</summary>
    internal bool AcceptQueuedAdvance()
    {
        if (_engine.TakeQueuedAdvance() is not QueuedAdvance advance)
            return false;
        if (_queuedSessionStream is PendingSessionStream stream
            && string.Equals(CurrentSource, stream.CurrentSource, StringComparison.Ordinal)
            && Paths.AreSame(advance.Path, stream.Url))
        {
            SavePosition(CurrentPath, advance.PreviousPosition);
            _queuedSessionStream = null;
            if (!_playlist.OpenFile(stream.Url))
                return false;
            Describe(stream.Url, stream.Title, stream.Source, stream.AudioFile, stream.AudioOnly);
        }
        else
        {
            SavePosition(CurrentPath, advance.PreviousPosition);
            if (_playlist.PeekNext(_wrapPlaylist) is { } next && advance.ItemId == next.Id)
            {
                if (!_playlist.MoveNext(_wrapPlaylist))
                    return false;
            }
            else
            {
                // Shuffle or a removal can change the visible order after mpv commits a transition.
                // Follow the entry it actually started without shuffling again or reloading that file.
                var files = _playlist.Items;
                var index = -1;
                for (var item = 0; item < files.Count; item++)
                {
                    if (files[item].Id == advance.ItemId)
                    {
                        index = item;
                        break;
                    }
                }
                if (index < 0)
                {
                    if (_playlist.MoveNext(_wrapPlaylist))
                        return LoadCurrent();
                    Stop();
                    return false;
                }
                if (index != _playlist.CurrentIndex && !_playlist.GoToIndex(index))
                    return false;
            }
        }
        _loaded = true;
        PrepareNext();
        CurrentChanged?.Invoke();
        StateChanged?.Invoke();
        return true;
    }

    private void PrepareNext()
    {
        _engine.SetRepeatFile(_playlist.IsRepeatFileEnabled);
        if (_radioSession || !_loaded || (!_crossfade && _gaplessPlayback == GaplessPlaybackMode.Disabled) || _holdAutomaticAdvance
            || _stopAtChapterEnd || _chapterStopHeld || _endBehavior != EndBehavior.Advance || _playlist.IsRepeatFileEnabled)
        {
            _engine.ClearQueuedNext();
            return;
        }
        if (_queuedSessionStream is PendingSessionStream stream
            && string.Equals(CurrentSource, stream.CurrentSource, StringComparison.Ordinal))
        {
            _engine.QueueNext(stream.Url, audioFile: stream.AudioFile, noVideo: stream.AudioOnly);
            return;
        }
        if (_playlist.PeekNext(_wrapPlaylist) is not { } next)
        {
            _engine.ClearQueuedNext();
            return;
        }
        var path = next.Path;
        double? start = next.Chapter?.Start;
        if (start is null && _trackPositions && _positions is not null && File.Exists(path))
            start = _positions.Get(path);
        _engine.QueueNext(path, start, _playlist.GetAudioFile(path), _playlist.GetAudioOnly(path),
            RangeFor(next), next.Id);
    }

    internal void HoldAutomaticAdvance(bool hold, bool prepare = true)
    {
        if (_holdAutomaticAdvance == hold)
            return;
        _holdAutomaticAdvance = hold;
        _engine.HoldAutomaticAdvance(hold);
        if (!hold && prepare)
        {
            PrepareNext();
            NextPreparationRequested?.Invoke();
        }
    }
}
