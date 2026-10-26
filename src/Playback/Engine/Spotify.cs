using System.Collections.Concurrent;
using System.Globalization;
using LunaPlayer.Spotify.Playback;
using MpvNet;

namespace LunaPlayer.Playback;

internal sealed partial class Engine
{
    private ConcurrentDictionary<string, AudioDecryptStream>? _spotifyAudio;
    private bool _spotifyProtocolRegistered;
    internal string RegisterSpotifyAudio(AudioDecryptStream audio)
    {
        var streams = _spotifyAudio ??= new(StringComparer.Ordinal);
        if (!_spotifyProtocolRegistered)
        {
            // Optional Spotify initialization must not affect ordinary player startup.
            _mpv.RegisterStreamProtocol("luna-spotify", OpenSpotifyStream);
            _spotifyProtocolRegistered = true;
        }
        var id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        foreach (var pending in streams)
            if (streams.TryRemove(pending.Key, out var unused)) unused.Dispose();
        streams[id] = audio;
        return "luna-spotify://" + id;
    }

    internal void ReleaseSpotifyAudio(string url)
    {
        const string prefix = "luna-spotify://";
        if (url.StartsWith(prefix, StringComparison.Ordinal)
            && _spotifyAudio is { } streams
            && streams.TryRemove(url[prefix.Length..], out var pending)) pending.Dispose();
    }

    private IMpvStream OpenSpotifyStream(string uri)
    {
        const string prefix = "luna-spotify://";
        if (!uri.StartsWith(prefix, StringComparison.Ordinal) || _spotifyAudio is not { } streams
            || !streams.TryRemove(uri[prefix.Length..], out var audio))
            throw new InvalidDataException();
        return new AudioStream(audio);
    }

    private sealed class AudioStream(AudioDecryptStream audio) : IMpvStream
    {
        public long? Size => audio.Length;
        public int Read(byte[] buffer, int offset, int count) => audio.Read(buffer, offset, count);
        public long Seek(long offset) => audio.Seek(offset, SeekOrigin.Begin);
        public void Cancel() => audio.Cancel();
        public void Dispose() => audio.Dispose();
    }

}
