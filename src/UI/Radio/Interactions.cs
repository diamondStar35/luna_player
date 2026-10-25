using LunaPlayer.Radio;
using WxSharp;

namespace LunaPlayer.UI.Radio;

internal sealed partial class BrowserDialog
{
    private void BindEvents()
    {
        _tree.SelectionChanged += (_, _) => SelectCategory();
        _tree.ItemExpanding += (_, args) => ExpandDirectory(args.Item);
        _tree.ItemCollapsed += (_, args) =>
        {
            if (_changingTree) return;
            if (!_categories.TryGetValue(args.Item, out var category)) return;
            _expanded.Remove(category);
            if (_directoryActive == category) CancelDirectory();
        };
        _tree.ItemMenu += (_, args) =>
        {
            if (args.Item.IsValid) _tree.Selection = args.Item;
            TreeMenu();
        };
        _list.ItemSelected += (_, args) =>
        {
            if (_binding) return;
            _selectedRow = (int)args.Index;
            if (Selected is { } station) _results.View!.Selected = station.StationUuid;
            UpdateButtons();
            if (_selectedRow == _results.Rows.Count - 1) LoadMore(announce: true);
        };
        _list.ItemDeselected += (_, args) =>
        {
            if (_binding || _selectedRow != args.Index) return;
            _selectedRow = -1;
            UpdateButtons();
        };
        _list.ItemActivated += (_, _) => Play();
        _list.Bind(WxEvents.ContextMenu, (_, _) => StationMenu());
        _list.CacheHint += (_, args) =>
        {
            if (args.CacheTo >= _results.Rows.Count - 1) QueueNextPage();
            args.Skip();
        };
        _dialog.Bind(WxEvents.CharHook, OnDialogKey);
        _list.Bind(WxEvents.CharHook, OnListKey);
        _search.EnterPressed += (_, _) => Search();
        _search.TextChanged += (_, _) =>
        {
            _searchText = _search.Value;
            UpdateSearchButton();
        };
        _searchButton.Click += (_, _) =>
        {
            if (_clearSearch) { _searchText = string.Empty; _search.ChangeValue(string.Empty); }
            Search();
        };
    }

    private void UpdateButtons()
    {
        var selected = _selectedRow >= 0 && _selectedRow < _results.Rows.Count;
        if (_canPlay != selected) { _canPlay = selected; _play.Enabled = selected; }
        var details = Selected is not null;
        if (_canShowDetails != details) { _canShowDetails = details; _details.Enabled = details; }
    }

    private void UpdateSearchButton()
    {
        var query = _state.GetView(SearchCategory).Query;
        var clear = query.Length > 0 && _searchText.Trim() == query;
        if (_clearSearch == clear) return;
        _clearSearch = clear;
        _searchButton.Label = clear
            // Translators: Radio browser button clearing the current directory or station-name search.
            ? Tr("Clear search")
            // Translators: Radio browser button searching the current directory or station list.
            : Tr("Search");
    }

    private void Search()
    {
        var query = _searchText.Trim();
        var category = SearchCategory;
        var view = _state.GetView(category);
        if (view.Query != query)
        {
            CancelRead();
            view.Query = query;
            view.Reset();
        }
        Navigate(category, load: true);
        _list.Focus();
    }

    private void Refresh()
    {
        CancelRead();
        var category = _selectedCategory;
        if (category.IsContainer) { RefreshDirectory(category); return; }
        _results.View?.Reset();
        _sources.Remove(category);
        Navigate(category, load: true);
    }

    private void EditFilters()
    {
        var category = SearchCategory;
        using var dialog = new FiltersDialog(_dialog, _state, category);
        if (dialog.Show() is not { } choice) return;
        CancelRead();
        _state.Apply(category, choice);
        Navigate(_selectedCategory, load: !_selectedCategory.IsContainer);
    }

    private void OnDialogKey(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape && args.Modifiers == KeyModifiers.None)
            _dialog.EndModal(StandardId.Cancel);
        else args.Skip();
    }

    private void OnListKey(object? sender, KeyEventArgs args)
    {
        if (args.Code is Key.Down or Key.NumpadDown && args.Modifiers == KeyModifiers.None
            && _results.Rows.Count > 0 && _selectedRow == _results.Rows.Count - 1)
        {
            LoadMore(announce: true);
            args.Skip();
            return;
        }
        if (args.Code is Key.Enter or Key.NumpadEnter && !args.Control && !args.Alt)
        {
            if (args.Shift) Details(); else Play();
            return;
        }
        if (args.Code is Key.Space or Key.NumpadSpace && args.Modifiers == KeyModifiers.Control)
        {
            ToggleFavorite();
            return;
        }
        args.Skip();
    }
}
