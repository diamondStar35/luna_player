using WxSharp;

namespace LunaPlayer.UI;

/// <summary>A read-only report of a batch that mostly worked: a summary line, then a scrollable list of the
/// files that did not convert, each with its reason. A batch with no failures gets a plain message box
/// instead, so this is only ever shown when there is a list to read.</summary>
internal sealed class ConversionReportDialog : IDisposable
{
    private readonly Dialog _dialog;

    internal ConversionReportDialog(Window parent, string title, string message, string details)
    {
        _dialog = new Dialog(parent, title: title, style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var summary = new StaticText(_dialog, label: message);
        // The label is made right before the box it names so Windows reads it as the box's accessible name.
        // Translators: Label of the box listing the files a conversion could not convert.
        var detailsLabel = new StaticText(_dialog, label: Tr("Details"));
        var detailsBox = new TextCtrl(_dialog, value: details,
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        detailsBox.InsertionPoint = 0;
        detailsBox.ShowPosition(0);
        var close = new Button(_dialog, StandardId.Close, Tr("Close"));
        close.Click += (_, _) => _dialog.EndModal(StandardId.Close);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(summary, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        sizer.Add(detailsLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        sizer.Add(detailsBox, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        sizer.Add(close, flags: SizerFlags.All | SizerFlags.AlignRight, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(520, 360);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        // Focus the list rather than the Close button, so its label and the failures are read straight away.
        detailsBox.Focus();
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
