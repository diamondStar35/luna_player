using LunaPlayer.Equalizer;
using LunaPlayer.Playback;
using WxSharp;

namespace LunaPlayer.UI.Equalizer;

/// <summary>The window listing every preset, where the user makes, changes and removes their own.
/// </summary>
///
/// <remarks>
/// Presets the player ships are in the list but cannot be worked on from here: choosing one hides the
/// edit and remove buttons rather than greying them, so a keyboard user does not tab onto a control that
/// will not do anything. Editing one of those is done from the menu instead, on whichever preset is
/// playing, where the change can be heard as it is made.
///
/// There is no button for copying a preset. Making a new one already asks which preset to start from, so
/// a copy is a new preset with a base chosen and nothing else touched.
/// </remarks>
internal sealed class EqualizerPresetsDialog : IDisposable
{
    private const int NewId = 17010;
    private const int EditId = 17011;
    private const int DeleteId = 17012;

    private readonly Dialog _dialog;
    private readonly ListBox _list;
    private readonly Button _edit;
    private readonly Button _delete;
    private readonly Library _library;
    private IReadOnlyList<PresetEntry> _entries = [];

    internal EqualizerPresetsDialog(Window parent, Library library)
    {
        _library = library;
        _dialog = new Dialog(parent,
            // Translators: Title of the window listing every equalizer preset, where the user's own are made and removed.
            title: Tr("Manage equalizer presets"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Label above the list of equalizer presets.
        var label = new StaticText(_dialog, label: Tr("Presets"));
        _list = new ListBox(_dialog);
        _list.SelectionChanged += (_, _) => UpdateButtons();

        var buttons = new BoxSizer(Orientation.Horizontal);
        // Translators: Button that makes a new equalizer preset.
        var create = new Button(_dialog, NewId, Tr("New..."));
        create.Click += (_, _) => Create();
        // Translators: Button that opens the chosen equalizer preset for editing.
        _edit = new Button(_dialog, EditId, Tr("Edit..."));
        _edit.Click += (_, _) => Edit();
        // Translators: Button that removes the chosen equalizer preset.
        _delete = new Button(_dialog, DeleteId, Tr("Delete"));
        _delete.Click += (_, _) => Delete();
        // Translators: Button that closes a window when there is nothing to accept or cancel.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));
        close.SetDefault();
        buttons.Add(create, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_edit, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(_delete, flags: SizerFlags.BorderRight, border: 6);
        buttons.AddStretchSpacer();
        buttons.Add(close);

        const SizerFlags Side = SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom;
        var root = new BoxSizer(Orientation.Vertical);
        root.Add(label, flags: SizerFlags.All, border: 8);
        root.Add(_list, proportion: 1, flags: Side | SizerFlags.Expand, border: 8);
        root.Add(buttons, flags: Side | SizerFlags.Expand, border: 8);
        _dialog.SetSizer(root);
        _dialog.Fit();
        _dialog.MinSize = new Size(420, 360);
        _dialog.Center(onParent: true);
        Fill(select: 0);
        _list.Focus();
    }

    /// <summary>Whether anything was added, changed or removed, so the caller knows to build the menu
    /// again and reapply what is playing.</summary>
    internal bool Changed { get; private set; }

    internal bool Show()
    {
        _dialog.ShowModal();
        return Changed;
    }

    public void Dispose() => _dialog.Dispose();

    private void Fill(int select)
    {
        _entries = _library.All;
        _list.Set(_entries.Select(entry => entry.Name));
        if (_entries.Count > 0)
            _list.SelectedIndex = Math.Clamp(select, 0, _entries.Count - 1);
        UpdateButtons();
    }

    private PresetEntry? Selected()
    {
        var index = _list.SelectedIndex;
        return index >= 0 && index < _entries.Count ? _entries[index] : null;
    }

    private void UpdateButtons()
    {
        var editable = Selected() is PresetEntry entry && !entry.IsSystem;
        _edit.Show(editable);
        _delete.Show(editable);
        _dialog.Layout();
    }

    private void Create()
    {
        using var editor = new EqualizerEditDialog(
            _dialog,
            // Translators: Title of the window for making a new equalizer preset.
            Tr("New equalizer preset"),
            string.Empty,
            canRename: true,
            Presets.CustomId,
            _library.Slots(Presets.CustomId),
            _library.All,
            _library.Slots,
            _library.DefaultSlots,
            preview: null);
        if (editor.Show() is not EqualizerEditResult result)
            return;
        if (_library.Add(result.Name, result.Slots) is not string id)
        {
            Report(_library.LastError);
            return;
        }
        Changed = true;
        Fill(IndexOf(id));
    }

    private void Edit()
    {
        if (Selected() is not PresetEntry entry || entry.IsSystem)
            return;
        using var editor = new EqualizerEditDialog(
            _dialog,
            // Translators: Title of the window for changing an equalizer preset. {name} is the preset's name.
            TrFormat("Edit {name}", entry.Name),
            entry.Name,
            canRename: true,
            entry.Id,
            _library.Slots(entry.Id),
            _library.All,
            _library.Slots,
            _library.DefaultSlots,
            preview: null);
        if (editor.Show() is not EqualizerEditResult result)
            return;
        if (!_library.Save(entry.Id, result.Name, result.Slots))
        {
            Report(_library.LastError);
            return;
        }
        Changed = true;
        Fill(IndexOf(entry.Id));
    }

    private void Delete()
    {
        if (Selected() is not PresetEntry entry || entry.IsSystem)
            return;
        var answer = Wx.MessageBox(
            // Translators: Asks the user to confirm removing an equalizer preset. {name} is the preset's name.
            TrFormat("Delete the preset {name}? This cannot be undone.", entry.Name),
            // Translators: Title of the window asking the user to confirm removing an equalizer preset.
            Tr("Delete preset"),
            MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning | MessageBoxStyle.NoDefault, _dialog);
        if (answer != MessageBoxStyle.Yes)
            return;
        var index = _list.SelectedIndex;
        if (!_library.Delete(entry.Id))
        {
            Report(_library.LastError);
            return;
        }
        Changed = true;
        Fill(index);
    }

    private void Report(string error)
        => Wx.MessageBox(
            error.Length > 0
                ? error
                // Translators: Shown when an equalizer preset could not be saved and there is no more detail to give.
                : Tr("The preset could not be saved."),
            // Translators: Title of the window shown when an equalizer preset could not be saved.
            Tr("Equalizer"),
            MessageBoxStyle.Ok | MessageBoxStyle.IconError, _dialog);

    private int IndexOf(string id)
    {
        for (var index = 0; index < _entries.Count; index++)
        {
            if (string.Equals(_entries[index].Id, id, StringComparison.Ordinal))
                return index;
        }
        return 0;
    }
}
