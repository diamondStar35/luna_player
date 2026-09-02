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
/// There are two halves to being named. This says which ID the process answers to; turning that ID into
/// something readable is the shortcut's job, and the installer puts the ID on the Start menu shortcut it
/// creates. A copy that was simply unzipped has no shortcut and so stays unnamed in the media overlay -
/// there is no registry registration that stands in for one, which was tried and does not work.
///
/// <see cref="Apply"/> has to run before anything registers with the shell on the process's behalf - in
/// particular before the media transport session is created, which takes the identity as it finds it and
/// does not look again.
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
