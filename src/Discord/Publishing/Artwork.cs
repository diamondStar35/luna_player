using System.Diagnostics;

namespace LunaPlayer.Discord.Publishing;

internal sealed partial class Worker
{
    private readonly IApplicationArtwork _artwork;
    private string? _icon;
    private Task<string?>? _artworkTask;
    private CancellationTokenSource? _artworkCancellation;
    private TimeSpan _nextArtworkRefresh;

    private void StartArtwork()
    {
        if (_artworkTask is not null || Elapsed < _nextArtworkRefresh) return;
        _artworkCancellation?.Dispose();
        _artworkCancellation = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
        _artworkTask = LoadArtworkAsync(_artworkCancellation.Token);
    }

    private async Task<string?> LoadArtworkAsync(CancellationToken token)
    {
        try { return await _artwork.ReadAsync(_applicationId, token).ConfigureAwait(false); }
        finally { Wake(); }
    }

    private async Task ReadArtworkAsync(bool wait = false)
    {
        if (_artworkTask is null || !wait && !_artworkTask.IsCompleted) return;
        try
        {
            // Retain the last successful icon if the public metadata endpoint is temporarily unavailable.
            var icon = await _artworkTask.ConfigureAwait(false);
            if (icon is not null) _icon = icon;
            _nextArtworkRefresh = Elapsed + (icon is null ? _timing.Retry : _timing.ArtworkRefresh);
        }
        catch (OperationCanceledException) { _nextArtworkRefresh = TimeSpan.Zero; }
        catch (Exception exception) when (exception is HttpRequestException or IOException or System.Text.Json.JsonException)
        {
            Trace.TraceWarning("Could not read Discord application artwork: {0}", exception.Message);
            _nextArtworkRefresh = Elapsed + _timing.Retry;
        }
        finally { _artworkTask = null; }
    }
}
