using System;

namespace PrismSharp
{
    /// <summary>
    /// One chunk of synthesized audio produced by <see cref="Backend.SpeakToMemory"/>.
    /// </summary>
    /// <remarks>
    /// The format travels with each chunk rather than being read back from the backend. Prism
    /// delivers it alongside every callback, and asking the backend for
    /// <see cref="Backend.SampleRate"/> mid-enumeration would block: synthesis holds the backend lock
    /// until it finishes.
    /// </remarks>
    public readonly struct AudioChunk
    {
        internal AudioChunk(ReadOnlyMemory<float> samples, int channels, int sampleRate)
        {
            Samples = samples;
            Channels = channels;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Interleaved 32-bit float samples in <c>[-1.0, 1.0]</c>; for stereo, left and right
        /// alternate. Valid only until the enumeration advances - copy anything that must outlive it.
        /// </summary>
        public ReadOnlyMemory<float> Samples { get; }

        /// <summary>Number of audio channels, typically 1 or 2.</summary>
        public int Channels { get; }

        /// <summary>Sample rate in Hz - samples per second per channel.</summary>
        public int SampleRate { get; }

        /// <summary>
        /// Number of frames in this chunk: <see cref="Samples"/> length divided by
        /// <see cref="Channels"/>.
        /// </summary>
        public int FrameCount => Channels <= 0 ? 0 : Samples.Length / Channels;
    }
}
