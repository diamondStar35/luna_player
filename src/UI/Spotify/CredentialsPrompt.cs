using WxSharp;

namespace LunaPlayer.UI.Spotify;

internal sealed class SpotifyCredentialsPrompt : IDisposable
{
    private readonly Dialog _dialog;

    internal SpotifyCredentialsPrompt(Window parent)
    {
        _dialog = new Dialog(parent, title: Tr("Spotify"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        var message = new StaticText(_dialog,
            label: Tr("Spotify credentials have not been configured. Would you like to go to Preferences to configure them?"));
        message.Wrap(440);
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        var preferences = new Button(_dialog, StandardId.Ok, Tr("Go to Preferences"));
        preferences.SetDefault();
        _dialog.SetEscapeId(StandardId.Cancel);

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.AddStretchSpacer();
        buttons.Add(cancel, flags: SizerFlags.BorderRight, border: 8);
        buttons.Add(preferences);
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(message, flags: SizerFlags.Expand | SizerFlags.All, border: 12);
        layout.Add(buttons, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 12);
        _dialog.SetSizer(layout);
        _dialog.Fit();
        _dialog.MinSize = new Size(500, 160);
        _dialog.Center(onParent: true);
        preferences.Focus();
    }

    internal bool Show() => _dialog.ShowModal() == StandardId.Ok;

    public void Dispose() => _dialog.Dispose();
}
