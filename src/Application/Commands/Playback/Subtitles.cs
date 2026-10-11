using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Commands.Playback;

/// <summary>Reads embedded subtitles aloud. The Subtitles menu lists the file's tracks with Off at the top;
/// choosing one has mpv decode it, and each line it puts on screen is spoken as it comes. A quick key turns
/// reading on and off without opening the menu.</summary>
/// <remarks>
/// Like the equalizer, the menu's track choices do not travel through <see cref="ActionRouter"/>: which
/// subtitle tracks exist changes with the file, so there is no fixed action for one. Only the on/off toggle
/// is a bound action. mpv's <c>sid</c> is the single source of truth for what is being read; the menu's
/// ticked item and the toggle both read and drive it.
/// </remarks>
internal sealed class Subtitles : IDisposable
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly ISpeechOutput _speech;
    private readonly PlayerSettings _settings;
    private readonly IApplicationDispatcher _dispatcher;
    // The track last read, so the toggle can bring it back after an off. Reset to the first track when the
    // remembered id is not among the current file's tracks, since ids do not carry across files.
    private int? _lastTrackId;
    // Session-only, off by default, never saved: while on, the chosen subtitle's language is kept and
    // re-selected on each new file in the playlist, so it carries across episodes. Language, not id, because
    // track ids do not survive a file change.
    private bool _rememberOn;
    private string? _rememberedLanguage;

    internal Subtitles(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        ISpeechOutput speech,
        PlayerSettings settings,
        IApplicationDispatcher dispatcher)
    {
        _view = view;
        _player = player;
        _speech = speech;
        _settings = settings;
        _dispatcher = dispatcher;
        router.Register(ActionId.ToggleSubtitles, Toggle);
        router.Register(ActionId.LoadSubtitleFile, LoadFromFile);
        router.Register(ActionId.LoadSubtitleUrl, LoadFromUrl);
        router.Register(ActionId.RememberSubtitle, ToggleRemember);
        _view.SubtitleTrackRequested += Choose;
        // Both fire on mpv's event thread, so each hop back to the UI thread goes through the dispatcher.
        _player.SubtitleTracksChanged += OnTracksChanged;
        _player.SubtitleTextChanged += OnSubtitleText;
    }

    // The user picked a row in the Subtitles menu: a track id, or null for Off.
    private void Choose(int? id)
    {
        if (id is null)
        {
            TurnOff(announce: true);
            return;
        }
        var tracks = _player.GetSubtitleTracks();
        var index = tracks.ToList().FindIndex(track => track.Id == id.Value);
        if (index >= 0)
            Enable(tracks[index], index + 1);
    }

    // The quick on/off key. With no subtitles it says so; otherwise it flips between the active track and Off,
    // bringing back the last track that was read when turning back on.
    private void Toggle()
    {
        var tracks = _player.GetSubtitleTracks();
        if (tracks.Count == 0)
        {
            _speech.Speak(
                // Translators: Spoken when the subtitle key is pressed but the file carries no subtitles.
                Tr("No subtitles in this file."),
                // Translators: The short wording spoken when the file has no subtitles.
                Tr("No subtitles."));
            return;
        }
        if (tracks.Any(track => track.Selected))
        {
            TurnOff(announce: true);
            return;
        }
        var index = _lastTrackId is int last ? tracks.ToList().FindIndex(track => track.Id == last) : -1;
        var position = index < 0 ? 0 : index;
        Enable(tracks[position], position + 1);
    }

    private void Enable(SubtitleTrack track, int number)
    {
        if (!_player.SetSubtitleTrack(track.Id))
        {
            _speech.Speak(
                // Translators: Spoken when the player could not start reading the subtitle track the user picked.
                Tr("Could not turn on subtitles."),
                // Translators: The short wording spoken when a subtitle track could not be turned on.
                Tr("Subtitles failed."));
            return;
        }
        _lastTrackId = track.Id;
        if (_rememberOn)
            _rememberedLanguage = track.Language;
        _view.SetSubtitleSelection(track.Id);
        _speech.Speak(
            // Translators: Spoken when subtitle reading starts. {name} is the subtitle's name, such as "English".
            TrFormat("Reading subtitles: {name}", Describe(track, number)),
            Describe(track, number));
    }

    private void TurnOff(bool announce)
    {
        _player.DisableSubtitles();
        // While remembering, turning subtitles off is itself the choice to remember - so later files stay off
        // too, rather than the previous language coming back.
        if (_rememberOn)
            _rememberedLanguage = null;
        _view.SetSubtitleSelection(null);
        if (announce)
            _speech.Speak(
                // Translators: Spoken when subtitle reading is turned off.
                Tr("Subtitles off."),
                // Translators: The short wording spoken when subtitle reading is turned off.
                Tr("Subtitles off"));
    }

    // Loads a subtitle from a file on this computer. mpv selects it, so the track-list change below rebuilds
    // the menu and reading begins on its own.
    private void LoadFromFile()
    {
        if (NoFileOpen())
            return;
        if (_view.ChooseSubtitleFile() is string path)
            Add(path, System.IO.Path.GetFileNameWithoutExtension(path));
    }

    // Loads a subtitle from a web address. mpv opens http(s) through ffmpeg, the same as a local path.
    private void LoadFromUrl()
    {
        if (NoFileOpen())
            return;
        if (_view.PromptSubtitleUrl() is string url)
            Add(url, title: null);
    }

    private void Add(string source, string? title)
    {
        if (_player.AddSubtitle(source, title, language: null))
            _speech.Speak(
                // Translators: Spoken when an external subtitle has been loaded and starts being read.
                Tr("Subtitle loaded."),
                // Translators: The short wording spoken when an external subtitle has been loaded.
                Tr("Loaded."));
        else
            _speech.Speak(
                // Translators: Spoken when a subtitle file or web address could not be loaded.
                Tr("Could not load that subtitle."),
                // Translators: The short wording spoken when a subtitle could not be loaded.
                Tr("Subtitle failed."));
    }

    // Turns session-remembering on or off. On: pin whatever subtitle is current now; off: forget it. Nothing
    // is written to disk - a restart begins with it off again.
    private void ToggleRemember()
    {
        _rememberOn = !_rememberOn;
        if (_rememberOn)
        {
            var current = _player.GetSubtitleTracks().FirstOrDefault(track => track.Selected);
            _rememberedLanguage = current.Language;
            _speech.Speak(
                // Translators: Spoken when the player starts keeping the chosen subtitle across the playlist for this session.
                Tr("Remembering this subtitle for the session."),
                // Translators: The short wording spoken when subtitle remembering is turned on.
                Tr("Remembering subtitle."));
        }
        else
        {
            _rememberedLanguage = null;
            _speech.Speak(
                // Translators: Spoken when the player stops keeping the chosen subtitle across the playlist.
                Tr("No longer remembering a subtitle."),
                // Translators: The short wording spoken when subtitle remembering is turned off.
                Tr("Not remembering."));
        }
        _view.SetSubtitleRemember(_rememberOn);
    }

    // True, with a spoken note, when nothing is open to attach a subtitle to - the Ctrl+J/Ctrl+Shift+J
    // accelerators fire even while the Subtitles menu is disabled.
    private bool NoFileOpen()
    {
        if (_player.CurrentPath is not null)
            return false;
        _speech.Speak(
            // Translators: Spoken when the user tries to load a subtitle but no file is open.
            Tr("Open a file before loading a subtitle."),
            // Translators: The short wording spoken when there is no open file to load a subtitle for.
            Tr("No file open."));
        return true;
    }

    private void OnTracksChanged() => _dispatcher.Post(RebuildMenu);

    private void RebuildMenu()
    {
        var tracks = _player.GetSubtitleTracks();
        // Carrying a remembered subtitle into a freshly loaded file: once per file, only while nothing is yet
        // selected, pick the track whose language matches. Selecting it makes a later pass see it chosen, so
        // this does not fire again.
        if (_rememberOn && _rememberedLanguage is { Length: > 0 } remembered
            && !tracks.Any(track => track.Selected))
        {
            var match = tracks.ToList().FindIndex(track =>
                string.Equals(track.Language, remembered, StringComparison.OrdinalIgnoreCase));
            if (match >= 0 && _player.SetSubtitleTrack(tracks[match].Id))
                tracks = _player.GetSubtitleTracks();
        }
        var entries = tracks.Select((track, i) => new SubtitleMenuEntry(track.Id, Describe(track, i + 1))).ToArray();
        int? activeId = null;
        foreach (var track in tracks)
            if (track.Selected) { activeId = track.Id; break; }
        // Keep the "last read" track in step with whatever is actually selected now - a menu pick, a loaded
        // file, or a remembered language - so the on/off toggle brings back the right one.
        if (activeId is int selected)
            _lastTrackId = selected;
        _view.RebuildSubtitleMenu(entries, activeId);
    }

    // Each subtitle line arrives on mpv's thread; speak it on the UI thread. interrupt follows the setting:
    // on, a new line cuts off the last so speech keeps pace; off, each line is read in full.
    private void OnSubtitleText(string text)
        => _dispatcher.Post(() => _speech.SpeakText(text, _settings.General.SubtitleInterrupt));

    // The label a track shows: its own title, else its language name, and only a numbered stand-in when it
    // has neither - so tracks are not all prefixed with "Subtitle", and an unnamed one still stays distinct.
    private static string Describe(SubtitleTrack track, int number)
    {
        var language = Localization.LanguageName(track.Language);
        if (!string.IsNullOrWhiteSpace(track.Title))
            return language.Length > 0 && !track.Title.Contains(language, StringComparison.CurrentCultureIgnoreCase)
                ? $"{track.Title}, {language}"
                : track.Title;
        if (language.Length > 0)
            return language;
        // Translators: Stand-in label for a subtitle track with no title or language. {number} is its place in the list.
        return TrFormat("Subtitle {number}", number);
    }

    public void Dispose()
    {
        _view.SubtitleTrackRequested -= Choose;
        _player.SubtitleTracksChanged -= OnTracksChanged;
        _player.SubtitleTextChanged -= OnSubtitleText;
    }
}
