using WxSharp;

namespace LunaPlayer.UI;

/// <summary>The window shown while a long job runs: a message, a bar, and a button to give up.</summary>
///
/// <remarks>
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
/// The bar counts from nought to a hundred and is never put into the marquee mode wxGauge::Pulse asks for.
/// Two reasons: switching between marquee and a value makes the bar appear to restart every time, and a
/// marquee bar has no value at all for a screen reader to read - it is announced as nought per cent however
/// busy it looks. Taking a percentage rather than a count is what lets the range stay fixed, so a job that
/// changes what it is counting cannot drag the bar backwards or pin it to its end.
///
/// A job that cannot say how far through it is gets no bar at all, only its message and the way out. That is
/// the honest arrangement: the alternative is a bar sitting at nought for the whole run, which tells a
/// sighted user nothing and tells a screen reader something untrue.
///
/// The parent is deliberately left enabled. Disabling it would disable this dialog too - a child window of a
/// disabled window cannot be clicked - which is exactly how the Cancel button stops answering. The Python
/// player does not make its progress dialog application modal either.
/// </remarks>
internal sealed class ProgressView : IProgressView
{
    private readonly Dialog _dialog;
    private readonly StaticText _message;
    private readonly Gauge? _gauge;
    private bool _cancelled;
    private bool _disposed;

    internal ProgressView(Window parent, string title, string message, bool proportional)
    {
        // Caption only: with no close box and no system menu there is no way for the window to be destroyed
        // out from under the job that is driving it. Cancel is the way out.
        _dialog = new Dialog(parent, title: title, style: DialogStyle.Caption);
        _message = new StaticText(_dialog, label: message);
        _gauge = proportional ? new Gauge(_dialog, range: 100) : null;
        // Translators: The button that stops a job that is running behind a progress window.
        var cancel = new Button(_dialog, label: Tr("Cancel"));
        cancel.Click += (_, _) => _cancelled = true;

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_message, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        if (_gauge is not null)
            sizer.Add(_gauge, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.Expand, border: 10);
        sizer.Add(cancel, flags: SizerFlags.All | SizerFlags.AlignCenterHorizontal, border: 10);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(360, _dialog.MinSize.Height);
        _dialog.Center(onParent: true);

        _dialog.Show();
        // So a screen reader lands on the way out rather than on the bar.
        cancel.Focus();
    }

    public bool Cancelled => _cancelled;

    public void Update(int percent, string message)
    {
        if (_disposed)
            return;
        if (_gauge is not null)
            _gauge.Value = Math.Clamp(percent, 0, 100);
        _message.Label = message;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _dialog.Dispose();
    }
}
