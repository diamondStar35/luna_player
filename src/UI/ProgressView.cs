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
/// There is deliberately no Pulse. On MSW, wxGauge::Pulse turns on the marquee window style and SetValue
/// turns it off again, so a caller that pulses whenever it has nothing new and sets a value when it does
/// makes the bar restart on every alternation. The bar simply holds its last value instead.
///
/// The parent is deliberately left enabled. Disabling it would disable this dialog too - a child window of a
/// disabled window cannot be clicked - which is exactly how the Cancel button stops answering. The Python
/// player does not make its progress dialog application modal either.
/// </remarks>
internal sealed class ProgressView : IProgressView
{
    private readonly Dialog _dialog;
    private readonly StaticText _message;
    private readonly Gauge _gauge;
    private readonly int _maximum;
    private bool _cancelled;
    private bool _disposed;

    internal ProgressView(Window parent, string title, string message, int maximum)
    {
        _maximum = Math.Max(1, maximum);
        // Caption only: with no close box and no system menu there is no way for the window to be destroyed
        // out from under the job that is driving it. Cancel is the way out.
        _dialog = new Dialog(parent, title: title, style: DialogStyle.Caption);
        _message = new StaticText(_dialog, label: message);
        _gauge = new Gauge(_dialog, range: _maximum);
        // Translators: The button that stops a job that is running behind a progress window.
        var cancel = new Button(_dialog, label: Tr("Cancel"));
        cancel.Click += (_, _) => _cancelled = true;

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_message, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
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

    public void Update(int value, string message)
    {
        if (_disposed)
            return;
        _gauge.Value = Math.Clamp(value, 0, _maximum);
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
