using LunaPlayer.UI.Services;
using LunaPlayer.UI.Services.Dialogs;
using LunaPlayer.Playlist;
using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI;

/// <summary>Routes dialog requests to the feature services and owns their lifetimes.</summary>
///
/// <remarks>
/// Dialogs are parented to the active top-level window, not always the main one: a dialog opened from inside
/// another - the Preferences window's "download components" button is the case in point - must be a child of
/// that window, or closing it hands activation back to a disabled window and the player looks hung. Falls
/// back to the main frame when nothing is active (startup, or when Luna is not foreground).
/// </remarks>
internal sealed class DialogService : IDialogService, IDisposable
{
    private readonly PlaylistsDialogs _playlists;
    private readonly SpotifyDialogs _spotify;
    private readonly RadioDialogs _radio;
    private readonly AudioDialogs _audio;
    private readonly PlaybackDialogs _playback;
    private readonly SelectionsDialogs _selections;
    private readonly YouTubeDialogs _youtube;
    private readonly IptvDialogs _iptv;
    private readonly EqualizerDialogs _equalizer;
    private readonly ConversionDialogs _conversion;
    private readonly RecordingDialogs _recording;
    private readonly PreferencesDialogs _preferences;
    private readonly InformationDialogs _information;
    private readonly Frame _frame;
    private bool? _toastNotificationsAvailable;

    internal DialogService(
        Frame frame, IApplicationDispatcher dispatcher,
        LunaPlayer.Recording.AudioCatalog catalog, LunaPlayer.UI.Services.GlobalShortcuts globalShortcuts,
        Action<ActionId> requestAction)
    {
        _frame = frame;
        var context = new DialogContext(frame, dispatcher, catalog, globalShortcuts, requestAction, this);
        _playlists = new PlaylistsDialogs(context);
        _spotify = new SpotifyDialogs(context);
        _radio = new RadioDialogs(context);
        _audio = new AudioDialogs(context);
        _playback = new PlaybackDialogs(context);
        _selections = new SelectionsDialogs(context);
        _youtube = new YouTubeDialogs(context);
        _iptv = new IptvDialogs(context);
        _equalizer = new EqualizerDialogs(context);
        _conversion = new ConversionDialogs(context);
        _recording = new RecordingDialogs(context);
        _preferences = new PreferencesDialogs(context);
        _information = new InformationDialogs(context);

    }

    private Window DialogParent => Wx.GetActiveWindow() ?? _frame;

    public LunaPlayer.UI.Contracts.FileSelection? ChooseFile(string initialDirectory)
    {
        // Translators: Extra Open File filter for Luna playlist documents; keep JSON and the wildcard unchanged.
        var jsonFilter = Tr("JSON playlists (*.json)|*.json");
        using var dialog = new FileDialog(DialogParent, message: "", directory: initialDirectory, wildcard: MediaLibrary.DialogWildcard + "|" + jsonFilter, style: FileDialogStyle.DefaultOpen);
        return dialog.ShowModal() == StandardId.Ok ? new(dialog.Path, dialog.Directory) : null;
    }
    public string? ChooseFolder(string initialDirectory, string message = "")
    {
        using var dialog = new DirDialog(DialogParent, message: message, defaultPath: initialDirectory, style: DirDialogStyle.DirMustExist);
        return dialog.ShowModal() == StandardId.Ok ? dialog.Path : null;
    }
    public string? PromptText(string message, string caption, string value = "")
    { using var dialog = new TextEntryDialog(DialogParent, message, caption, value); return dialog.ShowModal() == StandardId.Ok ? dialog.Value : null; }
    public bool Confirm(string message) => Messages.Show(message, MessageBoxStyle.YesNo | MessageBoxStyle.IconWarning, DialogParent) == MessageBoxStyle.Yes;
    public void ShowInfo(string message) => Messages.Show(message, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, DialogParent);
    public void ShowMainInfo(string message) => Messages.Show(message, MessageBoxStyle.Ok | MessageBoxStyle.IconInformation, _frame);
    public void ShowWarning(string message) => Messages.Show(message, MessageBoxStyle.Ok | MessageBoxStyle.IconWarning, DialogParent);
    public void ShowError(string message) => Messages.Show(message, MessageBoxStyle.Ok | MessageBoxStyle.IconError, DialogParent);
    public void ShowMainError(string message) => Messages.Show(message, MessageBoxStyle.Ok | MessageBoxStyle.IconError, _frame);
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
            ShowInfo(message);
    }
    public LunaPlayer.UI.Contracts.IProgressView BeginProgress(string title, string message, bool proportional, bool detailed) => new LunaPlayer.UI.Controls.ProgressView(DialogParent, title, message, proportional, detailed);

    /// <summary>Tears down the reused results window. Called before the frame is destroyed, since that window
    /// is a child of it.</summary>
    public void Dispose() { _spotify.Dispose(); _radio.Dispose(); _youtube.Dispose(); }
    public Document? EditPlaylist(Document document, Action<string> speak, bool metadataOnly = false) => _playlists.EditPlaylist(document, speak, metadataOnly);
    public LunaPlayer.UI.Playlists.PlaylistRequest? ManagePlaylists(IReadOnlyList<Document> documents) => _playlists.ManagePlaylists(documents);
    public string[] ChoosePlaylistFiles() => _playlists.ChoosePlaylistFiles();
    public string? ChoosePlaylistDestination() => _playlists.ChoosePlaylistDestination();
    public Spotify.DialogOutcome ShowSpotifyBrowser(LunaPlayer.Spotify.Catalog catalog,
        LunaPlayer.Spotify.Auth auth, SpotifySettings settings, Action<string> speak,
        Spotify.DialogOutcome? playback = null) => _spotify.ShowSpotifyBrowser(catalog, auth, settings, speak, playback);
    public LunaPlayer.Radio.StationView? ShowRadioBrowser(LunaPlayer.Radio.Browsing state,
        LunaPlayer.Radio.Catalog catalog, LunaPlayer.Radio.Library library, Action<string> speak,
        IClipboardService clipboard, bool returnToStation) => _radio.ShowRadioBrowser(state, catalog, library, speak, clipboard, returnToStation);
    public int? ChooseAudioDevice(IReadOnlyList<string> descriptions, int selectedIndex) => _audio.ChooseAudioDevice(descriptions, selectedIndex);
    public int? ChooseAudioTrack(IReadOnlyList<string> labels, int selectedIndex) => _audio.ChooseAudioTrack(labels, selectedIndex);
    public string? ChooseSubtitleFile() => _playback.ChooseSubtitleFile();
    public string? PromptSubtitleUrl() => _playback.PromptSubtitleUrl();
    public double? ChooseTime(double duration, double elapsed) => _playback.ChooseTime(duration, elapsed);
    public LunaPlayer.UI.Playback.SleepTimerDialogResult? ChooseSleepTimer(LunaPlayer.UI.Playback.SleepTimerRequest initial, bool armed, string? statusText) => _playback.ChooseSleepTimer(initial, armed, statusText);
    public double? ChooseSelectionTime(string title, string prompt, double duration, double elapsed) => _selections.ChooseSelectionTime(title, prompt, duration, elapsed);
    public string? ChooseSelectionDestination(string source, bool video) => _selections.ChooseSelectionDestination(source, video);
    public LunaPlayer.UI.Selections.SelectionSaveOptions? ChooseSelectionBatch(string source, bool video) => _selections.ChooseSelectionBatch(source, video);
    public bool ConfirmCloseWithDownloads() => _youtube.ConfirmCloseWithDownloads();
    public LunaPlayer.UI.YouTube.YouTubeLinkKind? ChooseYouTubeLinkKind() => _youtube.ChooseYouTubeLinkKind();
    public LunaPlayer.UI.YouTube.YouTubeSearchRequest? SearchYouTube(LunaPlayer.UI.YouTube.YouTubeSearchPrompt prompt) => _youtube.SearchYouTube(prompt);
    public LunaPlayer.UI.YouTube.FavoriteRequest? ManageFavorites(IReadOnlyList<LunaPlayer.UI.YouTube.FavoriteListItem> favorites, string selectedId) => _youtube.ManageFavorites(favorites, selectedId);
    public LunaPlayer.UI.YouTube.FavoriteDraft? EditFavorite(string caption, LunaPlayer.UI.YouTube.FavoriteDraft value) => _youtube.EditFavorite(caption, value);
    public LunaPlayer.UI.YouTube.ResultChoice? ShowYouTubeResults(LunaPlayer.UI.YouTube.YouTubeResultsPrompt prompt) => _youtube.ShowYouTubeResults(prompt);
    public bool OfferYouTubeComponents() => _youtube.OfferYouTubeComponents();
    public Downloads.OptionsResult? ShowDownloadOptions(Downloads.Request defaults) => _youtube.ShowDownloadOptions(defaults);
    public void ShowDownloadQueue(
        Downloads.DownloadQueue queue,
        Func<IReadOnlyList<string>, string?> addLinks,
        Action start) => _youtube.ShowDownloadQueue(queue, addLinks, start);
    public void ShowDownloadProgress(
        Downloads.DownloadQueue queue,
        Guid itemId,
        Action<Downloads.Outcome?> completed) => _youtube.ShowDownloadProgress(queue, itemId, completed);
    internal void CloseDownloadProgressWindows() => _youtube.CloseDownloadProgressWindows();
    public void ShowVideoDescription(string title, string description) => _youtube.ShowVideoDescription(title, description);
    public LunaPlayer.UI.Iptv.IptvSourceRequest? ManageIptvSources(IReadOnlyList<LunaPlayer.UI.Iptv.IptvSourceListItem> sources, string selectedId) => _iptv.ManageIptvSources(sources, selectedId);
    public LunaPlayer.Iptv.IptvSourceDraft? EditIptvSource(string caption, LunaPlayer.Iptv.IptvSourceDraft value) => _iptv.EditIptvSource(caption, value);
    public int? BrowseChannels(LunaPlayer.UI.Iptv.ChannelBrowserPrompt prompt) => _iptv.BrowseChannels(prompt);
    public LunaPlayer.UI.Equalizer.EqualizerEditResult? EditEqualizerPreset(LunaPlayer.UI.Equalizer.EqualizerEditContext context) => _equalizer.EditEqualizerPreset(context);
    public bool ManageEqualizerPresets(LunaPlayer.Equalizer.Library library) => _equalizer.ManageEqualizerPresets(library);
    public ConversionRequest? ShowMediaConverter(IReadOnlyList<string>? initialFiles = null) => _conversion.ShowMediaConverter(initialFiles);
    public ConversionOutcome? RunConversion(
        Func<Action<ConversionProgress>, CancellationToken, ConversionOutcome> work) => _conversion.RunConversion(work);
    public void ShowConversionReport(string title, string message, string details) => _conversion.ShowConversionReport(title, message, details);
    public void ShowRecording(
        LunaPlayer.Recording.AudioCatalog catalog, LunaPlayer.Recording.RecordingSources sources, LunaPlayer.Recording.RecordingEngine engine) => _recording.ShowRecording(catalog, sources, engine);
    public Settings? EditPreferences(Settings settings, LunaPlayer.UI.Preferences.PrefsOps operations, Action<string> speakHelp,
        bool openSpotifyCredentials = false) => _preferences.EditPreferences(settings, operations, speakHelp, openSpotifyCredentials);
    public void ShowAbout() => _information.ShowAbout();
    public bool OfferAppUpdate(LunaPlayer.UI.Updates.AppUpdatePrompt prompt) => _information.OfferAppUpdate(prompt);
    public LunaPlayer.UI.Bookmarks.BookmarkManagementRequest? ManageBookmarks(IReadOnlyList<LunaPlayer.UI.Bookmarks.BookmarkListItem> bookmarks) => _information.ManageBookmarks(bookmarks);
    public LunaPlayer.UI.Playlists.OpenedFilesRequest? ChooseOpenedFile(int count, Func<int, string> nameAt, int selectedIndex) => _information.ChooseOpenedFile(count, nameAt, selectedIndex);
    public void ShowTextInfo(string title, string text) => _information.ShowTextInfo(title, text);
}
