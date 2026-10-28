using WxSharp;

namespace LunaPlayer.UI.Controls;

/// <summary>A transport button whose whole face is its icon, with no visible text, but which still carries a
/// spoken name for screen readers (wxBitmapButton draws only the bitmap and uses the label purely as the
/// accessible name). Like the text buttons it replaced, it declines keyboard focus: the main window's
/// controls are reached through the menus and shortcut keys, never Tab.</summary>
internal sealed class CustomBitmapButton : BitmapButton
{
    internal CustomBitmapButton(Window parent, Bitmap bitmap, string accessibleName)
        : base(parent, bitmap)
        => Label = accessibleName;

    public override bool AcceptsFocus() => false;

    public override bool AcceptsFocusFromKeyboard() => false;
}
