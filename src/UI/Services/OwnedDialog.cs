using WxSharp;

namespace LunaPlayer.UI.Services;

/// <summary>Runs a child dialog while disabling only its owner, without a nested wx modal loop.</summary>
internal sealed class OwnedDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly Window? _owner;
    private bool _ownerWasEnabled;
    private bool _shown;
    private bool _finished;
    private bool _disposed;

    internal OwnedDialog(Dialog dialog, Window? owner)
    {
        _dialog = dialog;
        _owner = owner;
        _dialog.Bind(WxEvents.ButtonClicked, (_, _) => Complete(false), StandardId.Cancel);
        _dialog.Bind(WxEvents.CharHook, (_, args) =>
        {
            if (args.Code == Key.Escape) Complete(false);
            else args.Skip();
        });
        _dialog.Closing += (_, args) =>
        {
            args.Veto();
            Complete(false);
        };
    }

    internal event Action<bool>? Closed;
    internal Func<bool>? CanCancel { get; set; }

    internal void Show()
    {
        if (_finished) return;
        if (!_shown)
        {
            _ownerWasEnabled = _owner?.Enabled ?? true;
            _dialog.Show();
            if (_owner is not null) _owner.Enabled = false;
            _shown = true;
        }
        Activate();
    }

    internal bool Activate()
    {
        if (_finished || !_dialog.Enabled) return false;
        _dialog.Raise();
        return true;
    }

    internal void Complete(bool accepted)
    {
        if (_finished || !accepted && CanCancel?.Invoke() == false) return;
        _finished = true;
        RestoreOwner();
        _dialog.Hide();
        _owner?.Raise();
        Closed?.Invoke(accepted);
    }

    private void RestoreOwner()
    {
        if (!_shown) return;
        _shown = false;
        if (_owner is not null) _owner.Enabled = _ownerWasEnabled;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _finished = true;
        RestoreOwner();
        _dialog.Dispose();
    }
}
