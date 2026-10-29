using LunaPlayer.UI.Services;
using LunaPlayer.Playlist;
using WxSharp;

namespace LunaPlayer.UI.Playlists;

internal sealed class EditorDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly ListCtrl _list;
    private readonly TextCtrl _name;
    private readonly TextCtrl _description;
    private readonly Button? _remove;
    private readonly Button? _moveUp;
    private readonly Button? _moveDown;
    private readonly Document _draft;
    private readonly IDialogService _dialogs;
    private readonly Action<string> _speak;
    private readonly bool _metadataOnly;
    private readonly Func<Document, bool> _save;
    private readonly Window? _owner;
    private bool _ownerWasEnabled;
    private bool _ownerDisabled;
    private bool _finished;
    private bool _saving;
    private bool _disposed;
    private bool _accepted;
    private bool _dirty;

    internal EditorDialog(Document document, IDialogService dialogs, Action<string> speak,
        Func<Document, bool> save, bool metadataOnly = false, Window? parent = null)
    {
        _draft = document.Copy();
        _dialogs = dialogs;
        _speak = speak;
        _metadataOnly = metadataOnly;
        _save = save;
        _owner = parent;
        // Editors opened by My Playlists belong to that window; standalone editors must suppress wx's default owner.
        _dialog = new Dialog(
            parent: parent,
            // Translators: Title of the dialog editing a playlist's name, description and entries.
            title: Tr("Playlist editor"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder | (parent is null ? DialogStyle.NoParent : DialogStyle.None));
        var layout = new BoxSizer(Orientation.Vertical);
        // Translators: Label of the playlist name text field in the editor.
        layout.Add(new StaticText(_dialog, label: Tr("Name")), flags: SizerFlags.All, border: 8);
        _name = new TextCtrl(_dialog, value: _draft.Name);
        layout.Add(_name, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        // Translators: Label of the optional playlist description text field.
        layout.Add(new StaticText(_dialog, label: Tr("Description (optional)")), flags: SizerFlags.All, border: 8);
        _description = new TextCtrl(_dialog, value: _draft.Description);
        layout.Add(_description, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        // Translators: Label above the list of files and folders in the playlist editor.
        var itemsLabel = new StaticText(_dialog, label: Tr("Items"));
        _list = new ListCtrl(_dialog, style: ListCtrlStyle.Report);
        // Translators: Column heading for the file or folder name in the playlist editor.
        _list.InsertColumn(0, Tr("Name"), 230);
        // Translators: Column heading for the complete file or folder location in the playlist editor.
        _list.InsertColumn(1, Tr("Full Path"), 450);
        if (!metadataOnly)
        {
            layout.Add(itemsLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderTop | SizerFlags.BorderRight, border: 8);
            layout.Add(_list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
            var editing = new BoxSizer(Orientation.Horizontal);
            // Translators: Playlist editor button opening a menu to add files or a folder.
            AddButton(editing, Tr("Add..."), Add);
            // Translators: Playlist editor button removing selected entries without deleting media files.
            _remove = AddButton(editing, Tr("Remove"), Remove);
            // Translators: Playlist editor button moving selected entries earlier.
            _moveUp = AddButton(editing, Tr("Move Up"), () => Move(-1));
            // Translators: Playlist editor button moving selected entries later.
            _moveDown = AddButton(editing, Tr("Move Down"), () => Move(1));
            layout.Add(editing, flags: SizerFlags.All, border: 8);
        }
        else
        {
            itemsLabel.Hide();
            _list.Hide();
        }
        var buttons = new BoxSizer(Orientation.Horizontal);
        // Translators: Playlist editor button accepting the draft and saving it.
        var saveButton = new Button(_dialog, StandardId.Ok, Tr("Save"));
        saveButton.SetDefault();
        saveButton.Click += (_, _) => Accept();
        buttons.Add(saveButton, flags: SizerFlags.BorderRight, border: 6);
        // Translators: Playlist editor button discarding changes.
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        _dialog.SetEscapeId(StandardId.Cancel);
        buttons.Add(cancel);
        layout.Add(buttons, flags: SizerFlags.All, border: 8);
        _dialog.SetSizer(layout);
        _dialog.Fit();
        _dialog.MinSize = metadataOnly ? new Size(450, 260) : new Size(760, 480);
        _dialog.Center(onParent: parent is not null);
        _name.TextChanged += (_, _) => _dirty = true;
        _description.TextChanged += (_, _) => _dirty = true;
        _dialog.Closing += (_, args) =>
        {
            if (_saving && args.CanVeto) { args.Veto(); return; }
            if (_dirty && !_accepted && args.CanVeto)
            {
                // Translators: Question shown when closing a modified playlist editor.
                var result = Messages.Show(Tr("Save changes to this playlist?"),
                    MessageBoxStyle.YesNo | MessageBoxStyle.Cancel | MessageBoxStyle.IconWarning, _dialog);
                if (result == MessageBoxStyle.Yes)
                {
                    Accept();
                    if (!_accepted) args.Veto();
                    return;
                }
                if (result == MessageBoxStyle.Cancel)
                {
                    args.Veto();
                    return;
                }
            }
            Complete(false);
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
        _list.ItemSelected += (_, _) => UpdateButtons();
        _list.ItemDeselected += (_, _) => UpdateButtons();
        Refresh([]);
        _name.Focus();
    }

    private Button AddButton(BoxSizer sizer, string label, Action action)
    {
        var button = new Button(_dialog, label: label);
        button.Click += (_, _) => action();
        sizer.Add(button, flags: SizerFlags.BorderRight, border: 6);
        return button;
    }
    private void UpdateButtons()
    {
        if (_metadataOnly) return;
        var selected = _list.GetSelectedIndices().ToArray();
        var hasSelection = selected.Length > 0;
        _remove?.Show(hasSelection);
        _moveUp?.Show(hasSelection && selected[0] > 0);
        _moveDown?.Show(hasSelection && selected[^1] < _draft.Entries.Count - 1);
        _dialog.Layout();
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
        UpdateButtons();
    }
    private void Accept()
    {
        if (_finished || _saving) return;
        if (string.IsNullOrWhiteSpace(_name.Value))
        {
            // Translators: Validation warning for an empty playlist name in the editor.
            _dialogs.ShowWarning(Tr("A playlist name is required."));
            _name.Focus();
            return;
        }
        _draft.Name = _name.Value.Trim();
        _draft.Description = _description.Value.Trim();
        _saving = true;
        try
        {
            if (!_save(_draft.Copy())) return;
        }
        finally { _saving = false; }
        Complete(true);
    }
    internal event Action<EditorDialog, bool>? Closed;

    internal void Show()
    {
        // ShowModal disables every top-level window on Windows. Keep the normal event loop running
        // and disable only this editor's owner until the editor is dismissed.
        _dialog.Show();
        if (_owner is not null)
        {
            _ownerWasEnabled = _owner.Enabled;
            _owner.Enabled = false;
            _ownerDisabled = true;
        }
        Activate();
    }

    internal void Activate() { _dialog.Raise(); _name.Focus(); }

    internal bool TryClose()
    {
        if (_finished) return true;
        if (_saving) return false;
        Activate();
        _dialog.Close();
        return _finished;
    }

    private void RestoreOwner()
    {
        if (!_ownerDisabled) return;
        _ownerDisabled = false;
        _owner!.Enabled = _ownerWasEnabled;
    }

    private void Complete(bool accepted)
    {
        if (_finished) return;
        _accepted = accepted;
        _finished = true;
        RestoreOwner();
        _dialog.Hide();
        _owner?.Raise();
        Closed?.Invoke(this, accepted);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _finished = true;
        RestoreOwner();
        _dialog.Dispose();
    }
}
