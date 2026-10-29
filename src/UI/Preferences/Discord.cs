using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
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
        var guideLabel = new StaticText(panel, label: Tr("Enable activity sharing in Discord"));
        var guide = new TextCtrl(panel, style: TextCtrlStyle.MultiLine | TextCtrlStyle.ReadOnly,
            // Translators: Instructions in Discord preferences. Activity Privacy is a page in Discord's settings.
            value: Tr("Open Discord Desktop, go to User Settings > Activity Privacy, and turn on Share your detected activities with others. Then select Enable Discord Rich Presence above and choose OK."),
            size: new Size(-1, 120)) { Name = guideLabel.Label };
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
        Help(_details, Tr("Shares media titles, artists, book chapters and available online artwork. Local files use Luna's icon. Clear this to hide media details, artwork and source links."));
        // Translators: F1 help for Discord link buttons.
        Help(_buttons, Tr("Adds a download button and a source link for YouTube, Spotify or public radio streams when media details are shared. Local paths and private stream addresses are not linked."));
        // Translators: F1 help for idle Discord presence.
        Help(_idle, Tr("Keeps a generic Luna Player activity visible when playback is stopped or nothing is loaded. Clear this to remove the activity when idle. Paused media still counts as loaded."));
        // Translators: F1 help for the read-only Discord setup guide.
        Help(guide, Tr("Explains how to enable activity sharing in Discord Desktop."));
        Refresh();
    }

    public override void Apply()
    {
        _settings.Enabled = _enabled.Checked;
        _settings.ShowDetails = _details.Checked;
        _settings.ShowButtons = _buttons.Checked;
        _settings.ShowIdle = _idle.Checked;
    }

    public override void Refresh()
    {
        _enabled.Checked = _settings.Enabled;
        _details.Checked = _settings.ShowDetails;
        _buttons.Checked = _settings.ShowButtons;
        _idle.Checked = _settings.ShowIdle;
    }
}
