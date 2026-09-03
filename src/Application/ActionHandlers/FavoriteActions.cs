using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Favorites;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;
using LunaPlayer.YouTube;

namespace LunaPlayer.Application.ActionHandlers;

/// <summary>The window of saved links, and what opening one does.</summary>
///
/// <remarks>
/// The one kind that works today is the plain stream: it needs nothing from YouTube and goes straight to
/// the player. The three YouTube kinds are refused until the resolver is written, which is why the store
/// keeps them apart rather than treating every saved link as an address to hand to mpv.
/// </remarks>
internal sealed class FavoriteActions
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly ISpeechOutput _speech;
    private readonly FavoriteStore _store;
    private readonly YouTubeSessions _sessions;

    internal FavoriteActions(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        ISpeechOutput speech,
        FavoriteStore store,
        YouTubeSessions sessions)
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
            var request = _view.ManageFavorites(List(), selectedId);
            if (request is not FavoriteRequest chosen)
                return;
            selectedId = chosen.Id;
            switch (chosen.Action)
            {
                case FavoriteAction.Open:
                    if (Open(chosen.Id))
                        return;
                    break;
                case FavoriteAction.Add:
                    selectedId = Add() ?? selectedId;
                    break;
                case FavoriteAction.Edit:
                    Edit(chosen.Id);
                    break;
                case FavoriteAction.Remove:
                    if (Remove(chosen.Id))
                        selectedId = string.Empty;
                    break;
            }
        }
    }

    private IReadOnlyList<FavoriteListItem> List()
        => [.. _store.ListAll().Select(favorite =>
            new FavoriteListItem(favorite.Id, favorite.Name, FavoriteStore.Describe(favorite.Kind), favorite.Link))];

    /// <summary>Plays what a saved link points at. True when the window should close behind it.</summary>
    private bool Open(string id)
    {
        if (_store.Get(id) is not Favorite favorite)
            return false;
        if (favorite.Kind == FavoriteKind.Stream)
        {
            if (_player.OpenStream(favorite.Link))
                return true;
            _view.ShowError(
                // Translators: Shown when the address saved as a plain stream could not be played.
                Tr("Could not open stream link."),
                // Translators: Title of the messages shown about the window of saved links.
                Tr("Favorite videos"));
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
        var draft = _view.EditFavorite(
            // Translators: Title of the window for saving a new link.
            Tr("Add favorite"),
            new FavoriteDraft(string.Empty, FavoriteKind.Video, string.Empty));
        if (draft is not FavoriteDraft value)
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
        var draft = _view.EditFavorite(
            // Translators: Title of the window for changing a link already saved.
            Tr("Edit favorite"),
            new FavoriteDraft(favorite.Name, favorite.Kind, favorite.Link));
        if (draft is not FavoriteDraft value)
            return;
        if (!_store.Update(id, value.Name, value.Kind, value.Link))
            ReportStoreFailure();
    }

    private bool Remove(string id)
    {
        if (_store.Get(id) is not Favorite favorite)
            return false;
        if (!_view.Confirm(
            // Translators: Asks the user to confirm removing one saved link. {name} is what it is called and
            // {kind} is what sort of thing it points at.
            TrFormat("Remove favorite '{name}' ({kind})?", favorite.Name, FavoriteStore.Describe(favorite.Kind)),
            // Translators: Title of the window that asks the user to confirm removing a saved link.
            Tr("Confirm remove")))
            return false;
        if (_store.Delete(id))
            return true;
        ReportStoreFailure();
        return false;
    }

    /// <summary>Reports why the store refused. It has already worded the reason.</summary>
    private void ReportStoreFailure()
    {
        if (_store.LastError.Length > 0)
            _view.ShowError(_store.LastError, Tr("Favorite videos"));
    }
}
