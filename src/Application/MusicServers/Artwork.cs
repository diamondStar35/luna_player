using LunaPlayer.Subsonic.Playback;

namespace LunaPlayer.Application.MusicServers;

internal sealed partial class Metadata
{
    private CancellationTokenSource? _artworkRequest;
    private string? _artworkIdentity;
    private bool _artworkStarted;
    private readonly Dictionary<(Guid Server, string Cover), byte[]> _covers = [];
    private const int CoverLimit = 4 * 1024 * 1024;
    private const int CacheLimit = 8 * 1024 * 1024;

    private void Artwork()
    {
        var reference = Reference.Parse(_player.CurrentPath);
        var identity = reference?.Kind == MediaKind.Song ? _player.CurrentPath : null;
        if (identity != _artworkIdentity)
        {
            _artworkRequest?.Cancel(); _artworkRequest?.Dispose(); _artworkRequest = null;
            _artworkIdentity = identity; _artworkStarted = false; _player.SetArtwork(identity, null);
        }
        if (identity is null || !_player.IsLoaded || _artworkStarted) return;
        _artworkStarted = true;
        _artworkRequest = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var token = _artworkRequest.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                var client = _connections.Client(reference!.ServerId);
                var song = _connections.Metadata.Get(reference.ServerId, reference.Id)
                    ?? await client.Library.GetSongAsync(reference.Id, token).ConfigureAwait(false);
                if (song.CoverArt.Length == 0) return;
                var key = (reference.ServerId, song.CoverArt);
                byte[]? image;
                lock (_covers) _covers.TryGetValue(key, out image);
                if (image is null)
                {
                    using var response = await client.Media.OpenCoverArtAsync(song.CoverArt, 512, token).ConfigureAwait(false);
                    using var buffer = new MemoryStream(); var block = new byte[16384];
                    while (true)
                    {
                        var read = await response.Stream.ReadAsync(block, token).ConfigureAwait(false);
                        if (read == 0) break;
                        if (buffer.Length + read > CoverLimit) throw new InvalidDataException("Artwork exceeds the cache limit.");
                        buffer.Write(block, 0, read);
                    }
                    image = buffer.ToArray();
                    lock (_covers)
                    {
                        if (token.IsCancellationRequested) return;
                        while (_covers.Count > 0 && _covers.Values.Sum(cover => cover.Length) + image.Length > CacheLimit)
                            _covers.Remove(_covers.Keys.First());
                        _covers[key] = image;
                    }
                }
                _lifetime.Post(_dispatcher.Post, () => { if (!token.IsCancellationRequested) _player.SetArtwork(identity, image); });
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception error)
            {
                // Missing or invalid artwork does not invalidate an otherwise playable stream.
                _lifetime.Post(_dispatcher.Post, () => Status?.Invoke(reference!.ServerId, error));
            }
        }, token);
    }
}
