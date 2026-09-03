using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>Offers to fetch the programs the yt-dlp resolver needs.</summary>
///
/// <remarks>
/// Not shown at startup, which is where the Python player shows it. Nothing the player does by default
/// needs these programs: searching, playing and saving are its own work now, and only the optional yt-dlp
/// resolver wants them. So the offer is made at the moment one is asked for, where it has a reason the user
/// can see, rather than on first launch about something they may never use.
///
/// The tick box is the reason it can be offered at the point of use at all: somebody who says no and means
/// it can say so once.
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
        // Bound by hand, and they have to be: wxWidgets ends a modal dialog by itself only for OK and
        // Cancel. A Yes or a No button that nothing listens to leaves a window that cannot be dismissed at
        // all, which is what this one was.
        yes.Click += (_, _) => _dialog.EndModal(StandardId.Yes);
        no.Click += (_, _) => _dialog.EndModal(StandardId.No);
        // There is no Cancel button for Escape to stand for, so it is pointed at No. The Python player
        // leaves Escape doing nothing here, which makes the window a trap for anyone who reaches it by
        // accident.
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
