using LunaPlayer.Media;
using LunaPlayer.Playlist;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    private bool _audiobookMode;
    private bool _stopAtChapterEnd;
    private bool _chapterStopHeld;
    private double _chapterStopPosition;
    private double? _armedChapterEnd;
    private double _requestedSourcePosition;

    internal bool AudiobookMode => _audiobookMode;
    internal bool StopAtChapterEnd => _stopAtChapterEnd;
    internal double? SourceElapsed => _chapterStopHeld ? _chapterStopPosition
        : _engine.IsLoading ? _requestedSourcePosition : _engine.Elapsed;
    internal IReadOnlyList<Chapter> CurrentChapters => _loaded ? _playlist.ChaptersFor(CurrentPath) : [];
    internal Chapter? CurrentChapter => _playlist.CurrentItem?.Chapter
        ?? Media.Chapters.At(CurrentChapters, _chapterStopHeld ? Math.Max(0, _chapterStopPosition - 0.001) : SourceElapsed ?? 0);

    internal static string ChapterTitle(Chapter chapter)
        => chapter.Title ??
            // Translators: Fallback title for a chapter without a name. {number} is its one-based number in the source file.
            TrFormat("Chapter {number}", chapter.Index + 1);

    internal double ToSourceTime(double seconds)
        => _playlist.CurrentItem?.Chapter is Chapter chapter
            ? chapter.Start + Math.Clamp(seconds, 0, chapter.Duration)
            : Math.Max(0, seconds);

    internal double ToPlaybackTime(double seconds)
        => _playlist.CurrentItem?.Chapter is Chapter chapter
            ? Math.Clamp(seconds - chapter.Start, 0, chapter.Duration)
            : Math.Max(0, seconds);

    private static PlaybackRange? RangeFor(PlaylistItem item)
        => item.Chapter is Chapter chapter ? new PlaybackRange(chapter.Start, chapter.End) : null;

    internal void SetAudiobookMode(bool enabled)
    {
        if (_audiobookMode == enabled)
            return;
        var position = SourceElapsed ?? 0;
        var paused = IsPaused;
        _audiobookMode = enabled;
        _playlist.SetAudiobookMode(enabled, position);
        if (_suspendedPlaylist is { } suspended)
            suspended.Playlist.SetAudiobookMode(enabled, suspended.Position);
        if (_loaded)
            LoadCurrent(position, paused);
        else
        {
            CurrentChanged?.Invoke();
            StateChanged?.Invoke();
        }
    }

    internal void RefreshChapters()
    {
        if (_loaded && !_engine.IsLoading && CurrentPath is string path)
            SetChapters(path, _engine.GetChapters());
    }

    internal void SetChapters(string path, IReadOnlyList<Chapter> chapters)
    {
        if (!_playlist.SourceFiles.Any(source => Configuration.Paths.AreSame(path, source)))
            return;
        var oldItem = _playlist.CurrentItem;
        var position = SourceElapsed ?? 0;
        var paused = IsPaused;
        if (!_playlist.SetChapters(path, chapters, position))
            return;
        if (_loaded && oldItem != _playlist.CurrentItem)
            LoadCurrent(position, paused);
        else
        {
            FollowChapterStop(position);
            PrepareNext();
            StateChanged?.Invoke();
        }
    }

    internal bool GoToChapter(int index)
    {
        var chapters = CurrentChapters;
        if (index < 0 || index >= chapters.Count)
            return false;
        var start = chapters[index].Start;
        SavePosition();
        if (!_audiobookMode && !_chapterStopHeld)
        {
            // A chapter marker is a seek within the same file in normal mode. Preserve its selected
            // audio and subtitles, while releasing any selection that would pull playback back again.
            _engine.ClearSelectionRange(restoreEndBehavior: false);
            _engine.ClearLoop();
            SeekAbsolute(start);
            CurrentChanged?.Invoke();
            StateChanged?.Invoke();
            PrepareNext();
            return true;
        }
        if (_audiobookMode && !_playlist.GoToChapter(index))
            return false;
        return LoadCurrent(start, paused: IsPaused, fadeTransition: true);
    }

    /// <summary>Bookmarks retain source timestamps, so a jump may need to select another chapter before
    /// using the player's chapter-relative seek interface.</summary>
    internal void SeekSourcePosition(double position)
    {
        var chapter = Media.Chapters.At(CurrentChapters, position);
        if (_audiobookMode && chapter is not null && chapter != _playlist.CurrentItem?.Chapter)
        {
            SavePosition();
            if (_playlist.GoToChapter(chapter.Index))
                LoadCurrent(position, paused: IsPaused);
            return;
        }
        SeekAbsolute(ToPlaybackTime(position));
    }

    internal bool ToggleChapterStop()
    {
        if (CurrentChapter is null)
            return false;
        Interlocked.Increment(ref _playbackRevision);
        _stopAtChapterEnd = !_stopAtChapterEnd;
        FollowChapterStop(SourceElapsed ?? 0);
        PrepareNext();
        StateChanged?.Invoke();
        return _stopAtChapterEnd;
    }

    private void FollowChapterStop(double position)
    {
        if (!_chapterStopHeld)
        {
            _armedChapterEnd = _stopAtChapterEnd ? Media.Chapters.At(CurrentChapters, position)?.End : null;
            _engine.SetChapterStop(_armedChapterEnd);
        }
    }

    internal void HoldAtChapterEnd()
    {
        _chapterStopPosition = _armedChapterEnd ?? CurrentChapter?.End ?? SourceElapsed ?? 0;
        _chapterStopHeld = true;
        _stopAtChapterEnd = false;
        Pause();
    }

    private void ResumeAfterChapterStop()
    {
        SavePosition();
        if (_audiobookMode)
        {
            var next = (_playlist.CurrentItem?.Chapter?.Index ?? -1) + 1;
            if (_playlist.GoToChapter(next))
                LoadCurrent(_playlist.CurrentItem!.Start, paused: false);
            else if (_playlist.MoveNext(_wrapPlaylist))
                LoadCurrent();
            else
                Stop();
        }
        else if (_engine.Duration is double duration && _chapterStopPosition >= duration - 0.001)
        {
            if (!Next(_wrapPlaylist)) Stop();
        }
        else
            LoadCurrent(_chapterStopPosition, paused: false);
    }
}
