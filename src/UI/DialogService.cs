using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI;

/// <summary>Builds and shows every dialog the player offers, and nothing else. It is the one place coupled
/// to the concrete dialog classes, so the main window is not.</summary>
///
/// <remarks>
/// Dialogs are parented to the active top-level window, not always the main one: a dialog opened from inside
/// another - the Preferences window's "download components" button is the case in point - must be a child of
/// that window, or closing it hands activation back to a disabled window and the player looks hung. Falls
/// back to the main frame when nothing is active (startup, or when Luna is not foreground).
/// </remarks>
internal sealed class DialogService : IDialogService, IDisposable
{
    private readonly Frame _frame;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly LunaPlayer.Recording.AudioCatalog _catalog;
    private readonly GlobalShortcuts _globalShortcuts;
    private readonly Action<ActionId> _requestAction;
    private readonly HashSet<YouTube.DownloadProgressDialog> _downloadProgressDialogs = [];
    private bool? _toastNotificationsAvailable;

    /// <summary>The single-list YouTube results window, built once and reused: a search, a playlist and the
    /// return from the player all show the same plain list, so the window is kept and its contents refreshed
    /// rather than rebuilt on every open. A channel's tabbed browser has a different structure and is built
    /// per open.</summary>
    private YouTube.ResultsDialog? _youTubeResults;
    private Radio.BrowserDialog? _radioBrowser;
    private Spotify.BrowserDialog? _spotifyBrowser;

    internal DialogService(
        Frame frame, IApplicationDispatcher dispatcher,
        LunaPlayer.Recording.AudioCatalog catalog, GlobalShortcuts globalShortcuts,
        Action<ActionId> requestAction)
    {
        _frame = frame;
        _dispatcher = dispatcher;
        _catalog = catalog;
        _globalShortcuts = globalShortcuts;
        _requestAction = requestAction;
    }

    private Window DialogParent => Wx.GetActiveWindow() ?? _frame;

    public Spotify.DialogOutcome ShowSpotifyPreview(LunaPlayer.Spotify.Catalog catalog,
        LunaPlayer.Spotify.Auth auth, SpotifySettings settings,
        Spotify.DialogOutcome? playback = null)
    {
        if (string.IsNullOrWhiteSpace(settings.ClientId))
        {
            using var prompt = new Spotify.CredentialsPrompt(DialogParent);
            if (prompt.Show())
                Wx.CallAfter(() => _requestAction(ActionId.OpenSpotifyCredentials));
            return new Spotify.DialogOutcome();
        }
        _spotifyBrowser ??= new Spotify.BrowserDialog(_frame, catalog, auth, settings);
        return _spotifyBrowser.Show(playback);
    }

    public LunaPlayer.Radio.StationView? ShowRadioBrowser(LunaPlayer.Radio.Browsing state,
        LunaPlayer.Radio.Catalog catalog, LunaPlayer.Radio.Library library, Action<string> speak,
        IClipboardService clipboard, bool returnToStation)
    {
        // Keep the controls and cached browsing locations while a radio session is playing.
        _radioBrowser ??= new Radio.BrowserDialog(_frame, _dispatcher, state, catalog, library, speak, clipboard);
        return _radioBrowser.Show(returnToStation);
    }

    public FileSelection? ChooseFile(string initialDirectory)
    {
        using var dialog = new FileDialog(DialogParent, message: "", directory: initialDirectory, wildcard: MediaLibrary.DialogWildcard, style: FileDialogStyle.DefaultOpen);
        return dialog.ShowModal() == StandardId.Ok ? new(dialog.Path, dialog.Directory) : null;
    }

    public string? ChooseSubtitleFile()
    {
        using var dialog = new FileDialog(DialogParent,
            // Translators: Title of the window that picks a subtitle file to load, and its file-type filter.
            message: Tr("Load subtitle"),
            // Translators: File-type filter for supported subtitle files, followed by all files.
            wildcard: Tr("Subtitle files (*.srt;*.ass;*.ssa;*.vtt;*.sub)|*.srt;*.ass;*.ssa;*.vtt;*.sub|All files (*.*)|*.*"),
            style: FileDialogStyle.Open | FileDialogStyle.FileMustExist);
        return dialog.ShowModal() == StandardId.Ok ? dialog.Path : null;
    }

    public string? PromptSubtitleUrl()
    {
        // Translators: Prompt asking for a subtitle file's web address.
        var prompt = Tr("Enter the web address of a subtitle file:");
        // Translators: Title of the box asking for a subtitle file's web address.
        var title = Tr("Load subtitle from URL");
        var url = PromptText(prompt, title);
        return string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }
    public string? ChooseFolder(string initialDirectory, string message = "")
    {
        using var dialog = new DirDialog(DialogParent, message: message, defaultPath: initialDirectory, style: DirDialogStyle.DirMustExist);
        return dialog.ShowModal() == StandardId.Ok ? dialog.Path : null;
    }
    public string? PromptText(string message, string caption, string value = "")
    { using var dialog = new TextEntryDialog(DialogParent, message, caption, value); return dialog.ShowModal() == StandardId.Ok ? dialog.Value : null; }
    public bool Confirm(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, DialogParent) == MessageBoxStyle.Yes;
    public bool ConfirmCloseWithDownloads()
    { using var dialog = new YouTube.DownloadExitDialog(DialogParent); return dialog.Show(); }
    public void ShowInfo(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, DialogParent);
    public void ShowMainInfo(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, _frame);
    public void ShowWarning(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, DialogParent);
    public void ShowError(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconError, DialogParent);
    public void ShowMainError(string message, string caption) => Wx.MessageBox(message, caption, MessageBoxStyle.Ok | MessageBoxStyle.IconError, _frame);
    public void ShowNotification(string title, string message)
    {
        // wxWidgets falls back to a notification-area icon for balloon messages. Luna never owns a tray
        // icon, so use the Windows toast implementation only and fall back to an ordinary accessible box.
        _toastNotificationsAvailable ??= NotificationMessage.UseToasts(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                AppInfo.Name, $"{AppInfo.Name}.lnk"),
            AppInfo.AppUserModelId);
        if (!_toastNotificationsAvailable.Value
            || !new NotificationMessage(title, message).Show())
            ShowInfo(message, title);
    }
    public void ShowAbout() { using var dialog = new AboutDialog(DialogParent); dialog.Show(); }
    public bool OfferAppUpdate(AppUpdatePrompt prompt) { using var dialog = new AppUpdateDialog(DialogParent, prompt); return dialog.Show(); }
    public double? ChooseTime(double duration, double elapsed) { using var dialog = new GoToTimeDialog(DialogParent, duration, elapsed); return dialog.Show(); }

    public double? ChooseSelectionTime(string title, string prompt, double duration, double elapsed)
    {
        using var dialog = new SelectionTimeDialog(DialogParent, title, prompt, duration, elapsed);
        return dialog.Show();
    }

    public string? ChooseSelectionDestination(string source, bool video)
    {
        var directory = File.Exists(source) ? Path.GetDirectoryName(source) ?? string.Empty : string.Empty;
        // Translators: Default base name for a selected part when the source has no usable file name.
        var sourceName = File.Exists(source) ? Path.GetFileNameWithoutExtension(source) : Tr("selection");
        // Translators: Default file name for a saved selection. {name} is the source file's base name.
        var name = TrFormat("{name} - selection", sourceName);
        var formats = SelectionFormats(video);
        var extensions = new HashSet<string>(formats.Select(format => format.Extension), StringComparer.OrdinalIgnoreCase);
        var sourceExtension = Path.GetExtension(source);
        var selectedFormat = Array.FindIndex(formats,
            format => format.Extension.Equals(sourceExtension, StringComparison.OrdinalIgnoreCase));
        if (selectedFormat < 0)
            selectedFormat = 0;
        name += formats[selectedFormat].Extension;
        var wildcard = string.Join('|', formats.Select(format => $"{format.Label}|*{format.Extension}"));
        using var dialog = new FileDialog(DialogParent,
            // Translators: Title of the file picker used to save the currently selected part of a file.
            message: Tr("Save selection"), directory: directory, fileName: name,
            wildcard: wildcard, style: FileDialogStyle.DefaultSave);
        dialog.FilterIndex = selectedFormat;
        if (dialog.ShowModal() != StandardId.Ok)
            return null;
        var path = dialog.Path;
        var chosenFormat = Math.Clamp(dialog.FilterIndex, 0, formats.Length - 1);
        var chosenExtension = Path.GetExtension(path);
        // Windows leaves the pre-filled extension in the name when the file-type choice changes. Replace
        // that untouched default so the type combo really does choose the format; a different supported
        // extension typed into the name is an explicit choice and wins.
        if (chosenFormat != selectedFormat
            && chosenExtension.Equals(formats[selectedFormat].Extension, StringComparison.OrdinalIgnoreCase))
            return Path.ChangeExtension(path, formats[chosenFormat].Extension);
        return extensions.Contains(chosenExtension)
            ? path
            : path + formats[chosenFormat].Extension;
    }

    public SelectionSaveOptions? ChooseSelectionBatch(string source, bool video)
    {
        var folder = File.Exists(source)
            ? Path.GetDirectoryName(source) ?? string.Empty
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        // Translators: Default common name for numbered files created by Save Selection Points.
        var name = Tr("selection");
        using var dialog = new SaveSelectionsDialog(
            DialogParent, SelectionFormats(video), folder, name, Path.GetExtension(source));
        return dialog.Show();
    }

    private static SelectionFileFormat[] SelectionFormats(bool video) => video
        ?
        [
            // Translators: MP4 choice in the format list when saving selected parts of a video.
            new(Tr("MP4 video (*.mp4)"), ".mp4"),
            // Translators: Matroska choice in the format list when saving selected parts of a video.
            new(Tr("Matroska video (*.mkv)"), ".mkv"),
            // Translators: WebM choice in the format list when saving selected parts of a video.
            new(Tr("WebM video (*.webm)"), ".webm"),
            // Translators: QuickTime choice in the format list when saving selected parts of a video.
            new(Tr("QuickTime video (*.mov)"), ".mov"),
            // Translators: AVI choice in the format list when saving selected parts of a video.
            new(Tr("AVI video (*.avi)"), ".avi"),
        ]
        :
        [
            // Translators: MP3 choice in the format list when saving selected parts of an audio file.
            new(Tr("MP3 audio (*.mp3)"), ".mp3"),
            // Translators: M4A choice in the format list when saving selected parts of an audio file.
            new(Tr("M4A audio (*.m4a)"), ".m4a"),
            // Translators: FLAC choice in the format list when saving selected parts of an audio file.
            new(Tr("FLAC audio (*.flac)"), ".flac"),
            // Translators: WAV choice in the format list when saving selected parts of an audio file.
            new(Tr("WAV audio (*.wav)"), ".wav"),
            // Translators: Ogg Vorbis choice in the format list when saving selected parts of an audio file.
            new(Tr("Ogg Vorbis audio (*.ogg)"), ".ogg"),
            // Translators: Opus choice in the format list when saving selected parts of an audio file.
            new(Tr("Opus audio (*.opus)"), ".opus"),
        ];
    public SleepTimerDialogResult? ChooseSleepTimer(SleepTimerRequest initial, bool armed, string? statusText) { using var dialog = new SleepTimerDialog(DialogParent, initial, armed, statusText); return dialog.Show(); }
    public int? ChooseAudioDevice(IReadOnlyList<string> descriptions, int selectedIndex) { using var dialog = new AudioDeviceDialog(DialogParent, descriptions, selectedIndex); return dialog.Show(); }
    public int? ChooseAudioTrack(IReadOnlyList<string> labels, int selectedIndex) { using var dialog = new AudioTrackDialog(DialogParent, labels, selectedIndex); return dialog.Show(); }
    public BookmarkManagementRequest? ManageBookmarks(IReadOnlyList<BookmarkListItem> bookmarks) { using var dialog = new BookmarkManagerDialog(DialogParent, bookmarks); return dialog.Show(); }
    public OpenedFilesRequest? ChooseOpenedFile(int count, Func<int, string> nameAt, int selectedIndex) { using var dialog = new OpenedFilesDialog(DialogParent, count, nameAt, selectedIndex); return dialog.Show(); }
    public IProgressView BeginProgress(string title, string message, bool proportional, bool detailed) => new ProgressView(DialogParent, title, message, proportional, detailed);

    public EqualizerEditResult? EditEqualizerPreset(EqualizerEditContext context)
    {
        using var dialog = new Equalizer.EqualizerEditDialog(
            _frame, context.Title, context.Name, context.CanRename, context.BasePresetId, context.Slots,
            context.Library.All, context.Library.Slots, context.Library.DefaultSlots, context.Preview);
        return dialog.Show();
    }

    public bool ManageEqualizerPresets(LunaPlayer.Equalizer.Library library)
    {
        using var dialog = new Equalizer.EqualizerPresetsDialog(_frame, library);
        return dialog.Show();
    }

    public YouTubeLinkKind? ChooseYouTubeLinkKind() { using var dialog = new YouTube.LinkKindDialog(DialogParent); return dialog.Show(); }
    public YouTubeSearchRequest? SearchYouTube(YouTubeSearchPrompt prompt) { using var dialog = new YouTube.SearchDialog(DialogParent, _dispatcher, prompt); return dialog.Show(); }
    public FavoriteRequest? ManageFavorites(IReadOnlyList<FavoriteListItem> favorites, string selectedId) { using var dialog = new YouTube.FavoritesDialog(DialogParent, favorites, selectedId); return dialog.Show(); }
    public FavoriteDraft? EditFavorite(string caption, FavoriteDraft value) { using var dialog = new YouTube.FavoriteEditDialog(DialogParent, caption, value); return dialog.Show(); }
    public IptvSourceRequest? ManageIptvSources(IReadOnlyList<IptvSourceListItem> sources, string selectedId) { using var dialog = new Iptv.IptvSourcesDialog(DialogParent, sources, selectedId); return dialog.Show(); }
    public LunaPlayer.Iptv.IptvSourceDraft? EditIptvSource(string caption, LunaPlayer.Iptv.IptvSourceDraft value) { using var dialog = new Iptv.IptvSourceEditDialog(DialogParent, caption, value); return dialog.Show(); }
    public int? BrowseChannels(ChannelBrowserPrompt prompt) { using var dialog = new Iptv.ChannelBrowserDialog(DialogParent, prompt); return dialog.Show(); }
    public ResultChoice? ShowYouTubeResults(YouTubeResultsPrompt prompt)
    {
        // A channel's tabbed browser has a different control structure, so it is still built per open.
        if (prompt.Tabs is { Count: > 0 })
        {
            using var channel = new YouTube.ResultsDialog(DialogParent, prompt);
            return channel.Show();
        }
        // The plain single-list window is built once and reused: reopening refreshes the list instead of
        // reconstructing the window. Parented to the main frame, which outlives every such open.
        if (_youTubeResults is null || !_youTubeResults.IsSingleList)
            _youTubeResults = new YouTube.ResultsDialog(_frame, prompt);
        else
            _youTubeResults.Refresh(prompt);
        return _youTubeResults.Show();
    }
    public bool OfferYouTubeComponents() { using var dialog = new YouTube.ComponentsDialog(DialogParent); return dialog.Show(); }

    public Downloads.OptionsResult? ShowDownloadOptions(Downloads.Request defaults)
    { using var dialog = new YouTube.DownloadDialog(DialogParent, defaults); return dialog.Show(); }

    public void ShowDownloadQueue(
        Downloads.DownloadQueue queue,
        Func<IReadOnlyList<string>, string?> addLinks,
        Action start)
    { using var dialog = new YouTube.DownloadQueueDialog(DialogParent, _dispatcher, queue, addLinks, start); dialog.Show(); }

    public void ShowDownloadProgress(
        Downloads.DownloadQueue queue,
        Guid itemId,
        Action<Downloads.Outcome?> completed)
    {
        var dialog = new YouTube.DownloadProgressDialog(_dispatcher, queue, itemId);
        dialog.Closed += (closed, outcome) =>
        {
            _downloadProgressDialogs.Remove(closed);
            completed(outcome);
        };
        _downloadProgressDialogs.Add(dialog);
        dialog.Show();
    }

    internal void CloseDownloadProgressWindows()
    {
        foreach (var dialog in _downloadProgressDialogs.ToArray())
            dialog.Dispose();
        _downloadProgressDialogs.Clear();
    }
    public void ShowTextInfo(string title, string text) { using var dialog = new TextInfoDialog(DialogParent, title, text); dialog.Show(); }
    public void ShowVideoDescription(string title, string description) { using var dialog = new YouTube.DescriptionDialog(DialogParent, title, description); dialog.Show(); }
    public void ShowRecording(
        LunaPlayer.Recording.AudioCatalog catalog, LunaPlayer.Recording.RecordingSources sources, LunaPlayer.Recording.RecordingEngine engine)
    { using var dialog = new Recording.RecordingDialog(DialogParent, _dispatcher, catalog, sources, engine); dialog.Show(); }
    public ConversionRequest? ShowMediaConverter(IReadOnlyList<string>? initialFiles = null)
    { using var dialog = new MediaConverterDialog(DialogParent, initialFiles); return dialog.Show(); }
    public ConversionOutcome? RunConversion(
        Func<Action<ConversionProgress>, CancellationToken, ConversionOutcome> work)
    { using var dialog = new ConversionProgressDialog(DialogParent, _dispatcher, work); return dialog.Show(); }
    public void ShowConversionReport(string title, string message, string details)
    { using var dialog = new ConversionReportDialog(DialogParent, title, message, details); dialog.Show(); }
    public Settings? EditPreferences(Settings settings, PrefsOps operations, Action<string> speakHelp,
        bool openSpotifyCredentials = false)
    { var editable = settings.Copy(); using var dialog = new PreferencesDialog(DialogParent, editable, operations, speakHelp, _dispatcher, _catalog, _globalShortcuts, openSpotifyCredentials); return dialog.Show() ? editable : null; }

    /// <summary>Tears down the reused results window. Called before the frame is destroyed, since that window
    /// is a child of it.</summary>
    public void Dispose()
    {
        _spotifyBrowser?.Dispose();
        _spotifyBrowser = null;
        _radioBrowser?.Dispose();
        _radioBrowser = null;
        _youTubeResults?.Dispose();
        _youTubeResults = null;
        CloseDownloadProgressWindows();
    }
}
