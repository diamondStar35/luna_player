using System.Collections.Concurrent;
using System.Globalization;
using LunaPlayer.Spotify.Playback;
using MpvNet;

namespace LunaPlayer.Playback;

internal sealed partial class PlaybackCoordinator
{
    private ConcurrentDictionary<string, RegisteredAudio>? _spotifyAudio;
    private bool _spotifyProtocolRegistered;
    internal string RegisterSpotifyAudio(AudioDecryptStream audio)
    {
        var streams = _spotifyAudio ??= new(StringComparer.Ordinal);
        if (!_spotifyProtocolRegistered)
        {
            // Optional Spotify initialization must not affect ordinary player startup.
            _session.Native.RegisterStreamProtocol("luna-spotify", OpenSpotifyStream);
            _spotifyProtocolRegistered = true;
        }
        var id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        streams[id] = new RegisteredAudio(audio);
        return "luna-spotify://" + id;
    }

    internal void ReleaseSpotifyAudio(string url)
    {
        const string prefix = "luna-spotify://";
        if (url.StartsWith(prefix, StringComparison.Ordinal)
            && _spotifyAudio is { } streams
            && streams.TryRemove(url[prefix.Length..], out var pending)) pending.Release();
    }

    // Retiring a URL prevents new opens; existing decoders keep their leases until close.
    private void ReleasePendingSpotifyAudio(string? keepPath = null)
    {
        if (_spotifyAudio is not { } streams) return;
        foreach (var pending in streams)
            if (!string.Equals("luna-spotify://" + pending.Key, keepPath, StringComparison.Ordinal)
                && streams.TryRemove(pending.Key, out var unused)) unused.Release();
    }

    private IMpvStream OpenSpotifyStream(string uri)
    {
        const string prefix = "luna-spotify://";
        if (!uri.StartsWith(prefix, StringComparison.Ordinal) || _spotifyAudio is not { } streams
            || !streams.TryGetValue(uri[prefix.Length..], out var audio))
            throw new InvalidDataException();
        return audio.Open();
    }

    /// <summary>Shares cached audio while each mpv decoder keeps its own position and cancellation.</summary>
    private sealed class RegisteredAudio(AudioDecryptStream audio)
    {
        private readonly object _lifetimeSync = new();
        private readonly object _readSync = new();
        private int _readers;
        private bool _released;

        internal IMpvStream Open()
        {
            lock (_lifetimeSync)
            {
                ObjectDisposedException.ThrowIf(_released, this);
                _readers++;
                return new AudioStream(this);
            }
        }

        internal void Release()
        {
            lock (_lifetimeSync)
            {
                if (_released) return;
                _released = true;
                if (_readers == 0) audio.Dispose();
            }
        }

        private void Close()
        {
            lock (_lifetimeSync)
            {
                if (--_readers == 0 && _released) audio.Dispose();
            }
        }

        private sealed class AudioStream(RegisteredAudio owner) : IMpvStream
        {
            private readonly CancellationTokenSource _cancellation = new();
            private readonly object _cancellationSync = new();
            private long _position;
            private int _disposed;
            public long? Size => owner.Length;
            public int Read(byte[] buffer, int offset, int count)
            {
                lock (owner._readSync)
                {
                    _cancellation.Token.ThrowIfCancellationRequested();
                    if (owner.Position != _position) owner.Seek(_position);
                    var read = owner.Read(buffer.AsSpan(offset, count), _cancellation.Token);
                    _position += read;
                    return read;
                }
            }
            public long Seek(long offset)
            {
                if (offset < 0) throw new IOException();
                return _position = offset;
            }
            // A discarded prefetch must not cancel the audio backing another decoder.
            public void Cancel()
            {
                lock (_cancellationSync)
                    if (_disposed == 0) _cancellation.Cancel();
            }
            public void Dispose()
            {
                lock (_cancellationSync)
                {
                    if (_disposed != 0) return;
                    _disposed = 1;
                    _cancellation.Cancel();
                    _cancellation.Dispose();
                }
                owner.Close();
            }
        }

        private long Length => audio.Length;
        private long Position => audio.Position;
        private void Seek(long position) => audio.Seek(position, SeekOrigin.Begin);
        private int Read(Span<byte> buffer, CancellationToken token) => audio.Read(buffer, token);
    }
}
