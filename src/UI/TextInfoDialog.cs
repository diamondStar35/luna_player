using WxSharp;

namespace LunaPlayer.UI;

internal sealed class TextInfoDialog : IDisposable
{
    private readonly Dialog _dialog;

    internal TextInfoDialog(Window parent, string title, string text)
    {
        _dialog = new Dialog(parent,
            // Translators: Title used for a window of read-only text when the caller supplies none.
            title: string.IsNullOrWhiteSpace(title) ? Tr("Information") : title,
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var textBox = new TextCtrl(_dialog, value: text, style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        textBox.InsertionPoint = 0;
        textBox.ShowPosition(0);
        var close = new Button(_dialog, StandardId.Close, Tr("Close"));
        close.Click += (_, _) => _dialog.EndModal(StandardId.Close);
        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(textBox, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        sizer.Add(close, flags: SizerFlags.All | SizerFlags.AlignRight, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(500, 320);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
    }

    internal void Show() => _dialog.ShowModal();
    public void Dispose() => _dialog.Dispose();

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
            _dialog.EndModal(StandardId.Cancel);
        else
            args.Skip();
    }
}
