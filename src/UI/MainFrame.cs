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
    private readonly CustomBitmapButton _playButton;
    private readonly Bitmap _playBitmap;
    private readonly Bitmap _pauseBitmap;
    private readonly Bitmap[] _transportBitmaps;
    private readonly MenuController _menus;
    private readonly WindowInput _input;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly LunaPlayer.Recording.AudioCatalog _catalog;
    private DialogService? _dialogs;
    private bool _disposed;
    private bool _waitingForCloseKeys;
    private bool _closing;
    // The title last handed to the frame, so an unchanged one is not set again: every set is a name-change
    // event a screen reader reads out, and repeating the same text would announce it for nothing.
    private string _windowTitle = AppInfo.Name;

    /// <param name="dispatcher">Handed on to the windows that fetch things in the background - the
    /// recording window asks Windows for its devices and its encoders - so they post their answers back
    /// through the same guarded path as everything else.</param>
    internal MainFrame(
        ShortcutManager shortcuts,
        IReadOnlyList<ActionDefinition> actions,
        IApplicationDispatcher dispatcher,
        LunaPlayer.Recording.AudioCatalog catalog,
        IReadOnlyList<PresetEntry> presets)
    {
        _dispatcher = dispatcher;
        _catalog = catalog;
        _frame = new Frame(title: AppInfo.Name, size: new Size(420, 160));
        // The themed window background rather than the grey a frame paints by default, so the main window
        // follows the chosen light/dark/high-contrast appearance. Asked of the system (not hard-coded), so
        // it is already the dark colour when dark mode is on.
        _frame.BackgroundColour = SystemSettings.GetColour(SystemColour.Window);
        // The window and taskbar icon, the same V2 moon-and-play mark embedded in the executable. Guarded so
        // a missing or unreadable asset file can never stop the window from opening.
        try
        {
            _frame.SetIcon(new Icon(Path.Combine(Paths.IconsDirectory, "LunaPlayer.ico")));
        }
        catch (Exception)
        {
        }
        _menus = new MenuController(_frame, actions, shortcuts, presets);
        _input = new WindowInput(_frame, _menus, shortcuts);
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
        _playBitmap = LoadControl("play");
        _pauseBitmap = LoadControl("pause");
        var forwardBitmap = LoadControl("forward");
        var nextBitmap = LoadControl("next");
        // Held for the button lifetime: the Play button swaps between play and pause, and keeping every
        // bitmap alive until the frame goes avoids disposing one a button still draws.
        _transportBitmaps = [previousBitmap, rewindBitmap, _playBitmap, _pauseBitmap, forwardBitmap, nextBitmap];

        // Translators: Spoken name of the main-window button that plays the previous track. The button shows an icon, not this text.
        var previousButton = new CustomBitmapButton(_frame, previousBitmap, Tr("Previous"));
        // Translators: Spoken name of the main-window button that steps playback backward. The button shows an icon, not this text.
        var rewindButton = new CustomBitmapButton(_frame, rewindBitmap, Tr("Rewind"));
        // Translators: Spoken name of the main-window button that starts playback. It becomes Pause while playing. The button shows an icon, not this text.
        _playButton = new CustomBitmapButton(_frame, _playBitmap, Tr("Play"));
        // Translators: Spoken name of the main-window button that steps playback forward. The button shows an icon, not this text.
        var forwardButton = new CustomBitmapButton(_frame, forwardBitmap, Tr("Forward"));
        // Translators: Spoken name of the main-window button that plays the next track. The button shows an icon, not this text.
        var nextButton = new CustomBitmapButton(_frame, nextBitmap, Tr("Next"));

        var buttonSizer = new BoxSizer(Orientation.Horizontal);
        buttonSizer.Insert(0, previousButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(rewindButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(_playButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(forwardButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(nextButton, flags: SizerFlags.All, border: 5);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(buttonSizer, flags: SizerFlags.AlignCenterHorizontal | SizerFlags.All, border: 15);
        _frame.SetSizer(sizer);
        _frame.Fit();

        previousButton.Click += (_, _) => _input.Request(ActionId.PreviousTrack);
        rewindButton.Click += (_, _) => _input.Request(ActionId.SeekBackward);
        _playButton.Click += (_, _) => _input.Request(ActionId.PlayPause);
        forwardButton.Click += (_, _) => _input.Request(ActionId.SeekForward);
        nextButton.Click += (_, _) => _input.Request(ActionId.NextTrack);
        _frame.Closing += OnClosing;
        _menus.SetMediaLoaded(false);
    }
    // Input events are raised by WindowInput and forwarded here; CloseRequested is the window's own.
    public event Action<ActionId>? ActionRequested
    {
        add => _input.ActionRequested += value;
        remove => _input.ActionRequested -= value;
    }
    public event Action<string?>? EqualizerPresetRequested
    {
        add => _input.EqualizerPresetRequested += value;
        remove => _input.EqualizerPresetRequested -= value;
    }
    public event Action<int?>? SubtitleTrackRequested
    {
        add => _input.SubtitleTrackRequested += value;
        remove => _input.SubtitleTrackRequested -= value;
    }
    public event Action<RecentCommand>? RecentRequested
    {
        add => _input.RecentRequested += value;
        remove => _input.RecentRequested -= value;
    }
    public event Func<bool>? EscapePressed
    {
        add => _input.EscapePressed += value;
        remove => _input.EscapePressed -= value;
    }
    public event Func<bool>? CloseConfirmationRequested;
    public event Action? CloseRequested;

    public nint NativeHandle => _frame.NativeHandle;

    public void Show() => _frame.Show();

    public void Close() => _frame.Close();

    public void RestoreAndRaise()
    {
        _frame.Iconize(false);
        _frame.Raise();
    }

    public void SetPlaying(bool isPlaying)
    {
        _playButton.SetBitmap(isPlaying ? _pauseBitmap : _playBitmap);
        _playButton.Label = isPlaying
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
        _menus.SetFullScreenChecked(fullScreen);
    }

    public bool IsFullScreen => _frame.IsFullScreen;

    /// <summary>Dialogs, built lazily and parented to this window.</summary>
    public IDialogService Dialogs => _dialogs ??= new DialogService(_frame, _dispatcher, _catalog, _input.GlobalShortcuts);

    public bool ApplyGlobalShortcuts(ShortcutManager shortcuts) => _input.ApplyGlobalShortcuts(shortcuts);

    public void ApplyShortcuts(ShortcutManager shortcuts)
    {
        _menus.ApplyShortcuts(shortcuts);
        _input.ApplyAccelerators(shortcuts);
    }
    public void SetShuffleChecked(bool isChecked) => _menus.SetShuffleChecked(isChecked);

    public void SetRepeatFileChecked(bool isChecked) => _menus.SetRepeatFileChecked(isChecked);

    public void SetSilenceRemovalChecked(bool isChecked) => _menus.SetSilenceRemovalChecked(isChecked);

    public void SetFullScreenAvailable(bool available) => _menus.SetFullScreenAvailable(available);

    public void SetEqualizerPreset(string? presetId) => _menus.SetEqualizerPreset(presetId);

    public void RebuildEqualizerMenu(IReadOnlyList<PresetEntry> presets, string? selected)
        => _menus.RebuildEqualizerMenu(presets, selected);

    public void RebuildSubtitleMenu(IReadOnlyList<SubtitleMenuEntry> entries, int? activeId)
        => _menus.RebuildSubtitleMenu(entries, activeId);

    public void SetSubtitleRemember(bool on) => _menus.SetSubtitleRemember(on);

    public void RebuildRecentsMenu(
        IReadOnlyList<RecentMenuEntry> files,
        IReadOnlyList<RecentMenuEntry> folders,
        IReadOnlyList<RecentMenuEntry> playlists)
        => _menus.RebuildRecentsMenu(files, folders, playlists);

    public void SetSubtitleSelection(int? activeId) => _menus.SetSubtitleSelection(activeId);

    public void SetSubtitleControlsEnabled(bool enabled) => _menus.SetSubtitleControlsEnabled(enabled);

    public void SetMediaLoaded(bool loaded) => _menus.SetMediaLoaded(loaded);

    public void SetEditState(bool hasLocalFile, bool hasMedia) => _menus.SetEditState(hasLocalFile, hasMedia);

    public void SetBookmarkState(bool enabled) => _menus.SetBookmarkState(enabled);

    public void SetMarkState(bool currentMarked, bool allMarked) => _menus.SetMarkState(currentMarked, allMarked);

    public void SetRecordingState(RecordingState state) => _menus.SetRecordingState(state);

    public void SetVideoOptionsEnabled(bool enabled) => _menus.SetVideoOptionsEnabled(enabled);

    public void SetAudioTrackControlsEnabled(bool enabled) => _menus.SetAudioTrackControlsEnabled(enabled);

    public void SetMarkedActionsEnabled(bool enabled) => _menus.SetMarkedActionsEnabled(enabled);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        // Before the frame goes: a keyboard hook left installed keeps being called into a process that is
        // shutting down.
        _input.Dispose();
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
        // Alt+F4 asks the window to close while Alt is commonly still held. Destroying the foreground
        // window before Windows delivers the rest of that chord can leave the newly focused thread with a
        // stale logical modifier state. Keep this window as the keyboard target until the physical keys are
        // up, then issue a fresh close on the UI thread. No synthetic key events are sent.
        if (!_closing && args.CanVeto && _input.CloseChordIsDown())
        {
            args.Veto();
            if (!_waitingForCloseKeys)
            {
                _waitingForCloseKeys = true;
                _ = CloseAfterKeysAreReleasedAsync();
            }
            return;
        }

        if (!_closing && args.CanVeto && !CanClose())
        {
            args.Veto();
            return;
        }

        _closing = true;
        // Stop system-wide shortcuts before the close handlers save state and stop playback, so no action can
        // arrive while shutdown is in progress.
        _input.StopGlobalShortcuts();
        CloseRequested?.Invoke();
        // Parentless modeless progress windows would otherwise keep wxWidgets' event loop alive after the
        // main frame disappears. They are views only; their jobs remain owned by the queue until host disposal.
        _dialogs?.CloseDownloadProgressWindows();
        args.Skip();
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

    private async Task CloseAfterKeysAreReleasedAsync()
    {
        while (!_disposed && _input.CloseChordIsDown())
            await Task.Delay(10).ConfigureAwait(false);

        if (_disposed)
            return;

        _dispatcher.Post(() =>
        {
            if (_disposed)
                return;
            _waitingForCloseKeys = false;
            _frame.Close();
        });
    }
}
