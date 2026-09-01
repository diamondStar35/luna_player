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
        // Translators: The name a new bookmark is given unless the user types another one. {time} is the point in the
        // file the bookmark was made at, such as 00:01:20.
        var defaultName = TrFormat("Bookmark {time}", PlaybackTimeFormatter.Format(position) ?? "00:00:00");
        var name = _view.PromptText(
            // Translators: Asks the user what the new bookmark should be called.
            Tr("Enter bookmark name"), Tr("Add a new bookmark"), defaultName);
        if (name is null)
            return;
        name = name.Trim();
        if (name.Length == 0)
        {
            _view.ShowWarning(
                // Translators: Shown when the user confirms a bookmark name without typing anything.
                Tr("Bookmark name cannot be empty."), Tr("Bookmarks"));
            return;
        }
        _store.Add(path, name, position);
        _view.ShowInfo(
            // Translators: Shown once a bookmark has been made. {name} is what it is called and {time} the point in the
            // file it was made at, such as 00:01:20.
            TrFormat("Bookmark '{name}' added at {time}.", name, PlaybackTimeFormatter.Format(position)),
            // Translators: Title of the message shown once a bookmark has been made.
            Tr("Success"));
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
                _view.ShowInfo(
                    // Translators: Shown when the user opens the bookmark list but the current file has none.
                    Tr("No bookmarks found for the current file."), Tr("Bookmarks"));
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
                    if (_view.Confirm(
                        // Translators: Asks the user to confirm removing one bookmark. {name} is what the bookmark is called.
                        TrFormat("Delete bookmark '{name}'?", bookmark.Name),
                        // Translators: Title of the window that asks the user to confirm removing a bookmark.
                        Tr("Confirm delete")))
                    {
                        _store.Delete(path, bookmark.Id);
                        if (_store.ListFor(path).Count == 0)
                        {
                            _view.ShowInfo(
                                // Translators: Shown once the last bookmark of the current file has been removed.
                                Tr("All bookmarks for this file were removed."), Tr("Bookmarks"));
                            return;
                        }
                    }
                    break;
            }
        }
    }

    private void Rename(string path, Bookmark bookmark)
    {
        var name = _view.PromptText(
            // Translators: Asks the user for the new name of a bookmark they are renaming.
            Tr("Edit bookmark name"), Tr("Manage bookmarks"), bookmark.Name);
        if (name is null)
            return;
        name = name.Trim();
        if (name.Length == 0)
        {
            _view.ShowWarning(Tr("Bookmark name cannot be empty."),
                // Translators: Title of a message telling the user that something went wrong.
                Tr("Error"));
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
            _speech.Speak(
                // Translators: Spoken when the user asks for one of the ten numbered bookmark slots but nothing is saved
                // in it. {slot} is the slot number, 1 to 10.
                TrFormat("No bookmark in slot {slot}.", slot),
                // Translators: The short wording spoken when a numbered bookmark slot is empty. {slot} is the slot number, 1 to 10.
                TrFormat("No bookmark {slot}.", slot));
            return;
        }
        Jump(bookmark);
    }

    private void Jump(Bookmark bookmark)
    {
        _player.SeekAbsolute(bookmark.Position);
        _speech.Speak(
            // Translators: Spoken once playing has moved to a bookmark. {name} is what the bookmark is called.
            TrFormat("Jumped to bookmark {name}.", bookmark.Name),
            // Translators: The short wording spoken once playing has moved to a bookmark. {name} is what the bookmark is
            // called and {time} the point in the file it sits at, such as 00:01:20.
            TrFormat("Jumped to '{name}' at {time}.", bookmark.Name, PlaybackTimeFormatter.Format(bookmark.Position)));
    }

    private bool TryGetContext(out string path, out double position)
    {
        path = _player.CurrentPath ?? string.Empty;
        position = Math.Max(0, _player.Elapsed ?? 0);
        if (path.Length > 0 && File.Exists(path))
            return true;
        _speech.Speak(
            // Translators: Spoken when a command needs to know where playing has got to but the player cannot tell.
            Tr("Time is not available for the current file."),
            // Translators: The short wording spoken when the player cannot tell where playing has got to.
            Tr("Time unavailable."));
        return false;
    }

    private static BookmarkListItem ToListItem(Bookmark bookmark)
        => new(bookmark.Id, bookmark.Name, PlaybackTimeFormatter.Format(bookmark.Position) ?? "00:00");
}
