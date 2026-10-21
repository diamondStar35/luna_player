using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Before anything at all, so a failure in the setting up below is still reported.
        CrashReport.Install();

        var singleInstance = new SingleInstanceService();
        if (!singleInstance.IsPrimary)
        {
            var hasPaths = args.Any(static value => !string.IsNullOrWhiteSpace(value));
            if (!hasPaths || SettingsStore.PeekOpenFilesInExistingWindow(Paths.SettingsFile))
            {
                if (singleInstance.ForwardPaths(args))
                {
                    singleInstance.Dispose();
                    return 0;
                }

                // The primary process owns the mutex but could not receive the request. Keep this process
                // alive as an independent instance so an Explorer-opened file is never silently discarded.
                singleInstance.Dispose();
                singleInstance = new SingleInstanceService(independent: true);
            }
            else
            {
                // This launch was explicitly configured to own a separate window. It neither receives paths
                // intended for the original window nor competes for that window's pipe and mutex.
                singleInstance.Dispose();
                singleInstance = new SingleInstanceService(independent: true);
            }
        }

        // Before anything registers with the shell on this process's behalf: the media transport session
        // reads the identity once, when it is created, and never looks again. A forwarding-only process
        // never reaches this work.
        ProcessIdentity.Apply();

        NativeLibraryBootstrap.Initialize();

        // Apply the saved theme before the App is created: wxWidgets reads the msw.dark-mode system option
        // as it initializes. Only the theme is read here, with a light-weight peek - the full settings load
        // validates settings, which builds the translated action tables and so needs a running App, so it
        // stays in ApplicationHost.
        LunaPlayer.UI.ThemeService.Apply(SettingsStore.PeekTheme(Paths.SettingsFile));

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
