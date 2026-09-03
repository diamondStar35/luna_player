using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>The window listing the links the user has saved.</summary>
/// <remarks>
/// Add is the one button that works with nothing selected, so it is the one button that is not tied to
/// the selection. The caller reopens this window after every add, edit or removal, handing back the id it
/// last touched so the list comes up on the same row rather than at the top.
/// </remarks>
internal sealed class FavoritesDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly ListCtrl _list;
    private readonly Button _open;
    private readonly Button _edit;
    private readonly Button _remove;
    private FavoriteAction? _action;

    internal FavoritesDialog(Window parent, IReadOnlyList<FavoriteListItem> favorites, string selectedId)
    {
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window listing the YouTube links and streams the user has saved.
            title: Tr("Favorite videos"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _list = new ListCtrl(_dialog, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection);
        // Translators: Heading of the favourites list column holding what each saved link is called.
        _list.InsertColumn(0, Tr("Name"), 180);
        // Translators: Heading of the favourites list column saying what kind of thing each saved link points at.
        _list.InsertColumn(1, Tr("Type"), 140);
        // Translators: Heading of the favourites list column holding the address of each saved link.
        _list.InsertColumn(2, Tr("Link"), 340);
        var selectedRow = 0;
        for (var index = 0; index < favorites.Count; index++)
        {
            var favorite = favorites[index];
            var row = _list.AddItem(favorite.Name);
            _list.SetItem(row, 1, favorite.Type);
            _list.SetItem(row, 2, favorite.Link);
            _list.SetItemData(row, favorite.Id);
            if (favorite.Id == selectedId)
                selectedRow = index;
        }
        if (favorites.Count > 0)
        {
            _list.SelectedIndex = selectedRow;
            _list.SetFocused(selectedRow);
            _list.EnsureVisible(selectedRow);
        }

        // Translators: Button that plays the saved link chosen in the list.
        _open = ActionButton(Tr("Open"), FavoriteAction.Open);
        // Translators: Button that saves a new link. The three dots mean it opens a window to type it in.
        var add = ActionButton(Tr("Add..."), FavoriteAction.Add);
        // Translators: Button that changes a saved link. The three dots mean it opens a window to change it in.
        _edit = ActionButton(Tr("Edit..."), FavoriteAction.Edit);
        // Translators: Button that removes a saved link. The three dots mean it asks first.
        _remove = ActionButton(Tr("Remove..."), FavoriteAction.Remove);
        _open.SetDefault();
        // Translators: The button that closes a window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(_open, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(add, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_edit, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_remove);
        buttons.AddStretchSpacer();
        buttons.Add(close);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        sizer.Add(buttons, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(740, 360);
        _dialog.Center(onParent: true);

        _list.ItemActivated += (_, _) => End(FavoriteAction.Open);
        _list.ItemSelected += (_, _) => SyncButtons();
        _list.ItemDeselected += (_, _) => SyncButtons();
        SyncButtons();
        _list.Focus();
    }

    internal FavoriteRequest? Show()
    {
        _dialog.ShowModal();
        if (_action is not FavoriteAction action)
            return null;
        // Adding needs no row, so it is the one action that answers without one.
        if (action == FavoriteAction.Add)
            return new FavoriteRequest(action, string.Empty);
        if (_list.SelectedIndex < 0)
            return null;
        return _list.GetItemData(_list.SelectedIndex) is string id ? new FavoriteRequest(action, id) : null;
    }

    public void Dispose() => _dialog.Dispose();

    private Button ActionButton(string label, FavoriteAction action)
    {
        var button = new Button(_dialog, label: label);
        button.Click += (_, _) => End(action);
        return button;
    }

    private void SyncButtons()
    {
        var enabled = _list.SelectedIndex >= 0;
        _open.Enabled = enabled;
        _edit.Enabled = enabled;
        _remove.Enabled = enabled;
    }

    private void End(FavoriteAction action)
    {
        if (action != FavoriteAction.Add && _list.SelectedIndex < 0)
            return;
        _action = action;
        _dialog.EndModal(StandardId.Ok);
    }
}
