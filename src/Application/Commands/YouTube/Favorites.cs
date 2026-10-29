using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Favorites;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.YouTube;
using LunaPlayer.YouTube.Playback;

namespace LunaPlayer.Application.Commands.YouTube;

/// <summary>The window of saved links, and what opening one does.</summary>
///
/// <remarks>Generic streams open directly; YouTube favorites are handed to the YouTube session manager.</remarks>
internal sealed class Favorites
{
    private readonly IMainView _view;
    private readonly Player _player;
    private readonly ISpeechOutput _speech;
    private readonly FavoriteStore _store;
    private readonly Sessions _sessions;

    internal Favorites(
        ActionRouter router,
        IMainView view,
        Player player,
        ISpeechOutput speech,
        FavoriteStore store,
        Sessions sessions)
    {
        _view = view;
        _player = player;
        _speech = speech;
        _store = store;
        _sessions = sessions;
        router.Register(ActionId.OpenFavorites, Manage);
    }

    /// <remarks>
    /// The window is reopened after each change, carrying the id it last dealt with so the list comes back
    /// on the same row. Opening a favourite successfully is the only thing that closes it for good.
    /// </remarks>
    private void Manage()
    {
        var selectedId = string.Empty;
        while (true)
        {
            var favorites = List();
            if (ReportStoreFailure())
                return;
            var request = _view.Dialogs.ManageFavorites(favorites, selectedId);
            if (request is not LunaPlayer.UI.YouTube.FavoriteRequest chosen)
                return;
            selectedId = chosen.Id;
            switch (chosen.Action)
            {
                case LunaPlayer.UI.YouTube.FavoriteAction.Open:
                    if (Open(chosen.Id))
                        return;
                    break;
                case LunaPlayer.UI.YouTube.FavoriteAction.Add:
                    selectedId = Add() ?? selectedId;
                    break;
                case LunaPlayer.UI.YouTube.FavoriteAction.Edit:
                    Edit(chosen.Id);
                    break;
                case LunaPlayer.UI.YouTube.FavoriteAction.Remove:
                    if (Remove(chosen.Id))
                        selectedId = string.Empty;
                    break;
            }
        }
    }

    private IReadOnlyList<LunaPlayer.UI.YouTube.FavoriteListItem> List()
        => [.. _store.ListAll().Select(favorite =>
            new LunaPlayer.UI.YouTube.FavoriteListItem(favorite.Id, favorite.Name, FavoriteStore.Describe(favorite.Kind), favorite.Link))];

    /// <summary>Plays what a saved link points at. True when the window should close behind it.</summary>
    private bool Open(string id)
    {
        if (_store.Get(id) is not Favorite favorite)
            return false;
        if (favorite.Kind == FavoriteKind.Stream)
        {
            if (_player.OpenStream(favorite.Link))
                return true;
            _view.Dialogs.ShowError(
                // Translators: Shown when the address saved as a plain stream could not be played.
                Tr("Could not open stream link."));
            return false;
        }
        // Opening reports for itself, in its own words and its own progress window, so there is nothing
        // left here to refuse: the caller only needs to know the window may close.
        if (KindOf(favorite.Kind) is LinkKind.Playlist)
            _sessions.OpenPlaylist(favorite.Link);
        else
            _sessions.PlayLink(favorite.Link);
        return true;
    }

    private static LinkKind KindOf(FavoriteKind kind) => kind switch
    {
        FavoriteKind.Playlist => LinkKind.Playlist,
        // A combined favourite was saved because it holds both; the video is the half that plays.
        _ => LinkKind.Video,
    };

    /// <summary>The id of what was saved, or null when nothing was.</summary>
    private string? Add()
    {
        var draft = _view.Dialogs.EditFavorite(
            // Translators: Title of the window for saving a new link.
            Tr("Add favorite"),
            new LunaPlayer.UI.YouTube.FavoriteDraft(string.Empty, FavoriteKind.Video, string.Empty));
        if (draft is not LunaPlayer.UI.YouTube.FavoriteDraft value)
            return null;
        var added = _store.Add(value.Name, value.Kind, value.Link);
        if (added is not null)
            return added.Id;
        ReportStoreFailure();
        return null;
    }

    private void Edit(string id)
    {
        if (_store.Get(id) is not Favorite favorite)
            return;
        var draft = _view.Dialogs.EditFavorite(
            // Translators: Title of the window for changing a link already saved.
            Tr("Edit favorite"),
            new LunaPlayer.UI.YouTube.FavoriteDraft(favorite.Name, favorite.Kind, favorite.Link));
        if (draft is not LunaPlayer.UI.YouTube.FavoriteDraft value)
            return;
        if (!_store.Update(id, value.Name, value.Kind, value.Link))
            ReportStoreFailure();
    }

    private bool Remove(string id)
    {
        if (_store.Get(id) is not Favorite favorite)
            return false;
        if (!_view.Dialogs.Confirm(
            // Translators: Asks the user to confirm removing one saved link. {name} is what it is called and
            // {kind} is what sort of thing it points at.
            TrFormat("Remove favorite '{name}' ({kind})?", favorite.Name, FavoriteStore.Describe(favorite.Kind))))
            return false;
        if (_store.Delete(id))
            return true;
        ReportStoreFailure();
        return false;
    }

    /// <summary>Reports why the store refused. It has already worded the reason.</summary>
    private bool ReportStoreFailure()
    {
        if (_store.LastError.Length == 0)
            return false;
        _view.Dialogs.ShowError(
            _store.LastError);
        return true;
    }
}
