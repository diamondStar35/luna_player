using LunaPlayer.UI.Conversion;
using LunaPlayer.Media;
namespace LunaPlayer.Tests.Presentation;
[TestClass, TestCategory("Unit")]
public sealed class ConversionTests
{
    [TestMethod]
    [DataRow(".mp3", "libmp3lame")]
    [DataRow(".flac", "flac")]
    [DataRow(".opus", "libopus")]
    public void CodecSelectionIsExplicit(string extension, string codec)
        => Assert.AreEqual(codec, RequestBuilder.CodecFor(extension));
    [TestMethod]
    public void PreservedLocationsCreateJobsBesideEachSource()
    {
        var request = RequestBuilder.Create([@"D:\one\song.wav", @"D:\two\song.wav"], ".mp3", true, @"D:\output", new ConversionSettings("libmp3lame", null, null, null, null), 2, false, "MP3");
        Assert.AreEqual(2, request.Jobs.Count);
        Assert.AreEqual(@"D:\one\song.mp3", request.Jobs[0].Destination);
        Assert.AreEqual(@"D:\two\song.mp3", request.Jobs[1].Destination);
    }
}
