using LunaPlayer.Playlist;
using WxSharp;

namespace LunaPlayer.UI.Playlists;

internal sealed class ManagerDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly ListCtrl _list;
    private PlaylistRequest? _request;
    internal ManagerDialog(Window parent, IReadOnlyList<Document> documents)
    {
        // Translators: Title of the saved playlist manager.
        _dialog = new Dialog(parent, title: Tr("My Playlists"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _list = new ListCtrl(_dialog, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection);
        // Translators: Column heading for saved playlist names in the manager.
        _list.InsertColumn(0, Tr("Name"), 240);
        // Translators: Column heading for optional descriptions in the playlist manager.
        _list.InsertColumn(1, Tr("Description"), 400);
        foreach (var document in documents)
        {
            var row = _list.AddItem(document.Name);
            _list.SetItem(row, 1, document.Description);
            _list.SetItemData(row, document.Id);
        }
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
        buttons.Add(new Button(_dialog, StandardId.Cancel, Tr("Close")));
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(_list, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        layout.Add(buttons, flags: SizerFlags.All, border: 8);
        _dialog.SetSizer(layout);
        _dialog.Fit();
        _dialog.MinSize = new Size(720, 380);
        _dialog.Center(onParent: true);
        _list.ItemActivated += (_, _) => Select("open");
        if (documents.Count > 0) _list.SelectedIndex = 0;
        _list.Focus();
    }
    private void Add(BoxSizer buttons, string label, string action)
    {
        var button = new Button(_dialog, label: label);
        button.Click += (_, _) => Select(action);
        buttons.Add(button, flags: SizerFlags.BorderRight, border: 6);
    }
    private void Select(string action)
    {
        if (action != "new" && _list.SelectedIndex < 0) return;
        _request = new(action, action == "new" ? Guid.Empty : (Guid)_list.GetItemData(_list.SelectedIndex)!);
        _dialog.EndModal(StandardId.Ok);
    }
    internal PlaylistRequest? Show() { _dialog.ShowModal(); return _request; }
    public void Dispose() => _dialog.Dispose();
}
