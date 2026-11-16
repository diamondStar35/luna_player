using System.Runtime.ExceptionServices;
using LunaPlayer.Tests.Infrastructure;
using LunaPlayer.UI.Services;
using WxSharp;

namespace LunaPlayer.Tests.Presentation;

[TestClass, DoNotParallelize]
public sealed class NativeDialogsTests
{
    [TestMethod, TestCategory("Unit")]
    [DataRow(MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, 0x40u)]
    [DataRow(MessageBoxStyle.Ok | MessageBoxStyle.IconError, 0x10u)]
    [DataRow(MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, 0x34u)]
    [DataRow(MessageBoxStyle.YesNo | MessageBoxStyle.NoDefault | MessageBoxStyle.IconQuestion, 0x134u)]
    [DataRow(MessageBoxStyle.YesNo | MessageBoxStyle.Cancel | MessageBoxStyle.CancelDefault, 0x203u)]
    public void OwnedMessagesPreserveButtonsWithoutTaskModality(MessageBoxStyle style, uint expected)
    {
        var flags = Messages.NativeStyle(style);
        Assert.AreEqual(expected, flags);
        Assert.AreEqual(0u, flags & 0x2000u, "MB_TASKMODAL would disable unrelated windows.");
    }

    [TestMethod, TestCategory("Integration")]
    [DataRow(false)]
    [DataRow(true)]
    public void FolderPickerConfiguresAndReleasesWithoutChangingWindowState(bool missingFolder)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                NativeHost.Initialize();
                using var app = new App();
                using var main = new Frame(title: "Main");
                using var owner = new Dialog(parent: null, title: "Recording", style: DialogStyle.Default | DialogStyle.NoParent);
                using var workspace = new Workspace();
                var initial = missingFolder ? Path.Combine(workspace.Root, "missing") : workspace.Root;
                Assert.AreNotEqual(nint.Zero, owner.NativeHandle);
                Assert.AreNotEqual(main.NativeHandle, owner.NativeHandle);
                using (var picker = new FolderDialog(owner, "Choose folder", initial))
                {
                    Assert.IsTrue(main.Enabled);
                    Assert.IsTrue(owner.Enabled);
                    picker.Dispose();
                }
                Assert.IsTrue(main.Enabled);
                Assert.IsTrue(owner.Enabled);
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
