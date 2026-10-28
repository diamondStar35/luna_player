using LunaPlayer.Recording;
namespace LunaPlayer.UI.Recording;
internal sealed class EncoderChoices
{
    internal IReadOnlyList<int> Bitrates = [];
    internal FormatSupport Support = new([.. AudioCatalog.SampleRates], [.. AudioCatalog.SampleRates.Select(_ => (IReadOnlyList<int>)new[] { 1, 2 })]);
    internal IReadOnlyList<int> Rates = [.. AudioCatalog.SampleRates];
    internal IReadOnlyList<int> ChannelCounts = [1, 2];
    internal void SelectRate(int index)
    {
        index = Math.Clamp(index, 0, Math.Max(0, Support.Rates.Count - 1));
        ChannelCounts = Support.Rates.Count == 0 ? [] : Support.Channels[index];
    }
}
