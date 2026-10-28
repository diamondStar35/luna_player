using WxSharp;

namespace LunaPlayer.UI.Audio;

internal sealed class TracksDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Choice _choice;

    internal TracksDialog(Window parent, IReadOnlyList<string> labels, int selectedIndex)
    {
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window listing the audio tracks in the file to switch between.
            title: Tr("Audio Tracks"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var sizer = new BoxSizer(Orientation.Vertical);
        // Translators: Label above the list of audio tracks in the file to switch between.
        sizer.Add(new StaticText(_dialog, label: Tr("Select audio track")), flags: SizerFlags.All, border: 8);
        _choice = new Choice(_dialog);
        foreach (var label in labels)
            _choice.Add(label);
        if (labels.Count > 0)
            _choice.SelectedIndex = Math.Clamp(selectedIndex, 0, labels.Count - 1);
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
