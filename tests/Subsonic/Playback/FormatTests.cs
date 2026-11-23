using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.Subsonic.Playback;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Subsonic.Playback;

[TestClass, TestCategory("Unit"), TestCategory("Subsonic")]
public sealed class FormatTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DecoderMeasurementsWinAndAnotherCodecDoesNotInheritNegotiatedQuality(bool fallback)
    {
        using var engine = new FakeEngine(); using var player = new Player(engine); var pump = new Pump();
        engine.ReportedFormat = new(fallback ? "opus" : "flac", fallback ? "ogg" : "flac", SampleRate: fallback ? 48000 : 96000, Channels: 2);
        player.SetMediaResolver(new Resolver(), pump.Post);
        player.InsertMedia([(new Reference(Guid.NewGuid(), "song").Address, MediaTags.None)], true);
        await pump.Until(() => player.IsLoaded);
        var format = player.CurrentStreamFormat!;
        Assert.AreEqual(fallback ? "opus" : "flac", format.Codec);
        Assert.AreEqual(fallback ? 48000 : 96000, format.SampleRate);
        Assert.AreEqual(fallback ? (int?)null : 24, format.BitDepth);
        Assert.AreEqual(fallback ? (int?)null : 1000000, format.BitRate);
        Assert.AreEqual(fallback ? (bool?)null : true, format.Transcoded);
    }
    private sealed class Resolver : IMediaResolver
    {
        public bool Handles(string address) => Reference.IsAddress(address);
        public Task<MediaInput> ResolveAsync(string address, double? position, CancellationToken token)
            => Task.FromResult(new MediaInput("https://invalid/stream", MediaTags.None, Format:
                new("flac", "flac", 1000000, 44100, 24, 2, true)));
    }
}
