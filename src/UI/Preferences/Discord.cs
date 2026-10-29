using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord;
using WxSharp;

namespace LunaPlayer.UI.Preferences;

internal sealed class DiscordPreferences : PreferencesPage
{
    private readonly DiscordSettings _settings;
    private readonly CheckBox _enabled;
    private readonly CheckBox _details;
    private readonly CheckBox _buttons;
    private readonly CheckBox _idle;

    internal DiscordPreferences(Window parent, DiscordSettings settings)
        // Translators: F1 help for the Discord settings page.
        : base(new Panel(parent), Tr("Share your Luna Player activity with Discord. Move with Tab or Shift+Tab and press F1 for help. Changes take effect when you accept Preferences."))
    {
        _settings = settings;
        var panel = (Panel)Window;
        // Translators: Opt-in checkbox for publishing playback activity to Discord.
        _enabled = new CheckBox(panel, label: Tr("Enable Discord Rich Presence"));
        // Translators: Privacy control for titles, artists, thumbnails and public media links in Discord.
        _details = new CheckBox(panel, label: Tr("Share media titles, artists and artwork"));
        // Translators: Whether the Discord activity includes public media and Luna download buttons.
        _buttons = new CheckBox(panel, label: Tr("Show link buttons"));
        // Translators: Whether Discord shows Luna while no media is loaded.
        _idle = new CheckBox(panel, label: Tr("Show activity when idle"));
        // Translators: Accessible name for the read-only Discord setup instructions.
        var guideLabel = new StaticText(panel, label: Tr("Discord setup and privacy"));
        var guide = new TextCtrl(panel, style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly,
            value: string.Join(Environment.NewLine,
                // Translators: Build status on the Discord preferences page when the publisher has not configured it.
                !PublisherApplication.IsConfigured
                    // Translators: Explain why enabling Discord is unavailable in this build; users do not enter credentials.
                    ? Tr("Discord Rich Presence is unavailable in this build because the publisher has not configured Luna's application ID. No user API key or token is required.")
                    // Translators: Explain that Discord developer configuration belongs to the publisher, not the user.
                    : Tr("how to enable ritch presence?."),
                // Translators: Instructions to make Rich Presence visible in the Discord desktop client.
                Tr("Run the Discord desktop app and allow activity sharing in Discord's Activity Privacy settings. Enable Rich Presence above and choose OK."),
                // Translators: Privacy explanation for Discord presence settings.
                Tr("Sharing is off by default. Media details can include local file names. Hiding details also hides media artwork and media links. Only recognized YouTube and Spotify public pages are linked; local paths and raw stream addresses are never linked."),
                // Translators: Troubleshooting information for Discord presence.
                Tr("Discord reconnects automatically. The browser version is not supported. Changes may take a few seconds to appear.")),
            size: new Size(-1, 160)) { Name = guideLabel.Label };
        guide.InsertionPoint = 0;
        var layout = new BoxSizer(Orientation.Vertical);
        layout.Add(_enabled, flags: SizerFlags.All, border: 8);
        layout.Add(_details, flags: SizerFlags.All, border: 8);
        layout.Add(_buttons, flags: SizerFlags.All, border: 8);
        layout.Add(_idle, flags: SizerFlags.All, border: 8);
        layout.Add(guideLabel, flags: SizerFlags.All, border: 8);
        layout.Add(guide, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        panel.SetSizer(layout);
        // Translators: F1 help for enabling Discord Rich Presence.
        Help(_enabled, Tr("Publishes your activity to the local Discord desktop app. Disabled by default. Turning this off clears Luna's Discord activity without stopping playback."));
        // Translators: F1 help for sharing media details with Discord.
        Help(_details, Tr("Shares the title and artist of the current media, including local file names, and YouTube artwork when available. Clear this to show only generic playback activity and hide media links and artwork."));
        // Translators: F1 help for Discord link buttons.
        Help(_buttons, Tr("Adds a Luna Player download button and, when media details are shared, a public YouTube or Spotify link. Raw stream URLs and local file paths are never shared as buttons."));
        // Translators: F1 help for idle Discord presence.
        Help(_idle, Tr("Keeps a generic Luna Player activity visible when playback is stopped or nothing is loaded. Clear this to remove the activity when idle. Paused media still counts as loaded."));
        // Translators: F1 help for the read-only Discord setup guide.
        Help(guide, Tr("Use the Arrow keys to read or select these instructions. No Discord login credentials are stored by Luna."));
        Refresh();
    }

    public override void Apply()
    {
        _settings.Enabled = PublisherApplication.IsConfigured && _enabled.Checked;
        _settings.ShowDetails = _details.Checked;
        _settings.ShowButtons = _buttons.Checked;
        _settings.ShowIdle = _idle.Checked;
    }

    public override void Refresh()
    {
        _enabled.Enabled = PublisherApplication.IsConfigured;
        _enabled.Checked = PublisherApplication.IsConfigured && _settings.Enabled;
        _details.Checked = _settings.ShowDetails;
        _buttons.Checked = _settings.ShowButtons;
        _idle.Checked = _settings.ShowIdle;
    }
}
