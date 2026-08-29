using LunaPlayer.UI;
using WxSharp;

namespace LunaPlayer;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        NativeLibraryBootstrap.Initialize();

        using var app = new App();
        using var frame = MainFrame.Create();

        frame.Show();
        return app.MainLoop();
    }
}
