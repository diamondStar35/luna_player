using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Presentation;

/// <summary>Puts what is playing, and whether it is paused, into the window title when the general setting
/// asks for it, and otherwise leaves the plain program name there.</summary>
///
/// <remarks>
/// A screen reader announces the foreground window whenever its title changes, so this speaks over the
/// player on every track change and pause. That is why the setting is off by default and why nothing here
/// tries to suppress the announcement - some users want the title to follow playback and accept the cost.
/// The setting is read each time, so turning it off takes the title back to the program name while the
/// player runs.
/// </remarks>
internal sealed class WindowTitlePresenter
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly PlayerSettings _settings;
    private readonly MediaTagsCache _tags;

    internal WindowTitlePresenter(IMainView view, MediaPlayer player, PlayerSettings settings, MediaTagsCache tags)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _tags = tags;
    }

    internal void Update()
        => _view.SetWindowTitle(_settings.General.SpeakWindowTitle ? Compose() : AppInfo.Name);

    /// <summary>The window title for the current state: the plain program name when nothing is open, the
    /// track's title otherwise, marked as paused when it is.</summary>
    private string Compose()
    {
        var path = _player.CurrentPath;
        if (string.IsNullOrEmpty(path))
            return AppInfo.Name;
        var tags = _tags.For(path);
        // The same title the overlay shows: what the media calls itself, falling back to the name on disk.
        var title = tags.Title.Length > 0 ? tags.Title : _player.CurrentDisplayName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
            return AppInfo.Name;
        // Loaded but not playing is paused; stopped unloads mpv and leaves the entry, which is not the same
        // thing and reads as the plain title.
        return _player.IsLoaded && !_player.IsPlaying
            // Translators: Window title while playback is paused. {title} is the track, {app} the program name.
            ? TrFormat("[Paused]: {title} - {app}", title, AppInfo.Name)
            // Translators: Window title while a track is open. {title} is the track, {app} the program name.
            : TrFormat("{title} - {app}", title, AppInfo.Name);
    }
}
