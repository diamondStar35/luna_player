using System.Runtime.ExceptionServices;
using LunaPlayer.Tests.Infrastructure;
using LunaPlayer.UI.Subsonic;
using WxSharp;

namespace LunaPlayer.Tests.Subsonic.Presentation;

[TestClass, DoNotParallelize, TestCategory("Integration"), TestCategory("Subsonic")]
public sealed class HelpTests
{
    [TestMethod]
    public void FocusedControlHelpTakesPriorityOverItsTabAndFallbackTracksTheSelectedTab()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                NativeHost.Initialize(); using var app = new App(); using var form = new Form(null, "Help lookup");
                Assert.AreEqual("", form.GetContextHelp(null));
                var tab = new Panel(form.Panel); var field = new TextCtrl(tab);
                var selectedTab = "Connection help";
                form.PageHelp = () => selectedTab;
                form.Help(tab, "Playback help");
                Assert.AreEqual("Playback help", form.GetContextHelp(field));
                form.Help(field, "Bitrate help");
                Assert.AreEqual("Bitrate help", form.GetContextHelp(field));
                Assert.AreEqual("Connection help", form.GetContextHelp(null));
                selectedTab = "Browsing help";
                Assert.AreEqual("Browsing help", form.GetContextHelp(null));
            }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
