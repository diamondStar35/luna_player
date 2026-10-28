using WxSharp;

namespace LunaPlayer.UI.Playlists;

internal sealed class OpenedFilesDialog : IDisposable
{
    private const int InformationId = 16001;
    private readonly Dialog _dialog;
    private readonly TextCtrl _search;
    private readonly FileList _list;
    private readonly Func<int, string> _nameAt;
    private readonly int _count;
    // The rows that pass the search box, each value an index into the full loaded-files list. Identity until
    // the user types, so an unfiltered list shows and returns every file in order.
    private readonly List<int> _filtered = [];
    private OpenedFilesAction? _action;

    /// <param name="count">How many files are loaded.</param>
    /// <param name="nameAt">The name to show for one row, asked for only as the row is drawn.</param>
    internal OpenedFilesDialog(Window parent, int count, Func<int, string> nameAt, int selectedIndex)
    {
        _nameAt = nameAt;
        _count = count;
        _dialog = new Dialog(
            parent,
            // Translators: Title of the window listing the files currently loaded in the player.
            title: Tr("Opened Files"),
            style: DialogStyle.Default | DialogStyle.ResizeBorder);
        // Translators: Label of the box that filters the loaded files by part of their name as the user types.
        var searchLabel = new StaticText(_dialog, label: Tr("Search"));
        _search = new TextCtrl(_dialog);
        _list = new FileList(_dialog, VisibleName);
        // One unnamed column filling the width: the list has nothing to head, and a report list is what
        // virtual mode requires.
        _list.InsertColumn(0, string.Empty, 360);
        // Start showing every file in order; the search box narrows this.
        for (var i = 0; i < count; i++)
            _filtered.Add(i);
        _list.SetItemCount(count);
        if (selectedIndex >= 0 && selectedIndex < count)
        {
            _list.SelectedIndex = selectedIndex;
            _list.EnsureVisible(selectedIndex);
        }

        // Translators: Button that shows details of every file in the playlist.
        var information = new Button(_dialog, InformationId, Tr("Playlist info"));
        // Translators: Button that starts playing the file chosen in the list.
        var jump = new Button(_dialog, StandardId.Ok, Tr("Jump to selected"));
        // Translators: Button that closes the Opened Files window without taking action.
        var cancel = new Button(_dialog, StandardId.Cancel, Tr("Cancel"));
        jump.SetDefault();
        information.Click += (_, _) => End(OpenedFilesAction.Information);
        jump.Click += (_, _) => End(OpenedFilesAction.Jump);
        _list.ItemActivated += (_, _) => End(OpenedFilesAction.Jump);
        _search.TextChanged += (_, _) => Rebuild();

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(information, flags: SizerFlags.BorderRight, border: 6);
        buttons.AddStretchSpacer();
        buttons.Add(jump, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(cancel);
        var search = new BoxSizer(Orientation.Horizontal);
        search.Add(searchLabel, flags: SizerFlags.AlignCenterVertical | SizerFlags.BorderRight, border: 6);
        search.Add(_search, proportion: 1, flags: SizerFlags.Expand);
        var sizer = new BoxSizer(Orientation.Vertical);
        // Translators: Label above the list of loaded files, telling the user to pick the one to play.
        sizer.Add(new StaticText(_dialog, label: Tr("Select file to jump to.")), flags: SizerFlags.All, border: 8);
        sizer.Add(search, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(_list, proportion: 1, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        sizer.Add(buttons, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom | SizerFlags.Expand, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(420, 260);
        _dialog.Center(onParent: true);
        _list.Focus();
    }

    internal OpenedFilesRequest? Show()
    {
        _dialog.ShowModal();
        var row = _list.SelectedIndex;
        // The list holds filtered rows; map the chosen row back to its place in the full loaded-files list.
        return _action.HasValue && row >= 0 && row < _filtered.Count
            ? new(_action.Value, _filtered[(int)row])
            : null;
    }

    public void Dispose() => _dialog.Dispose();

    /// <summary>Narrows the list to the files whose name contains the search text, keeping the full list when
    /// it is empty, and selects the first match.</summary>
    private void Rebuild()
    {
        var term = _search.Value.Trim();
        _filtered.Clear();
        for (var i = 0; i < _count; i++)
            if (term.Length == 0 || _nameAt(i).IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0)
                _filtered.Add(i);
        _list.SetItemCount(_filtered.Count);
        if (_filtered.Count > 0)
        {
            _list.SelectedIndex = 0;
            _list.EnsureVisible(0);
        }
        _list.RefreshItems(0, Math.Max(0, _filtered.Count - 1));
    }

    /// <summary>The name for a visible row, mapping it through the filter to the file it stands for.</summary>
    private string VisibleName(int row)
        => row >= 0 && row < _filtered.Count ? _nameAt(_filtered[row]) : string.Empty;

    private void End(OpenedFilesAction action)
    {
        if (_list.SelectedIndex < 0) return;
        _action = action;
        _dialog.EndModal(StandardId.Ok);
    }

    /// <summary>The list of loaded files, which holds none of them.</summary>
    ///
    /// <remarks>
    /// A virtual list asks for the text of a row as it draws it, so only the rows on screen cost anything.
    /// The alternative - handing a control every name up front - is what makes opening this window with a
    /// large playlist stall: a folder opened with its subfolders can hold a hundred thousand files, and
    /// pushing that many strings across to the control freezes the player for as long as it takes.
    ///
    /// Overriding requires deriving, because that is what makes the wrapper create a control that asks.
    /// </remarks>
    private sealed class FileList(Window parent, Func<int, string> nameAt) : ListCtrl(parent,
        style: ListCtrlStyle.Report | ListCtrlStyle.Virtual | ListCtrlStyle.SingleSelection | ListCtrlStyle.NoHeader)
    {
        // Called while the control is painting, so it does no more than look the name up.
        protected override string OnGetItemText(long item, int column)
            => item >= 0 && item <= int.MaxValue ? nameAt((int)item) : string.Empty;
    }
}
