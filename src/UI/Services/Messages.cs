using WxSharp;

namespace LunaPlayer.UI.Services;

internal static class Messages
{
    internal static MessageBoxStyle Show(string message, MessageBoxStyle style, Window? parent)
    {
        string caption;
        if ((style & MessageBoxStyle.IconError) != 0)
        {
            // Translators: Title of a message box reporting an error.
            caption = Tr("Error");
        }
        else if ((style & (MessageBoxStyle.IconWarning | MessageBoxStyle.IconQuestion)) != 0)
        {
            // Translators: Title of a warning or confirmation message box.
            caption = Tr("Warning");
            style = (style & ~MessageBoxStyle.IconQuestion) | MessageBoxStyle.IconWarning;
        }
        else
        {
            // Translators: Title of an informational message box.
            caption = Tr("Information");
        }
        return Wx.MessageBox(message, caption, style, parent);
    }
}
