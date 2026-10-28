using System.Runtime.InteropServices;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer.UI.Services;

/// <summary>Applies the user's theme choice through the wxWidgets <c>msw.dark-mode</c> system option.
/// wxWidgets reads that option while the application initializes, so this must run before the App is
/// created - which is why Program loads the settings and calls it up front.</summary>
internal static partial class ThemeService
{
    // wxWidgets reads this during wxApp start-up: 0 leaves dark mode off (a light interface), 1 turns it on
    // when the system is using dark mode, 2 forces dark mode on. See wxSystemOptions / wxApp::MSWEnableDarkMode.
    private const string MswDarkModeOption = "msw.dark-mode";

    public static void Apply(AppTheme theme)
    {
        // A high-contrast scheme is an accessibility choice, and its own palette must win. Forcing light or
        // dark over it would replace the colours the user depends on, so under high contrast the preference
        // is ignored and the system is followed (option 1) instead.
        var value = IsHighContrast()
            ? 1
            : theme switch
            {
                AppTheme.Dark => 2,
                AppTheme.Light => 0,
                _ => 1,
            };
        SystemOptions.SetOption(MswDarkModeOption, value);
    }

    private static bool IsHighContrast()
    {
        var info = new HighContrast { Size = (uint)Marshal.SizeOf<HighContrast>() };
        if (!SystemParametersInfo(SpiGetHighContrast, info.Size, ref info, 0))
            return false;
        return (info.Flags & HcfHighContrastOn) != 0;
    }

    private const uint SpiGetHighContrast = 0x0042;
    private const uint HcfHighContrastOn = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct HighContrast
    {
        public uint Size;
        public uint Flags;
        public nint DefaultScheme;
    }

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(uint action, uint param, ref HighContrast data, uint winIni);
}
