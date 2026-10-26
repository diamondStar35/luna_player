using LunaPlayer.Media;
using LunaPlayer.Playback;

namespace LunaPlayer.Application.Presentation;

/// <summary>Supplies live radio tags and caches local file headers for the title bar and media overlay.</summary>
///
/// <remarks>
/// The overlay runs on a clock while a file is open, and reading a file from disk every second to learn
/// something that cannot have changed would be a waste; the path the tags came from is kept so the read
/// happens only when it moves on. A stream has no header on disk to read; its playback owner supplies
/// live broadcast or catalog metadata instead.
/// </remarks>
internal sealed class MediaTagsCache
{
    private string? _path;
    private MediaTags _tags = MediaTags.None;

    internal MediaTags For(Player player)
    {
        var tags = player.InRadioSession ? player.CurrentBroadcast : ForFile(player.CurrentPath);
        return player.AudiobookMode && player.CurrentChapter is Chapter chapter
            ? tags with { Title = Player.ChapterTitle(chapter) } : tags;
    }

    private MediaTags ForFile(string? path)
    {
        if (string.IsNullOrEmpty(path) || LinkValidator.IsHttpUrl(path))
        {
            _path = null;
            _tags = MediaTags.None;
        }
        else if (!string.Equals(path, _path, StringComparison.Ordinal))
        {
            _path = path;
            _tags = MediaHeader.ReadTags(path);
        }
        return _tags;
    }
}
