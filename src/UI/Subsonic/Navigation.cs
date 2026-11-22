using LunaPlayer.Subsonic.Browsing;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private bool _connecting;
    internal void Profiles()
    {
        if (_disposed || !_initialized) return;
        var selected = _server;
        var changed = !ReferenceEquals(_profiles, _connections.Profiles);
        _profiles = _connections.Profiles;
        var index = Array.FindIndex(_profiles, x => x.Id == selected); if (index < 0 && _profiles.Length > 0) index = 0;
        if (changed)
        {
            _binding = true; _servers.Clear(); foreach (var profile in _profiles) _servers.Add(profile.Name);
            _servers.SelectedIndex = index; _binding = false;
        }
        if (index >= 0 && (_server != _profiles[index].Id || _activeProfile != _profiles[index] || _rows.Length == 0 && !_connecting)) SelectServer();
        else if (index < 0) { _server = Guid.Empty; Publish([]); }
    }
    private void Remember()
    {
        if (_server != Guid.Empty) _memory[_server] = Capture() with { Derived = null };
    }
    private void SelectServer(bool announce = false)
    {
        Remember(); _reading.Cancel(); _revision++; _sortRevision++; _history.Clear();
        if (_servers.SelectedIndex < 0) return;
        _server = _profiles[_servers.SelectedIndex].Id;
        var revision = ++_connectionRevision;
        _activeProfile = _profiles[_servers.SelectedIndex];
        _derived = null;
        _connecting = true;
        _user = null; _capabilities = null; _username = _connections.Profile(_server).Credentials.Username;
        _location = new(Category.Artists);
        _binding = true; _search.ChangeValue(""); _types.SelectedIndex = 0; _sort.SelectedIndex = 0; _direction.SelectedIndex = 0;
        if (_memory.TryGetValue(_server, out var memory))
        { _location = memory.Location; _search.ChangeValue(memory.Query); _types.SelectedIndex = memory.Type; _sort.SelectedIndex = memory.Sort; _direction.SelectedIndex = memory.Direction; }
        var visible = Sections.Visible(_activeProfile);
        if (!visible.Contains(_location.Category)) _location = new(visible.FirstOrDefault());
        _libraries.Clear(); _libraries.Add(Words.AllLibraries); _libraries.SelectedIndex = 0; _binding = false;
        Categories(); SynchronizeView(announce); Publish([]);
        var server = _server;
        Run(async token =>
        {
            var client = _connections.Client(server);
            var capabilities = await client.DiscoverAsync(token: token);
            var folders = await client.Library.GetMusicFoldersAsync(token);
            LunaPlayer.Subsonic.Models.User? user = null;
            try
            {
                var name = _connections.Profile(server).Credentials.Kind == LunaPlayer.Subsonic.AuthenticationKind.ApiKey
                    ? (await client.GetTokenInfoAsync(token)).Username : _connections.Profile(server).Credentials.Username;
                user = await client.GetUserAsync(name, token);
            }
            catch (LunaPlayer.Subsonic.RequestException error) when (error.Kind is LunaPlayer.Subsonic.FailureKind.Unsupported or LunaPlayer.Subsonic.FailureKind.Forbidden)
            { /* Permission introspection is optional on older servers. The server still authorizes each action. */ }
            return (capabilities, folders, user);
        }, result =>
        {
            if (_server != server || revision != _connectionRevision) return;
            _connecting = false;
            _capabilities = result.capabilities; _user = result.user; _username = result.user?.Username ?? _username;
            var folders = result.folders.Where(folder => _activeProfile!.LibraryIds.Length == 0 || _activeProfile.LibraryIds.Contains(folder.Id, StringComparer.Ordinal)).ToArray();
            _binding = true; _libraryIds = folders.Select(x => x.Id).ToArray();
            foreach (var folder in folders) _libraries.Add(folder.Name);
            if (_memory.TryGetValue(server, out var saved) && saved.Library is not null) _libraries.SelectedIndex = Array.IndexOf(_libraryIds, saved.Library) + 1;
            _binding = false; Reload();
        }, failed: error =>
        {
            if (_server != server || revision != _connectionRevision) return;
            _connecting = false; Error(error);
        });
    }

    private void Navigate(Location location, bool announce = false)
    { _history.Add(Capture()); _derived = null; _location = location; _binding = true; _search.ChangeValue(""); _sort.SelectedIndex = 0; _binding = false; SynchronizeView(announce); Reload(); }
    private void Back()
    {
        if (_history.Count == 0) return;
        var place = _history[^1]; _history.RemoveAt(_history.Count - 1); _location = place.Location; _derived = place.Derived;
        UpdateButtons();
        _binding = true; _search.ChangeValue(place.Query); _types.SelectedIndex = place.Type;
        _sort.SelectedIndex = place.Sort; _direction.SelectedIndex = place.Direction;
        _libraries.SelectedIndex = place.Library is null ? 0 : Math.Max(0, Array.IndexOf(_libraryIds, place.Library) + 1);
        _binding = false; SynchronizeView(); Reload();
    }
    private Place Capture() => new(_location, _search.Value, _types.SelectedIndex, _sort.SelectedIndex, _direction.SelectedIndex,
        _libraries.SelectedIndex > 0 && _libraries.SelectedIndex <= _libraryIds.Length ? _libraryIds[_libraries.SelectedIndex - 1] : null, _derived);
    private void ShowDerived(Row[] rows, Func<CancellationToken, Task<Row[]>> read, Category category = Category.Songs)
    {
        _history.Add(Capture()); _derived = read; _rows = rows; _query = ""; _location = new(category);
        _connections.Metadata.Observe(_server, rows.Where(row => row.Song is not null).Select(row => row.Song!));
        _binding = true; _search.ChangeValue(""); _sort.SelectedIndex = 0; _binding = false; SynchronizeView(announce: true); Sort();
    }
    private void Open()
    {
        var rows = Selected;
        if (rows.Length == 1 && rows[0].Kind is RowKind.Artist or RowKind.Album or RowKind.Playlist or RowKind.Genre or RowKind.PodcastChannel)
            Navigate(new(_location.Category, rows[0].Kind, rows[0].Id, rows[0].Name), announce: true);
        else Execute(Command.Play);
    }
}
