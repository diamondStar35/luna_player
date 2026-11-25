using System.Runtime.InteropServices;
using Windows.Storage.Streams;

namespace LunaPlayer.Playback;

internal sealed partial class SystemMediaControls
{
    private byte[]? _artwork;
    private readonly Lock _artworkGate = new();
    private InMemoryRandomAccessStream? _artworkStream;
    private long _artworkRevision;

    private async void UpdateArtwork(byte[]? image)
    {
        long revision;
        lock (_artworkGate)
        {
            if (ReferenceEquals(_artwork, image)) return;
            _artwork = image; revision = ++_artworkRevision;
        }
        InMemoryRandomAccessStream? stream = null;
        try
        {
            if (image is { Length: > 0 })
            {
                stream = new InMemoryRandomAccessStream();
                using var writer = new DataWriter(stream);
                writer.WriteBytes(image);
                await writer.StoreAsync();
                writer.DetachStream(); stream.Seek(0);
            }
            lock (_artworkGate)
            {
                if (_disposed || revision != _artworkRevision || _controls is not { } controls) return;
                var updater = controls.DisplayUpdater;
                // Publish authenticated artwork as bytes. A shell thumbnail request must never receive
                // a private server URL carrying an account token or custom authentication headers.
                updater.Thumbnail = stream is null ? null : RandomAccessStreamReference.CreateFromStream(stream);
                updater.Update();
                var previous = _artworkStream; _artworkStream = stream; stream = null;
                previous?.Dispose();
            }
        }
        catch (Exception error) when (error is COMException or ObjectDisposedException or InvalidOperationException or ArgumentException)
        { /* Artwork is optional; a revoked Windows session or rejected picture must not stop playback. */ }
        finally { stream?.Dispose(); }
    }

    private void ClearArtwork()
    {
        lock (_artworkGate)
        {
            _artwork = null; ++_artworkRevision;
            _artworkStream?.Dispose(); _artworkStream = null;
        }
    }
}
