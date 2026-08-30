using LunaPlayer.Application;
using WxSharp;

namespace LunaPlayer;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
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
        finally
        {
            singleInstance.Dispose();
        }
    }
}
