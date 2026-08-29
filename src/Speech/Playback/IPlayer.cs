using System;

namespace PrismSharp.Speech.Playback
{
    /// <summary>
    /// An audio sink for speech synthesized to memory rather than played by the backend itself.
    /// </summary>
    /// <remarks>
    /// Implemented by the host, so speech can be routed through the mixer the application already
    /// owns - useful for applying its own volume, ducking, or spatialization. Prism hands over
    /// pooled, short-lived buffers, so an implementation must copy anything it needs to keep.
    /// </remarks>
    public interface IPlayer
    {
        /// <summary>Whether audio previously written is still playing.</summary>
        bool IsSpeaking { get; }

        /// <summary>Queues a chunk of synthesized audio for playback.</summary>
        /// <param name="samples">
        /// Interleaved 32-bit float samples in <c>[-1.0, 1.0]</c>. Valid only for the duration of this
        /// call; copy anything that must outlive it.
        /// </param>
        /// <param name="channels">Number of audio channels the samples are interleaved across.</param>
        /// <param name="sampleRate">Sample rate in Hz.</param>
        /// <param name="interrupt">Whether to discard audio already queued before writing this.</param>
        /// <remarks>May be called from a Prism-owned synthesis thread.</remarks>
        void Write(ReadOnlySpan<float> samples, int channels, int sampleRate, bool interrupt);

        /// <summary>Stops playback and discards anything queued.</summary>
        void Stop();
    }
}
