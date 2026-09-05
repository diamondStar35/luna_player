using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LunaPlayer.Recording;

/// <summary>The two short tones that say a recording has begun and ended.</summary>
///
/// <remarks>
/// A port of the Python player's beeps, and the ordering around them is the point of them. The rising tone
/// is played and allowed to finish <em>before</em> the sources are opened, and the falling one only
/// <em>after</em> they have been closed - so neither ends up in the recording, even when what is being
/// recorded is everything the speakers play. Getting that order wrong would put a beep at each end of every
/// loopback recording.
///
/// They are the confirmation, which is why nothing is spoken alongside them: a tone is immediate, does not
/// interrupt a screen reader mid-sentence, and is the same whatever language the player is in.
/// </remarks>
internal static class RecordingTone
{
    /// <summary>The tone that says recording has begun.</summary>
    internal const double Started = 880;

    /// <summary>The tone that says recording has ended. Lower, so the two are told apart without being
    /// looked at.</summary>
    internal const double Stopped = 440;

    /// <summary>Long enough to be heard as a note rather than a click, short enough not to be a wait.
    /// </summary>
    private static readonly TimeSpan Length = TimeSpan.FromMilliseconds(120);

    /// <summary>Plays one tone and returns once it has finished sounding.</summary>
    ///
    /// <remarks>
    /// Blocking on purpose: the caller needs the tone out of the way before it opens or after it closes the
    /// capture, and it is called from a worker thread where waiting a tenth of a second costs nothing.
    ///
    /// A machine that will not play it - no output device, an exclusive-mode program holding the card - is
    /// not a reason to refuse to record, so every failure here is swallowed. The tone is a courtesy; the
    /// recording is the point.
    /// </remarks>
    internal static void Play(double frequency)
    {
        try
        {
            // WASAPI, which is what everything else here records through, rather than the WinMM
            // player. On the published NAudio 3.0.1 packages WaveOut was silent in this build - its
            // WAVEHDR did not survive being marshalled ahead of time, so the first write was rejected
            // and the wait below ended after five milliseconds without a note being heard. The NAudio
            // submodule fixes that, but there is no reason to go back: this plays through the same API
            // the recording itself uses, and measures at exactly the gain asked for.
            using var output = new WasapiPlayerBuilder().Build();
            var tone = new SignalGenerator(44100, 1)
            {
                Gain = 0.3,
                Frequency = frequency,
                Type = SignalGeneratorType.Sin,
            }.Take(Length);
            output.Init(tone);
            output.Play();
            while (output.PlaybackState is PlaybackState.Playing)
                Thread.Sleep(10);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Nothing to be done and nothing worth saying: the user is about to hear the recording start
            // or stop by other means.
        }
    }
}
