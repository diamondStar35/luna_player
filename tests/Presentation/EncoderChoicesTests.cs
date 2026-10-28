using LunaPlayer.UI.Recording;
using LunaPlayer.Recording;
namespace LunaPlayer.Tests.Presentation;
[TestClass, TestCategory("Unit")]
public sealed class EncoderChoicesTests
{
    [TestMethod]
    [DataRow(-1, 1)]
    [DataRow(0, 1)]
    [DataRow(99, 2)]
    public void RateSelectionUsesTheCorrespondingChannelChoices(int selection, int expected)
    {
        var model = new EncoderChoices { Support = new FormatSupport([16000, 48000], [new[] { 1 }, new[] { 1, 2 }]) };
        model.SelectRate(selection);
        Assert.AreEqual(expected, model.ChannelCounts.Count);
    }
}
