using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Iptv;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Commands.Iptv;

/// <summary>The IPTV source manager, and what opening a source and picking a channel does.</summary>
///
/// <remarks>
/// Two doors lead in. The manager (Ctrl+Shift+I) is where sources are added, edited and removed, modelled on
/// <see cref="Favorites"/>'s reopen-on-same-row loop. Browsing opens the channel browser on a source;
/// because loading a source reaches the network, it runs behind a progress window and the browser opens only
/// once the channels have arrived - so opening a source ends the manager rather than looping back into it, the
/// browser being what comes next.
///
/// A chosen channel plays through <see cref="MediaPlayer.OpenStream"/>, the generic-stream path mpv opens a
/// transport stream or an HLS manifest through directly. The YouTube session mechanism is deliberately not
/// used: IPTV channels are picked one at a time and are live, not a sequence to auto-advance.
/// </remarks>
internal sealed class Iptv
{
    private readonly IMainView _view;
    private readonly MediaPlayer _player;
    private readonly ISpeechOutput _speech;
    private readonly IApplicationDispatcher _dispatcher;
    private readonly IptvSourceStore _store;

    /// <summary>The source browsed most recently, so "Browse channels" can reopen it without going through
    /// the manager. Cleared when it turns out no longer to exist.</summary>
    private string _lastSourceId = string.Empty;

    internal Iptv(
        ActionRouter router,
        IMainView view,
        MediaPlayer player,
        ISpeechOutput speech,
        IApplicationDispatcher dispatcher,
        IptvSourceStore store)
    {
        _view = view;
        _player = player;
        _speech = speech;
        _dispatcher = dispatcher;
        _store = store;
        router.Register(ActionId.OpenIptvSources, Manage);
        router.Register(ActionId.OpenIptvChannels, Browse);
    }

    /// <remarks>
    /// The window reopens after each change, carrying the id it last touched so the list comes back on the
    /// same row. Opening a source is the one action that closes it for good: the browser opens after the
    /// channels have loaded, and that is where the user goes next.
    /// </remarks>
    private void Manage()
    {
        var selectedId = string.Empty;
        while (true)
        {
            var sources = List();
            if (ReportStoreFailure())
                return;
            var request = _view.ManageIptvSources(sources, selectedId);
            if (request is not IptvSourceRequest chosen)
                return;
            selectedId = chosen.Id;
            switch (chosen.Action)
            {
                case IptvSourceAction.Open:
                    OpenSource(chosen.Id);
                    return;
                case IptvSourceAction.Add:
                    selectedId = Add() ?? selectedId;
                    break;
                case IptvSourceAction.Edit:
                    Edit(chosen.Id);
                    break;
                case IptvSourceAction.Remove:
                    if (Remove(chosen.Id))
                        selectedId = string.Empty;
                    break;
            }
        }
    }

    /// <summary>Opens the channel browser on the source browsed last, falling back to the manager when there
    /// is no such source - which is also the case the first time, before anything has been opened.</summary>
    private void Browse()
    {
        if (_lastSourceId.Length > 0 && _store.Get(_lastSourceId) is not null)
        {
            OpenSource(_lastSourceId);
            return;
        }
        Manage();
    }

    private IReadOnlyList<IptvSourceListItem> List()
        => [.. _store.ListAll().Select(source => new IptvSourceListItem(
            source.Id, source.Name, IptvSourceStore.Describe(source.Kind), source.Url))];

    /// <summary>Loads a source's channels behind a progress window, then opens the browser on them. Returns
    /// at once; the browser opens later, on the UI thread, once the channels have arrived.</summary>
    private void OpenSource(string id)
    {
        if (_store.Get(id) is not IptvSource source)
            return;
        _lastSourceId = source.Id;
        // Chosen here, on the UI thread, because the loader runs on a worker thread where the translation
        // lookup must not be touched.
        var uncategorized = Tr("Uncategorized");
        var prompt = new ProgressPrompt(
            // Translators: Title of the progress window shown while an IPTV source's channels are loaded.
            Tr("Opening IPTV source"),
            // Translators: Message shown while an IPTV source's channels are loaded.
            Tr("Loading channels..."),
            _ => Tr("Loading channels..."))
        {
            Proportional = false,
        };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (_, token) => IptvLoader.Load(source, uncategorized, token),
            result => ShowChannels(source, result));
    }

    /// <summary>Opens the browser on a loaded source, or reports why it could not be loaded. On the UI
    /// thread, so this is where the not-yet-supported wording is chosen for a kind whose client is not built.
    /// </summary>
    private void ShowChannels(IptvSource source, IptvLoadResult result)
    {
        if (result.Catalog is not IptvCatalog catalog)
        {
            _view.ShowError(
                result.Error ??
                // Translators: Shown when an IPTV source is of a kind Luna Player cannot open yet (Xtream or
                // Stalker, before those are supported).
                Tr("This kind of IPTV source is not supported yet."),
                // Translators: Title of the messages shown about IPTV.
                Tr("IPTV"));
            return;
        }
        if (catalog.Channels.Count == 0)
        {
            _view.ShowError(
                // Translators: Shown when an IPTV source loaded but holds no channels to play.
                Tr("This source holds no channels."), Tr("IPTV"));
            return;
        }
        var chosen = _view.BrowseChannels(new ChannelBrowserPrompt(
            // Translators: Title of the channel browser window. {name} is the source being browsed.
            TrFormat("Channels - {name}", source.Name),
            catalog.Categories,
            catalog.Channels,
            SpeakBrowserHelp,
            catalog.Guide));
        if (chosen is int index && index >= 0 && index < catalog.Channels.Count)
            Play(source, catalog.Channels[index]);
    }

    /// <summary>Plays a channel the user chose in the browser.</summary>
    /// <remarks>
    /// A channel with an address plays straight away. A channel without one is a Stalker command that is
    /// turned into a short-lived address at the moment of playing, behind a brief progress window, then played
    /// like any other stream. The resolved address is never saved.
    /// </remarks>
    private void Play(IptvSource source, IptvChannel channel)
    {
        if (channel.Url is { Length: > 0 } url)
        {
            OpenStream(url, channel.Name);
            return;
        }
        if (channel.StalkerCmd is { Length: > 0 } cmd)
        {
            ResolveAndPlay(source, channel, cmd);
            return;
        }
        _view.ShowError(
            // Translators: Shown when a chosen channel carries no address and none could be worked out.
            Tr("This channel cannot be played."), Tr("IPTV"));
    }

    /// <summary>Resolves a Stalker channel's short-lived address behind a progress window, then plays it.</summary>
    private void ResolveAndPlay(IptvSource source, IptvChannel channel, string cmd)
    {
        var prompt = new ProgressPrompt(
            // Translators: Title of the brief progress window shown while a channel's address is worked out.
            Tr("Opening channel"),
            // Translators: Message shown while a Stalker channel's short-lived play address is worked out.
            Tr("Preparing the channel..."),
            _ => Tr("Preparing the channel..."))
        {
            Proportional = false,
        };
        BackgroundProgress.Start(_view, _dispatcher, prompt,
            (_, token) => StalkerClient.ResolveLink(source, cmd, token),
            resolved =>
            {
                if (resolved is { Length: > 0 } url)
                    OpenStream(url, channel.Name);
                else
                    _view.ShowError(
                        // Translators: Shown when a Stalker channel's play address could not be worked out.
                        Tr("Could not open the channel."), Tr("IPTV"));
            });
    }

    private void OpenStream(string url, string name)
    {
        if (!_player.OpenStream(url, title: name))
            _view.ShowError(
                // Translators: Shown when a chosen IPTV channel could not be played.
                Tr("Could not open the channel."), Tr("IPTV"));
    }

    /// <summary>The id of what was saved, or null when nothing was.</summary>
    private string? Add()
    {
        var draft = _view.EditIptvSource(
            // Translators: Title of the window for saving a new IPTV source.
            Tr("Add IPTV source"),
            new IptvSourceDraft());
        if (draft is not IptvSourceDraft value)
            return null;
        var added = _store.Add(value);
        if (added is not null)
            return added.Id;
        ReportStoreFailure();
        return null;
    }

    private void Edit(string id)
    {
        if (_store.Get(id) is not IptvSource source)
            return;
        var draft = _view.EditIptvSource(
            // Translators: Title of the window for changing an IPTV source already saved.
            Tr("Edit IPTV source"),
            IptvSourceDraft.From(source));
        if (draft is not IptvSourceDraft value)
            return;
        if (!_store.Update(id, value))
            ReportStoreFailure();
    }

    private bool Remove(string id)
    {
        if (_store.Get(id) is not IptvSource source)
            return false;
        if (!_view.Confirm(
            // Translators: Asks the user to confirm removing one saved IPTV source. {name} is what it is
            // called and {kind} is what sort of source it is.
            TrFormat("Remove IPTV source '{name}' ({kind})?", source.Name, IptvSourceStore.Describe(source.Kind)),
            // Translators: Title of the window that asks the user to confirm removing a saved IPTV source.
            Tr("Confirm remove")))
            return false;
        if (_store.Delete(id))
            return true;
        ReportStoreFailure();
        return false;
    }

    private void SpeakBrowserHelp(string text) => _speech.Speak(text, text);

    /// <summary>Reports why the store refused. It has already worded the reason.</summary>
    private bool ReportStoreFailure()
    {
        if (_store.LastError.Length == 0)
            return false;
        _view.ShowError(_store.LastError, Tr("IPTV"));
        return true;
    }
}
