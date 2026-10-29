using LunaPlayer.Iptv;
using WxSharp;

namespace LunaPlayer.UI.Iptv;

/// <summary>The window for browsing and playing the channels a loaded IPTV source holds.</summary>
///
/// <remarks>
/// A source can hold a hundred thousand channels, so the list is virtual: it asks for the text of a row only
/// as it draws it, over an index of the channels that pass the current filters rather than over the channels
/// themselves. The filters - kind, category and a search box - rebuild that index; the list is told its new
/// length and redrawn, and never holds a row of its own.
///
/// The window answers with the position of the chosen channel in the source's full channel list, so the
/// caller need not know anything about how the browser filtered or ordered what it showed.
/// </remarks>
internal sealed class ChannelBrowserDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly IReadOnlyList<IptvChannel> _channels;
    private readonly IReadOnlyList<IptvCategory> _categories;
    private readonly Dictionary<string, string> _categoryNames;
    private readonly Action<string> _speakHelp;
    private readonly EpgGuide? _guide;

    private readonly Choice _kind;
    private readonly Choice _category;
    private readonly TextCtrl _search;
    private readonly ChannelList _list;
    private readonly StaticText _count;

    /// <summary>The channels that pass the current filters, each value a position in <see cref="_channels"/>.
    /// </summary>
    private readonly List<int> _filtered = [];

    private int? _chosen;

    internal ChannelBrowserDialog(Window parent, ChannelBrowserPrompt prompt)
    {
        _channels = prompt.Channels;
        _categories = prompt.Categories;
        _speakHelp = prompt.SpeakHelp;
        _guide = prompt.Guide is { IsEmpty: false } guide ? guide : null;
        _categoryNames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var category in _categories)
            _categoryNames[category.Id] = category.Name;

        _dialog = new Dialog(parent, title: prompt.Title, style: DialogStyle.Default | DialogStyle.ResizeBorder);

        // Translators: Label of the list that filters channels by whether they are live TV, films or series.
        var kindLabel = new StaticText(_dialog, label: Tr("Show"));
        _kind = new Choice(_dialog);
        // Translators: Channel-browser filter choice: show channels of every kind.
        _kind.Add(Tr("All"));
        // Translators: Channel-browser filter choice: show only live TV channels.
        _kind.Add(Tr("Live TV"));
        // Translators: Channel-browser filter choice: show only films (video on demand).
        _kind.Add(Tr("Movies"));
        // Translators: Channel-browser filter choice: show only series.
        _kind.Add(Tr("Series"));
        _kind.SelectedIndex = 0;

        // Translators: Label of the list that filters channels by the category they belong to.
        var categoryLabel = new StaticText(_dialog, label: Tr("Category"));
        _category = new Choice(_dialog);
        // Translators: Channel-browser filter choice: show channels from every category.
        var categoryItems = new List<string>(_categories.Count + 1) { Tr("All categories") };
        categoryItems.AddRange(_categories.Select(category => category.Name));
        // One bulk crossing rather than an Add per category - a source can carry a great many of them.
        _category.Set(categoryItems);
        _category.SelectedIndex = 0;

        // Translators: Label of the box that filters channels by part of their name as the user types.
        var searchLabel = new StaticText(_dialog, label: Tr("Search"));
        _search = new TextCtrl(_dialog);

        _list = new ChannelList(_dialog, CellText);
        // Translators: Heading of the channel list column holding each channel's name.
        _list.InsertColumn(0, Tr("Name"), 320);
        // Translators: Heading of the channel list column holding each channel's category.
        _list.InsertColumn(1, Tr("Category"), 200);
        // Translators: Heading of the channel list column holding each channel's number.
        _list.InsertColumn(2, Tr("Number"), 80);
        if (_guide is not null)
            // Translators: Heading of the channel list column holding what is showing now (from the guide).
            _list.InsertColumn(3, Tr("Now"), 260);

        _count = new StaticText(_dialog, label: string.Empty);

        BuildLayout(kindLabel, categoryLabel, searchLabel);
        WireEvents();
        Rebuild();
        _list.Focus();
    }

    internal int? Show()
    {
        _dialog.ShowModal();
        return _chosen;
    }

    public void Dispose() => _dialog.Dispose();

    /// <summary>Lays the window out: the three filters above the list, a count line, then Play and Close.
    /// </summary>
    private void BuildLayout(StaticText kindLabel, StaticText categoryLabel, StaticText searchLabel)
    {
        var filters = new FlexGridSizer(0, 2, 6, 8);
        filters.AddGrowableColumn(1, 1);
        filters.Add(kindLabel, flags: SizerFlags.AlignCenterVertical);
        filters.Add(_kind, proportion: 1, flags: SizerFlags.Expand);
        filters.Add(categoryLabel, flags: SizerFlags.AlignCenterVertical);
        filters.Add(_category, proportion: 1, flags: SizerFlags.Expand);
        filters.Add(searchLabel, flags: SizerFlags.AlignCenterVertical);
        filters.Add(_search, proportion: 1, flags: SizerFlags.Expand);

        // Translators: Button that plays the channel chosen in the list.
        var play = new Button(_dialog, label: Tr("Play"));
        play.Click += (_, _) => Play();
        // Translators: The button that closes a window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(play, flags: SizerFlags.BorderRight, border: 6);
        buttons.AddStretchSpacer();
        buttons.Add(close);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(filters, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        sizer.Add(_list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight, border: 8);
        sizer.Add(_count, flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderTop, border: 8);
        sizer.Add(buttons, flags: SizerFlags.Expand | SizerFlags.All, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.MinSize = new Size(820, 520);
        _dialog.Center(onParent: true);
    }
    // PLACEHOLDER
    private void WireEvents()
    {
        _kind.SelectionChanged += (_, _) => Rebuild();
        _category.SelectionChanged += (_, _) => Rebuild();
        _search.TextChanged += (_, _) => Rebuild();
        _list.ItemActivated += (_, _) => Play();
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
    }

    /// <summary>Rebuilds the index of channels that pass the filters, then tells the list its new length.
    /// </summary>
    private void Rebuild()
    {
        var kind = _kind.SelectedIndex switch
        {
            1 => (StreamKind?)StreamKind.Live,
            2 => StreamKind.Movie,
            3 => StreamKind.Series,
            _ => null,
        };
        var categoryId = _category.SelectedIndex > 0 && _category.SelectedIndex - 1 < _categories.Count
            ? _categories[_category.SelectedIndex - 1].Id
            : null;
        var term = _search.Value.Trim();

        _filtered.Clear();
        for (var index = 0; index < _channels.Count; index++)
        {
            var channel = _channels[index];
            if (kind is StreamKind wanted && channel.Kind != wanted)
                continue;
            if (categoryId is not null && !string.Equals(channel.CategoryId, categoryId, StringComparison.Ordinal))
                continue;
            if (term.Length > 0 && channel.Name.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) < 0)
                continue;
            _filtered.Add(index);
        }

        _list.SetItemCount(_filtered.Count);
        if (_filtered.Count > 0)
        {
            _list.SelectedIndex = 0;
            _list.SetFocused(0);
            _list.EnsureVisible(0);
        }
        _list.RefreshItems(0, Math.Max(0, _filtered.Count - 1));
        // Translators: Channel-browser status line. {count} is how many channels pass the current filters.
        _count.Label = TrPluralFormat("{count} channel", "{count} channels", _filtered.Count, _filtered.Count);
    }
    // PLACEHOLDER
    /// <summary>Answers the list as it draws a row, mapping the visible row back to a channel.</summary>
    private string CellText(long row, int column)
    {
        if (row < 0 || row >= _filtered.Count)
            return string.Empty;
        var channel = _channels[_filtered[(int)row]];
        return column switch
        {
            0 => channel.Name,
            1 => _categoryNames.TryGetValue(channel.CategoryId, out var name) ? name : string.Empty,
            2 => channel.Number ?? string.Empty,
            3 => _guide?.NowNext(channel.EpgId, DateTimeOffset.Now).Now?.Title ?? string.Empty,
            _ => string.Empty,
        };
    }

    /// <summary>Ends the window, naming the channel to play. The one thing here that closes it.</summary>
    private void Play()
    {
        var row = _list.SelectedIndex;
        if (row < 0 || row >= _filtered.Count)
            return;
        _chosen = _filtered[(int)row];
        _dialog.EndModal(StandardId.Ok);
    }

    /// <remarks>
    /// Return plays only while the list has focus, so it does not fire while the user is typing in the search
    /// box. Play is not the default button for the same reason. F1 speaks how to use the window.
    /// </remarks>
    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
        {
            _dialog.EndModal(StandardId.Cancel);
            return;
        }
        if (args.Code == Key.F1)
        {
            _speakHelp(HelpText());
            return;
        }
        if (args.Code is Key.Enter or Key.NumpadEnter && ReferenceEquals(Window.FindFocus(), _list))
        {
            Play();
            return;
        }
        // A letter arrives as its uppercase ASCII value on a key-down event. Ctrl+I speaks what is on the
        // selected channel now and next, on demand, so arrowing the list never floods speech with the guide.
        if (args.Control && (char)args.Code == 'I')
        {
            SpeakNowNext();
            return;
        }
        args.Skip();
    }

    /// <summary>Speaks what is on the selected channel now and what follows it, or that there is no guide.
    /// Bound to an explicit key rather than firing on selection so moving through the list stays quiet.</summary>
    private void SpeakNowNext()
    {
        var row = _list.SelectedIndex;
        if (row < 0 || row >= _filtered.Count)
            return;
        var channel = _channels[_filtered[(int)row]];
        if (_guide is null)
        {
            // Translators: Spoken when the user asks what is on a channel but no programme guide was loaded.
            _speakHelp(Tr("No programme guide is available for this source."));
            return;
        }

        var (now, next) = _guide.NowNext(channel.EpgId, DateTimeOffset.Now);
        if (now is null && next is null)
        {
            // Translators: Spoken when the guide holds nothing for the selected channel right now.
            _speakHelp(Tr("No programme information for this channel."));
            return;
        }

        var nowText = now is EpgProgramme onNow
            // Translators: Spoken now-playing line. {title} is the programme showing on the channel now.
            ? TrFormat("Now: {title}", onNow.Title)
            // Translators: Spoken when nothing is scheduled on the channel at the moment.
            : Tr("Now: nothing scheduled");
        var nextText = next is EpgProgramme onNext
            // Translators: Spoken next-up line. {title} is the programme showing on the channel next.
            ? TrFormat("Next: {title}", onNext.Title)
            // Translators: Spoken when the guide lists nothing after the current programme.
            : Tr("Next: nothing scheduled");
        _speakHelp($"{nowText}. {nextText}");
    }

    private static string HelpText() =>
        // Translators: Spoken help for the channel browser, read aloud when the user presses F1.
        Tr("Choose what to show and a category from the lists, or type part of a name in the search box to narrow the channels. Press Enter on a channel to play it. Press Control plus I to hear what is on now and next. Press Escape to close.");

    /// <summary>The virtual list. It owns no rows; it asks the dialog for each cell as it draws it.</summary>
    private sealed class ChannelList(Window parent, Func<long, int, string> cell) : ListCtrl(parent,
        style: ListCtrlStyle.Report | ListCtrlStyle.Virtual | ListCtrlStyle.SingleSelection)
    {
        protected override string OnGetItemText(long item, int column) => cell(item, column);
    }
}
