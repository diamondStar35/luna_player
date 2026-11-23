using LunaPlayer.Subsonic;
using LunaPlayer.Subsonic.Servers;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed class ProfileEditor : IDisposable
{
    private readonly Form _form;
    internal ProfileEditor(Window owner, Profile? profile, Func<Profile, CancellationToken, Task> save, Action<string> speak)
    {
        var draft = profile ?? new Profile { Name = "", Server = new Uri("https://localhost/"), ReportListening = true };
        _form = new Form(owner, Words.Server) { SpeakHelp = speak };
        _form.HelpFooter(Words.HelpSaveServer, Words.HelpCancelServer);
        _form.Window.MinSize = new Size(660, 650);
        var tabs = new Notebook(_form.Panel); _form.Body.Add(tabs, proportion: 1, flags: SizerFlags.Expand);
        var pageHelp = new List<string>();
        _form.PageHelp = () => pageHelp.ElementAtOrDefault(tabs.SelectedIndex) ?? Words.HelpConnection;
        (Window, BoxSizer) Page(string title, string help)
        {
            var panel = new ScrolledWindow(tabs); panel.SetScrollRate(0, 16);
            var body = new BoxSizer(Orientation.Vertical); panel.SetSizer(body); tabs.AddPage(panel, title);
            _form.Help(panel, help); pageHelp.Add(help);
            return (panel, body);
        }
        var labels = new Dictionary<Window, StaticText>();
        TextCtrl Text(Window panel, BoxSizer body, string label, string value = "", bool secret = false, bool multiline = false)
        {
            var field = Form.Field(panel, body, label, () => new TextCtrl(panel, value: value,
                style: secret ? TextCtrlStyle.Password : multiline ? TextCtrlStyle.MultiLine : TextCtrlStyle.None), out var caption);
            if (multiline) field.MinSize = new Size(300, 120);
            labels[field] = caption; return field;
        }
        Choice Choose(Window panel, BoxSizer body, string label, string[] values, int index)
        { var field = Form.Field(panel, body, label, () => new Choice(panel), out var caption); foreach (var value in values) field.Add(value); field.SelectedIndex = index; labels[field] = caption; return field; }
        var (connection, first) = Page(Words.Connection, Words.HelpConnection);
        var name = Text(connection, first, Words.Name, draft.Name);
        var address = Text(connection, first, Words.Address, profile?.Server.AbsoluteUri ?? "");
        var auth = Choose(connection, first, Words.Auth, [Words.TokenAuth, Words.LegacyAuth, Words.KeyAuth], (int)draft.Credentials.Kind);
        var user = Text(connection, first, Words.Username, draft.Credentials.Username);
        var password = Text(connection, first, Words.Password, draft.Credentials.Password, secret: true);
        var key = Text(connection, first, Words.ApiKey, draft.Credentials.ApiKey, secret: true);
        void Authentication()
        {
            user.Show(auth.SelectedIndex != 2); password.Show(auth.SelectedIndex != 2); key.Show(auth.SelectedIndex == 2);
            labels[user].Show(auth.SelectedIndex != 2); labels[password].Show(auth.SelectedIndex != 2); labels[key].Show(auth.SelectedIndex == 2);
            connection.Layout();
        }
        auth.SelectionChanged += (_, _) => Authentication();
        var test = new Button(connection, label: Words.Test); first.Add(test, flags: SizerFlags.All, border: 5);
        var status = new StaticText(connection, label: ""); first.Add(status, flags: SizerFlags.Expand | SizerFlags.All, border: 5);
        var (playback, second) = Page(Words.Playback, Words.HelpPlayback);
        var mode = Choose(playback, second, Words.Mode, [Words.Original, Words.ServerDefault, Words.Transcode], (int)draft.Streaming.Mode);
        var format = Choose(playback, second, Words.Format, ["opus", "mp3", "aac", "flac", "wav"], 0);
        var formats = new[] { "opus", "mp3", "aac", "flac", "wav" }; format.SelectedIndex = Math.Max(0, Array.IndexOf(formats, draft.Streaming.Format));
        var bitrate = Form.Field(playback, second, Words.Bitrate, () => new SpinCtrl(playback, draft.Streaming.MaximumBitRate ?? 0, 0, 3200), out var bitrateLabel);
        void Streaming()
        {
            format.Show(mode.SelectedIndex == 2); bitrate.Show(mode.SelectedIndex == 2);
            labels[format].Show(mode.SelectedIndex == 2); bitrateLabel.Show(mode.SelectedIndex == 2);
            playback.Layout();
        }
        mode.SelectionChanged += (_, _) => Streaming();
        var report = new CheckBox(playback, label: Words.Report) { Checked = draft.ReportListening }; second.Add(report, flags: SizerFlags.All, border: 5);
        var sync = new CheckBox(playback, label: Words.Sync) { Checked = draft.SynchronizeQueue }; second.Add(sync, flags: SizerFlags.All, border: 5);
        var (browsing, browsingBody) = Page(Words.BrowseSettings, Words.HelpBrowsing);
        var browsingOptions = new BrowsingPage(browsing, browsingBody, draft, _form);
        var (downloads, third) = Page(Words.Downloads, Words.HelpDownloads);
        var folder = Text(downloads, third, Words.Folder, draft.Downloads.Folder);
        var browse = new Button(downloads, label: Words.Browse); third.Add(browse, flags: SizerFlags.All, border: 5);
        browse.Click += (_, _) => { if (_form.ChooseFolder(folder.Value) is { } path) folder.Value = path; };
        var albumFolders = new CheckBox(downloads, label: Words.AlbumFolders) { Checked = draft.Downloads.AlbumFolders }; third.Add(albumFolders, flags: SizerFlags.All, border: 5);
        var (advanced, fourth) = Page(Words.Advanced, Words.HelpAdvanced);
        var headers = Text(advanced, fourth, Words.Headers, string.Join(Environment.NewLine, draft.Headers.Select(x => x.Key + ": " + x.Value)), multiline: true);
        var (ai, fifth) = Page(Words.AudioMuse, Words.HelpAudioMuse);
        var aiAddress = Text(ai, fifth, Words.AudioMuseAddress, draft.AudioMuse?.Server?.AbsoluteUri ?? "");
        var aiToken = Text(ai, fifth, Words.AudioMuseToken, draft.AudioMuse?.Token ?? "", secret: true);
        var aiServer = Text(ai, fifth, Words.AudioMuseServer, draft.AudioMuse?.MusicServer ?? "");
        var count = Form.Field(ai, fifth, Words.Count, () => new SpinCtrl(ai, draft.AudioMuse?.ResultCount ?? 50, 1, 500));
        _form.Help(name, Words.HelpServerName); _form.Help(address, Words.HelpServerAddress);
        _form.Help(auth, Words.HelpAuthentication); _form.Help(user, Words.HelpUsername);
        _form.Help(password, Words.HelpPassword); _form.Help(key, Words.HelpApiKey); _form.Help(test, Words.HelpTestConnection);
        _form.Help(mode, Words.HelpStreamingMode); _form.Help(format, Words.HelpStreamingFormat);
        _form.Help(bitrate, Words.HelpStreamingBitrate); _form.Help(report, Words.HelpListening); _form.Help(sync, Words.HelpQueueSync);
        _form.Help(folder, Words.HelpDownloadFolder); _form.Help(browse, Words.HelpBrowseDownloads);
        _form.Help(albumFolders, Words.HelpAlbumFolders); _form.Help(headers, Words.HelpHeaders);
        _form.Help(aiAddress, Words.HelpAudioMuseAddress); _form.Help(aiToken, Words.HelpAudioMuseToken);
        _form.Help(aiServer, Words.HelpAudioMuseServer); _form.Help(count, Words.HelpAudioMuseCount);
        Profile Read(bool browsingSettings = true)
        {
            var location = address.Value.Trim();
            if (!location.Contains("://", StringComparison.Ordinal)) location = "https://" + location;
            if (!Uri.TryCreate(location, UriKind.Absolute, out var server) || server.Scheme is not ("http" or "https"))
                throw new ArgumentException(Words.InvalidAddress);
            if (browsingSettings && name.Value.Trim().Length == 0) throw new ArgumentException(Words.NameRequired);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in headers.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            { var colon = line.IndexOf(':'); if (colon <= 0) throw new ArgumentException(Words.Headers); map.Add(line[..colon].Trim(), line[(colon + 1)..].Trim()); }
            var value = draft with
            {
                Name = name.Value.Trim(), Server = server, Headers = map,
                Credentials = new() { Kind = (AuthenticationKind)auth.SelectedIndex, Username = user.Value, Password = password.Value, ApiKey = key.Value },
                Streaming = new() { Mode = (StreamingMode)mode.SelectedIndex, Format = formats[format.SelectedIndex], MaximumBitRate = bitrate.Value },
                ReportListening = report.Checked, SynchronizeQueue = sync.Checked,
                Downloads = new() { Folder = folder.Value, AlbumFolders = albumFolders.Checked },
                AudioMuse = string.IsNullOrWhiteSpace(aiAddress.Value) ? null : new AudioMuseSettings
                { Server = new Uri(aiAddress.Value.Trim()), Token = aiToken.Value, MusicServer = aiServer.Value.Trim().Length == 0 ? null : aiServer.Value.Trim(), ResultCount = count.Value }
            };
            return browsingSettings ? browsingOptions.Apply(value) : value;
        }
        browsingOptions.LoadAsync = token =>
        {
            var value = Read(browsingSettings: false);
            return Task.Run(async () => { using var client = new Client(value.ConnectionOptions()); return await client.Library.GetMusicFoldersAsync(token); }, token);
        };
        test.Click += async (_, _) =>
        {
            test.Enabled = false; status.Label = Words.Loading;
            try
            {
                var value = Read(browsingSettings: false);
                var libraries = await Task.Run(async () => { using var client = new Client(value.ConnectionOptions()); await client.DiscoverAsync(token: _form.Token); return await client.Library.GetMusicFoldersAsync(_form.Token); });
                _form.Post(() => { status.Label = Words.Connected; browsingOptions.Libraries(libraries); _form.Success(Words.ConnectionSuccessful); });
            }
            catch (OperationCanceledException) when (_form.Token.IsCancellationRequested) { }
            catch (Exception error) { _form.Post(() => _form.Error(error)); }
            finally { _form.Post(() => test.Enabled = true); }
        };
        _form.SaveAsync = async token => { var value = Read(); await Task.Run(() => save(value, token), token); return true; };
        string Signature() => string.Join("\0", name.Value, address.Value, auth.SelectedIndex, user.Value,
            password.Value, key.Value, mode.SelectedIndex, format.SelectedIndex, bitrate.Value,
            report.Checked, sync.Checked, folder.Value, albumFolders.Checked, headers.Value,
            aiAddress.Value, aiToken.Value, aiServer.Value, count.Value, browsingOptions.Signature);
        var initial = Signature(); _form.HasChanges = () => Signature() != initial;
        Authentication(); Streaming();
    }
    internal event Action<bool>? Closed { add => _form.Closed += value; remove => _form.Closed -= value; }
    internal Window Window => _form.Window;
    internal void Show() => _form.Show();
    public void Dispose() => _form.Dispose();
}
