using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void CreateConnection()
    {
        var connection = new BoxSizer(Orientation.Horizontal);
        _servers = Choice(_panel, connection, Words.Server, []);
        Button(_panel, connection, Words.Manage, Manage);
        _layout.Add(connection, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
    }
    private void CreateSearch()
    {
        var search = new BoxSizer(Orientation.Horizontal);
        search.Add(new StaticText(_panel, label: Words.Search), flags: SizerFlags.AlignCenterVertical | SizerFlags.BorderRight, border: 6);
        _search = new TextCtrl(_panel, style: TextCtrlStyle.ProcessEnter) { Name = Words.Search };
        search.Add(_search, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderRight, border: 8);
        _types = Choice(_panel, search, Words.Type, [Words.All, Words.Artists, Words.Albums, Words.Songs]);
        _layout.Add(search, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        var filters = new BoxSizer(Orientation.Horizontal);
        _libraries = Choice(_panel, filters, Words.Library, [Words.AllLibraries]);
        _sort = Choice(_panel, filters, Words.Sort, [Words.Default, Words.Name, Words.Artist, Words.Album, Words.Track, Words.Duration, Words.Year, Words.Added, Words.Plays, Words.Rating,
            Words.Format, Words.SourceBitrate, Words.SortSampleRate, Words.SortBitDepth]);
        _direction = Choice(_panel, filters, Words.Order, [Words.Ascending, Words.Descending]);
        Button(_panel, filters, Words.Filters, Filters);
        _layout.Add(filters, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
    }
    private void CreateCollections()
    {
        var body = new BoxSizer(Orientation.Horizontal);
        var categories = new BoxSizer(Orientation.Vertical);
        categories.Add(new StaticText(_panel, label: Words.Categories), flags: SizerFlags.BorderBottom, border: 5);
        _tree = new TreeCtrl(_panel, style: TreeCtrlStyle.Default | TreeCtrlStyle.HideRoot | TreeCtrlStyle.HasButtons) { Name = Words.Categories };
        _tree.MinSize = new Size(200, 250); categories.Add(_tree, proportion: 1, flags: SizerFlags.Expand);
        _tree.AddRoot(Words.Categories);
        Categories();
        body.Add(categories, flags: SizerFlags.Expand | SizerFlags.BorderRight, border: 8);
        var results = new BoxSizer(Orientation.Vertical);
        _resultsLabel = new StaticText(_panel, label: Words.Artists);
        results.Add(_resultsLabel, flags: SizerFlags.BorderBottom, border: 5);
        _list = new ListCtrl(_panel, style: ListCtrlStyle.Report | ListCtrlStyle.Virtual) { Name = Words.Artists };
        ConfigureColumns();
        _list.SetDataSource(_view, Cell); results.Add(_list, proportion: 1, flags: SizerFlags.Expand);
        body.Add(results, proportion: 1, flags: SizerFlags.Expand); _layout.Add(body, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
    }
    private void CreateActions()
    {
        var actions = new BoxSizer(Orientation.Horizontal);
        _back = Button(_panel, actions, Words.Back, Back);
        _play = Button(_panel, actions, Words.Play, () => Execute(Command.Play));
        _append = Button(_panel, actions, Words.Append, () => Execute(Command.Append));
        Button(_panel, actions, Words.Queue, _showQueue);
        Button(_panel, actions, Words.Refresh, Refresh);
        actions.AddStretchSpacer(); var close = new Button(_panel, StandardId.Cancel, Words.Close);
        close.Click += (_, _) => _window.Close(); actions.Add(close); _window.SetEscapeId(StandardId.Cancel);
        _layout.Add(actions, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
    }
}
