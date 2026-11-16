using LunaPlayer.Media.Discs;

namespace LunaPlayer.Playback;

internal sealed partial class Player
{
    private DiscInfo? _disc;
    private (string Path, int Index)? _pendingDiscChapter;
    internal bool IsDisc => _disc is not null && Addressing.IsDisc(CurrentPath);
    private bool DiscChapterNavigation => IsDisc && _disc!.Titles.Count == 1;
    internal bool IsDiscAudio => IsDisc && _disc!.Format == DiscFormat.Audio;
    private string? DiscTrackName => DiscChapterNavigation && CurrentChapter is { } chapter
        ? chapter.Title ?? (IsDiscAudio
            // Translators: Fallback name for a DVD-Audio song without metadata. {number} is its one-based track number.
            ? TrFormat("Track {number}", chapter.Index + 1) : ChapterTitle(chapter)) : null;

    private LunaPlayer.Media.Chapter? PendingDiscChapter
    {
        get
        {
            if (_pendingDiscChapter is not { } pending) return null;
            var chapters = CurrentChapters;
            if (!IsDisc || pending.Path != CurrentPath || pending.Index >= chapters.Count)
            {
                _pendingDiscChapter = null;
                return null;
            }
            if (LunaPlayer.Media.Chapters.At(chapters, _engine.Elapsed ?? 0)?.Index == pending.Index)
            {
                _pendingDiscChapter = null;
                return null;
            }
            // Native seeks are queued. Repeated Next/Previous must advance from the requested
            // destination rather than whichever old position mpv has reported so far.
            return chapters[pending.Index];
        }
    }

    private void RememberDiscSeek(double position)
    {
        _pendingDiscChapter = IsDisc && CurrentPath is string path
            && LunaPlayer.Media.Chapters.At(CurrentChapters, position) is { } chapter ? (path, chapter.Index) : null;
    }

    internal bool OpenDisc(DiscInfo disc, int? title = null)
    {
        var selected = title ?? disc.DefaultTitle;
        if (!disc.Titles.Any(item => item.Index == selected)) return false;
        LeaveSession();
        SavePosition();
        _disc = disc;
        var addresses = disc.Titles.Select(item => disc.Address(item.Index)).ToArray();
        if (!_playlist.OpenFiles(addresses, disc.Address(selected))) return false;
        _playlist.SetAudiobookMode(false, 0);
        return LoadCurrent();
    }

    private string? DiscDisplayName(string path)
    {
        if (_disc is null) return null;
        var title = _disc.Titles.FirstOrDefault(item => string.Equals(path, _disc.Address(item.Index), StringComparison.OrdinalIgnoreCase));
        if (title is null) return null;
        // Translators: Fallback name for a numbered DVD program; {number} is the one-based title number.
        var label = TrFormat("Title {number}", title.Index + 1);
        return string.IsNullOrWhiteSpace(_disc.Name) ? label
            // Translators: DVD display name; {disc} is the disc's name and {title} identifies its selected program.
            : TrFormat("{disc} - {title}", _disc.Name, label);
    }

    private void LeaveDisc()
    {
        if (_disc is null) return;
        _disc = null;
        _playlist.SetAudiobookMode(_audiobookMode, SourceElapsed ?? 0);
    }

    internal bool CanNavigateDisc(int direction)
    {
        if (!IsDisc || !_loaded) return false;
        if (DiscChapterNavigation)
            return Navigation.Target(CurrentChapters.Count, CurrentChapter?.Index ?? 0, direction, _wrapPlaylist).HasValue;
        return Navigation.Target(Count, CurrentIndex, direction, _wrapPlaylist).HasValue;
    }

    private bool MoveDiscChapter(int direction, bool wrap)
    {
        var next = Navigation.Target(CurrentChapters.Count, CurrentChapter?.Index ?? 0, direction, wrap);
        return next is int index && GoToChapter(index);
    }

    private string? DiscPositionKey(string path)
    {
        if (_disc is null) return null;
        foreach (var title in _disc.Titles)
            if (string.Equals(path, _disc.Address(title.Index), StringComparison.OrdinalIgnoreCase))
                return _disc.PositionKey(title.Index);
        return null;
    }
}
