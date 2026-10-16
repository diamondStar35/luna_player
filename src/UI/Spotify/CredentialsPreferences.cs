using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using WxSharp;

namespace LunaPlayer.UI.Spotify;

internal sealed class CredentialsPreferences : PreferencesPage
{
    private readonly SpotifySettings _settings;
    private readonly TextCtrl _clientId;
    private readonly Choice _quality;
    private static readonly string[] QualityValues = ["NORMAL", "HIGH", "VERY_HIGH"];

    internal CredentialsPreferences(Window parent, SpotifySettings settings)
        : base(new Panel(parent), Tr("Enter the Client ID from your own Spotify developer app for Home, Search and Library. Register the redirect URI shown below. Playback signs in separately. A client secret is not needed."))
    {
        _settings = settings;
        var panel = (Panel)Window;
        var heading = new StaticText(panel, label: Tr("Spotify"));
        var idLabel = new StaticText(panel, label: Tr("Client ID"));
        _clientId = new TextCtrl(panel, value: settings.ClientId);
        var qualityLabel = new StaticText(panel, label: Tr("Audio quality"));
        _quality = new Choice(panel);
        _quality.Add(Tr("Normal (96 kbps)"));
        _quality.Add(Tr("High (160 kbps)"));
        _quality.Add(Tr("Very high (320 kbps)"));
        _quality.SelectedIndex = QualityIndex(settings.AudioQuality);
        var guideLabel = new StaticText(panel, label: Tr("How to get a Spotify Client ID"));
        var guide = new TextCtrl(panel,
            value: string.Join(Environment.NewLine,
                Tr("1. Open https://developer.spotify.com/dashboard and sign in."),
                Tr("2. Choose Create app. Enter a name and select Web API."),
                Tr("3. Add this Redirect URI in the app settings:"),
                LunaPlayer.Spotify.SpotifyAuth.RedirectUri,
                Tr("4. Save the app. Copy its Client ID and paste it in the box above."),
                Tr("You do not need the Client Secret."),
                Tr("Web API is required for Home, Search and Library. Spotify audio playback signs in separately.")),
            style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly,
            size: new Size(-1, 155));
        guide.InsertionPoint = 0;
        var note = new StaticText(panel, label: Tr("Spotify uses Authorization Code with PKCE. Never enter a client secret here."));
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(heading, flags: SizerFlags.All, border: 8);
        layout.Add(idLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        layout.Add(_clientId, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        layout.Add(qualityLabel, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        layout.Add(_quality, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        layout.Add(guideLabel, flags: SizerFlags.All, border: 8);
        layout.Add(guide, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        layout.Add(note, flags: SizerFlags.All, border: 8);
        panel.SetSizer(layout);
        Help(_clientId, Tr("Paste the Client ID of your Spotify developer app. This public ID identifies your app when you sign in."));
        Help(guide, Tr("Follow these steps in the Spotify developer dashboard. The redirect URI must match exactly."));
        Help(_quality, Tr("Select the Spotify stream quality: Normal 96 kbps, High 160 kbps, or Very high 320 kbps. Changing this while a track plays restarts that track at its current position."));
    }

    public override string? Validate()
    {
        var value = _clientId.Value.Trim();
        return value.Length == 0 || value.Length == 32 && value.All(Uri.IsHexDigit)
            ? null : Tr("The Spotify Client ID must be empty or exactly 32 hexadecimal characters.");
    }

    public override void Apply()
    {
        _settings.ClientId = _clientId.Value.Trim();
        var index = _quality.SelectedIndex;
        _settings.AudioQuality = index >= 0 && index < QualityValues.Length ? QualityValues[index] : "VERY_HIGH";
    }

    public override void Refresh()
    {
        _clientId.ChangeValue(_settings.ClientId);
        _quality.SelectedIndex = QualityIndex(_settings.AudioQuality);
    }

    private static int QualityIndex(string quality)
    {
        var index = Array.IndexOf(QualityValues, quality);
        return index >= 0 ? index : QualityValues.Length - 1; // Default to the highest quality.
    }
}
