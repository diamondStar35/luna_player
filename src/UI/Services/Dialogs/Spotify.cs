using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class SpotifyDialogs(DialogContext context) : IDisposable
{
    private readonly DialogContext _context = context;
    private Spotify.BrowserDialog? _spotifyBrowser;

    public Spotify.DialogOutcome ShowSpotifyBrowser(LunaPlayer.Spotify.Catalog catalog,
        LunaPlayer.Spotify.Auth auth, SpotifySettings settings, Action<string> speak,
        Spotify.DialogOutcome? playback = null)
    {
        if (string.IsNullOrWhiteSpace(settings.ClientId))
        {
            using var prompt = new Spotify.CredentialsPrompt(_context.Parent);
            if (prompt.Show())
                Wx.CallAfter(() => _context.RequestAction(ActionId.OpenSpotifyCredentials));
            return new Spotify.DialogOutcome();
        }
        _spotifyBrowser ??= new Spotify.BrowserDialog(_context.Frame, catalog, auth, settings, speak);
        return _spotifyBrowser.Show(playback);
    }
    public void Dispose() { _spotifyBrowser?.Dispose(); _spotifyBrowser = null; }
}
