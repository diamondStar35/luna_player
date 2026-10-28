using LunaPlayer.Iptv;
using LunaPlayer.Tests.Infrastructure;
namespace LunaPlayer.Tests.Media;

[TestClass, TestCategory("Unit")]
public sealed class PlaylistReaderTests
{
    [TestMethod]
    [DataRow(".m3u")]
    [DataRow(".m3u8")]
    public void RelativeUnicodePathsAndDuplicatesArePreserved(string extension)
    {
        using var workspace = new Workspace();
        var song = workspace.File("√€‰Ì….mp3");
        var playlist = workspace.File("list" + extension, "#EXTM3U\n#EXTINF:-1,Title\n√€‰Ì….mp3\n√€‰Ì….mp3\n");
        var result = PlaylistReader.ReadLocal(playlist);
        Assert.IsNull(result.Error);
        Assert.AreEqual(2, result.Entries.Count);
        Assert.AreEqual(song, result.Entries[0].Location);
        Assert.AreEqual(song, result.Entries[1].Location);
        Assert.AreEqual("Title", result.Entries[0].Title);
    }
    [TestMethod]
    public void HlsChunksAreNotPresentedAsSeparateSongs()
    {
        using var workspace = new Workspace();
        var path = workspace.File("live.m3u8", "#EXTM3U\n#EXT-X-TARGETDURATION:10\n#EXTINF:10,\nchunk.ts\n");
        Assert.IsTrue(PlaylistReader.ReadLocal(path).IsHls);
    }
}
