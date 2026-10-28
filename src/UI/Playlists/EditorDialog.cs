using LunaPlayer.Playlist;
using WxSharp;

namespace LunaPlayer.UI.Playlists;

internal sealed class EditorDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly ListCtrl _list;
    private readonly TextCtrl _name;
    private readonly TextCtrl _description;
    private readonly Document _draft;
    private readonly IDialogService _dialogs;
    private readonly Action<string> _speak;
    private readonly bool _metadataOnly;
    private bool _accepted;
    private bool _dirty;

    internal EditorDialog(Window parent, Document document, IDialogService dialogs, Action<string> speak, bool metadataOnly = false)
    {
        _draft = document.Copy();
        _dialogs = dialogs;
        _speak = speak;
        _metadataOnly = metadataOnly;
        // Translators: Title of the dialog editing a playlist's name, description and entries.
        _dialog = new Dialog(parent, title: Tr("Playlist editor"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var layout = new BoxSizer(Orientation.Vertical);
        // Translators: Label of the playlist name text field in the editor.
        layout.Add(new StaticText(_dialog, label: Tr("Name")), flags: SizerFlags.All, border: 8);
        _name = new TextCtrl(_dialog, value: _draft.Name);
        layout.Add(_name, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        // Translators: Label of the optional playlist description text field.
        layout.Add(new StaticText(_dialog, label: Tr("Description (optional)")), flags: SizerFlags.All, border: 8);
        _description = new TextCtrl(_dialog, value: _draft.Description);
        layout.Add(_description, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        _list = new ListCtrl(_dialog, style: ListCtrlStyle.Report);
        // Translators: Column heading for the file or folder name in the playlist editor.
        _list.InsertColumn(0, Tr("Name"), 230);
        // Translators: Column heading for the complete file or folder location in the playlist editor.
        _list.InsertColumn(1, Tr("Full Path"), 450);
        if (!metadataOnly)
        {
            layout.Add(_list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
            var editing = new BoxSizer(Orientation.Horizontal);
            // Translators: Playlist editor button opening a menu to add files or a folder.
            AddButton(editing, Tr("Add..."), Add);
            // Translators: Playlist editor button removing selected entries without deleting media files.
            AddButton(editing, Tr("Remove"), Remove);
            // Translators: Playlist editor button moving selected entries earlier.
            AddButton(editing, Tr("Move Up"), () => Move(-1));
            // Translators: Playlist editor button moving selected entries later.
            AddButton(editing, Tr("Move Down"), () => Move(1));
            layout.Add(editing, flags: SizerFlags.All, border: 8);
        }
        else _list.Hide();
        var buttons = new BoxSizer(Orientation.Horizontal);
        // Translators: Playlist editor button accepting the draft and saving it.
        AddButton(buttons, Tr("Save"), Accept);
        // Translators: Playlist editor button discarding changes.
        var cancel = new Button(_dialog, label: Tr("Cancel"));
        buttons.Add(cancel);
        layout.Add(buttons, flags: SizerFlags.All, border: 8);
        _dialog.SetSizer(layout);
        _dialog.Fit();
        _dialog.MinSize = metadataOnly ? new Size(450, 260) : new Size(760, 480);
        _dialog.Center(onParent: true);
        _name.TextChanged += (_, _) => _dirty = true;
        _description.TextChanged += (_, _) => _dirty = true;
        _dialog.Closing += (_, args) =>
        {
            if (_dirty && !_accepted && args.CanVeto)
            {
                // Translators: Question shown when closing a modified playlist editor.
                var result = Wx.MessageBox(Tr("Save changes to this playlist?"), _dialog.Title,
                    MessageBoxStyle.YesNo | MessageBoxStyle.Cancel | MessageBoxStyle.IconWarning, _dialog);
                if (result == MessageBoxStyle.Yes) { args.Veto(); Accept(); }
                else if (result == MessageBoxStyle.Cancel) args.Veto();
            }
        };
        cancel.Click += (_, _) => _dialog.Close();
        _dialog.Bind(WxEvents.CharHook, (_, args) =>
        {
            if (args.Code == Key.Escape) { _dialog.Close(); return; }
            if (!metadataOnly && args.Modifiers == (KeyModifiers.Control | KeyModifiers.Alt))
            {
                if (args.Code == (Key)'F') { AddFiles(); return; }
                if (args.Code == (Key)'D') { AddFolder(); return; }
            }
            args.Skip();
        });
        _list.Bind(WxEvents.CharHook, (_, args) =>
        {
            if (args.Code == Key.Delete) Remove();
            else if (args.Modifiers == KeyModifiers.Shift && args.Code == Key.Up) Move(-1);
            else if (args.Modifiers == KeyModifiers.Shift && args.Code == Key.Down) Move(1);
            else if (args.Modifiers == KeyModifiers.Control && args.Code == (Key)'A')
                for (var i = 0; i < _draft.Entries.Count; i++) _list.SetSelected(i);
            else args.Skip();
        });
        Refresh([]);
        _name.Focus();
    }

    private void AddButton(BoxSizer sizer, string label, Action action)
    {
        var button = new Button(_dialog, label: label);
        button.Click += (_, _) => action();
        sizer.Add(button, flags: SizerFlags.BorderRight, border: 6);
    }
    private void Add()
    {
        using var menu = new Menu();
        var files = IdManager.NewId();
        var folder = IdManager.NewId();
        // Translators: Playlist Add menu item selecting multiple files.
        menu.Append(files, Tr("Files..."));
        // Translators: Playlist Add menu item selecting a folder reference.
        menu.Append(folder, Tr("Folder..."));
        void Selected(object? sender, CommandEventArgs args)
        { if (args.Id == files) AddFiles(); else if (args.Id == folder) AddFolder(); }
        _dialog.MenuCommand += Selected;
        try { _dialog.PopupMenu(menu); }
        finally { _dialog.MenuCommand -= Selected; }
    }
    private void AddFiles() => Append(_dialogs.ChoosePlaylistFiles());
    private void AddFolder()
    {
        if (_dialogs.ChooseFolder(string.Empty) is string folder) Append([folder]);
    }
    private void Append(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;
        var start = _draft.Entries.Count;
        _draft.Entries.AddRange(paths.Select(SavedEntry.FromPath));
        _dirty = true;
        Refresh(Enumerable.Range(start, paths.Count));
        // Translators: Spoken after adding entries to a playlist. {count} is the number added.
        _speak(TrFormat("{count} entries added to playlist.", paths.Count));
    }
    private void Remove()
    {
        var selected = _list.GetSelectedIndices().Select(index => (int)index).ToArray();
        if (selected.Length == 0) return;
        foreach (var index in selected.Reverse()) _draft.Entries.RemoveAt(index);
        _dirty = true;
        Refresh(_draft.Entries.Count == 0 ? [] : [Math.Min(selected[0], _draft.Entries.Count - 1)]);
        // Translators: Spoken after removing entries from a playlist only. {count} is the number removed.
        _speak(TrFormat("{count} entries removed from playlist.", selected.Length));
    }
    private void Move(int direction)
    {
        var indices = _list.GetSelectedIndices().Select(index => (int)index).ToArray();
        if (indices.Length == 0 || direction < 0 && indices[0] == 0
            || direction > 0 && indices[^1] == _draft.Entries.Count - 1) return;
        foreach (var index in direction < 0 ? indices : indices.Reverse())
            (_draft.Entries[index], _draft.Entries[index + direction]) = (_draft.Entries[index + direction], _draft.Entries[index]);
        _dirty = true;
        Refresh(indices.Select(index => index + direction));
        // Translators: Spoken after reordering the selected playlist entries.
        _speak(Tr("Playlist entries moved."));
    }
    private void Refresh(IEnumerable<int> selection)
    {
        _list.Clear();
        foreach (var entry in _draft.Entries)
        {
            var row = _list.AddItem(entry.Name);
            _list.SetItem(row, 1, entry.Location);
        }
        var selected = selection.ToArray();
        foreach (var index in selected) _list.SetSelected(index);
        if (selected.Length > 0) { _list.SetFocused(selected[0]); _list.EnsureVisible(selected[0]); }
        if (!_metadataOnly && selected.Length > 0) _list.Focus();
    }
    private void Accept()
    {
        if (string.IsNullOrWhiteSpace(_name.Value))
        {
            // Translators: Validation warning for an empty playlist name in the editor.
            _dialogs.ShowWarning(Tr("A playlist name is required."), _dialog.Title);
            _name.Focus();
            return;
        }
        _draft.Name = _name.Value.Trim();
        _draft.Description = _description.Value.Trim();
        _accepted = true;
        _dialog.EndModal(StandardId.Ok);
    }
    internal Document? Show() { _dialog.ShowModal(); return _accepted ? _draft : null; }
    public void Dispose() => _dialog.Dispose();
}
