using System.Diagnostics;
using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Recording;
using LunaPlayer.UI;

namespace LunaPlayer.Application.ActionHandlers;

/// <summary>The commands that record sound.</summary>
///
/// <remarks>
/// Recording can be run two ways and both have to work. From the window, where sources have been set up
/// and the settings on it are the ones used; and from a shortcut with nothing set up at all, which is how
/// somebody who never opens the window records - that case falls back to the saved settings and the
/// default microphone, which is the whole of what the Python player could do.
///
/// Starting and stopping both talk to devices and both wait for a file to be closed, so both are done on
/// a worker and answered back on the UI thread.
/// </remarks>
internal sealed class RecordingActions
{
    private readonly IMainView _view;
    private readonly PlayerSettings _settings;
    private readonly ISpeechOutput _speech;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly AudioCatalog _catalog;
    private readonly RecordingSources _sources;
    private readonly RecordingEngine _engine;

    internal RecordingActions(
        ActionRouter router,
        IMainView view,
        PlayerSettings settings,
        ISpeechOutput speech,
        IApplicationDispatcher dispatcher,
        AudioCatalog catalog,
        RecordingSources sources,
        RecordingEngine engine)
    {
        _view = view;
        _settings = settings;
        _speech = speech;
        _dispatcher = dispatcher;
        _catalog = catalog;
        _sources = sources;
        _engine = engine;
        router.Register(ActionId.OpenRecordingInterface, Open);
        router.Register(ActionId.StartRecording, Start);
        router.Register(ActionId.PauseRecording, TogglePause);
        router.Register(ActionId.StopRecording, Stop);
        router.Register(ActionId.OpenRecordingsFolder, OpenFolder);
        // The menu follows the recorder rather than being set by whoever happened to start it, so a
        // recording begun from a shortcut greys the same items as one begun from the window. Posted
        // because the engine raises this from the thread that stopped the capture.
        _engine.StateChanged += () => _dispatcher.Post(() => _view.SetRecordingState(_engine.State));
        _view.SetRecordingState(_engine.State);
    }

    private void Open() => _view.ShowRecording(_catalog, _sources, _engine);

    private void Start()
    {
        if (_engine.State is not RecordingState.Idle)
        {
            _speech.Speak(
                // Translators: Spoken when the user asks to start recording and a recording is already running.
                Tr("A recording is already running."),
                // Translators: The short wording spoken when a recording is already running.
                Tr("Already recording."));
            return;
        }
        var options = _sources.Count > 0 ? _sources.Options : Defaults();
        // With nothing set up, the default microphone stands in - which is what somebody pressing a
        // recording shortcut without ever having opened the window means by it.
        var sources = _sources.Count > 0
            ? _sources.All.Select(source => source.Copy()).ToList()
            : [DefaultMicrophone()];
        // Nothing is spoken. The engine plays a rising tone before it opens anything, which is the
        // confirmation - immediate, the same in every language, and it does not talk over a screen reader
        // that is already saying something. The Python player says nothing here either.
        _ = Task.Run(() =>
        {
            try
            {
                var outcome = _engine.Start(options, sources, out var failures);
                _dispatcher.Post(() => Started(outcome, failures));
            }
            catch (Exception failure)
            {
                // Reported, not dropped. A faulted task nobody awaits is collected in silence, which is
                // exactly how a recording that never started came to say nothing at all about it.
                _dispatcher.Post(() => Started(
                    RecordingOutcome.Failed(RecordingFailure.Failed, failure.Message), []));
            }
        });
    }

    private void Started(RecordingOutcome outcome, IReadOnlyList<string> failures)
    {
        if (!outcome.Success)
        {
            _view.ShowError(RecordingSources.Describe(outcome.Failure, outcome.Detail), Title);
            return;
        }
        if (failures.Count > 0)
        {
            _view.ShowWarning(
                // Translators: Shown when recording started but some sources would not open. {names} is a
                // list of what the user called them, separated by commas.
                TrFormat("Recording started, but these sources could not be opened: {names}",
                    string.Join(", ", failures)),
                Title);
            return;
        }
    }

    private void TogglePause()
    {
        if (_engine.State is RecordingState.Paused)
        {
            if (_engine.Resume())
                _speech.Speak(
                    // Translators: Spoken when a paused recording is started again.
                    Tr("Recording resumed."),
                    // Translators: The short wording spoken when a paused recording is started again.
                    Tr("Resumed."));
            return;
        }
        if (_engine.Pause())
        {
            _speech.Speak(
                // Translators: Spoken when a recording is held where it is.
                Tr("Recording paused."),
                // Translators: The short wording spoken when a recording is held where it is.
                Tr("Paused."));
            return;
        }
        NothingRunning();
    }

    private void Stop()
    {
        if (_engine.State is RecordingState.Idle)
        {
            NothingRunning();
            return;
        }
        _ = Task.Run(() =>
        {
            try
            {
                var outcome = _engine.Stop();
                _dispatcher.Post(() => Stopped(outcome));
            }
            catch (Exception failure)
            {
                _dispatcher.Post(() => Stopped(
                    RecordingOutcome.Failed(RecordingFailure.Failed, failure.Message)));
            }
        });
    }

    private void Stopped(RecordingOutcome outcome)
    {
        // The falling tone the engine plays is the confirmation, so nothing is spoken on success either.
        if (!outcome.Success)
            _view.ShowError(RecordingSources.Describe(outcome.Failure, outcome.Detail), Title);
    }

    private void OpenFolder()
    {
        var folder = _sources.Count > 0 ? _sources.Options.Folder : _settings.Recording.Folder;
        try
        {
            Directory.CreateDirectory(folder);
            using var opened = Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException or System.ComponentModel.Win32Exception)
        {
            _view.ShowError(
                // Translators: Shown when the folder recordings are saved into could not be opened.
                // {reason} is what went wrong, in the language of the system rather than the player.
                TrFormat("{message}\n{reason}", Tr("Could not open the recordings folder."), failure.Message),
                Title);
        }
    }

    private void NothingRunning() => _speech.Speak(
        // Translators: Spoken when the user asks to pause or stop recording and nothing is being recorded.
        Tr("Nothing is being recorded."),
        // Translators: The short wording spoken when nothing is being recorded.
        Tr("Not recording."));

    private RecordingOptions Defaults() => new(
        _settings.Recording.Format,
        _settings.Recording.SampleRate,
        _settings.Recording.Channels,
        _settings.Recording.Bitrate,
        _settings.Recording.Folder);

    private static RecordingSource DefaultMicrophone() => new()
    {
        Kind = RecordingSourceKind.InputDevice,
        // Translators: What the source is called when recording was started from a shortcut with nothing
        // set up, so the default microphone is used.
        Name = Tr("Default input device"),
    };

    private static string Title =>
        // Translators: Title of the messages the player shows about recording.
        Tr("Recording");
}
