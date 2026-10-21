using LunaPlayer.Actions;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Equalizer;
using LunaPlayer.Recording;
using WxSharp;

namespace LunaPlayer.UI;

/// <summary>The main window: frame, transport buttons, and lifetime. Menu state lives in MenuController and
/// keyboard input in WindowInput; this forwards IMenuState calls and input events to them.</summary>
internal sealed class MainFrame : IMainView
{
    private readonly Frame _frame;
    private readonly List<Bitmap> _transportBitmaps = [];
    private CustomBitmapButton? _playButton;
    private Bitmap? _playBitmap;
    private Bitmap? _pauseBitmap;
    private MenuController? _menus;
    private WindowInput? _input;
    private IApplicationDispatcher? _dispatcher;
    private LunaPlayer.Recording.AudioCatalog? _catalog;
    private DialogService? _dialogs;
    private bool _disposed;
    private bool _closing;
    private bool _closeFinished;
    private Task? _shutdownTask;
    // The title last handed to the frame, so an unchanged one is not set again: every set is a name-change
    // event a screen reader reads out, and repeating the same text would announce it for nothing.
    private string _windowTitle = AppInfo.Name;

    /// <summary>Creates only the top-level window. Its menus and controls are added after the window has
    /// been shown, so native and configuration startup work cannot delay the first visible frame.</summary>
    internal MainFrame()
    {
        _frame = new Frame(title: AppInfo.Name, size: new Size(420, 160));
        // The themed window background rather than the grey a frame paints by default, so the main window
        // follows the chosen light/dark/high-contrast appearance. Asked of the system (not hard-coded), so
        // it is already the dark colour when dark mode is on.
        _frame.BackgroundColour = SystemSettings.GetColour(SystemColour.Window);
        _frame.Closing += OnClosing;
    }

    /// <param name="dispatcher">Handed on to the windows that fetch things in the background - the
    /// recording window asks Windows for its devices and its encoders - so they post their answers back
    /// through the same guarded path as everything else.</param>
    internal void Initialize(
        ShortcutManager shortcuts,
        IReadOnlyList<ActionDefinition> actions,
        IApplicationDispatcher dispatcher,
        LunaPlayer.Recording.AudioCatalog catalog,
        IReadOnlyList<PresetEntry> presets,
        RecentMenuState recents)
    {
        if (_input is not null)
            return;
        _dispatcher = dispatcher;
        _catalog = catalog;
        // The window and taskbar icon, the same V2 moon-and-play mark embedded in the executable. Guarded so
        // a missing or unreadable asset file can never stop the window from opening.
        try
        {
            _frame.SetIcon(new Icon(Path.Combine(Paths.IconsDirectory, "LunaPlayer.ico")));
        }
        catch (Exception)
        {
        }
        var menus = new MenuController(_frame, actions, shortcuts, presets, recents);
        _menus = menus;
        var input = new WindowInput(_frame, menus, shortcuts);
        _input = input;
        // The transport buttons show the control icons rather than text. They sit directly on the frame, not
        // on a wxPanel: a panel with no keyboard-focusable children becomes the focus target when the window
        // opens, and a screen reader announces "panel" on every launch. Each is a CustomBitmapButton, which
        // draws only its icon, keeps a spoken name through its label, and declines focus so the window's
        // controls are reached through the menus and shortcut keys rather than Tab. The grey a bare frame
        // would paint is handled by the themed background set above.
        var controls = Paths.IconsDirectory;
        Bitmap LoadControl(string name) => new(Path.Combine(controls, name + ".png"));
        var previousBitmap = LoadControl("previous");
        var rewindBitmap = LoadControl("rewind");
        var playBitmap = LoadControl("play");
        _playBitmap = playBitmap;
        var pauseBitmap = LoadControl("pause");
        _pauseBitmap = pauseBitmap;
        var forwardBitmap = LoadControl("forward");
        var nextBitmap = LoadControl("next");
        // Held for the button lifetime: the Play button swaps between play and pause, and keeping every
        // bitmap alive until the frame goes avoids disposing one a button still draws.
        _transportBitmaps.AddRange(
            [previousBitmap, rewindBitmap, playBitmap, pauseBitmap, forwardBitmap, nextBitmap]);

        // Translators: Spoken name of the main-window button that plays the previous track. The button shows an icon, not this text.
        var previousButton = new CustomBitmapButton(_frame, previousBitmap, Tr("Previous"));
        // Translators: Spoken name of the main-window button that steps playback backward. The button shows an icon, not this text.
        var rewindButton = new CustomBitmapButton(_frame, rewindBitmap, Tr("Rewind"));
        // Translators: Spoken name of the main-window button that starts playback. It becomes Pause while playing. The button shows an icon, not this text.
        var playButton = new CustomBitmapButton(_frame, playBitmap, Tr("Play"));
        _playButton = playButton;
        // Translators: Spoken name of the main-window button that steps playback forward. The button shows an icon, not this text.
        var forwardButton = new CustomBitmapButton(_frame, forwardBitmap, Tr("Forward"));
        // Translators: Spoken name of the main-window button that plays the next track. The button shows an icon, not this text.
        var nextButton = new CustomBitmapButton(_frame, nextBitmap, Tr("Next"));

        var buttonSizer = new BoxSizer(Orientation.Horizontal);
        buttonSizer.Insert(0, previousButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(rewindButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(playButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(forwardButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(nextButton, flags: SizerFlags.All, border: 5);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(buttonSizer, flags: SizerFlags.AlignCenterHorizontal | SizerFlags.All, border: 15);
        _frame.SetSizer(sizer);
        _frame.Fit();

        previousButton.Click += (_, _) => input.Request(ActionId.PreviousTrack);
        rewindButton.Click += (_, _) => input.Request(ActionId.SeekBackward);
        playButton.Click += (_, _) => input.Request(ActionId.PlayPause);
        forwardButton.Click += (_, _) => input.Request(ActionId.SeekForward);
        nextButton.Click += (_, _) => input.Request(ActionId.NextTrack);
        menus.SetMediaLoaded(false);
    }
    // Input events are raised by WindowInput and forwarded here; CloseRequested is the window's own.
    public event Action<ActionId>? ActionRequested
    {
        add => Input.ActionRequested += value;
        remove => Input.ActionRequested -= value;
    }
    public event Action<string?>? EqualizerPresetRequested
    {
        add => Input.EqualizerPresetRequested += value;
        remove => Input.EqualizerPresetRequested -= value;
    }
    public event Action<int?>? SubtitleTrackRequested
    {
        add => Input.SubtitleTrackRequested += value;
        remove => Input.SubtitleTrackRequested -= value;
    }
    public event Action<RecentCommand>? RecentRequested
    {
        add => Input.RecentRequested += value;
        remove => Input.RecentRequested -= value;
    }
    public event Func<bool>? EscapePressed
    {
        add => Input.EscapePressed += value;
        remove => Input.EscapePressed -= value;
    }
    public event Func<bool>? CloseConfirmationRequested;
    public event Func<Task>? CloseRequested;

    public nint NativeHandle => _frame.NativeHandle;

    public void Show()
    {
        _frame.Show();
        // wxWidgets normally leaves the first paint in the event queue. Force the lightweight shell to draw
        // now, before menu construction or any service completion callback can occupy the UI thread.
        _frame.Update();
    }

    public void Close() => _frame.Close();

    public void RestoreAndRaise()
    {
        _frame.Iconize(false);
        _frame.Raise();
    }

    public void SetPlaying(bool isPlaying)
    {
        var playButton = PlayButton;
        playButton.SetBitmap(isPlaying ? _pauseBitmap! : _playBitmap!);
        playButton.Label = isPlaying
            // Translators: Spoken name of the play/pause button while a track is playing, so the key pauses it.
            ? Tr("Pause")
            // Translators: Spoken name of the play/pause button while playback is stopped or paused.
            : Tr("Play");
    }

    public void SetWindowTitle(string title)
    {
        if (string.Equals(title, _windowTitle, StringComparison.Ordinal)) return;
        _windowTitle = title;
        _frame.Title = title;
    }

    public void SetFullScreen(bool fullScreen)
    {
        _frame.ShowFullScreen(fullScreen, FullScreenStyle.All);
        Menus.SetFullScreenChecked(fullScreen);
    }

    public bool IsFullScreen => _frame.IsFullScreen;

    /// <summary>Dialogs, built lazily and parented to this window.</summary>
    public IDialogService Dialogs => _dialogs ??= new DialogService(
        _frame, _dispatcher!, _catalog!, Input.GlobalShortcuts);

    public bool ApplyGlobalShortcuts(ShortcutManager shortcuts) => Input.ApplyGlobalShortcuts(shortcuts);

    public void ApplyShortcuts(ShortcutManager shortcuts)
    {
        Menus.ApplyShortcuts(shortcuts);
        Input.ApplyAccelerators(shortcuts);
    }
    public void SetShuffleChecked(bool isChecked) => Menus.SetShuffleChecked(isChecked);

    public void SetRepeatFileChecked(bool isChecked) => Menus.SetRepeatFileChecked(isChecked);

    public void SetSilenceRemovalChecked(bool isChecked) => Menus.SetSilenceRemovalChecked(isChecked);

    public void SetSelectionState(SelectionMenuState state) => Menus.SetSelectionState(state);

    public void SetFullScreenAvailable(bool available) => Menus.SetFullScreenAvailable(available);

    public void SetEqualizerPreset(string? presetId) => Menus.SetEqualizerPreset(presetId);

    public void RebuildEqualizerMenu(IReadOnlyList<PresetEntry> presets, string? selected)
        => Menus.RebuildEqualizerMenu(presets, selected);

    public void RebuildSubtitleMenu(IReadOnlyList<SubtitleMenuEntry> entries, int? activeId)
        => Menus.RebuildSubtitleMenu(entries, activeId);

    public void SetSubtitleRemember(bool on) => Menus.SetSubtitleRemember(on);

    public void RebuildRecentsMenu(
        IReadOnlyList<RecentMenuEntry> files,
        IReadOnlyList<RecentMenuEntry> folders,
        IReadOnlyList<RecentMenuEntry> playlists)
        => Menus.RebuildRecentsMenu(files, folders, playlists);

    public void SetSubtitleSelection(int? activeId) => Menus.SetSubtitleSelection(activeId);

    public void SetSubtitleControlsEnabled(bool enabled) => Menus.SetSubtitleControlsEnabled(enabled);

    public void SetMediaLoaded(bool loaded) => Menus.SetMediaLoaded(loaded);

    public void SetEditState(bool hasLocalFile, bool hasMedia) => Menus.SetEditState(hasLocalFile, hasMedia);

    public void SetBookmarkState(bool enabled) => Menus.SetBookmarkState(enabled);

    public void SetMarkState(bool currentMarked, bool allMarked) => Menus.SetMarkState(currentMarked, allMarked);

    public void SetRecordingState(RecordingState state) => Menus.SetRecordingState(state);

    public void SetVideoOptionsEnabled(bool enabled) => Menus.SetVideoOptionsEnabled(enabled);

    public void SetAudioTrackControlsEnabled(bool enabled) => Menus.SetAudioTrackControlsEnabled(enabled);

    public void SetMarkedActionsEnabled(bool enabled) => Menus.SetMarkedActionsEnabled(enabled);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        // Before the frame goes: a keyboard hook left installed keeps being called into a process that is
        // shutting down.
        _input?.Dispose();
        // Before the frame: the reused results window is a child of it, so it must be torn down before the
        // frame destroys the native window out from under it.
        _dialogs?.Dispose();
        _frame.Dispose();
        // After the frame, so the buttons that drew them are gone first.
        foreach (var bitmap in _transportBitmaps)
            bitmap.Dispose();
    }

    private void OnClosing(object? sender, CloseEventArgs args)
    {
        if (_closing)
        {
            if (!_closeFinished && args.CanVeto)
                args.Veto();
            else
            {
                // A non-vetoable system close must not destroy the video handle under an in-flight mpv
                // shutdown. Ordinary closes reach here only after CompleteCloseAsync marks it finished.
                if (!_closeFinished)
                    WaitForShutdown();
                args.Skip();
            }
            return;
        }

        if (!_closing && args.CanVeto && !CanClose())
        {
            args.Veto();
            return;
        }

        _closing = true;
        // Disappear as soon as the close is accepted, before settings, media devices and network streams are
        // released. The native frame is deliberately not destroyed yet: mpv renders into its handle and must
        // detach while that handle remains valid. This leaves slow device-specific teardown invisible without
        // racing it against destruction of the video window.
        _frame.Hide();
        // Request shutdown before releasing peripheral services. The controller stops playback synchronously
        // before its returned task begins waiting for file writes and native teardown, so audio stops in the
        // same close-event dispatch that Windows initiated.
        _shutdownTask = RequestShutdownAsync();
        // Parentless modeless progress windows do not follow the frame when it is hidden and would otherwise
        // remain visible during teardown. They are views only; their jobs stay owned by the queue.
        _dialogs?.CloseDownloadProgressWindows();
        // The listener marks itself inactive before asking its background message loop to exit. It therefore
        // cannot dispatch another action even though shutdown does not wait for that thread to finish.
        _input?.StopGlobalShortcuts();
        if (!args.CanVeto)
        {
            WaitForShutdown();
            args.Skip();
            return;
        }

        args.Veto();
        _ = CompleteCloseAsync(_shutdownTask);
    }

    private async Task RequestShutdownAsync()
    {
        if (CloseRequested is null)
            return;
        foreach (Func<Task> close in CloseRequested.GetInvocationList())
            await close().ConfigureAwait(false);
    }

    private async Task CompleteCloseAsync(Task shutdown)
    {
        Exception? failure = null;
        try
        {
            await shutdown.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        try
        {
            void Finish()
            {
                if (_disposed)
                    return;
                if (failure is not null)
                    CrashReport.Report(failure);
                _closeFinished = true;
                _frame.Close(force: true);
            }

            if (_dispatcher is null)
                Wx.CallAfter(() => CrashReport.Guard(Finish));
            else
                _dispatcher.Post(Finish);
        }
        catch (InvalidOperationException)
        {
            // The event loop has already ended, so there is no frame left to close.
        }
    }

    private void WaitForShutdown()
    {
        try
        {
            _shutdownTask?.GetAwaiter().GetResult();
        }
        catch (Exception failure)
        {
            CrashReport.Report(failure);
        }
        _closeFinished = true;
    }

    private bool CanClose()
    {
        if (CloseConfirmationRequested is null)
            return true;
        foreach (Func<bool> confirm in CloseConfirmationRequested.GetInvocationList())
            if (!confirm())
                return false;
        return true;
    }

    private WindowInput Input
        => _input ?? throw new InvalidOperationException("The main window has not finished initializing.");

    private MenuController Menus
        => _menus ?? throw new InvalidOperationException("The main window has not finished initializing.");

    private CustomBitmapButton PlayButton
        => _playButton ?? throw new InvalidOperationException("The main window has not finished initializing.");
}
