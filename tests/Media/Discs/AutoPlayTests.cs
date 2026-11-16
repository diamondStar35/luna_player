using Microsoft.Win32;
using LunaPlayer.Media.Discs;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Media.Discs;

[TestClass, TestCategory("Unit")]
public sealed class AutoPlayTests
{
    [TestMethod]
    public void RegisteredCommandsPreserveTheDiscFormatAndQuotedDriveRoot()
        => WithUserKey(user =>
        {
            const string executable = @"C:\Media Players\Luna Player\LunaPlayer.exe";
            AutoPlay.Register(user, executable, "Play DVD-Video", "Play DVD-Audio");
            foreach (var handler in AutoPlay.Handlers)
            {
                using var definition = user.OpenSubKey($@"{AutoPlay.Root}\Handlers\{handler.Name}");
                Assert.AreEqual(handler.Program, definition!.GetValue("InvokeProgID"));
                Assert.AreEqual("open", definition.GetValue("InvokeVerb"));
                using var events = user.OpenSubKey($@"{AutoPlay.Root}\EventHandlers\{handler.Event}");
                Assert.AreEqual(string.Empty, events!.GetValue(handler.Name));
                using var program = user.OpenSubKey($@"Software\Classes\{handler.Program}\shell\open\command");
                var command = (string)program!.GetValue(null)!;
                foreach (var location in new[] { "F:", @"F:\", @"C:\Disc Images\Album" })
                {
                    var arguments = CommandLine.Parse(command.Replace("%1", location, StringComparison.Ordinal));
                    Assert.AreEqual(executable, arguments[0]);
                    var request = Request.FromArguments(arguments[1..]);
                    Assert.IsNotNull(request);
                    Assert.AreEqual(handler.Format, request.Format);
                    Assert.AreEqual(Path.GetFullPath(location.Length == 2 ? location + "\\" : location), request.Location);
                }
            }
        });

    [TestMethod]
    public void RemovingRegistrationPreservesOtherHandlersAndAutoPlayChoices()
        => WithUserKey(user =>
        {
            foreach (var handler in AutoPlay.Handlers)
            {
                using var events = user.CreateSubKey($@"{AutoPlay.Root}\EventHandlers\{handler.Event}");
                events.SetValue("OtherPlayer", string.Empty);
                using var choice = user.CreateSubKey($@"{AutoPlay.Root}\UserChosenExecuteHandlers\{handler.Event}");
                choice.SetValue(null, "OtherPlayer");
            }
            AutoPlay.Register(user, @"C:\LunaPlayer.exe", "Video", "Audio");
            AutoPlay.Register(user, @"D:\New location\LunaPlayer.exe", "Video", "Audio");
            AutoPlay.Unregister(user);
            AutoPlay.Unregister(user);
            foreach (var handler in AutoPlay.Handlers)
            {
                using var definition = user.OpenSubKey($@"{AutoPlay.Root}\Handlers\{handler.Name}");
                Assert.IsNull(definition);
                using var program = user.OpenSubKey($@"Software\Classes\{handler.Program}");
                Assert.IsNull(program);
                using var events = user.OpenSubKey($@"{AutoPlay.Root}\EventHandlers\{handler.Event}");
                Assert.IsNull(events!.GetValue(handler.Name));
                Assert.AreEqual(string.Empty, events.GetValue("OtherPlayer"));
                using var choice = user.OpenSubKey($@"{AutoPlay.Root}\UserChosenExecuteHandlers\{handler.Event}");
                Assert.AreEqual("OtherPlayer", choice!.GetValue(null));
            }
        });

    private static void WithUserKey(Action<RegistryKey> action)
    {
        var path = $@"Software\LunaPlayer.Tests\AutoPlay-{Guid.NewGuid():N}";
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(path, writable: true);
            action(key);
        }
        finally { Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false); }
    }

}
