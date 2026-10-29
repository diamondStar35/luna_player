using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Commands.Playback;

/// <summary>Chapter commands and their dynamic menu. Probing belongs here, alongside the dispatcher:
/// the player and its playlist are only changed on the UI thread.</summary>
internal sealed class Chapters : IDisposable
{
    private readonly IMainView _view;
    private readonly Player _player;
    private readonly Settings _settings;
    private readonly SettingsStore _store;
    private readonly ISpeechOutput _speech;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly Probe _probe = new();
    private CancellationTokenSource? _probing;
    private string[] _probePaths = [];
    private string? _probeIdentity;
    private bool _disposed;

    internal Chapters(ActionRouter router, IMainView view, Player player, Settings settings,
        SettingsStore store, ISpeechOutput speech, IApplicationDispatcher dispatcher)
    {
        _view = view;
        _player = player;
        _settings = settings;
        _store = store;
        _speech = speech;
        _dispatcher = dispatcher;
        router.Register(ActionId.PreviousChapter, () => Move(-1));
        router.Register(ActionId.NextChapter, () => Move(1));
        router.Register(ActionId.AnnounceChapter, Announce);
        router.Register(ActionId.ToggleChapterStop, ToggleStop);
        router.Register(ActionId.ToggleAudiobookMode, ToggleMode);
        _view.ChapterRequested += Choose;
        _player.ChaptersChanged += OnChaptersChanged;
        _player.CurrentChanged += OnCurrentChanged;
        _player.StateChanged += Sync;
        Sync();
    }

    private void Choose(int index)
    {
        var chapters = _player.CurrentChapters;
        if (index >= 0 && index < chapters.Count && _player.GoToChapter(index))
            _speech.SpeakText(Player.ChapterTitle(chapters[index]));
    }

    private void Move(int direction)
    {
        var index = (_player.CurrentChapter?.Index ?? -1) + direction;
        if (index >= 0 && index < _player.CurrentChapters.Count) Choose(index);
        else if (_player.CurrentChapters.Count == 0) NoChapters();
        else
            _speech.SpeakText(direction < 0
                // Translators: Spoken when previous chapter is requested in the source file's first chapter.
                ? Tr("No previous chapter.")
                // Translators: Spoken when next chapter is requested in the source file's last chapter.
                : Tr("No next chapter."));
    }

    private void Announce()
    {
        if (_player.CurrentChapter is Chapter chapter)
            _speech.SpeakText(Player.ChapterTitle(chapter));
        else NoChapters();
    }

    private void NoChapters()
        => _speech.Speak(
            // Translators: Spoken when a chapter command is used but the current file has no usable chapters.
            Tr("No chapters in the current file."),
            // Translators: Brief announcement when no chapters are available.
            Tr("No chapters."));

    private void ToggleStop()
    {
        if (_player.CurrentChapter is null)
        {
            NoChapters();
            return;
        }
        var enabled = _player.ToggleChapterStop();
        _speech.SpeakText(enabled
            // Translators: Spoken when the player is armed to pause once at the current chapter's end.
            ? Tr("Playback will pause at the end of the current chapter.")
            // Translators: Spoken when the one-time chapter-end pause is cancelled.
            : Tr("Chapter-end stop cancelled."));
    }

    private void ToggleMode()
    {
        _settings.Audio.AudiobookMode = !_player.AudiobookMode;
        _player.SetAudiobookMode(_settings.Audio.AudiobookMode);
        if (!_store.SaveExplicit(_settings))
            _view.Dialogs.ShowError(_store.LastError);
        _speech.SpeakText(_player.AudiobookMode
            // Translators: Spoken after enabling playback of chapters as individual playlist items.
            ? Tr("Audiobook mode on.")
            // Translators: Spoken after returning to playback of complete files.
            : Tr("Audiobook mode off."));
        StartProbes();
    }

    private void OnChaptersChanged() => _dispatcher.Post(() =>
    {
        if (_disposed) return;
        _player.RefreshChapters();
        Sync();
    });

    private void OnCurrentChanged()
    {
        Sync();
        StartProbes();
    }

    private void Sync()
    {
        if (_disposed) return;
        var entries = _player.CurrentChapters.Select(chapter =>
        {
            // Source titles are data, not menu accelerators. Escape ampersands and control characters.
            var title = Player.ChapterTitle(chapter).Replace("&", "&&").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
            return new LunaPlayer.UI.Menus.ChapterMenuEntry(chapter.Index,
                // Translators: A chapter in Player > Chapters. {number} is its source order, {title} its name,
                // and {duration} the full chapter length, not its start time or remaining playback time.
                TrFormat("{number}. {title}: {duration}", chapter.Index + 1, title, PlaybackTimeFormatter.Format(chapter.Duration)));
        }).ToArray();
        _view.SetChapterState(entries, _player.CurrentChapter?.Index, _player.StopAtChapterEnd, _player.AudiobookMode);
    }

    private void StartProbes()
    {
        // Live radio and Spotify sessions are not finite chapter playlists. Ordinary seekable links can
        // still expose chapters through the active engine without probing every network entry in advance.
        var paths = _player.AudiobookMode && !_player.InRadioSession && !_player.InSpotifySession
            ? _player.SourceFiles.Where(File.Exists).ToArray() : [];
        var identity = _player.SourceIdentity;
        if (_probeIdentity == identity && _probePaths.SequenceEqual(paths, StringComparer.OrdinalIgnoreCase)) return;
        _probePaths = paths;
        _probeIdentity = identity;
        _probing?.Cancel();
        _probing?.Dispose();
        var cancellation = new CancellationTokenSource();
        _probing = cancellation;
        var token = cancellation.Token;
        _ = Task.Run(() =>
        {
            foreach (var path in paths)
            {
                if (token.IsCancellationRequested) return;
                var result = _probe.Read(path, token);
                if (token.IsCancellationRequested || _disposed) return;
                if (result?.Chapters is not { } chapters) continue;
                _dispatcher.Post(() =>
                {
                    if (!_disposed && !token.IsCancellationRequested)
                        _player.SetChapters(path, chapters);
                });
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _probing?.Cancel();
        _probing?.Dispose();
        _view.ChapterRequested -= Choose;
        _player.ChaptersChanged -= OnChaptersChanged;
        _player.CurrentChanged -= OnCurrentChanged;
        _player.StateChanged -= Sync;
        // The probe may be finishing a demuxer read under its lock. Never block the window's close path.
        _ = Task.Run(_probe.Dispose);
    }
}
