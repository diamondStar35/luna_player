using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>The window listing the links the user has saved.</summary>
/// <remarks>
/// Changes refresh the retained window so the selection survives add, edit and remove.
/// </remarks>
internal sealed class FavoritesDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly ListCtrl _list;
    private readonly Button _open;
    private readonly Button _edit;
    private readonly Button _remove;
    private readonly Button _add;
    private readonly Action<FavoriteRequest> _handleRequest;
    private bool _busy;
    private bool _disposed;

    internal FavoritesDialog(IReadOnlyList<FavoriteListItem> favorites, Action<FavoriteRequest> handleRequest)
    {
        _handleRequest = handleRequest;
        _dialog = new Dialog(
            parent: null,
            // Translators: Title of the window listing the YouTube links and streams the user has saved.
            title: Tr("Favorite videos"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder | DialogStyle.NoParent);
        _list = new ListCtrl(_dialog, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection);
        // Translators: Heading of the favourites list column holding what each saved link is called.
        _list.InsertColumn(0, Tr("Name"), 180);
        // Translators: Heading of the favourites list column saying what kind of thing each saved link points at.
        _list.InsertColumn(1, Tr("Type"), 140);
        // Translators: Heading of the favourites list column holding the address of each saved link.
        _list.InsertColumn(2, Tr("Link"), 340);

        // Translators: Button that plays the saved link chosen in the list.
        _open = ActionButton(Tr("Open"), FavoriteAction.Open);
        // Translators: Button that saves a new link. The three dots mean it opens a window to type it in.
        _add = ActionButton(Tr("Add..."), FavoriteAction.Add);
        // Translators: Button that changes a saved link. The three dots mean it opens a window to change it in.
        _edit = ActionButton(Tr("Edit..."), FavoriteAction.Edit);
        // Translators: Button that removes a saved link. The three dots mean it asks first.
        _remove = ActionButton(Tr("Remove..."), FavoriteAction.Remove);
        _open.SetDefault();
        // Translators: The button that closes a window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(_open, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_add, flags: SizerFlags.BorderRight, border: 6);
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
        _dialog.Center(onParent: false);
        close.Click += (_, _) => Hide();
        _dialog.Closing += (_, args) =>
        {
            args.Veto();
            Hide();
        };

        _list.ItemActivated += (_, _) => Select(FavoriteAction.Open);
        _list.ItemSelected += (_, _) => SyncButtons();
        _list.ItemDeselected += (_, _) => SyncButtons();
        Refresh(favorites);
        _list.Focus();
    }

    internal void Show()
    {
        _dialog.Show();
        _dialog.Raise();
        _list.Focus();
    }

    internal void Hide() => _dialog.Hide();

    internal void Refresh(IReadOnlyList<FavoriteListItem> favorites, string? selectedId = null)
    {
        var previous = _list.SelectedIndex;
        if (selectedId is null && previous >= 0)
            selectedId = _list.GetItemData(previous) as string;
        _list.Clear();
        long selected = -1;
        foreach (var favorite in favorites)
        {
            var row = _list.AddItem(favorite.Name);
            _list.SetItem(row, 1, favorite.Type);
            _list.SetItem(row, 2, favorite.Link);
            _list.SetItemData(row, favorite.Id);
            if (favorite.Id == selectedId) selected = row;
        }
        if (selected < 0 && favorites.Count > 0)
            selected = Math.Clamp(previous, 0, favorites.Count - 1);
        if (selected >= 0)
        {
            _list.SelectedIndex = selected;
            _list.SetFocused(selected);
            _list.EnsureVisible(selected);
        }
        SyncButtons();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _dialog.Dispose();
    }

    private Button ActionButton(string label, FavoriteAction action)
    {
        var button = new Button(_dialog, label: label);
        button.Click += (_, _) => Select(action);
        return button;
    }

    private void SyncButtons()
    {
        if (_disposed) return;
        var enabled = !_busy && _list.SelectedIndex >= 0;
        _add.Enabled = !_busy;
        _open.Enabled = enabled;
        _edit.Enabled = enabled;
        _remove.Enabled = enabled;
    }

    private void Select(FavoriteAction action)
    {
        if (_busy || action != FavoriteAction.Add && _list.SelectedIndex < 0)
            return;
        var id = action == FavoriteAction.Add ? string.Empty : (string)_list.GetItemData(_list.SelectedIndex)!;
        _busy = true;
        SyncButtons();
        try { _handleRequest(new FavoriteRequest(action, id)); }
        finally { _busy = false; SyncButtons(); }
    }
}
