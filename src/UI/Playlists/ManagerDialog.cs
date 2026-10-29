using LunaPlayer.Playlist;
using WxSharp;

namespace LunaPlayer.UI.Playlists;

internal sealed class ManagerDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly ListCtrl _list;
    private readonly List<Button> _selectionButtons = [];
    private readonly List<Button> _actionButtons = [];
    private readonly Action<PlaylistRequest> _handleRequest;
    private bool _busy;
    internal ManagerDialog(IReadOnlyList<Document> documents, Action<PlaylistRequest> handleRequest)
    {
        _handleRequest = handleRequest;
        // A null parent alone lets wxWidgets choose the main window as the native owner.
        _dialog = new Dialog(
            parent: null,
            // Translators: Title of the saved playlist manager.
            title: Tr("My Playlists"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder | DialogStyle.NoParent);
        // Translators: Label above the list of saved playlists in My Playlists.
        var itemsLabel = new StaticText(_dialog, label: Tr("Items"));
        _list = new ListCtrl(_dialog, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection);
        // Translators: Column heading for saved playlist names in the manager.
        _list.InsertColumn(0, Tr("Name"), 240);
        // Translators: Column heading for optional descriptions in the playlist manager.
        _list.InsertColumn(1, Tr("Description"), 400);
        var buttons = new BoxSizer(Orientation.Horizontal);
        // Translators: Playlist manager button opening the selected playlist for playback.
        Add(buttons, Tr("Open"), "open");
        // Translators: Playlist manager button editing the selected saved playlist.
        Add(buttons, Tr("Edit"), "edit");
        // Translators: Playlist manager button creating a new playlist.
        Add(buttons, Tr("New"), "new");
        // Translators: Playlist manager button removing the selected saved playlist.
        Add(buttons, Tr("Remove"), "remove");
        // Translators: Playlist manager button exporting the selected playlist to a file.
        Add(buttons, Tr("Export"), "export");
        // Translators: Button closing the playlist manager.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));
        close.Click += (_, _) => _dialog.Close();
        buttons.Add(close);
        _dialog.SetEscapeId(StandardId.Cancel);
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(itemsLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderTop | SizerFlags.BorderRight, border: 8);
        layout.Add(_list, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        layout.Add(buttons, flags: SizerFlags.All, border: 8);
        _dialog.SetSizer(layout);
        _dialog.Fit();
        _dialog.MinSize = new Size(720, 380);
        _dialog.Center(onParent: false);
        _list.ItemActivated += (_, _) => Select("open");
        _list.ItemSelected += (_, _) => UpdateButtons();
        _list.ItemDeselected += (_, _) => UpdateButtons();
        _dialog.Closing += (_, _) =>
        {
            // Keep this modeless window for reuse; only application shutdown disposes it.
            _dialog.Hide();
        };
        Refresh(documents);
    }
    private void Add(BoxSizer buttons, string label, string action)
    {
        var button = new Button(_dialog, label: label);
        button.Click += (_, _) => Select(action);
        _actionButtons.Add(button);
        if (action != "new") _selectionButtons.Add(button);
        buttons.Add(button, flags: SizerFlags.BorderRight, border: 6);
    }
    private void UpdateButtons()
    {
        var selected = _list.SelectedIndex >= 0;
        foreach (var button in _selectionButtons) button.Show(selected);
        foreach (var button in _actionButtons) button.Enabled = !_busy;
        _dialog.Layout();
    }
    private void Select(string action)
    {
        if (_busy || action != "new" && _list.SelectedIndex < 0) return;
        var request = new PlaylistRequest(action, action == "new" ? Guid.Empty : (Guid)_list.GetItemData(_list.SelectedIndex)!);
        _busy = true;
        UpdateButtons();
        try { _handleRequest(request); }
        finally { _busy = false; UpdateButtons(); }
    }
    internal void Refresh(IReadOnlyList<Document> documents, Guid? selectedId = null)
    {
        var previousIndex = _list.SelectedIndex;
        if (selectedId is null && previousIndex >= 0)
            selectedId = (Guid)_list.GetItemData(previousIndex)!;
        _list.Clear();
        var selectedIndex = -1L;
        foreach (var document in documents)
        {
            var row = _list.AddItem(document.Name);
            _list.SetItem(row, 1, document.Description);
            _list.SetItemData(row, document.Id);
            if (document.Id == selectedId) selectedIndex = row;
        }
        if (selectedIndex < 0 && documents.Count > 0)
            selectedIndex = Math.Clamp(previousIndex, 0, documents.Count - 1);
        if (selectedIndex >= 0)
        {
            _list.SelectedIndex = selectedIndex;
            _list.SetFocused(selectedIndex);
            _list.EnsureVisible(selectedIndex);
        }
        UpdateButtons();
    }
    internal void Show()
    {
        _dialog.Show();
        _dialog.Raise();
        _list.Focus();
    }
    public void Dispose() => _dialog.Dispose();
}
