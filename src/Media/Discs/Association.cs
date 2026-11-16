using System.Runtime.Versioning;
using Microsoft.Win32;

namespace LunaPlayer.Media.Discs;

/// <summary>The DVD class's Explorer play verb. Ordinary folder and drive associations are independent.</summary>
[SupportedOSPlatform("windows")]
internal static class Association
{
    internal const string Verb = @"Software\Classes\DVD\shell\play_with_luna";

    internal static void Register(RegistryKey user, string executable, string label)
    {
        using var verb = user.CreateSubKey(Verb, writable: true);
        verb.SetValue(null, label, RegistryValueKind.String);
        verb.SetValue("Icon", $"\"{executable}\",0", RegistryValueKind.String);
        using var command = verb.CreateSubKey("command", writable: true);
        command.SetValue(null, Request.Command(executable, Request.Argument), RegistryValueKind.String);
    }

    internal static void Unregister(RegistryKey user)
        => user.DeleteSubKeyTree(Verb, throwOnMissingSubKey: false);
}
