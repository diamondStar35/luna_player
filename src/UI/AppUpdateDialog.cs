using WxSharp;

namespace LunaPlayer.UI;

/// <summary>Offers an available player release and displays its change list.</summary>
internal sealed class AppUpdateDialog : IDisposable
{
    private readonly Dialog _dialog;

    internal AppUpdateDialog(Window parent, AppUpdatePrompt prompt)
    {
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window offering a newer Luna Player release.
            title: Tr("New version detected"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Message shown when a newer Luna Player release is available. {current} is the
        // installed version and {available} is the version that can be downloaded.
        var message = new StaticText(_dialog, label: TrFormat(
            "A new version of Luna is available. Your current version is {current}. Available version: {available}. Would you like to update?",
            prompt.CurrentVersion, prompt.AvailableVersion));
        message.Wrap(560);

        // Translators: Label above the list of changes in a new Luna Player release.
        var changesLabel = new StaticText(_dialog, label: Tr("What's new"));
        var changes = new TextCtrl(
            _dialog,
            value: prompt.Changes,
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        changes.InsertionPoint = 0;

        // Translators: Button that downloads and installs the offered Luna Player release.
        var update = new Button(_dialog, StandardId.Ok, Tr("Update"));
        update.SetDefault();
        // Translators: Button that dismisses an available update without installing it.
        var later = new Button(_dialog, StandardId.Cancel, Tr("Later"));
        _dialog.SetEscapeId(StandardId.Cancel);

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        buttons.Add(update, flags: SizerFlags.BorderRight, border: 8);
        buttons.Add(later);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(message, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        sizer.Add(changesLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 10);
        sizer.Add(changes, proportion: 1, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 10);
        sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 10);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(620, 420);
        _dialog.Center(onParent: true);
        update.Focus();
    }

    internal bool Show() => _dialog.ShowModal() == StandardId.Ok;

    public void Dispose() => _dialog.Dispose();
}
