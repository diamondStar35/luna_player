using WxSharp;

namespace LunaPlayer.UI.Iptv;

/// <summary>The window listing the IPTV sources the user has saved.</summary>
/// <remarks>
/// A near-twin of the favourites window: Add is the one button that works with nothing selected, and the
/// caller reopens this window after every add, edit or removal, handing back the id it last touched so the
/// list comes up on the same row rather than at the top.
/// </remarks>
internal sealed class SourcesDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly ListCtrl _list;
    private readonly Button _open;
    private readonly Button _edit;
    private readonly Button _remove;
    private IptvSourceAction? _action;

    internal SourcesDialog(Window parent, IReadOnlyList<IptvSourceListItem> sources, string selectedId)
    {
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window listing the IPTV sources the user has saved.
            title: Tr("IPTV sources"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _list = new ListCtrl(_dialog, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection);
        // Translators: Heading of the IPTV sources list column holding what each source is called.
        _list.InsertColumn(0, Tr("Name"), 200);
        // Translators: Heading of the IPTV sources list column saying what kind of source each one is.
        _list.InsertColumn(1, Tr("Type"), 140);
        // Translators: Heading of the IPTV sources list column holding the server or file each source points at.
        _list.InsertColumn(2, Tr("Address"), 340);
        var selectedRow = 0;
        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            var row = _list.AddItem(source.Name);
            _list.SetItem(row, 1, source.Type);
            _list.SetItem(row, 2, source.Detail);
            _list.SetItemData(row, source.Id);
            if (source.Id == selectedId)
                selectedRow = index;
        }
        if (sources.Count > 0)
        {
            _list.SelectedIndex = selectedRow;
            _list.SetFocused(selectedRow);
            _list.EnsureVisible(selectedRow);
        }

        // Translators: Button that opens the source chosen in the list and browses its channels.
        _open = ActionButton(Tr("Open"), IptvSourceAction.Open);
        // Translators: Button that saves a new IPTV source. The three dots mean it opens a window to type it in.
        var add = ActionButton(Tr("Add..."), IptvSourceAction.Add);
        // Translators: Button that changes a saved IPTV source. The three dots mean it opens a window to change it in.
        _edit = ActionButton(Tr("Edit..."), IptvSourceAction.Edit);
        // Translators: Button that removes a saved IPTV source. The three dots mean it asks first.
        _remove = ActionButton(Tr("Remove..."), IptvSourceAction.Remove);
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
        _dialog.MinSize = new Size(760, 380);
        _dialog.Center(onParent: true);

        _list.ItemActivated += (_, _) => End(IptvSourceAction.Open);
        _list.ItemSelected += (_, _) => SyncButtons();
        _list.ItemDeselected += (_, _) => SyncButtons();
        SyncButtons();
        _list.Focus();
    }

    internal IptvSourceRequest? Show()
    {
        _dialog.ShowModal();
        if (_action is not IptvSourceAction action)
            return null;
        // Adding needs no row, so it is the one action that answers without one.
        if (action == IptvSourceAction.Add)
            return new IptvSourceRequest(action, string.Empty);
        if (_list.SelectedIndex < 0)
            return null;
        return _list.GetItemData(_list.SelectedIndex) is string id ? new IptvSourceRequest(action, id) : null;
    }

    public void Dispose() => _dialog.Dispose();

    private Button ActionButton(string label, IptvSourceAction action)
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

    private void End(IptvSourceAction action)
    {
        if (action != IptvSourceAction.Add && _list.SelectedIndex < 0)
            return;
        _action = action;
        _dialog.EndModal(StandardId.Ok);
    }
}
