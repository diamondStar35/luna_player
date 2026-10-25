using LunaPlayer.Radio;
using WxSharp;

namespace LunaPlayer.UI.Radio;

internal sealed partial class BrowserDialog
{
    private Category _selectedCategory;
    private readonly HashSet<Category> _expanded = [];

    private Category SearchCategory => _selectedCategory.Kind == CategoryKind.Country
        ? new(CategoryKind.All, CountryCode: _selectedCategory.Value, CountryName: _selectedCategory.CountryName)
        : _selectedCategory.IsContainer ? new(CategoryKind.All) : _selectedCategory;

    private void BuildTree()
    {
        var root = _tree.AddRoot(string.Empty);
        // Translators: Radio tree category containing every station in the public directory.
        AddCategory(root, new(CategoryKind.All), Tr("Global stations"));
        // Translators: Radio tree category containing stations saved by this user.
        AddCategory(root, new(CategoryKind.Favorites), Tr("Favorites"));
        // Translators: Radio tree category containing this user's recently played stations.
        AddCategory(root, new(CategoryKind.Recent), Tr("Recently played"));
        // Translators: Radio tree branch containing public station rankings.
        var discover = AddCategory(root, new(CategoryKind.Discover), Tr("Discover"));
        // Translators: Radio discovery category ordered by public playback counts.
        AddCategory(discover, new(CategoryKind.MostPlayed), Tr("Most played"));
        // Translators: Radio discovery category ordered by growth in playback counts.
        AddCategory(discover, new(CategoryKind.Trending), Tr("Trending"));
        // Translators: Radio discovery category ordered by public votes.
        AddCategory(discover, new(CategoryKind.MostVoted), Tr("Most voted"));
        // Translators: Radio discovery category containing stations recently played worldwide.
        AddCategory(discover, new(CategoryKind.RecentClicks), Tr("Recently clicked"));
        // Translators: Radio discovery category ordered by recent changes to station metadata.
        AddCategory(discover, new(CategoryKind.Updated), Tr("Recently updated"));
        // Translators: Expandable radio tree category listing countries and their regions.
        AddCategory(root, new(CategoryKind.Countries), Tr("Countries"));
        // Translators: Expandable radio tree category listing languages.
        AddCategory(root, new(CategoryKind.Languages), Tr("Languages"));
        // Translators: Expandable radio tree category listing genres and tags.
        AddCategory(root, new(CategoryKind.Tags), Tr("Genres and tags"));
        // Translators: Expandable radio tree category listing audio codecs.
        AddCategory(root, new(CategoryKind.Codecs), Tr("Formats"));
    }

    private TreeItemId AddCategory(TreeItemId parent, Category category, string label)
    {
        var item = _tree.Add(parent, label);
        // Translators: Error shown if the radio navigation tree cannot create a requested item.
        if (!item.IsValid) throw new InvalidOperationException(Tr("The radio tree could not insert an item."));
        _categories.Add(item, category);
        _nodes.Add(category, item);
        if (IsDirectory(category) || category.Kind == CategoryKind.Country) _tree.SetItemHasChildren(item);
        return item;
    }

    private void SelectTreeRoot()
    {
        if (!_nodes.TryGetValue(_selectedCategory, out var item)
            && !_nodes.TryGetValue(Browsing.Root(_selectedCategory), out item)) return;
        _changingTree = true;
        try { _tree.Selection = item; }
        finally { _changingTree = false; }
    }

    private Results Source(Category category)
    {
        var view = category.IsContainer ? null : _state.GetView(category);
        if (_sources.TryGetValue(category, out var source) && ReferenceEquals(source.View, view)
            && source.Revision == (view?.Revision ?? 0))
            return source;
        source = new Results(category, view);
        _sources[category] = source;
        return source;
    }

    private void Navigate(Category category, bool load = false)
    {
        _restoreStation = null;
        _selectedCategory = category;
        var source = Source(category);
        if (!ReferenceEquals(_results, source) || !_bound)
        {
            CancelRead();
            _results = source;
            _rowsChanged = _rowValuesChanged = false;
            _binding = true;
            try
            {
                _selectedRow = -1;
                if (source.View is { } view) view.Selected = null;
                _list.SetDataSource(source.Rows, static (row, column) => column >= 0 && column < row.Cells.Length
                    ? row.Cells[column] : string.Empty);
                _bound = true;
            }
            finally { _binding = false; }
        }
        if (!category.IsContainer && (load || source.Rows.Count > 0 || source.Loaded)) _state.ActiveCategory = category;
        var query = _state.GetView(SearchCategory).Query;
        if (_searchText != query)
        {
            _searchText = query;
            _search.ChangeValue(query);
        }
        UpdateSearchButton();
        UpdateButtons();
        if (load) EnsureLoaded();
    }

    private void SelectCategory()
    {
        if (_changingTree || !_visible || !_categories.TryGetValue(_tree.Selection, out var category)) return;
        Navigate(category, load: !category.IsContainer);
        if (_directoryParents.TryGetValue(category, out var entry) && entry.Parent.Kind == CategoryKind.Tags
            && _directories.TryGetValue(entry.Parent, out var directory) && entry.Position >= directory.Items.Count - 32)
            LoadDirectory(entry.Parent);
    }

    private void AddCountryChildren(Category country)
    {
        var all = new Category(CategoryKind.All, CountryCode: country.Value, CountryName: country.CountryName);
        if (_nodes.ContainsKey(all)) return;
        var parent = _nodes[country];
        // Translators: Country-specific radio tree item listing all working stations.
        var label = Tr("All stations");
        var count = _stationCounts.GetValueOrDefault(country);
        AddCategory(parent, all, CountLabel(label, count));
        // Translators: Country-specific radio tree item ranking stations by public playback counts.
        AddCategory(parent, all with { Kind = CategoryKind.MostPlayed }, Tr("Most played"));
        // Translators: Country-specific radio tree item ranking stations by growth in playback counts.
        AddCategory(parent, all with { Kind = CategoryKind.Trending }, Tr("Trending"));
        // Translators: Country-specific radio tree item ranking stations by public votes.
        AddCategory(parent, all with { Kind = CategoryKind.MostVoted }, Tr("Most voted"));
        // Translators: Country-specific radio tree branch containing its states and provinces.
        AddCategory(parent, all with { Kind = CategoryKind.Regions }, Tr("Regions"));
    }

    private static string CountLabel(string name, uint count)
    {
        // Translators: Radio tree item with its station count. {name} is a country, region, language, genre, codec or All stations label; {count} is its number of working stations.
        return TrPluralFormat("{name}, {count} station", "{name}, {count} stations",
            (int)Math.Min(count, int.MaxValue), name, count);
    }

    private void TreeMenu()
    {
        _clearHistoryAction.Enabled = _selectedCategory.Kind == CategoryKind.Recent;
        _tree.PopupMenu(_treeActions);
    }
}
