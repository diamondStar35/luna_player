using System.Runtime.InteropServices;
using WxSharp;

namespace LunaPlayer.UI.Services;

internal static partial class Messages
{
    internal static void Success(string message, Window? parent)
    {
        // Translators: Title of a message box confirming that an operation completed successfully.
        var caption = Tr("Success");
        Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, parent);
    }

    internal static MessageBoxStyle Show(string message, MessageBoxStyle style, Window? parent, bool ownerOnly = false)
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
        if (!ownerOnly || parent is null) return Wx.MessageBox(message, caption, style, parent);
        // wxMessageDialog disables every top-level window before calling Windows.
        // A native owner-modal box leaves the rest of the player available.
        return MessageBox(parent.NativeHandle, message, caption, NativeStyle(style)) switch
        {
            1 => MessageBoxStyle.Ok,
            6 => MessageBoxStyle.Yes,
            7 => MessageBoxStyle.No,
            _ => MessageBoxStyle.Cancel,
        };
    }

    internal static uint NativeStyle(MessageBoxStyle style)
    {
        var yesNo = (style & MessageBoxStyle.YesNo) == MessageBoxStyle.YesNo;
        var cancel = (style & MessageBoxStyle.Cancel) != 0;
        uint flags = yesNo ? cancel ? 3u : 4u : cancel ? 1u : 0u;
        if ((style & MessageBoxStyle.IconError) != 0) flags |= 0x10;
        else if ((style & (MessageBoxStyle.IconWarning | MessageBoxStyle.IconQuestion)) != 0) flags |= 0x30;
        else if ((style & MessageBoxStyle.IconInformation) != 0) flags |= 0x40;
        if ((style & MessageBoxStyle.NoDefault) != 0 && yesNo) flags |= 0x100;
        else if ((style & MessageBoxStyle.CancelDefault) != 0 && cancel) flags |= yesNo ? 0x200u : 0x100u;
        if ((style & MessageBoxStyle.StayOnTop) != 0) flags |= 0x40000;
        return flags;
    }

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBox(nint owner, string text, string caption, uint flags);
}
