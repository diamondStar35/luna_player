using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Bookmarks;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.ActionHandlers;

internal sealed class BookmarkActions
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly ISpeechOutput _speech;
    private readonly BookmarkStore _store;

    internal BookmarkActions(ActionRouter router, IMainView view, MediaPlayer player, ISpeechOutput speech, BookmarkStore store)
    {
        _view = view;
        _player = player;
        _speech = speech;
        _store = store;
        router.Register(ActionId.AddBookmark, Add);
        router.Register(ActionId.ManageBookmarks, Manage);
        foreach (var slot in BookmarkActionDefinitions.Slots)
            router.Register(slot.Id, () => JumpToSlot(slot.Slot));
    }

    private void Add()
    {
        if (!TryGetContext(out var path, out var position))
            return;
        var defaultName = $"Bookmark {PlaybackTimeFormatter.Format(position)}";
        var name = _view.PromptText("Enter bookmark name", "Add a new bookmark", defaultName);
        if (name is null)
            return;
        name = name.Trim();
        if (name.Length == 0)
        {
            _view.ShowWarning("Bookmark name cannot be empty.", "Bookmarks");
            return;
        }
        _store.Add(path, name, position);
        _view.ShowInfo($"Bookmark '{name}' added at {PlaybackTimeFormatter.Format(position)}.", "Success");
    }

    private void Manage()
    {
        var path = _player.CurrentPath;
        if (path is null || !File.Exists(path))
            return;
        while (true)
        {
            var bookmarks = _store.ListFor(path);
            if (bookmarks.Count == 0)
            {
                _view.ShowInfo("No bookmarks found for the current file.", "Bookmarks");
                return;
            }
            var request = _view.ManageBookmarks(bookmarks.Select(ToListItem).ToArray());
            if (request is null)
                return;
            var bookmark = bookmarks.FirstOrDefault(value => value.Id == request.Value.Id);
            if (bookmark is null)
                continue;
            switch (request.Value.Action)
            {
                case BookmarkManagementAction.Jump:
                    Jump(bookmark);
                    return;
                case BookmarkManagementAction.Rename:
                    Rename(path, bookmark);
                    break;
                case BookmarkManagementAction.Delete:
                    if (_view.Confirm($"Delete bookmark '{bookmark.Name}'?", "Confirm delete"))
                    {
                        _store.Delete(path, bookmark.Id);
                        if (_store.ListFor(path).Count == 0)
                        {
                            _view.ShowInfo("All bookmarks for this file were removed.", "Bookmarks");
                            return;
                        }
                    }
                    break;
            }
        }
    }

    private void Rename(string path, Bookmark bookmark)
    {
        var name = _view.PromptText("Edit bookmark name", "Manage bookmarks", bookmark.Name);
        if (name is null)
            return;
        name = name.Trim();
        if (name.Length == 0)
        {
            _view.ShowWarning("Bookmark name cannot be empty.", "Error");
            return;
        }
        _store.Rename(path, bookmark.Id, name);
    }

    private void JumpToSlot(int slot)
    {
        var path = _player.CurrentPath;
        var bookmark = path is null ? null : _store.Slot(path, slot);
        if (bookmark is null)
        {
            _speech.Speak($"No bookmark in slot {slot}.", $"No bookmark {slot}.");
            return;
        }
        Jump(bookmark);
    }

    private void Jump(Bookmark bookmark)
    {
        _player.SeekAbsolute(bookmark.Position);
        _speech.Speak($"Jumped to bookmark {bookmark.Name}.", $"Jumped to '{bookmark.Name}' at {PlaybackTimeFormatter.Format(bookmark.Position)}.");
    }

    private bool TryGetContext(out string path, out double position)
    {
        path = _player.CurrentPath ?? string.Empty;
        position = Math.Max(0, _player.Elapsed ?? 0);
        if (path.Length > 0 && File.Exists(path))
            return true;
        _speech.Speak("Time is not available for the current file.", "Time unavailable.");
        return false;
    }

    private static BookmarkListItem ToListItem(Bookmark bookmark)
        => new(bookmark.Id, bookmark.Name, PlaybackTimeFormatter.Format(bookmark.Position) ?? "00:00");
}
