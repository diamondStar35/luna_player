using WxSharp;

namespace LunaPlayer.UI;

internal sealed class ProgressView : IProgressView
{
    private readonly ProgressDialog _dialog;

    internal ProgressView(Window parent, string title, string message, int maximum)
        => _dialog = new ProgressDialog(title, message, Math.Max(1, maximum), parent,
            ProgressDialogStyle.CanAbort | ProgressDialogStyle.AutoHide | ProgressDialogStyle.AppModal);

    public bool Update(int value, string message) => _dialog.Update(value, message).Continue;
    public bool Pulse(string message) => _dialog.Pulse(message).Continue;
    public void Dispose() => _dialog.Destroy();
}
