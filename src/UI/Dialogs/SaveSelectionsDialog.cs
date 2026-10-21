using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>One format offered when selected parts are written to media files.</summary>
internal readonly record struct SelectionFileFormat(string Label, string Extension);

/// <summary>The naming, location, format and concurrency chosen for a batch of selection files.</summary>
internal readonly record struct SelectionSaveOptions(
    string Name,
    string Folder,
    string Extension,
    int MaxConcurrency);

/// <summary>Collects the settings for saving every completed selection in the current file.</summary>
internal sealed class SaveSelectionsDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly IReadOnlyList<SelectionFileFormat> _formats;
    private readonly TextCtrl _name;
    private readonly TextCtrl _folder;
    private readonly ComboBox _format;
    private readonly SpinCtrl _threads;
    private SelectionSaveOptions? _result;

    internal SaveSelectionsDialog(
        Window parent,
        IReadOnlyList<SelectionFileFormat> formats,
        string initialFolder,
        string initialName,
        string sourceExtension)
    {
        _formats = formats;
        _dialog = new Dialog(
            parent,
            // Translators: Title of the dialog for saving every selection point as a separate media file.
            title: Tr("Save selection points"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Labels are created immediately before their controls so Windows supplies useful accessible names.
        // Translators: Label of the name placed before each number in selection file names.
        var nameLabel = new StaticText(_dialog, label: Tr("Name"));
        _name = new TextCtrl(_dialog, value: initialName);

        // Translators: Label of the read-only folder box in the save-selection-points dialog.
        var folderLabel = new StaticText(_dialog, label: Tr("Folder"));
        _folder = new TextCtrl(
            _dialog,
            value: initialFolder,
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        _folder.MinSize = new Size(360, 80);
        // Translators: Button that opens a folder picker for selection files.
        var browse = new Button(_dialog, label: Tr("Browse..."));

        // Translators: Label of the combo box choosing the media format for saved selections.
        var formatLabel = new StaticText(_dialog, label: Tr("Format"));
        _format = new ComboBox(_dialog, readOnly: true);
        foreach (var format in formats)
            _format.Add(format.Label);
        var selectedFormat = 0;
        for (var index = 0; index < formats.Count; index++)
        {
            if (!formats[index].Extension.Equals(sourceExtension, StringComparison.OrdinalIgnoreCase))
                continue;
            selectedFormat = index;
            break;
        }
        _format.SelectedIndex = selectedFormat;

        // Translators: Label of the spin box choosing how many selections may be saved simultaneously.
        var threadsLabel = new StaticText(_dialog, label: Tr("Maximum number of threads"));
        _threads = new SpinCtrl(_dialog, value: 1, minimum: 1, maximum: 16);

        var form = new FlexGridSizer(0, 2, 8, 8);
        form.AddGrowableColumn(1, 1);
        AddRow(form, nameLabel, _name);
        form.Add(folderLabel, flags: SizerFlags.AlignTop);
        var folderRow = new BoxSizer(Orientation.Horizontal);
        folderRow.Add(_folder, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderRight, border: 6);
        folderRow.Add(browse, flags: SizerFlags.AlignCenterVertical);
        form.Add(folderRow, proportion: 1, flags: SizerFlags.Expand);
        AddRow(form, formatLabel, _format);
        AddRow(form, threadsLabel, _threads);

        // Translators: Button that starts saving all selection points with the chosen settings.
        var save = new Button(_dialog, label: Tr("Save"));
        // Translators: Button that closes the save-selection-points dialog without saving anything.
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        save.SetDefault();
        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        buttons.Add(save, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(cancel);

        var root = new BoxSizer(Orientation.Vertical);
        root.Add(form, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        root.Add(buttons,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand,
            border: 10);
        _dialog.SetSizer(root);

        browse.Click += (_, _) => Browse();
        save.Click += (_, _) => Save();

        _dialog.Fit();
        _dialog.MinSize = new Size(540, 330);
        _dialog.Center(onParent: true);
        _name.Focus();
        _name.SelectAll();
    }

    internal SelectionSaveOptions? Show()
        => _dialog.ShowModal() == StandardId.Ok ? _result : null;

    public void Dispose() => _dialog.Dispose();

    private static void AddRow(FlexGridSizer form, StaticText label, Window control)
    {
        form.Add(label, flags: SizerFlags.AlignCenterVertical);
        form.Add(control, proportion: 1, flags: SizerFlags.Expand);
    }

    private void Browse()
    {
        using var dialog = new DirDialog(
            _dialog,
            // Translators: Title of the folder picker opened while choosing where selection files are saved.
            message: Tr("Choose a folder for saved selections"),
            defaultPath: _folder.Value,
            style: DirDialogStyle.DirMustExist);
        if (dialog.ShowModal() == StandardId.Ok)
            _folder.Value = dialog.Path;
    }

    private void Save()
    {
        var name = _name.Value.Trim();
        if (name.Length == 0)
        {
            // Translators: Warning shown when the common name for saved selection files is empty.
            Warn(Tr("Enter a name for the selection files."));
            return;
        }
        if (name.Length > 120)
        {
            // Translators: Warning shown when the common name for saved selection files is too long.
            Warn(Tr("The name is too long."));
            return;
        }
        if (!string.Equals(name, Paths.SafeFileName(name, string.Empty), StringComparison.Ordinal))
        {
            // Translators: Warning shown when the common name contains characters Windows forbids in files.
            Warn(Tr("The name contains characters that cannot be used in a file name."));
            return;
        }
        var folder = _folder.Value.Trim();
        if (folder.Length == 0)
        {
            // Translators: Warning shown when no output folder was chosen for selection files.
            Warn(Tr("Choose a folder for the selection files."));
            return;
        }
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            // Translators: Warning when the selection folder cannot be created. {message} is the operating system's explanation.
            Warn(TrFormat("The folder could not be created.\n{message}", failure.Message));
            return;
        }
        var formatIndex = _format.SelectedIndex;
        if (formatIndex < 0 || formatIndex >= _formats.Count)
            return;
        _result = new SelectionSaveOptions(
            name, folder, _formats[formatIndex].Extension, _threads.Value);
        _dialog.EndModal(StandardId.Ok);
    }

    private void Warn(string message) => Wx.MessageBox(
        message,
        // Translators: Title of validation warnings in the save-selection-points dialog.
        Tr("Save selection points"),
        MessageBoxStyle.Ok | MessageBoxStyle.IconWarning,
        _dialog);
}
