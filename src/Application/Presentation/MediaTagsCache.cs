using LunaPlayer.Media;

namespace LunaPlayer.Application.Presentation;

/// <summary>What the current file says about itself, read once per file rather than once per use.</summary>
///
/// <remarks>
/// The overlay runs on a clock while a file is open, and reading a file from disk every second to learn
/// something that cannot have changed would be a waste; the path the tags came from is kept so the read
/// happens only when it moves on. A stream has no header on disk to read, so it is not asked for one - the
/// caller simply shows the name and no artist, which is honest about what is known.
/// </remarks>
internal sealed class MediaTagsCache
{
    private string? _path;
    private MediaTags _tags = MediaTags.None;

    internal MediaTags For(string? path)
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
