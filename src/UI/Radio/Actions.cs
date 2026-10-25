using System.Diagnostics;
using LunaPlayer.Radio;
using WxSharp;

namespace LunaPlayer.UI.Radio;

internal sealed partial class BrowserDialog
{
    private MenuItem AddAction(Menu menu, Action action, string label)
    {
        var id = IdManager.NewId();
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => action(), id);
        return menu.Append(id, label);
    }

    private void Play()
    {
        if (Selected is not { } station) return;
        _results.View!.Selected = station.StationUuid;
        _choice = _results.View.Snapshot();
        _dialog.EndModal(StandardId.Ok);
    }

    private void Details()
    {
        if (Selected is not { } station) return;
        using var dialog = new DetailsDialog(_dialog, station);
        dialog.Show();
    }

    private void ToggleFavorite()
    {
        if (_favoriteBusy || Selected is not { } station) return;
        var original = _results;
        var selectedIndex = _selectedRow;
        _favoriteBusy = true;
        Run(_ => _library.ToggleFavoriteAsync(station), added =>
        {
            _favoriteBusy = false;
            if (!_visible)
            {
                _state.Invalidate(CategoryKind.Favorites);
                UpdateFavorite(station.StationUuid);
                return;
            }
            _speak(added
                // Translators: Spoken after saving the selected radio station to favorites.
                ? Tr("Added to favorites.")
                // Translators: Spoken after removing the selected radio station from favorites.
                : Tr("Removed from favorites."));
            if (_results.Category.Kind == CategoryKind.Favorites && ReferenceEquals(_results, original) && !added)
            {
                CancelRead();
                _results.Remove(station.StationUuid);
                _selectedRow = -1;
                _list.UpdateItemCount();
                if (_results.Rows.Count > 0)
                {
                    var next = Math.Clamp(selectedIndex, 0, _results.Rows.Count - 1);
                    _list.SetSelected(next);
                    _list.SetFocused(next);
                }
                UpdateButtons();
            }
            else
            {
                _state.Invalidate(CategoryKind.Favorites);
                if (_results.Category.Kind == CategoryKind.Favorites) Refresh();
            }
            UpdateFavorite(station.StationUuid);
        }, _lifetime.Token, () => _favoriteBusy = false);
    }

    private void UpdateFavorite(Guid id)
    {
        foreach (var source in _sources.Values)
        {
            var index = source.UpdateStation(id, _text.Station);
            if (_visible && ReferenceEquals(source, _results) && index >= 0) _list.RefreshItem(index);
        }
    }

    private void StationMenu()
    {
        if (Selected is not { } station) return;
        _favoriteAction.Label = _library.IsFavorite(station.StationUuid)
            // Translators: Radio station context-menu action removing a saved station.
            ? Tr("Remove from favorites")
            // Translators: Radio station context-menu action saving a station.
            : Tr("Add to favorites");
        _favoriteAction.Enabled = !_favoriteBusy;
        _websiteAction.Enabled = Catalog.HttpUrl(station.Homepage) is not null;
        _copyUrlAction.Enabled = Catalog.StreamUrl(station) is not null;
        _list.PopupMenu(_actions);
    }

    private void OpenWebsite()
    {
        if (Selected is not { } station || Catalog.HttpUrl(station.Homepage) is not { } url) return;
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // Translators: Title of a failure to open a radio station's website in the user's browser.
            Wx.MessageBox(Catalog.DescribeError(error), Tr("Radio error"), MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);
        }
    }

    private void CopyUrl()
    {
        if (Selected is { } station && Catalog.StreamUrl(station) is { } url) Copy(url);
    }

    private void CopyName() { if (Selected is { } station) Copy(station.Name); }

    private void Copy(string value)
    {
        _speak(_clipboard.SetText(value)
            // Translators: Spoken after a radio station name or stream URL was copied to the clipboard.
            ? Tr("Copied to clipboard.")
            // Translators: Spoken when a radio station name or stream URL could not be copied.
            : Tr("Unable to copy to clipboard."));
    }

    private void Vote()
    {
        if (_voting || Selected is not { } station) return;
        _voting = true;
        Run(token => _catalog.Client.VoteAsync(station.StationUuid, token), _ =>
        {
            _voting = false;
            // Translators: Spoken confirmation after Radio Browser accepted a vote for the selected station.
            if (_visible) _speak(Tr("Vote submitted."));
        }, _lifetime.Token, () => _voting = false);
    }

    private void ClearHistory()
    {
        if (Wx.MessageBox(
            // Translators: Confirmation before deleting this user's local radio playback history.
            Tr("Clear all recently played radio stations?"),
            // Translators: Title of the radio listening-history deletion confirmation.
            Tr("Clear history"), MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, _dialog) != MessageBoxStyle.Yes) return;
        Run(async _ => { await _library.ClearHistoryAsync().ConfigureAwait(false); return true; }, _ =>
        {
            _state.Invalidate(CategoryKind.Recent);
            if (!_visible) return;
            // Translators: Spoken confirmation after deleting the local radio listening history.
            _speak(Tr("Radio history cleared."));
            if (_results.Category.Kind == CategoryKind.Recent) Refresh();
        }, _lifetime.Token);
    }

}
