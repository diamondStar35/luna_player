using WxSharp;

namespace LunaPlayer.UI;

internal sealed class CustomButton : Button
{
    internal CustomButton(Window parent, string label)
        : base(parent, label: label)
    {
    }

    public override bool AcceptsFocus() => false;

    public override bool AcceptsFocusFromKeyboard() => false;
}
