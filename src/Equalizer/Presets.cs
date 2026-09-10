namespace LunaPlayer.Equalizer;

/// <summary>The curves the player ships with.</summary>
///
/// <remarks>
/// All of these are designed here. An earlier version of this file carried the preset set Winamp
/// established and VLC still ships, taken from its source unaltered, and it was the wrong thing to
/// carry: those curves were drawn for a ten-band graphic equalizer in the 1990s, several of them lift
/// the top two octaves by eleven or twelve decibels at once, and octave-wide bands that all lift
/// together do not stay where they were written - Rock arrived as nearly twenty decibels of treble.
/// Scaling them down to fit was worse still, because a preset whose peak lives in a summed top end
/// loses everything else along with it. The names are worth keeping. The numbers were not.
///
/// Every curve below was measured rather than calculated, and the reason is worth recording: ffmpeg's
/// shelving filters do not behave the way the textbook shelf does when they are given a Q. A low shelf
/// at 105 Hz with a Q of 0.7 - the arrangement AutoEq uses, and the one this file used to - measures
/// +12 dB below 60 Hz but only +6.8 dB at 100 Hz, nothing at all by 150 Hz, and slightly below nothing
/// at 200 Hz. Most of the bass in most music sits in the part it does not reach, which is why a preset
/// announcing twelve decibels of bass could be barely audible.
///
/// What a shelf is worth is decided by where it stops, not by the number written on it. It reaches half
/// its stated gain at its corner and falls away above, so the corner belongs above the range being
/// lifted rather than in the middle of it. Measured at a Q of 0.5, a low shelf at 150 Hz gives +9.7 dB
/// at 100 Hz but only +1.7 at 250; the same shelf at 250 Hz gives +11.6 at 100 and +9.6 at 170, which
/// is where a kick drum and a bass guitar actually are. Going further does not help - a corner at
/// 400 Hz still lifts 310 Hz by 8.5 dB, and that region is mud rather than bass. The high shelf is the
/// same argument in reverse: at 10 kHz it reaches +5 dB at 10 kHz and nothing below 8 kHz, where most
/// music has little left to lift, so the general one sits at 6 kHz and the presets meant to be plainly
/// bright reach down to 4 kHz.
///
/// The peaking filters need no such care. Those do follow the usual arithmetic, confirmed against
/// ffmpeg the same way: a peak of eight decibels at 1 kHz with a Q of 2.145 measures +8.0 dB at 1 kHz
/// and +0.8 dB an octave either side, which is what the formula says it should.
///
/// One thing is deliberately not copied from players that do this with bells alone. Several of them
/// pair a boost with an uncompensated cut of two or three decibels, so the mids and the treble drop
/// while the bass rises. It makes a preset seem stronger than it is by making everything else quieter,
/// and it is the same trade this file already refused once: nothing here is scaled or attenuated to
/// leave clipping headroom. A preset that lifts is meant to be louder, and what a lift does to the peak
/// level of a particular recording is not something a frequency response can predict. The limiter in
/// the normalization filter sits after the equalizer and is on by default; that is where clipping is
/// answered.
/// </remarks>
internal static class Presets
{
    /// <summary>The empty preset: every slot where the player puts it, flat. It is what a new preset
    /// starts from, and it is in the menu so that a user who wants to build their own curve rather than
    /// adjust somebody else's has somewhere to begin.</summary>
    internal const string CustomId = "custom";

    /// <summary>What an unrecognised or empty saved name falls back to.</summary>
    /// <remarks>
    /// A literal rather than a lookup, and it has to be: the settings object is built before the
    /// application has chosen a language, so touching the table here would build every preset name in
    /// English and keep it. <see cref="Build"/> checks that it still names something.
    /// </remarks>
    internal const string DefaultId = "bassboost";

    /// <summary>Where a high shelf turns for a preset meant to be plainly bright rather than merely open
    /// at the top. Low enough to reach the range where most music still has something in it.</summary>
    private const double Bright = 4000;

    internal static IReadOnlyList<Preset> All { get; } = Build();

    /// <summary>The preset a saved name refers to, or null when it names nothing the player has.
    /// </summary>
    /// <remarks>
    /// A name that is no longer recognised gives back null rather than the default, so the caller can
    /// tell "the user chose Rock" from "the user chose something this version does not have" and leave
    /// the equalizer alone instead of applying a curve nobody asked for.
    /// </remarks>
    internal static Preset? Find(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        var wanted = id.Trim();
        foreach (var preset in All)
        {
            if (string.Equals(preset.Id, wanted, StringComparison.OrdinalIgnoreCase))
                return preset;
        }
        return null;
    }

    private static IReadOnlyList<Preset> Build()
    {
        var presets = new List<Preset>
        {
            // The six that are not about a kind of music first, because they are what an equalizer is
            // opened for most often.

            // Translators: Name of an equalizer preset that lifts the low frequencies.
            Define("bassboost", Tr("Bass Boost"), [Low(12)]),
            // Translators: Name of an equalizer preset that reduces the low frequencies.
            Define("bassreduce", Tr("Bass Reduce"), [Low(-12)]),
            // Translators: Name of an equalizer preset that lifts the high frequencies.
            Define("trebleboost", Tr("Treble Boost"), [High(10, Bright)]),
            // Translators: Name of an equalizer preset meant for listening through headphones.
            Define("headphones", Tr("Headphones"),
            [
                Low(7, 200),
                // Where headphones shout. Easing it is what separates a lively tilt from a tiring one.
                Peak(3500, 1.4, -3),
                High(4),
            ]),
            // Translators: Name of an equalizer preset that makes speech easier to follow. It suits audiobooks, podcasts and lectures rather than music.
            Define("voice", Tr("Voice"),
            [
                // Rumble, handling noise and room boom all live under the shelf; speech does not.
                Low(-8, 150),
                // The muddy region. Cutting it is what stops a close-miked voice sounding boxed in.
                Peak(300, 1.2, -4),
                // Two wide peaks rather than one narrow one, so the lift covers the whole span speech is
                // carried on instead of a single frequency somewhere inside it.
                Peak(1500, 0.8, 4),
                Peak(3500, 1, 5),
            ]),
            // Translators: Name of an equalizer preset for listening quietly, which lifts the lowest and highest frequencies without touching the middle.
            Define("loudness", Tr("Loudness"), [Low(10), High(7, Bright)]),

            // The kinds of music. Each is two or three moves that say what the music wants - weight, a
            // scooped or a forward middle, air - rather than a tilt from one end to the other.

            // Translators: Name of an equalizer preset suited to classical music.
            Define("classical", Tr("Classical"),
            [
                Low(3, 200),
                // Orchestral recordings collect weight here, and it is the first thing to muddy them.
                Peak(350, 1, -3),
                High(3),
            ]),
            // Translators: Name of an equalizer preset suited to club and dance-floor music.
            Define("club", Tr("Club"), [Low(7), Peak(2000, 1, 3), High(3)]),
            // Translators: Name of an equalizer preset suited to dance music.
            Define("dance", Tr("Dance"), [Low(9), Peak(500, 1, -4), High(6, Bright)]),
            // Translators: Name of an equalizer preset that imitates the sound of a live performance.
            Define("live", Tr("Live"),
                [Low(2, 150), Peak(300, 1, -3), Peak(2500, 1, 4), High(4)]),
            // Translators: Name of an equalizer preset suited to pop music.
            Define("pop", Tr("Pop"),
                [Low(5), Peak(400, 1, -3), Peak(3000, 1, 4), High(3)]),
            // Translators: Name of an equalizer preset suited to reggae music.
            Define("reggae", Tr("Reggae"), [Low(9), Peak(700, 1, -3), Peak(3000, 1, 3)]),
            // Translators: Name of an equalizer preset suited to rock music.
            Define("rock", Tr("Rock"),
                [Low(8), Peak(500, 1, -4), Peak(3000, 1, 3), High(5, Bright)]),
            // Translators: Name of an equalizer preset suited to ska music.
            Define("ska", Tr("Ska"),
                [Low(3, 200), Peak(1000, 1, 3), Peak(3000, 1, 4), High(4)]),
            // Translators: Name of an equalizer preset suited to soft and quiet music.
            Define("soft", Tr("Soft"), [Low(5), Peak(3000, 1, -3), High(3)]),
            // Translators: Name of an equalizer preset suited to soft rock music.
            Define("softrock", Tr("Soft Rock"), [Low(6), Peak(1000, 1, -4), High(3)]),
            // Translators: Name of an equalizer preset suited to techno music.
            Define("techno", Tr("Techno"), [Low(9), Peak(600, 1, -4), High(6, Bright)]),

            // Last, and on its own. It is not a curve anybody would choose to listen to - it is the empty
            // one, there to be edited - so it belongs at the end of the list rather than in front of the
            // presets somebody opened the menu to find.
            // Translators: Name of the equalizer preset that changes nothing, and which the user is meant to edit into whatever they want.
            Define("custom", Tr("Custom"), []),
        };

        if (!presets.Exists(preset => string.Equals(preset.Id, DefaultId, StringComparison.Ordinal)))
            throw new InvalidOperationException($"'{DefaultId}' is not one of the equalizer presets.");
        return presets;
    }

    /// <param name="frequency">Where the shelf turns. The default is above the bass of most music rather
    /// than inside it - see the note on this class about what these filters actually measure.</param>
    private static Band Low(double gain, double frequency = Bands.LowShelfFrequency)
        => new(frequency, Bands.ShelfQ, gain, BandType.LowShelf);

    private static Band High(double gain, double frequency = Bands.HighShelfFrequency)
        => new(frequency, Bands.ShelfQ, gain, BandType.HighShelf);

    private static Band Peak(double frequency, double q, double gain)
        => new(frequency, q, gain);

    private static Preset Define(string id, string name, Band[] bands)
    {
        var peaks = 0;
        foreach (var band in bands)
        {
            if (band.Type == BandType.Peaking)
                peaks++;
            if (band.Gain < Bands.MinimumGain || band.Gain > Bands.MaximumGain)
                throw new InvalidOperationException(
                    $"The '{id}' equalizer preset has a gain outside the range the player allows.");
        }
        // Refused rather than allowed to lose its last bands into a slot that is not there.
        if (peaks > Bands.PeakingSlotCount)
            throw new InvalidOperationException(
                $"The '{id}' equalizer preset has more than {Bands.PeakingSlotCount} peaking bands.");
        return new Preset(id, name, bands);
    }
}
