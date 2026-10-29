using System.Text.Json;
using LunaPlayer.Discord;

namespace LunaPlayer.Tests.Discord;

[TestClass, TestCategory("Unit")]
public sealed class ApplicationArtworkTests
{
    [TestMethod]
    public void ApplicationIconResolvesWithoutANamedPresenceAsset()
    {
        using var document = JsonDocument.Parse("""{"icon":"930f1667326a41fa0865bd709fa45f0c"}""");
        Assert.AreEqual("https://cdn.discordapp.com/app-icons/1565330415529762956/930f1667326a41fa0865bd709fa45f0c.png?size=256",
            ApplicationArtwork.IconUrl("1565330415529762956", document.RootElement));
    }

    [TestMethod]
    [DataRow("{}")]
    [DataRow("{\"icon\":null}")]
    [DataRow("{\"icon\":\"https://untrusted.example/image\"}")]
    [DataRow("{\"icon\":\"../../file\"}")]
    [DataRow("[]")]
    public void MissingOrMalformedIconsDoNotInventAnArtworkAddress(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.IsNull(ApplicationArtwork.IconUrl("1565330415529762956", document.RootElement));
    }

    [TestMethod]
    public void AnimatedIconsCanUseAStaticPng()
    {
        using var document = JsonDocument.Parse("""{"icon":"a_930f1667326a41fa0865bd709fa45f0c"}""");
        StringAssert.EndsWith(ApplicationArtwork.IconUrl("1565330415529762956", document.RootElement)!,
            "/a_930f1667326a41fa0865bd709fa45f0c.png?size=256");
    }
}
