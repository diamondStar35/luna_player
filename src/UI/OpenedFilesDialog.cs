using WxSharp;

namespace LunaPlayer.UI;

internal sealed class OpenedFilesDialog : IDisposable
{
    private const int InformationId = 16001;
    private readonly Dialog _dialog;
    private readonly ListBox _list;
    private OpenedFilesAction? _action;

    internal OpenedFilesDialog(Window parent, IReadOnlyList<string> entries, int selectedIndex)
    {
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window listing the files currently loaded in the player.
            title: Tr("Opened Files"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _list = new ListBox(_dialog);
        _list.Set(entries);
        if (selectedIndex >= 0 && selectedIndex < entries.Count) _list.SelectedIndex = selectedIndex;

        // Translators: Button that shows details of every file in the playlist.
        var information = new Button(_dialog, InformationId, Tr("Playlist info"));
        // Translators: Button that starts playing the file chosen in the list.
        var jump = new Button(_dialog, StandardId.Ok, Tr("Jump to selected"));
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        jump.SetDefault();
        information.Click += (_, _) => End(OpenedFilesAction.Information);
        jump.Click += (_, _) => End(OpenedFilesAction.Jump);
        _list.ItemActivated += (_, _) => End(OpenedFilesAction.Jump);

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(information, flags: SizerFlags.BorderRight, border: 6);
        buttons.AddStretchSpacer();
        buttons.Add(jump, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(cancel);
        var sizer = new BoxSizer(Orientation.Vertical);
        // Translators: Label above the list of loaded files, telling the user to pick the one to play.
        sizer.Add(new StaticText(_dialog, label: Tr("Select file to jump to.")), flags: SizerFlags.All, border: 8);
        sizer.Add(_list, proportion: 1, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(420, 260);
        _dialog.Center(onParent: true);
    }

    internal OpenedFilesRequest? Show()
    {
        _dialog.ShowModal();
        return _action.HasValue && _list.SelectedIndex >= 0 ? new(_action.Value, _list.SelectedIndex) : null;
    }

    public void Dispose() => _dialog.Dispose();

    private void End(OpenedFilesAction action)
    {
        if (_list.SelectedIndex < 0) return;
        _action = action;
        _dialog.EndModal(StandardId.Ok);
    }
}
