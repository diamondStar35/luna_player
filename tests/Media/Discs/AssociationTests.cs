using Microsoft.Win32;
using LunaPlayer.Media.Discs;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Media.Discs;

[TestClass, TestCategory("Unit")]
public sealed class AssociationTests
{
    [TestMethod]
    public void WindowsResolvesTheDvdPlayCommandWithoutAnExplicitVerb()
        => WithUserKey(user =>
        {
            const string executable = @"C:\Media Players\LunaPlayer.exe";
            Association.Register(user, executable, "Play DVD");
            using var dvd = user.OpenSubKey(@"Software\Classes\DVD");
            var command = CommandLine.Resolve(dvd!);
            var arguments = CommandLine.Parse(command.Replace("%1", @"F:\", StringComparison.Ordinal));
            Assert.AreEqual(executable, arguments[0]);
            var request = Request.FromArguments(arguments[1..]);
            Assert.IsNotNull(request);
            Assert.IsNull(request.Format);
            Assert.AreEqual(@"F:\", request.Location);
        });

    [TestMethod]
    public void RegistrationAndRemovalPreserveOtherDiscAndDriveDefaults()
        => WithUserKey(user =>
        {
            using var shell = user.CreateSubKey(@"Software\Classes\DVD\shell");
            shell.SetValue(null, "OtherPlayer");
            using var other = shell.CreateSubKey(@"OtherPlayer\command");
            other.SetValue(null, "other-player.exe");
            using var drive = user.CreateSubKey(@"Software\Classes\Drive\shell");
            drive.SetValue(null, "none");
            Association.Register(user, @"C:\LunaPlayer.exe", "Play DVD");
            Assert.AreEqual("OtherPlayer", shell.GetValue(null));
            Association.Unregister(user);
            Association.Unregister(user);
            Assert.AreEqual("OtherPlayer", shell.GetValue(null));
            Assert.AreEqual("other-player.exe", other.GetValue(null));
            Assert.AreEqual("none", drive.GetValue(null));
            using var removed = user.OpenSubKey(Association.Verb);
            Assert.IsNull(removed);
        });

    private static void WithUserKey(Action<RegistryKey> action)
    {
        var path = $@"Software\LunaPlayer.Tests\DVD-{Guid.NewGuid():N}";
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(path, writable: true);
            action(key);
        }
        finally { Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false); }
    }
}
