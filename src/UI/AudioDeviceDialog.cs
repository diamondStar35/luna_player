using WxSharp;

namespace LunaPlayer.UI;

internal sealed class AudioDeviceDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Choice _choice;

    internal AudioDeviceDialog(Window parent, IReadOnlyList<string> descriptions, int selectedIndex)
    {
        _dialog = new Dialog(parent, title: Tr("Sound Cards"), style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var sizer = new BoxSizer(Orientation.Vertical);
        // Translators: Label above the list of sound cards the player can send its sound to.
        sizer.Add(new StaticText(_dialog, label: Tr("Select sound card")), flags: SizerFlags.All, border: 8);
        _choice = new Choice(_dialog);
        foreach (var description in descriptions)
            _choice.Add(description);
        if (descriptions.Count > 0)
            _choice.SelectedIndex = Math.Clamp(selectedIndex, 0, descriptions.Count - 1);
        sizer.Add(_choice, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        var buttons = _dialog.CreateButtonSizer(ButtonSizerFlags.Ok | ButtonSizerFlags.Cancel);
        if (buttons is not null)
            sizer.Add(buttons, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(360, 180);
        _dialog.Center(onParent: true);
    }

    internal int? Show() => _dialog.ShowModal() == StandardId.Ok ? _choice.SelectedIndex : null;
    public void Dispose() => _dialog.Dispose();
}
