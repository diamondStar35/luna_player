using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>Offers to fetch the programs the yt-dlp resolver needs.</summary>
///
/// <remarks>
/// Shown only when the optional yt-dlp resolver is requested. The user may suppress future prompts.
/// </remarks>
internal sealed class ComponentsDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly CheckBox _remember;

    internal ComponentsDialog(Window parent)
    {
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window offering to fetch the extra programs yt-dlp needs.
            title: Tr("YouTube Components"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Message offering to fetch the extra programs yt-dlp needs. Searching, playing and
        // saving do not need them, which is what the last sentence says.
        var message = new StaticText(_dialog, label: Tr("The app detected that some components for YouTube are missing. Would you like to download the required libraries? If you do not wish to use the yt-dlp resolver, you can ignore this message."));
        message.Wrap(420);
        // Translators: Tick box on the window offering to fetch the extra programs, so it stops being offered.
        _remember = new CheckBox(_dialog, label: Tr("Don't show this message again"));

        // Translators: Button that agrees to fetch the extra programs the yt-dlp resolver needs.
        var yes = new Button(_dialog, StandardId.Yes, Tr("Yes"));
        yes.SetDefault();
        // Translators: Button that declines to fetch the extra programs the yt-dlp resolver needs.
        var no = new Button(_dialog, StandardId.No, Tr("No"));
        // wxWidgets auto-closes modal dialogs only for OK and Cancel, so Yes and No are bound explicitly.
        yes.Click += (_, _) => _dialog.EndModal(StandardId.Yes);
        no.Click += (_, _) => _dialog.EndModal(StandardId.No);
        // Treat Escape as No because this dialog has no Cancel button.
        _dialog.SetEscapeId(StandardId.No);

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        buttons.Add(yes, flags: SizerFlags.BorderRight, border: 8);
        buttons.Add(no);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(message, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        sizer.Add(_remember, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 10);
        sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 10);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(500, 220);
        _dialog.Center(onParent: true);
        yes.Focus();
    }

    /// <param name="doNotAskAgain">Whether the tick box was ticked. Honoured whichever button was pressed,
    /// and when the window was dismissed with Escape: the answer to "stop asking" does not depend on the
    /// answer to "fetch them now".</param>
    internal bool Show(out bool doNotAskAgain)
    {
        var answer = _dialog.ShowModal();
        doNotAskAgain = _remember.Checked;
        return answer == StandardId.Yes;
    }

    public void Dispose() => _dialog.Dispose();
}
