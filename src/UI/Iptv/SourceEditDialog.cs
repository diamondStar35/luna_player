using LunaPlayer.UI.Services;
using LunaPlayer.Iptv;
using WxSharp;

namespace LunaPlayer.UI.Iptv;

/// <summary>The window for saving an IPTV source or changing one already saved.</summary>
///
/// <remarks>
/// One form serves every kind of source. The kind is chosen from a list at the top, and the fields below
/// change to match it: an M3U web address shows one address box, an M3U file shows a box and a Browse
/// button, an Xtream account shows server, username and password, and a Stalker portal shows a portal
/// address, a MAC address and an optional serial. The rows that do not apply are hidden rather than disabled,
/// so a screen reader never lands on a field the chosen kind has no use for.
///
/// Validation runs before the window closes and uses <see cref="IptvSourceStore.Validate"/>, keeping the UI
/// and the store agreed on what may be saved.
/// </remarks>
internal sealed class SourceEditDialog : IDisposable
{
    /// <summary>The kinds in the order they are listed, which is the order they are declared in.</summary>
    private static readonly IptvSourceKind[] Kinds =
        [IptvSourceKind.M3uUrl, IptvSourceKind.M3uFile, IptvSourceKind.Xtream, IptvSourceKind.Stalker];

    private readonly Dialog _dialog;
    private readonly FlexGridSizer _form;
    private readonly TextCtrl _name;
    private readonly Choice _kind;

    private readonly StaticText _addressLabel;
    private readonly TextCtrl _address;

    private readonly StaticText _browseLabel;
    private readonly Button _browse;

    private readonly StaticText _usernameLabel;
    private readonly TextCtrl _username;
    private readonly StaticText _passwordLabel;
    private readonly TextCtrl _password;

    private readonly StaticText _macLabel;
    private readonly TextCtrl _mac;
    private readonly StaticText _serialLabel;
    private readonly TextCtrl _serial;

    private readonly StaticText _epgLabel;
    private readonly TextCtrl _epg;

    private readonly StaticText _hlsLabel;
    private readonly CheckBox _hls;

    internal SourceEditDialog(Window parent, string caption, IptvSourceDraft value)
    {
        _dialog = new Dialog(parent, title: caption, style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Label of the box holding what an IPTV source is called.
        var nameLabel = new StaticText(_dialog, label: Tr("Name"));
        _name = new TextCtrl(_dialog, value: value.Name);

        // Translators: Label of the list saying what kind of IPTV source is being saved.
        var kindLabel = new StaticText(_dialog, label: Tr("Type"));
        _kind = new Choice(_dialog);
        foreach (var kind in Kinds)
            _kind.Add(IptvSourceStore.Describe(kind));
        _kind.SelectedIndex = Math.Max(0, Array.IndexOf(Kinds, value.Kind));

        // The address label is set for the chosen kind by Refresh; it starts with the web-address wording.
        // Translators: Initial label of the box holding an IPTV source's web address.
        _addressLabel = new StaticText(_dialog, label: Tr("Web address"));
        _address = new TextCtrl(_dialog, value: value.Url);
        _browseLabel = new StaticText(_dialog, label: string.Empty);
        // Translators: Button beside the file box that opens a dialog to choose a playlist file.
        _browse = new Button(_dialog, label: Tr("Browse..."));
        _browse.Click += (_, _) => ChooseFile();

        // Translators: Label of the box holding the username of an Xtream IPTV account.
        _usernameLabel = new StaticText(_dialog, label: Tr("Username"));
        _username = new TextCtrl(_dialog, value: value.Username);
        // Translators: Label of the box holding the password of an Xtream IPTV account.
        _passwordLabel = new StaticText(_dialog, label: Tr("Password"));
        _password = new TextCtrl(_dialog, value: value.Password, style: TextCtrlStyle.Password);

        // Translators: Label of the box holding the MAC address a Stalker portal is bound to.
        _macLabel = new StaticText(_dialog, label: Tr("MAC address"));
        _mac = new TextCtrl(_dialog, value: value.MacAddress);
        // Translators: Label of the box holding the optional device serial some Stalker portals check.
        _serialLabel = new StaticText(_dialog, label: Tr("Serial (optional)"));
        _serial = new TextCtrl(_dialog, value: value.Serial);

        // Translators: Label of the box holding an optional programme-guide address. "XMLTV" is a format name.
        _epgLabel = new StaticText(_dialog, label: Tr("EPG address (optional)"));
        _epg = new TextCtrl(_dialog, value: value.EpgUrl);

        _hlsLabel = new StaticText(_dialog, label: string.Empty);
        // Translators: Tick box that opens live channels as HLS rather than as a transport stream.
        _hls = new CheckBox(_dialog, label: Tr("Open live channels as HLS")) { Checked = value.PreferHls };

        _form = new FlexGridSizer(0, 2, 8, 8);
        _form.AddGrowableColumn(1, 1);
        AddField(nameLabel, _name);
        AddField(kindLabel, _kind);
        AddField(_addressLabel, _address);
        AddField(_browseLabel, _browse);
        AddField(_usernameLabel, _username);
        AddField(_passwordLabel, _password);
        AddField(_macLabel, _mac);
        AddField(_serialLabel, _serial);
        AddField(_epgLabel, _epg);
        AddField(_hlsLabel, _hls);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_form, proportion: 1, flags: SizerFlags.All | SizerFlags.Expand, border: 10);
        var buttons = _dialog.CreateButtonSizer(ButtonSizerFlags.Ok | ButtonSizerFlags.Cancel);
        if (buttons is not null)
            sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 10);
        _dialog.SetSizer(sizer);

        _kind.SelectionChanged += (_, _) => Refresh();
        Refresh();

        _dialog.Fit();
        _dialog.MinSize = new Size(580, 260);
        _dialog.Center(onParent: true);
        _dialog.Bind(WxEvents.ButtonClicked, OnAccept, StandardId.Ok);
        _name.Focus();
    }

    internal IptvSourceDraft? Show()
        => _dialog.ShowModal() == StandardId.Ok ? Current() : null;

    public void Dispose() => _dialog.Dispose();

    private IptvSourceKind SelectedKind => Kinds[Math.Max(0, _kind.SelectedIndex)];

    private IptvSourceDraft Current() => new()
    {
        Name = _name.Value,
        Kind = SelectedKind,
        Url = _address.Value,
        Username = _username.Value,
        Password = _password.Value,
        MacAddress = _mac.Value,
        Serial = _serial.Value,
        EpgUrl = _epg.Value,
        PreferHls = _hls.Checked,
    };

    /// <summary>Shows the fields the chosen kind needs and hides the rest, then lays the form out again.
    /// </summary>
    private void Refresh()
    {
        var kind = SelectedKind;
        var isFile = kind == IptvSourceKind.M3uFile;
        var isXtream = kind == IptvSourceKind.Xtream;
        var isStalker = kind == IptvSourceKind.Stalker;

        _addressLabel.Label = kind switch
        {
            // Translators: Label of the address box when the source is an M3U playlist at a web address.
            IptvSourceKind.M3uUrl => Tr("Web address"),
            // Translators: Label of the address box when the source is an M3U playlist file on this computer.
            IptvSourceKind.M3uFile => Tr("Playlist file"),
            // Translators: Label of the address box when the source is an Xtream Codes account (its server).
            IptvSourceKind.Xtream => Tr("Server address"),
            // Translators: Label of the address box when the source is a Stalker portal (its portal address).
            _ => Tr("Portal address"),
        };

        ShowField(_browseLabel, _browse, isFile);
        ShowField(_usernameLabel, _username, isXtream);
        ShowField(_passwordLabel, _password, isXtream);
        ShowField(_macLabel, _mac, isStalker);
        ShowField(_serialLabel, _serial, isStalker);
        ShowField(_hlsLabel, _hls, isXtream);

        _dialog.Layout();
        _dialog.Fit();
    }

    private void ChooseFile()
    {
        // Translators: File-type name for M3U and M3U8 IPTV playlists.
        var playlists = Tr("Playlists");
        // Translators: File-type name for the entry that shows every file in a picker.
        var allFiles = Tr("All Files");
        using var dialog = new FileDialog(
            _dialog,
            message: "",
            wildcard: $"{playlists} (*.m3u;*.m3u8)|*.m3u;*.m3u8|{allFiles} (*.*)|*.*",
            style: FileDialogStyle.DefaultOpen);
        if (dialog.ShowModal() == StandardId.Ok)
            _address.Value = dialog.Path;
    }

    private void OnAccept(object? sender, CommandEventArgs args)
    {
        if (!IptvSourceStore.Validate(Current(), out var error))
        {
            Messages.Show(error,
                MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, _dialog);
            return;
        }
        _dialog.EndModal(StandardId.Ok);
    }

    /// <summary>One row of the form: the label centred against its box, the box taking the width.</summary>
    private void AddField(StaticText label, Window control)
    {
        _form.Add(label, flags: SizerFlags.AlignCenterVertical);
        _form.Add(control, proportion: 1, flags: SizerFlags.Expand);
    }

    /// <summary>Shows or hides a whole row - its label and its control - so the sizer gives it no space when
    /// it does not apply to the chosen kind.</summary>
    private void ShowField(StaticText label, Window control, bool show)
    {
        _form.Show(label, show);
        _form.Show(control, show);
    }
}
