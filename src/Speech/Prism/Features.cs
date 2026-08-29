using System;

namespace PrismSharp
{
    /// <summary>
    /// What a backend implements, plus whether its engine is available right now.
    /// </summary>
    /// <remarks>
    /// Mirrors the <c>PrismBackendFeature</c> bits from prism.h (v0.18.2). A set bit means the
    /// corresponding operation is implemented; a clear one means calling it returns
    /// <see cref="Error.NotImplemented"/>. Bit 1 is reserved and unused.
    /// <para>
    /// Read through <see cref="Backend.Features"/>, which is valid whether or not the backend has
    /// been initialized.
    /// </para>
    /// </remarks>
    [Flags]
    public enum Features : ulong
    {
        /// <summary>No features.</summary>
        None = 0,

        /// <summary>
        /// The underlying engine or service is available. Advisory: initialization may still fail.
        /// </summary>
        SupportedAtRuntime = 1UL << 0,

        /// <summary><see cref="Backend.Speak"/> is implemented.</summary>
        Speak = 1UL << 2,

        /// <summary><see cref="Backend.SpeakToMemory"/> is implemented.</summary>
        SpeakToMemory = 1UL << 3,

        /// <summary><see cref="Backend.Braille"/> is implemented.</summary>
        Braille = 1UL << 4,

        /// <summary><see cref="Backend.Output"/> is implemented.</summary>
        Output = 1UL << 5,

        /// <summary><see cref="Backend.IsSpeaking"/> is implemented.</summary>
        IsSpeaking = 1UL << 6,

        /// <summary><see cref="Backend.Stop"/> is implemented.</summary>
        Stop = 1UL << 7,

        /// <summary><see cref="Backend.Pause"/> is implemented.</summary>
        Pause = 1UL << 8,

        /// <summary><see cref="Backend.Resume"/> is implemented.</summary>
        Resume = 1UL << 9,

        /// <summary>Setting <see cref="Backend.Volume"/> is implemented.</summary>
        SetVolume = 1UL << 10,

        /// <summary>Reading <see cref="Backend.Volume"/> is implemented.</summary>
        GetVolume = 1UL << 11,

        /// <summary>Setting <see cref="Backend.Rate"/> is implemented.</summary>
        SetRate = 1UL << 12,

        /// <summary>Reading <see cref="Backend.Rate"/> is implemented.</summary>
        GetRate = 1UL << 13,

        /// <summary>Setting <see cref="Backend.Pitch"/> is implemented.</summary>
        SetPitch = 1UL << 14,

        /// <summary>Reading <see cref="Backend.Pitch"/> is implemented.</summary>
        GetPitch = 1UL << 15,

        /// <summary>The backend can re-enumerate its voice list.</summary>
        RefreshVoices = 1UL << 16,

        /// <summary>The backend can report how many voices it has.</summary>
        CountVoices = 1UL << 17,

        /// <summary>The backend can report voice names.</summary>
        GetVoiceName = 1UL << 18,

        /// <summary>The backend can report voice languages.</summary>
        GetVoiceLanguage = 1UL << 19,

        /// <summary>Reading <see cref="Backend.CurrentVoiceIndex"/> is implemented.</summary>
        GetVoice = 1UL << 20,

        /// <summary>Setting <see cref="Backend.CurrentVoiceIndex"/> is implemented.</summary>
        SetVoice = 1UL << 21,

        /// <summary><see cref="Backend.Channels"/> is implemented.</summary>
        GetChannels = 1UL << 22,

        /// <summary><see cref="Backend.SampleRate"/> is implemented.</summary>
        GetSampleRate = 1UL << 23,

        /// <summary><see cref="Backend.BitDepth"/> is implemented.</summary>
        GetBitDepth = 1UL << 24,

        /// <summary>The backend trims leading and trailing silence when speaking aloud.</summary>
        SilenceTrimmingOnSpeak = 1UL << 25,

        /// <summary>The backend trims leading and trailing silence when synthesizing to memory.</summary>
        SilenceTrimmingOnSpeakToMemory = 1UL << 26,

        /// <summary><see cref="Backend.Speak"/> accepts SSML markup.</summary>
        SpeakSsml = 1UL << 27,

        /// <summary><see cref="Backend.SpeakToMemory"/> accepts SSML markup.</summary>
        SpeakToMemorySsml = 1UL << 28
    }
}
