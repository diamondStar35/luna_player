using System.Runtime.InteropServices;
using LunaPlayer.Configuration;

namespace LunaPlayer.Application;

/// <summary>Tells Windows who this process is.</summary>
///
/// <remarks>
/// Windows identifies a running program by its AppUserModelID. A process that never sets one is anonymous:
/// its windows group under the executable rather than under the application, it can hold no jump list, and
/// the media overlay labels whatever it is playing "Unknown app" because it has no owner to name.
///
/// This has to run before anything that registers with the shell on the process's behalf - in particular
/// before the media transport session is created, which takes the identity as it finds it and does not look
/// again.
///
/// Setting the ID is only half of it. Windows turns an ID into a name and an icon by matching it against a
/// Start menu shortcut carrying the same <c>System.AppUserModel.ID</c> property, so until an installer
/// creates one there may still be no friendly name to show - but the grouping and the session ownership are
/// right either way, and nothing here can fail in a way worth telling the user about.
/// </remarks>
internal static partial class ProcessIdentity
{
    internal static void Apply()
    {
        try
        {
            _ = SetCurrentProcessExplicitAppUserModelID(AppInfo.AppUserModelId);
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            // A Windows without this entry point is old enough that none of what it buys us exists either.
        }
    }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SetCurrentProcessExplicitAppUserModelID(string appId);
}
