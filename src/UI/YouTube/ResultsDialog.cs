using LunaPlayer.YouTube;
using WxSharp;

namespace LunaPlayer.UI.YouTube;

/// <summary>The window listing what a search or a playlist turned up.</summary>
///
/// <remarks>
/// The window closes for one thing only: playing a video. Copying an address, opening a browser, going to a
/// channel and saving a video all happen with it still open and the user still on the row they chose, which
/// is what the Python player does and what a list is for. Closing and reopening it for those would lose the
/// scroll position and the focus, and read the whole list out again.
///
/// A plain list rather than a virtual one. The window that lists loaded files is virtual because a folder
/// opened with its subfolders can hold a hundred thousand names; a page of results holds fifty, and the
/// list grows only as far as somebody has scrolled. Virtual mode would also work against the design here:
/// it wants the count before the rows exist, and it asks for text while painting, which is the worst place
/// to be deciding whether to go to the network.
/// </remarks>
internal sealed class ResultsDialog : IDisposable
{
    private readonly Dialog _dialog;
    private readonly StaticText _label;
    private readonly ListBox _list;
    private readonly List<YouTubeResult> _results;
    private readonly IYouTubeResultsFeed _feed;
    private readonly int _copyId = IdManager.NewId();
    private readonly int _browserId = IdManager.NewId();
    private readonly int _channelId = IdManager.NewId();
    private readonly int _downloadId = IdManager.NewId();
    private readonly Menu _menu;
    private int? _chosen;
    private bool _exhausted;
    private bool _loading;

    internal ResultsDialog(Window parent, YouTubeResultsPrompt prompt)
    {
        _results = [.. prompt.Results];
        _feed = prompt.Feed;
        _dialog = new Dialog(parent, title: prompt.Title, style: DialogStyle.Default | DialogStyle.ResizeBorder);
        _label = new StaticText(_dialog, label: prompt.Label);
        _list = new ListBox(_dialog);
        foreach (var result in _results)
            _list.Add(result.Title);
        if (_results.Count > 0)
            _list.SelectedIndex = Math.Clamp(prompt.SelectedIndex, 0, _results.Count - 1);

        // Translators: Button that plays the video chosen in the list of results.
        var play = new Button(_dialog, label: Tr("Play"));
        play.Click += (_, _) => Play();
        // Translators: Button that saves the video chosen in the list of results to a folder on this computer.
        var download = new Button(_dialog, label: Tr("Download"));
        download.Click += (_, _) => WithSelection(_feed.Download);
        // Translators: The button that closes a window.
        var close = new Button(_dialog, StandardId.Cancel, Tr("Close"));

        var buttons = new BoxSizer(Orientation.Horizontal);
        buttons.Add(play, flags: SizerFlags.BorderRight, border: 6);
        buttons.Add(download, flags: SizerFlags.BorderRight, border: 6);
        buttons.AddStretchSpacer();
        buttons.Add(close);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(_label, flags: SizerFlags.All | SizerFlags.Expand, border: 8);
        sizer.Add(_list, proportion: 1, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        sizer.Add(buttons, flags: SizerFlags.Expand | SizerFlags.BorderLeft | SizerFlags.BorderRight | SizerFlags.BorderBottom, border: 8);
        _dialog.SetSizer(sizer);
        _dialog.Fit();
        _dialog.MinSize = new Size(600, 380);
        _dialog.Center(onParent: true);

        _list.ItemActivated += (_, _) => Play();
        _list.SelectionChanged += OnSelectionChanged;
        _list.Bind(WxEvents.ContextMenu, OnContextMenu);
        _menu = BuildMenu();
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.CopyLink), _copyId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.OpenInBrowser), _browserId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.OpenChannel), _channelId);
        _dialog.Bind(WxEvents.MenuCommand, (_, _) => WithSelection(_feed.Download), _downloadId);
        _dialog.Bind(WxEvents.CharHook, OnCharHook);
        _list.Focus();
    }

    /// <summary>The row the user chose to play, or null when they closed the window instead.</summary>
    internal int? Show()
    {
        _dialog.ShowModal();
        return _chosen;
    }

    /// <remarks>
    /// The ids are deliberately not released. <c>Menu.Append</c> hands the reservation to a
    /// <c>wxWindowIDRef</c>, and destroying the menu returns the id to the pool - so releasing it here
    /// would be a second free, which wxWidgets asserts on in a debug build. This is also why the menu is
    /// built once and popped up repeatedly rather than rebuilt on every right-click.
    /// </remarks>
    public void Dispose()
    {
        // Before anything else: a page already on its way must not be handed to a list that has gone.
        _feed.Close();
        _menu.Dispose();
        _dialog.Dispose();
    }

    /// <summary>Ends the window, naming the row to play. The one thing here that closes it.</summary>
    private void Play()
    {
        if (_list.SelectedIndex < 0)
            return;
        _chosen = _list.SelectedIndex;
        _dialog.EndModal(StandardId.Ok);
    }

    /// <summary>Runs one of the things that leave the window open, on the row the user is on.</summary>
    private void WithSelection(Action<int> action)
    {
        if (_list.SelectedIndex >= 0)
            action(_list.SelectedIndex);
    }

    /// <summary>Asks for the next page once the user reaches the bottom of the list.</summary>
    /// <remarks>
    /// Both guards are needed. Adding rows raises a selection change of its own, so without
    /// <c>_loading</c> the first fetch would ask for the second before it had finished; and without
    /// <c>_exhausted</c> the end of the results would be asked for again on every keypress. The request
    /// returns immediately and the rows arrive later, so the list keeps answering while a page is on its
    /// way.
    /// </remarks>
    private void OnSelectionChanged(object? sender, CommandEventArgs args)
    {
        var selected = _list.SelectedIndex;
        if (selected < 0)
            return;
        _feed.Selected(selected);
        if (_exhausted || _loading || selected != _results.Count - 1)
            return;
        _loading = true;
        _feed.RequestMore(Append);
    }

    private void Append(IReadOnlyList<YouTubeResult> page)
    {
        _loading = false;
        if (page.Count == 0)
        {
            _exhausted = true;
            return;
        }
        _results.AddRange(page);
        foreach (var result in page)
            _list.Add(result.Title);
    }

    private Menu BuildMenu()
    {
        var menu = new Menu();
        // Translators: Context menu item in the results list that copies the address of the chosen video.
        menu.Append(_copyId, $"{Tr("Copy link")}\tCtrl+C");
        // Translators: Context menu item in the results list that shows the chosen video in the web browser.
        menu.Append(_browserId, $"{Tr("Open in browser")}\tCtrl+B");
        // Translators: Context menu item in the results list that shows the channel that published the video.
        menu.Append(_channelId, $"{Tr("Navigate to channel")}\tCtrl+N");
        menu.AppendSeparator();
        // Translators: Context menu item in the results list that saves the chosen video to this computer.
        menu.Append(_downloadId, Tr("Download"));
        return menu;
    }

    private void OnContextMenu(object? sender, ContextMenuEventArgs args)
    {
        if (_list.SelectedIndex >= 0)
            _dialog.PopupMenu(_menu);
    }

    /// <remarks>
    /// Return is answered only while the list has the focus, which is also why Play is not made the default
    /// button: a default button takes Return wherever the focus is, so pressing it on Close would play a
    /// video instead of closing the window. The Python player has exactly that fault, through an
    /// accelerator table rather than a default button.
    /// </remarks>
    private void OnCharHook(object? sender, KeyEventArgs args)
    {
        if (args.Code == Key.Escape)
        {
            _dialog.EndModal(StandardId.Cancel);
            return;
        }
        if (args.Code is Key.Enter or Key.NumpadEnter && ReferenceEquals(Window.FindFocus(), _list))
        {
            Play();
            return;
        }
        // A letter arrives as its uppercase ASCII value on a key-down event.
        if (args.Control && _list.SelectedIndex >= 0)
        {
            switch ((char)args.Code)
            {
                case 'C': _feed.CopyLink(_list.SelectedIndex); return;
                case 'B': _feed.OpenInBrowser(_list.SelectedIndex); return;
                case 'N': _feed.OpenChannel(_list.SelectedIndex); return;
            }
        }
        args.Skip();
    }
}
