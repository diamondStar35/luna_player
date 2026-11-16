using System.Runtime.Versioning;
using Microsoft.Win32;
using LunaPlayer.Configuration;

namespace LunaPlayer.Media.Discs;

/// <summary>Per-user DVD shell registration, shared by file-association setup and portable copies.</summary>
[SupportedOSPlatform("windows")]
internal static class AutoPlay
{
    internal const string Root = @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers";
    private const string Classes = @"Software\Classes";
    private const string Capabilities = $@"Software\{AppInfo.Identifier}\Capabilities\AutoplayHandlers";
    internal sealed record Handler(string Event, string Name, string Program, string Argument, DiscFormat Format);
    internal static IReadOnlyList<Handler> Handlers { get; } = [
        new("PlayDVDMovieOnArrival", $"{AppInfo.Identifier}PlayDVDMovieOnArrival", $"{AppInfo.Identifier}.DVDVideo", Request.VideoArgument, DiscFormat.Video),
        new("PlayDVDAudioOnArrival", $"{AppInfo.Identifier}PlayDVDAudioOnArrival", $"{AppInfo.Identifier}.DVDAudio", Request.AudioArgument, DiscFormat.Audio),
    ];

    internal static void Register(RegistryKey user, string executable, string videoAction, string audioAction)
    {
        foreach (var handler in Handlers)
        {
            var program = $@"{Classes}\{handler.Program}";
            Write(user, $@"{program}\DefaultIcon", null, $"\"{executable}\",0");
            Write(user, $@"{program}\shell\open\command", null, Request.Command(executable, handler.Argument));
            var definition = $@"{Root}\Handlers\{handler.Name}";
            Write(user, definition, "Action", handler.Format == DiscFormat.Audio ? audioAction : videoAction);
            Write(user, definition, "Provider", AppInfo.Name);
            Write(user, definition, "DefaultIcon", $"\"{executable}\",0");
            Write(user, definition, "InvokeProgID", handler.Program);
            Write(user, definition, "InvokeVerb", "open");
            Write(user, $@"{Root}\EventHandlers\{handler.Event}", handler.Name, string.Empty);
            Write(user, Capabilities, handler.Event, handler.Name);
        }
    }

    internal static void Unregister(RegistryKey user)
    {
        foreach (var handler in Handlers)
        {
            using (var events = user.OpenSubKey($@"{Root}\EventHandlers\{handler.Event}", writable: true))
                events?.DeleteValue(handler.Name, throwOnMissingValue: false);
            using (var capabilities = user.OpenSubKey(Capabilities, writable: true))
                capabilities?.DeleteValue(handler.Event, throwOnMissingValue: false);
            user.DeleteSubKeyTree($@"{Root}\Handlers\{handler.Name}", throwOnMissingSubKey: false);
            user.DeleteSubKeyTree($@"{Classes}\{handler.Program}", throwOnMissingSubKey: false);
        }
    }

    private static void Write(RegistryKey user, string path, string? name, string value)
    {
        using var key = user.CreateSubKey(path, writable: true);
        key.SetValue(name, value, RegistryValueKind.String);
    }
}
