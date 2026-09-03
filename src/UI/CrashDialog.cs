using LunaPlayer.Application;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>Shown when something went wrong that would otherwise have ended the player without a word.
/// </summary>
///
/// <remarks>
/// The details are in a read-only text box rather than a message box, for two reasons: a stack trace does
/// not fit in one, and a screen reader can be walked through a text box a line at a time but reads a
/// message box once and forgets it. The Copy button is the point of the window - what is wanted from a
/// crash is the report, somewhere it can be pasted.
/// </remarks>
internal sealed class CrashDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly string _details;
    private readonly IClipboardService? _clipboard;

    /// <param name="path">Where the same text has already been written, named on the window so it can be
    /// found again after the window has gone.</param>
    internal CrashDialog(string details, string path, IClipboardService? clipboard)
    {
        _details = details;
        _clipboard = clipboard;
        _dialog = new Dialog(
            null,
            // Translators: Title of the window shown when the player has hit an error it cannot carry on from.
            title: Tr("Something went wrong"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Message at the top of the crash window. The player carries on where it can, so this
        // says what happened rather than announcing that it is closing.
        var message = new StaticText(_dialog, label: Tr("Luna hit an unexpected error it could not deal with. Please copy the details below and send them to the developer."));
        message.Wrap(520);
        var text = new TextCtrl(_dialog, value: details,
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
        text.InsertionPoint = 0;
        text.ShowPosition(0);
        // Translators: Tells the user where the crash details have also been written. {path} is a file name.
        var where = new StaticText(_dialog, label: TrFormat("This has also been written to {path}", path));

        // Translators: Button on the crash window that puts the details on the clipboard.
        var copy = new Button(_dialog, label: Tr("Copy details"));
        copy.Click += (_, _) => Copy();
        // Translators: The button that closes a window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(copy, flags: SizerFlags.BorderRight, border: 6);
        buttons.AddStretchSpacer();
        buttons.Add(close);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(message, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        sizer.Add(text, proportion: 1, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.Expand, border: 8);
        sizer.Add(where, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(640, 420);
        _dialog.Center(onParent: false);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        copy.Focus();
    }

    internal void Show() => _dialog.ShowModal();

    public void Dispose() => _dialog.Dispose();

    private void Copy()
    {
        if (_clipboard?.SetText(_details) == true)
            return;
        Wx.MessageBox(
            // Translators: Shown when the crash details could not be put on the clipboard. The file named
            // on the window still has them.
            Tr("The details could not be copied. They are in the file named above."),
            // Translators: Title of the window shown when the player has hit an error it cannot carry on from.
            Tr("Something went wrong"),
            MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
    }

    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
            _dialog.EndModal(StandardId.Cancel);
        else
            args.Skip();
    }
}
