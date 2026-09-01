using WxSharp;

namespace LunaPlayer.UI;

internal sealed class BookmarkManagerDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly ListCtrl _list;
    private readonly Button _jump;
    private readonly Button _edit;
    private readonly Button _delete;
    private BookmarkManagementAction? _action;

    internal BookmarkManagerDialog(Window parent, IReadOnlyList<BookmarkListItem> bookmarks)
    {
        _dialog = new Dialog(parent, title: Tr("Manage bookmarks"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _list = new ListCtrl(_dialog, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection);
        // Translators: Heading of the bookmark list column holding what each bookmark is called.
        _list.InsertColumn(0, Tr("Name"), 260);
        // Translators: Heading of the bookmark list column holding the point in the file each bookmark sits at.
        _list.InsertColumn(1, Tr("Position"), 120);
        foreach (var bookmark in bookmarks)
        {
            var row = _list.AddItem(bookmark.Name);
            _list.SetItem(row, 1, bookmark.Position);
            _list.SetItemData(row, bookmark.Id);
        }
        if (bookmarks.Count > 0)
        {
            _list.SelectedIndex = 0;
            _list.SetFocused(0);
        }

        // Translators: Button that moves playing to the chosen bookmark.
        _jump = ActionButton(Tr("Jump"), BookmarkManagementAction.Jump);
        _edit = ActionButton(Tr("Edit"), BookmarkManagementAction.Rename);
        _delete = ActionButton(Tr("Delete"), BookmarkManagementAction.Delete);
        _jump.SetDefault();
        // Translators: The button that closes a window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(_jump, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_edit, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_delete);
        buttons.AddStretchSpacer();
        buttons.Add(close);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        sizer.Add(buttons, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(500, 320);
        _dialog.Center(onParent: true);

        _list.ItemActivated += (_, _) => End(BookmarkManagementAction.Jump);
        _list.ItemSelected += (_, _) => SyncButtons();
        _list.ItemDeselected += (_, _) => SyncButtons();
        SyncButtons();
    }

    internal BookmarkManagementRequest? Show()
    {
        _dialog.ShowModal();
        if (_action is not BookmarkManagementAction action || _list.SelectedIndex < 0)
            return null;
        return _list.GetItemData(_list.SelectedIndex) is string id
            ? new BookmarkManagementRequest(action, id)
            : null;
    }

    public void Dispose() => _dialog.Dispose();

    private Button ActionButton(string label, BookmarkManagementAction action)
    {
        var button = new Button(_dialog, label: label);
        button.Click += (_, _) => End(action);
        return button;
    }

    private void SyncButtons()
    {
        var enabled = _list.SelectedIndex >= 0;
        _jump.Enabled = enabled;
        _edit.Enabled = enabled;
        _delete.Enabled = enabled;
    }

    private void End(BookmarkManagementAction action)
    {
        if (_list.SelectedIndex < 0)
            return;
        _action = action;
        _dialog.EndModal(StandardId.Ok);
    }
}
