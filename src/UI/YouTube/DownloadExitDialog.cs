using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>Confirms an application exit that will cancel or discard unfinished download jobs.</summary>
internal sealed class DownloadExitDialog : IDisposable
{
    private readonly Dialog _dialog;

    internal DownloadExitDialog(Window parent)
    {
        // Translators: Title of the warning shown when Luna is closed while downloads remain unfinished.
        _dialog = new Dialog(parent, title: Tr("Downloads still in progress"));
        // Translators: Warning shown when closing Luna would cancel downloads or discard queued videos.
        var message = new StaticText(_dialog, label: Tr(
            "There are items still downloading in the download queue. Closing Luna will cancel them."));
        message.Wrap(520);

        // Translators: Confirms closing Luna even though download queue work remains unfinished.
        var closeAnyway = new Button(_dialog, StandardId.Ok, Tr("Close anyway"));
        // Translators: Dismisses the close warning and keeps Luna and its downloads running.
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        cancel.SetDefault();
        _dialog.SetEscapeId(StandardId.Cancel);

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        buttons.Add(closeAnyway, flags: SizerFlags.BorderRight, border: 8);
        buttons.Add(cancel);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(message, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        sizer.Add(buttons,
            flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand,
            border: 10);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.Center(onParent: true);
        cancel.Focus();
    }

    internal bool Show() => _dialog.ShowModal() == StandardId.Ok;

    public void Dispose() => _dialog.Dispose();
}
