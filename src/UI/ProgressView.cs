using WxSharp;

namespace LunaPlayer.UI;

/// <summary>The window shown while a long job runs: what it is doing, how far it has got, and a way out.
/// </summary>
///
/// <remarks>
/// Two shapes, as the Python player has two. A job that reports one line at a time gets a label above the
/// bar; a job that reports a block - the file being written, its size, how much has arrived - gets a
/// read-only text area instead, because those lines are several and a growing label would move the button
/// under the user's pointer every time they changed.
///
/// Deliberately not wxProgressDialog. That class is built for a caller that owns the UI thread and calls
/// Update from inside its own loop, so its Update pumps the event loop itself to keep the window painted and
/// the Cancel button live. Driven from a timer instead, that turns into a trap: Update re-enters the timer
/// handler, and a nested tick can finish the job and destroy the dialog while the outer one is still using
/// it. This is a plain dialog whose Update only sets a value and a label, so it can never re-enter.
///
/// Cancelling is therefore an ordinary button press, delivered by the main event loop like any other. That is
/// the whole reason it works: nothing here blocks that loop, so the button is answered the moment it is
/// pressed rather than whenever some worker next decides to look.
///
/// Input is kept out of every other window while it runs, this one excepted, which is what the Python
/// player gets from showing its task dialog modally. Doing it with a window disabler rather than a nested
/// modal loop is what keeps the rule above true: nothing here blocks the event loop, so Cancel is still an
/// ordinary button press answered the moment it is made.
/// </remarks>
internal sealed class ProgressView : IProgressView
{
    /// <summary>The bar's range, as the Python player sets it: a percentage to one decimal place, so a
    /// long download does not sit on the same step for several seconds.</summary>
    private const int Range = 1000;

    private readonly Dialog _dialog;
    private readonly StaticText? _label;
    private readonly TextCtrl? _text;
    private readonly Gauge _gauge;
    private readonly bool _proportional;
    private readonly IDisposable _elsewhere;
    private bool _cancelled;
    private bool _disposed;

    /// <param name="proportional">Whether the job can say how far through it is. One that cannot gets a
    /// bar that sweeps rather than one pinned at nought, which is what the Python player shows and is the
    /// only honest way to say "still going" without claiming a figure.</param>
    /// <param name="detailed">Whether its reports are several lines rather than one.</param>
    internal ProgressView(Window parent, string title, string message, bool proportional, bool detailed)
    {
        _proportional = proportional;
        _dialog = new Dialog(parent, title: title, style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var sizer = new BoxSizer(Orientation.Vertical);
        if (detailed)
        {
            _text = new TextCtrl(_dialog, value: message,
                style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly | TextCtrlStyle.DontWrap);
            sizer.Add(_text, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        }
        else
        {
            _label = new StaticText(_dialog, label: message);
            sizer.Add(_label, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        }
        _gauge = new Gauge(_dialog, range: Range);
        // Translators: The button that stops a job that is running behind a progress window.
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        cancel.Click += (_, _) => _cancelled = true;

        sizer.Add(_gauge, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        sizer.Add(cancel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.AlignRight, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = detailed ? new Size(560, 320) : new Size(420, 160);
        _dialog.Center(onParent: true);
        // Closing the window means the same as pressing Cancel - and is refused, so the window stays until
        // the job it belongs to has actually stopped. Letting it go would destroy a dialog the timer is
        // still writing to. The Python player refuses it the same way, by not passing the event on.
        _dialog.Closing += (_, args) =>
        {
            _cancelled = true;
            args.Veto();
        };

        _dialog.Show();
        // Everything else is held off while the job runs - this window excepted, so its Cancel button still
        // answers. That is what the Python player's modal task dialog does, and it is not cosmetic: without
        // it the window this was opened from can be closed underneath it, taking the parent of a live
        // dialog with it while a timer is still writing to it.
        _elsewhere = Wx.DisableWindows(_dialog);
        // So a screen reader lands on the way out rather than on the bar.
        cancel.Focus();
    }

    public bool Cancelled => _cancelled;

    public void Update(int percent, string message)
    {
        if (_disposed)
            return;
        if (_proportional)
            _gauge.Value = Math.Clamp(percent, 0, 100) * (Range / 100);
        if (_text is not null)
            _text.Value = message;
        else if (_label is not null)
            _label.Label = message;
    }

    /// <summary>Moves a bar that has no figure behind it, so the window still says "working" while a job
    /// that cannot measure itself runs.</summary>
    public void Pulse()
    {
        if (!_disposed && !_proportional)
            _gauge.Pulse();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        // Let the rest of the player back in before this window goes, so whatever the job does next - a
        // message box, the window it came from - opens over something that can take the focus.
        _elsewhere.Dispose();
        _dialog.Dispose();
    }
}
