using LunaPlayer.YouTube;
using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>The window listing what a search or a playlist turned up, or a channel's tabbed browser.</summary>
///
/// <remarks>
/// Non-play actions leave the dialog open and preserve the current selection. Results are paged into a
/// regular list because each page is small and rows arrive incrementally. A channel carries several such
/// lists at once, one per section, held behind a notebook: each is its own tab with its own list, loaded
/// the first time it is opened and then kept, so returning to a tab is instant. The Play, Download and
/// Close buttons sit in a row of their own below the notebook, not inside any one tab.
/// </remarks>
internal sealed class ResultsDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly StaticText _label;
    private readonly Notebook? _notebook;
    private IYouTubeResultsFeed _feed;
    private readonly List<TabPage> _tabs = [];
    private readonly int _copyId = IdManager.NewId();
    private readonly int _browserId = IdManager.NewId();
    private readonly int _channelId = IdManager.NewId();
    private readonly int _goToChannelId = IdManager.NewId();
    private readonly int _favoriteId = IdManager.NewId();
    private readonly int _directDownloadId = IdManager.NewId();
    private readonly int _downloadId = IdManager.NewId();
    private readonly int _queueId = IdManager.NewId();
    private readonly Menu _menu;
    private MenuItem _favoriteItem = null!;
    private MenuItem _queueItem = null!;
    private TabPage _active;
    private ResultChoice? _chosen;

    /// <summary>Set the moment the window is disposed, so a tab load that answers back afterwards touches
    /// none of the controls that have gone with it.</summary>
    private bool _closed;

    /// <summary>One tab's list and the rows behind it. A search or a playlist has exactly one; a channel
    /// has one per section, each paged and populated on its own.</summary>
    private sealed class TabPage
    {
        internal required ListBox List { get; init; }
        internal List<YouTubeResult> Results { get; } = [];
        /// <summary>Whether this window has put this tab's rows into its list yet.</summary>
        internal bool Loaded { get; set; }
        /// <summary>Whether a fetch for this tab - a switch to it or another page of it - is in flight.
        /// </summary>
        internal bool Loading { get; set; }
        /// <summary>Whether this tab has given everything it has, so the end need not be asked for again.
        /// </summary>
        internal bool Exhausted { get; set; }
    }

    internal ResultsDialog(Window parent, YouTubeResultsPrompt prompt)
    {
        _feed = prompt.Feed;
        _dialog = new Dialog(parent, title: prompt.Title, style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _label = new StaticText(_dialog, label: prompt.Label);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_label, flags: SizerFlags.All | SizerFlags.Expand, border: 8);

        if (prompt.Tabs is { Count: > 0 } tabNames)
        {
            // A channel: a notebook with a page per section. Each page carries its own list and its own
            // Play and Download buttons, so all three ride with the tab; only Close sits apart, below the
            // notebook. Only the tab the channel opened on is filled here; the rest fill when first opened.
            _notebook = new Notebook(_dialog);
            foreach (var name in tabNames)
            {
                var panel = new Panel(_notebook);
                var list = new ListBox(panel);
                var pageSizer = new BoxSizer(Orientation.Vertical);
                pageSizer.Add(list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.All, border: 4);
                pageSizer.Add(PageButtons(panel), flags: SizerFlags.BorderLeft | SizerFlags.BorderBottom, border: 4);
                panel.SetSizer(pageSizer);
                _notebook.AddPage(panel, name);
                var tab = new TabPage { List = list };
                _tabs.Add(tab);
                Wire(list);
            }
            sizer.Add(_notebook, proportion: 1,
                flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
                border: 8);
            var opening = Math.Clamp(prompt.SelectedTab, 0, _tabs.Count - 1);
            _active = _tabs[opening];
            Populate(_active, prompt.Results, prompt.SelectedIndex);
            _notebook.SelectedIndex = opening;
        }
        else
        {
            // A search or a playlist: the plain single list, no notebook. The list and its Play and
            // Download buttons still sit together, with only Close set apart below.
            var list = new ListBox(_dialog);
            var tab = new TabPage { List = list };
            _tabs.Add(tab);
            _active = tab;
            Wire(list);
            sizer.Add(list, proportion: 1,
                flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
                border: 8);
            sizer.Add(PageButtons(_dialog),
                flags: SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
            Populate(tab, prompt.Results, prompt.SelectedIndex);
        }

        // Translators: The button that closes a window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));
        var closeRow = new BoxSizer(Orientation.Horizontal);
        closeRow.AddStretchSpacer();
        closeRow.Add(close);
        sizer.Add(closeRow,
            flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom,
            border: 8);

        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(600, 380);
        _dialog.Center(onParent: true);

        _menu = BuildMenu();
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.CopyLink), _copyId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.OpenInBrowser), _browserId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.OpenChannelInBrowser), _channelId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => ChooseChannel(), _goToChannelId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.ToggleFavorite), _favoriteId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.Download), _downloadId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.DirectDownload), _directDownloadId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.AddToDownloadQueue), _queueId);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        // Bound after the opening tab is chosen above, so selecting it fires no switch of its own.
        if (_notebook is not null)
            _notebook.PageChanged += OnPageChanged;
        _active.List.Focus();
    }

    /// <summary>Whether this window is the plain single-list kind (a search or playlist), as opposed to a
    /// channel's tabbed browser. Only the single-list kind is reused through <see cref="Refresh"/>, because
    /// the two have different control structures.</summary>
    internal bool IsSingleList => _notebook is null;

    /// <summary>The row the user chose to play and whether they asked for its picture or its sound, or null
    /// when they closed the window instead.</summary>
    internal ResultChoice? Show()
    {
        _chosen = null;
        _dialog.ShowModal();
        return _chosen;
    }

    /// <summary>Reopens this single-list window for a new result set instead of building a new window: it
    /// takes the new feed and rows and clears the previous choice, so reopening a search or returning from
    /// the player costs a list refresh rather than a full rebuild. The menu and key bindings read the feed
    /// through the field, so swapping the field is enough to repoint them. Only valid when
    /// <see cref="IsSingleList"/>.</summary>
    internal void Refresh(YouTubeResultsPrompt prompt)
    {
        _closed = false;
        _feed = prompt.Feed;
        _label.Label = prompt.Label;
        Populate(_active, prompt.Results, prompt.SelectedIndex);
        _active.List.Focus();
    }

    /// <remarks>
    /// The ids are deliberately not released. <c>Menu.Append</c> hands the reservation to a
    /// <c>wxWindowIDRef</c>, and destroying the menu returns the id to the pool - so releasing it here
    /// would be a second free, which wxWidgets asserts on in a debug build. This is also why the menu is
    /// built once and popped up repeatedly rather than rebuilt on every right-click.
    /// </remarks>
    public void Dispose()
    {
        // Before anything else: a page or a tab already on its way must not be handed to a list that has
        // gone. The flag stops the deferred callbacks, and closing the feed stops any it has not yet made.
        _closed = true;
        _feed.Close();
        _menu.Dispose();
        _dialog.Dispose();
    }

    private void Wire(ListBox list)
    {
        list.ItemActivated += (_, _) => Play();
        list.SelectionChanged += OnSelectionChanged;
        list.Bind(WxEvents.ContextMenu, OnContextMenu);
    }

    /// <summary>The Play and Download buttons that ride with a list - inside each notebook page for a
    /// channel, beside the single list otherwise. Both act on whichever tab is current, which is always the
    /// one whose buttons are on screen.</summary>
    private BoxSizer PageButtons(Window parent)
    {
        // Translators: Button that plays the video chosen in the list of results.
        var play = new Button(parent, label: Tr("Play"));
        play.Click += (_, _) => Play();
        // Translators: Button that plays only the sound of the video chosen in the list, with no picture.
        var playAudio = new Button(parent, label: Tr("Play as audio"));
        playAudio.Click += (_, _) => PlayAudio();
        // Translators: Button that saves the video chosen in the list of results to a folder on this computer.
        var download = new Button(parent, label: Tr("Download"));
        download.Click += (_, _) => WithSelection(_feed.Download);
        var row = new BoxSizer(Orientation.Horizontal);
        row.Add(play, flags: SizerFlags.BorderRight, border: 6);
        row.Add(playAudio, flags: SizerFlags.BorderRight, border: 6);
        row.Add(download);
        return row;
    }

    /// <summary>Fills one tab's list with a set of rows and marks it loaded.</summary>
    private void Populate(TabPage tab, IReadOnlyList<YouTubeResult> items, int selected)
    {
        tab.Results.Clear();
        tab.Results.AddRange(items);
        // One bulk crossing - Set replaces the list and settles it as a single change, rather than redrawing
        // and re-announcing a row at a time.
        tab.List.Set(items.Select(Compose));
        if (tab.Results.Count > 0)
            tab.List.SelectedIndex = Math.Clamp(selected, 0, tab.Results.Count - 1);
        tab.Loaded = true;
        tab.Loading = false;
        tab.Exhausted = false;
    }

    /// <summary>The user moved to another channel tab. A tab already loaded is shown at once; one not yet
    /// seen is fetched and filled when it arrives.</summary>
    private void OnPageChanged(object? sender, BookEventArgs args)
    {
        if (_closed || _notebook is null)
            return;
        var index = args.Selection;
        if (index < 0 || index >= _tabs.Count)
            return;
        var tab = _tabs[index];
        _active = tab;
        if (tab.Loaded)
        {
            // Its rows are already in hand; only the session need be told which tab is current now. No
            // fetch - this is the whole point of keeping each visited tab. Focus is left on the tab control
            // the user is moving through rather than pulled down into the list.
            _feed.SwitchTab(index, _ => { });
            return;
        }
        if (tab.Loading)
            return;
        tab.Loading = true;
        _feed.SwitchTab(index, items => OnTabLoaded(index, items));
    }

    /// <summary>A channel tab's rows have come back. Null means the fetch failed or was refused, so the tab
    /// is left unloaded and a later visit tries it again.</summary>
    private void OnTabLoaded(int index, IReadOnlyList<YouTubeResult>? items)
    {
        if (_closed || index < 0 || index >= _tabs.Count)
            return;
        var tab = _tabs[index];
        tab.Loading = false;
        if (items is null)
            return;
        // The rows land in the list, but focus is left where it is: a fetch finishes on its own schedule,
        // and pulling focus to the list after the user has moved on is jarring.
        Populate(tab, items, 0);
    }

    /// <summary>Ends the window, naming the row to play and how. Enter plays the picture, Ctrl+Enter the
    /// sound alone. The one thing here that closes it.</summary>
    private void Play() => Choose(PlayMode.Video);

    private void PlayAudio() => Choose(PlayMode.Audio);

    private void Choose(PlayMode mode)
    {
        if (_active.List.SelectedIndex < 0)
            return;
        _chosen = new ResultChoice(_active.List.SelectedIndex, mode);
        _dialog.EndModal(StandardId.Ok);
    }

    /// <summary>Ends the window to browse into the selected row's channel rather than play the row. It closes
    /// the same way a play does, so the session replaces this list with the channel cleanly instead of
    /// opening one over the top of a list that is still modal.</summary>
    private void ChooseChannel()
    {
        if (_active.List.SelectedIndex < 0)
            return;
        _chosen = new ResultChoice(_active.List.SelectedIndex, PlayMode.Video) { Channel = true };
        _dialog.EndModal(StandardId.Ok);
    }

    /// <summary>Runs one of the things that leave the window open, on the row the user is on.</summary>
    private void WithSelection(Action<int> action)
    {
        if (_active.List.SelectedIndex >= 0)
            action(_active.List.SelectedIndex);
    }

    /// <summary>Asks for the next page once the user reaches the bottom of the current tab's list.</summary>
    /// <remarks>
    /// Both guards are needed. Adding rows raises a selection change of its own, so without
    /// <c>Loading</c> the first fetch would ask for the second before it had finished; and without
    /// <c>Exhausted</c> the end of the results would be asked for again on every keypress. The request
    /// returns immediately and the rows arrive later, so the list keeps answering while a page is on its
    /// way. Every flag is the current tab's own, so paging one tab never blocks another.
    /// </remarks>
    private void OnSelectionChanged(object? sender, CommandEventArgs args)
    {
        var tab = _active;
        var selected = tab.List.SelectedIndex;
        if (selected < 0)
            return;
        _feed.Selected(selected);
        if (tab.Exhausted || tab.Loading || selected != tab.Results.Count - 1)
            return;
        tab.Loading = true;
        _feed.RequestMore(page => Append(tab, page));
    }

    private void Append(TabPage tab, IReadOnlyList<YouTubeResult> page)
    {
        if (_closed)
            return;
        tab.Loading = false;
        if (page.Count == 0)
        {
            tab.Exhausted = true;
            return;
        }
        tab.Results.AddRange(page);
        // The new page joins the list in one crossing rather than a row at a time.
        tab.List.AppendRange(page.Select(Compose));
    }

    /// <summary>The one line a row shows: the title, then the few
    /// secondary facts the kind of row has, joined with commas and skipping the ones the listing left out.
    /// </summary>
    private static string Compose(YouTubeResult result)
    {
        var parts = new List<string>(5) { result.Title };
        switch (result.ItemType)
        {
            case YouTubeItemType.Playlist:
                // Translators: Marks a row in the results list as a playlist rather than a single video.
                parts.Add(Tr("Playlist"));
                if (result.Author.Length > 0)
                    // Translators: Names who published a result. {author} is a channel name.
                    parts.Add(TrFormat("by {author}", result.Author));
                if (!string.IsNullOrEmpty(result.Views))
                    // Translators: How many videos a playlist holds. {count} is a number.
                    parts.Add(TrFormat("contains {count} videos", result.Views));
                break;
            case YouTubeItemType.Channel:
                // Translators: Marks a row in the results list as a channel rather than a single video.
                parts.Add(Tr("Channel"));
                if (!string.IsNullOrEmpty(result.Views))
                    parts.Add(result.Views);
                if (!string.IsNullOrEmpty(result.PublishedTime))
                    // Translators: How many videos a channel holds. {count} is a number.
                    parts.Add(TrFormat("contains {count} videos", result.PublishedTime));
                break;
            default:
                if (result.Duration is { } duration)
                    parts.Add(FormatDuration(duration));
                if (result.Author.Length > 0)
                    // Translators: Names who published a video result. {author} is a channel name.
                    parts.Add(TrFormat("by {author}", result.Author));
                if (!string.IsNullOrEmpty(result.Views))
                    parts.Add(result.Views);
                if (!string.IsNullOrEmpty(result.PublishedTime))
                    parts.Add(result.PublishedTime);
                break;
        }
        return string.Join(", ", parts);
    }

    /// <summary>A duration as "m:ss", or "h:mm:ss" once it runs past an hour.</summary>
    private static string FormatDuration(TimeSpan duration)
        => duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}"
            : $"{duration.Minutes}:{duration.Seconds:D2}";

    private Menu BuildMenu()
    {
        var menu = new Menu();
        // Translators: Context menu item in the results list that copies the address of the chosen video.
        menu.Append(_copyId, $"{Tr("Copy link")}\tCtrl+C");
        // Translators: Context menu item in the results list that shows the chosen video in the web browser.
        menu.Append(_browserId, $"{Tr("Open in browser")}\tCtrl+B");
        // Translators: Context menu item in the results list that opens the channel that published the video in the web browser.
        menu.Append(_channelId, $"{Tr("Open channel in browser")}\tCtrl+N");
        // Translators: Context menu item in the results list that opens the channel that published the video inside the player.
        menu.Append(_goToChannelId, $"{Tr("Go to channel")}\tCtrl+G");
        // The label switches between adding and removing just before the menu opens, in OnContextMenu, from
        // whether the chosen row is already a favourite. It starts on the add wording; the keep-the-id note
        // in Dispose is why it is relabelled rather than the menu rebuilt.
        // Translators: Context menu item in the results list that saves the chosen video to the favourites.
        _favoriteItem = menu.Append(_favoriteId, $"{Tr("Add to favorites")}\tCtrl+Space");
        menu.AppendSeparator();
        // Translators: Context menu item in the results list that opens the download window to choose how the chosen video is saved.
        menu.Append(_downloadId, $"{Tr("Download...")}\tCtrl+Shift+D");
        // Translators: Context menu item in the results list that saves the chosen video straight to disk with the saved download settings.
        menu.Append(_directDownloadId, $"{Tr("Direct download")}\tCtrl+D");
        // Translators: Context menu item that adds the selected video to the download queue.
        _queueItem = menu.Append(_queueId, $"{Tr("Add to download queue")}\tCtrl+Q");
        return menu;
    }

    private void OnContextMenu(object? sender, ContextMenuEventArgs args)
    {
        if (_active.List.SelectedIndex < 0)
            return;
        // Relabel the favourite entry for this row before the menu opens: an already-saved row offers to be
        // removed, any other to be added, so the one key does both.
        _favoriteItem.Label = _feed.IsFavorite(_active.List.SelectedIndex)
            // Translators: Context menu item in the results list that takes the chosen video back out of the favourites.
            ? $"{Tr("Remove from favorites")}\tCtrl+Space"
            // Translators: Context menu item in the results list that saves the chosen video to the favourites.
            : $"{Tr("Add to favorites")}\tCtrl+Space";
        _queueItem.Enabled = _active.Results[_active.List.SelectedIndex].ItemType is YouTubeItemType.Video;
        _dialog.PopupMenu(_menu);
    }

    /// <remarks>
    /// Return plays only while the list has focus. Play is not the default button because that would also
    /// capture Return from other controls.
    /// </remarks>
    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
        {
            _dialog.EndModal(StandardId.Cancel);
            return;
        }
        if (args.Code is Key.Enter or Key.NumpadEnter && ReferenceEquals(Window.FindFocus(), _active.List))
        {
            // Enter plays the picture; Ctrl+Enter plays the sound alone. The same row, two streams.
            if (args.Control)
                PlayAudio();
            else
                Play();
            return;
        }
        // A letter arrives as its uppercase ASCII value on a key-down event.
        if (args.Control && _active.List.SelectedIndex >= 0)
        {
            if (args.Code is Key.Space or Key.NumpadSpace)
            {
                _feed.ToggleFavorite(_active.List.SelectedIndex);
                return;
            }
            switch ((char)args.Code)
            {
                case 'C': _feed.CopyLink(_active.List.SelectedIndex); return;
                case 'B': _feed.OpenInBrowser(_active.List.SelectedIndex); return;
                case 'N': _feed.OpenChannelInBrowser(_active.List.SelectedIndex); return;
                case 'G': ChooseChannel(); return;
                case 'Q':
                    if (!args.Shift)
                        _feed.AddToDownloadQueue(_active.List.SelectedIndex);
                    return;
                case 'D':
                    // Ctrl+D saves straight away with the settings; Ctrl+Shift+D opens the download window.
                    if (args.Shift)
                        _feed.Download(_active.List.SelectedIndex);
                    else
                        _feed.DirectDownload(_active.List.SelectedIndex);
                    return;
            }
        }
        args.Skip();
    }
}
