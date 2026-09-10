using System.Globalization;
using System.Text.Json.Serialization;

namespace LunaPlayer.Equalizer;

/// <summary>What shape a band has.</summary>
///
/// <remarks>
/// This decides which filter the band is, not merely how it is set up, and a running filter cannot be
/// turned into a different one - only its settings can be changed. That is why the slots below are laid
/// out by shape and keep it: a preset wanting a shelf where another wanted a peak would otherwise mean
/// building the whole graph again, which is the break in the sound this design exists to avoid.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<BandType>))]
internal enum BandType
{
    /// <summary>A bell around one frequency, leaving everything either side of it alone.</summary>
    Peaking,

    /// <summary>Everything below the frequency, lifted or cut together. What a bass control is.</summary>
    LowShelf,

    /// <summary>Everything above the frequency, lifted or cut together. What a treble control is.</summary>
    HighShelf,
}

/// <summary>One band of the equalizer: what shape it is, where it sits, how wide it is, and how far it
/// lifts or cuts.</summary>
///
/// <param name="Frequency">The centre of a peak, or the corner a shelf turns at, in hertz.</param>
/// <param name="Q">How wide the band is, as a Q factor. Larger is narrower. See
/// <see cref="Bands.QForOctaves"/> for what a given width in octaves works out as.</param>
/// <param name="Gain">How far the band is lifted or cut, in decibels. Zero is untouched.</param>
/// <param name="Type">The band's shape. Peaking unless said otherwise, which is what most bands are.
/// </param>
internal readonly record struct Band(
    double Frequency,
    double Q,
    double Gain,
    BandType Type = BandType.Peaking);

/// <summary>A named curve: the bands it moves.</summary>
///
/// <param name="Id">What the setting file stores. Never translated and never changed once released, or a
/// saved choice would stop naming anything.</param>
/// <param name="Name">What the menu shows, in the user's language.</param>
/// <param name="Bands">The bands this preset moves. A preset carries its own frequencies rather than
/// gains against a fixed grid, so it is free to describe a curve the player's own layout cannot.</param>
///
/// <remarks>
/// There is no overall gain here, and there was: every preset that lifted a band used to turn the whole
/// signal down by as much as it lifted, to leave room for the lift. It is the right way to avoid clipping
/// and the wrong thing to do to a preset, because it inverts what the preset is for. Bass Boost became
/// bass at its original level with everything else eight decibels down; Loudness, which exists to lift
/// both ends for quiet listening, cut the midrange - the voice - by eight decibels and so did the exact
/// opposite of its name.
///
/// A lift is now simply a lift, and the preset is louder than Off. What guards against clipping is the
/// limiter in the normalization filter, which sits after the equalizer in the chain and is on by default;
/// a user who has turned it off has already chosen to do without it, as they have with a volume control
/// that reaches ten times unity.
/// </remarks>
internal sealed record Preset(
    string Id, string Name, IReadOnlyList<Band> Bands);

/// <summary>The equalizer's slot layout and its limits.</summary>
///
/// <remarks>
/// The filter is a fixed row of slots, each keeping its shape for the life of the engine: a low shelf,
/// then fifteen peaks, then a high shelf. Presets are poured into it. A slot no preset asked for is left
/// flat, and a flat band is transparent whatever its frequency, so unused slots cost nothing beyond the
/// arithmetic of a biquad that is not doing anything.
///
/// The peaks sit on the ISO 266 preferred centre frequencies. Not all of them: every second entry of
/// that one-third-octave series, which leaves neighbours a ratio of 1.585 apart - two thirds of an
/// octave, since 2^(2/3) is 1.587 - and fifteen of them reaching from 25 Hz to 16 kHz. It is worth being
/// exact about that, because the numbers 25, 40, 63, 100 are recognisable as the one-third-octave
/// series they are drawn from and it would be easy to read the spacing as a third of an octave as well.
/// It is not; a third of an octave would put 31.5 between 25 and 40. <see cref="DefaultQ"/> is
/// calculated from the same two thirds, so the bands are as wide as they are far apart.
///
/// The two shelves are what the peaks cannot do. A bell at 25 Hz lifts a narrow strip and leaves 60 Hz
/// where it was, which is not what anybody means by more bass; a shelf lifts everything below its corner
/// together, which is. Where those corners sit, and how wide they are, was decided by measuring what
/// ffmpeg's shelving filters actually do rather than by copying a figure - see the note on
/// <see cref="Presets"/>, which is where that measurement is recorded and why it mattered.
/// </remarks>
internal static class Bands
{
    /// <summary>The furthest a band can be cut or lifted, in decibels.</summary>
    /// <remarks>
    /// Twenty rather than the twelve a consumer equalizer usually offers, because a shelf needs the room:
    /// what a shelf is worth is decided well away from its corner, and the gain it is given is not what
    /// arrives there.
    /// </remarks>
    internal const double MinimumGain = -20;
    internal const double MaximumGain = 20;

    /// <summary>The range a band may be placed in, in hertz. The whole of hearing, so that editing a
    /// preset is not fenced in by where the player happens to put its own bands.</summary>
    internal const double MinimumFrequency = 20;
    internal const double MaximumFrequency = 20000;

    /// <summary>How narrow or broad a band may be made. Below a tenth it is barely a filter at all, and
    /// above ten it is narrower than anything musical.</summary>
    internal const double MinimumQ = 0.1;
    internal const double MaximumQ = 10;

    /// <summary>Where the low shelf turns, in hertz. High enough to reach the bass of most music
    /// rather than only what lies beneath it - a shelf is half its stated gain at its corner and falls
    /// away above, so the corner has to sit above the range it is meant to lift, not in the middle of it.
    /// </summary>
    internal const double LowShelfFrequency = 250;

    /// <summary>Where the high shelf turns, in hertz. Low enough that there is still something there to
    /// lift.</summary>
    internal const double HighShelfFrequency = 6000;

    /// <summary>How gently the shelves turn. Gentler than the usual 0.7, which in these filters is steep
    /// enough to dip below unity just past the corner instead of levelling out.</summary>
    internal const double ShelfQ = 0.5;

    /// <summary>The slot holding the low shelf.</summary>
    internal const int LowShelfSlot = 0;

    /// <summary>The first slot holding a peak.</summary>
    internal const int FirstPeakingSlot = 1;

    /// <summary>The centre frequencies of the peaking bands, in hertz.</summary>
    internal static IReadOnlyList<double> Frequencies { get; } =
        [25, 40, 63, 100, 160, 250, 400, 630, 1000, 1600, 2500, 4000, 6300, 10000, 16000];

    /// <summary>The width of the peaking bands, matching their two-thirds-octave spacing so that
    /// neighbouring bands meet rather than overlapping or leaving a gap between them.</summary>
    internal static double DefaultQ { get; } = QForOctaves(2d / 3d);

    /// <summary>How many peaks a preset may have.</summary>
    internal static int PeakingSlotCount => Frequencies.Count;

    /// <summary>The slot holding the high shelf, after every peak.</summary>
    internal static int HighShelfSlot => FirstPeakingSlot + Frequencies.Count;

    /// <summary>How many filters the equalizer holds: the two shelves and every peak.</summary>
    /// <remarks>Read off <see cref="Frequencies"/> rather than written down beside it, so that changing
    /// the layout cannot leave a count behind that no longer describes it.</remarks>
    internal static int SlotCount => HighShelfSlot + 1;

    /// <summary>What a slot holds when nothing is asking anything of it: its own shape and frequency,
    /// flat.</summary>
    internal static Band Neutral(int slot)
    {
        if (slot == LowShelfSlot)
            return new Band(LowShelfFrequency, ShelfQ, 0, BandType.LowShelf);
        if (slot == HighShelfSlot)
            return new Band(HighShelfFrequency, ShelfQ, 0, BandType.HighShelf);
        return new Band(Frequencies[slot - FirstPeakingSlot], DefaultQ, 0);
    }

    /// <summary>The bands a preset puts in each slot.</summary>
    /// <remarks>
    /// Every band goes to a slot of its own shape, and a preset's peaks fill the peaking slots in the
    /// order it lists them. A preset with more peaks than there are slots would lose the last of them,
    /// which <see cref="Presets"/> refuses to build rather than letting it happen quietly.
    /// </remarks>
    internal static Band[] Slots(Preset? preset)
    {
        var slots = new Band[SlotCount];
        for (var slot = 0; slot < slots.Length; slot++)
            slots[slot] = Neutral(slot);
        if (preset is null)
            return slots;

        var peaking = FirstPeakingSlot;
        foreach (var band in preset.Bands)
        {
            switch (band.Type)
            {
                case BandType.LowShelf:
                    slots[LowShelfSlot] = band;
                    break;
                case BandType.HighShelf:
                    slots[HighShelfSlot] = band;
                    break;
                default:
                    slots[peaking++] = band;
                    break;
            }
        }
        return slots;
    }

    /// <summary>The Q factor giving a band the requested width in octaves.</summary>
    /// <remarks>
    /// The standard relation for a peaking filter: a band an octave wide has a Q of about 1.41, and one
    /// two thirds of an octave wide about 2.14. Written out rather than given as a constant so that
    /// changing the band spacing changes the width with it.
    /// </remarks>
    internal static double QForOctaves(double octaves)
    {
        var ratio = Math.Pow(2, octaves);
        return Math.Sqrt(ratio) / (ratio - 1);
    }

    internal static string Format(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
