namespace LunaPlayer.Spotify.Playback;

internal sealed partial class Sessions
{
    private sealed class Successor : IDisposable
    {
        private CancellationTokenSource? _cancellation = new();

        internal Successor(Track track, int bitrate, int generation)
        {
            Track = track;
            Bitrate = bitrate;
            Generation = generation;
            Token = _cancellation.Token;
        }

        internal Track Track { get; }
        internal int Bitrate { get; }
        internal int Generation { get; }
        internal CancellationToken Token { get; }
        internal string? Url { get; private set; }

        internal void Complete(string url)
        {
            Url = url;
            // The engine now owns the audio lifetime. Cancelling preparation must no longer
            // interrupt a decoder that may be reading this stream during an overlap.
            _cancellation?.Dispose();
            _cancellation = null;
        }

        internal string? TakeUrl()
        {
            var url = Url;
            Url = null;
            return url;
        }

        public void Dispose()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }
}
