using LunaPlayer.Application;
using WxSharp;

namespace LunaPlayer;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Before anything at all, so a failure in the setting up below is still reported.
        CrashReport.Install();

        // Before anything else registers with the shell on this process's behalf: the media transport
        // session reads the identity once, when it is created, and never looks again.
        ProcessIdentity.Apply();

        var singleInstance = new SingleInstanceService();
        if (!singleInstance.IsPrimary)
        {
            using (singleInstance)
                singleInstance.ForwardPaths(args);
            return 0;
        }

        NativeLibraryBootstrap.Initialize();

        try
        {
            using var app = new App();
            using var host = new ApplicationHost(singleInstance, args);
            host.Show();
            return app.MainLoop();
        }
        catch (Exception failure)
        {
            // Only what escapes the event loop itself reaches here - starting up, shutting down, or a
            // callback the guards below do not cover. Everything inside the loop is caught nearer to where
            // it happened, because an exception cannot be unwound back through wxWidgets' own frames.
            CrashReport.Report(failure);
            return 1;
        }
        finally
        {
            singleInstance.Dispose();
        }
    }
}
