using System;

namespace PrismSharp.Speech.Playback
{
    /// <summary>
    /// A growable circular buffer of audio samples, to help implement <see cref="IPlayer"/>.
    /// </summary>
    /// <remarks>
    /// Speech arrives in bursts and is consumed at a steady rate, so a player needs somewhere to
    /// hold the difference. This buffer grows to fit whatever is written and never shrinks.
    /// <para>
    /// Not thread-safe. Writing happens on a synthesis thread and reading usually on an audio
    /// callback, so the owner must serialize access.
    /// </para>
    /// </remarks>
    public sealed class Ring
    {
        private float[] _buffer;
        private int _start;
        private int _count;

        /// <summary>Creates a buffer.</summary>
        /// <param name="capacity">Initial capacity in samples. Grows on demand.</param>
        public Ring(int capacity = 16384)
        {
            _buffer = new float[Math.Max(1, capacity)];
        }

        /// <summary>Number of samples currently buffered.</summary>
        public int Count => _count;

        /// <summary>Discards everything buffered, keeping the allocated capacity.</summary>
        public void Clear()
        {
            _start = 0;
            _count = 0;
        }

        /// <summary>Appends samples, growing the buffer if needed.</summary>
        /// <param name="samples">The samples to append.</param>
        public void Write(ReadOnlySpan<float> samples)
        {
            if (samples.IsEmpty)
                return;

            EnsureCapacity(_count + samples.Length);

            var end = (_start + _count) % _buffer.Length;
            var first = Math.Min(samples.Length, _buffer.Length - end);
            samples[..first].CopyTo(_buffer.AsSpan(end));

            var remaining = samples.Length - first;
            if (remaining > 0)
                samples.Slice(first, remaining).CopyTo(_buffer.AsSpan(0));

            _count += samples.Length;
        }

        /// <summary>Removes up to <paramref name="destination"/>'s length of samples from the front.</summary>
        /// <param name="destination">Receives the samples.</param>
        /// <returns>
        /// How many samples were written, which is fewer than requested when the buffer runs dry -
        /// the caller should treat the remainder as silence.
        /// </returns>
        public int Read(Span<float> destination)
        {
            if (destination.IsEmpty || _count == 0)
                return 0;

            var actual = Math.Min(destination.Length, _count);
            var first = Math.Min(actual, _buffer.Length - _start);
            _buffer.AsSpan(_start, first).CopyTo(destination);

            var remaining = actual - first;
            if (remaining > 0)
                _buffer.AsSpan(0, remaining).CopyTo(destination[first..]);

            _start = (_start + actual) % _buffer.Length;
            _count -= actual;
            if (_count == 0)
                _start = 0;

            return actual;
        }

        private void EnsureCapacity(int required)
        {
            if (_buffer.Length >= required)
                return;

            var expanded = new float[Math.Max(required, _buffer.Length * 2)];
            if (_count > 0)
            {
                var first = Math.Min(_count, _buffer.Length - _start);
                _buffer.AsSpan(_start, first).CopyTo(expanded);

                var remaining = _count - first;
                if (remaining > 0)
                    _buffer.AsSpan(0, remaining).CopyTo(expanded.AsSpan(first));
            }

            _buffer = expanded;
            _start = 0;
        }
    }
}
