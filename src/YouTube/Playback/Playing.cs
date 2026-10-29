using LunaPlayer.Accessibility;
using LunaPlayer.Application;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Media;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.YouTube.Playback;

internal sealed partial class Sessions
{
    // ---- playing ----

    /// <summary>Starts a video from a session and makes it the session's current one.</summary>
    private bool Start(Session session, int index, Resolved ready, int direction = 1)
    {
        if (!Owns(session))
            return false;
        var previous = _session;
        _session = session;
        if (!Open(ready, Session.AudioFor(session.Mode)))
        {
            _session = previous;
            return false;
        }
        _pending = null;
        if (!ReferenceEquals(previous, session)) previous?.Dispose();
        session.Selected = index;
        var adjacent = index + Math.Sign(direction);
        if (adjacent >= 0 && adjacent < session.Items.Count)
            Prefetch(session, adjacent, 1);
        var successor = index + 1;
        if (successor < session.Items.Count)
            PrepareSuccessor(session, index, successor);
        return true;
    }

    /// <summary>Starts a video from a list, in front of the playlist the user opened.</summary>
    /// <remarks>
    /// So that playlist keeps its files, its order and its place. Nothing here has to turn shuffle off
    /// either - the session's list starts without it, and the one the user set stays set on the playlist it
    /// belongs to.
    /// </remarks>
    /// <param name="audioOnly">Whether this play is sound only - Ctrl+Enter rather than Enter - so the
    /// player leaves any picture the stream carries undecoded.</param>
    private bool Open(Resolved ready, bool audioOnly)
    {
        _activeChannelUrl = ready.Item.ChannelUrl;
        _activeVideo = ready.Item;
        return Report(_player.PlaySessionStream(
            ready.Url, ready.Item.Title, ready.Item.Url, ready.AudioUrl, audioOnly,
            fadeTransition: _player.InSession && _player.IsLoaded));
    }

    /// <summary>Starts a video that came from a link rather than from a list.</summary>
    /// <remarks>
    /// Into the playlist the user is working in, which is where a single video belongs: there is no list
    /// behind it to move through, nothing for Escape to go back to,
    /// and nothing a stage of its own would keep separate. A plain network stream is opened the same way.
    /// </remarks>
    private bool OpenAlone(Resolved ready)
    {
        Clear();
        _activeChannelUrl = ready.Item.ChannelUrl;
        _activeVideo = ready.Item;
        return Report(_player.OpenStream(ready.Url, ready.Item.Title, ready.Item.Url, ready.AudioUrl));
    }

    private bool Report(bool opened)
    {
        if (opened)
            return true;
        ShowError(
            // Translators: Shown when a video was found but the player could not start playing it.
            Tr("Could not open YouTube stream."));
        return false;
    }

    /// <summary>Moves playback to a ready video if the session has not changed while it was resolving.</summary>
    private bool MoveTo(Session session, int from, int to, Resolved ready)
    {
        if (!ReferenceEquals(_session, session)
            || CurrentWatchUrl is not string playing || session.IndexOf(playing) != from)
            return false;
        return Start(session, to, ready, to - from);
    }

    /// <summary>Resolves an adjacent video in the background and moves to it when it arrives.</summary>
    private void LoadMove(Session session, int from, int to, YouTubeResult item, int direction)
    {
        var pending = new PendingMove(session, from, to, item.Url, Math.Sign(direction));
        _pending = pending;
        _speech.Speak(
            direction < 0
                // Translators: Spoken when the video before this one is being fetched before it can be played.
                ? Tr("Loading previous video...")
                // Translators: Spoken when the video after this one is being fetched before it can be played.
                : Tr("Loading next video..."),
            direction < 0
                // Translators: The short wording spoken while the video before this one is being fetched.
                ? Tr("Loading previous video...")
                // Translators: The short wording spoken while the video after this one is being fetched.
                : Tr("Loading next video..."));
        var task = _cache.Start(
            _cache.Key(item.Url, Session.AudioFor(session.Mode), session.QualityFor(session.Mode)),
            item.Url, item, Session.AudioFor(session.Mode), session.QualityFor(session.Mode),
            session.Token);
        _ = task.ContinueWith(
            finished =>
            {
                var outcome = finished.IsCompletedSuccessfully
                    ? finished.Result
                    : finished.IsCanceled
                        ? ResolveOutcome.Cancelled
                        : FailureMapping.Explain(
                            finished.Exception?.InnerException ?? new InvalidOperationException(
                                "The YouTube stream resolve failed."),
                            session.Token);
                _dispatcher.Post(() => MoveReady(pending, outcome));
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
    }

    /// <remarks>
    /// The new title is announced, so the user is not left to work out what is playing. Something is also
    /// said when the video could not be fetched, so the loading announcement is not left as the last thing
    /// said with nothing following it.
    /// </remarks>
    private void MoveReady(PendingMove pending, ResolveOutcome outcome)
    {
        if (!ReferenceEquals(_pending, pending) || !ReferenceEquals(_session, pending.Session))
            return;
        _pending = null;
        if (outcome.Value is not Resolved ready)
        {
            if (outcome.Failure is ResolveFailure.Cancelled)
                return;
            _speech.Speak(
                pending.Direction < 0
                    // Translators: Spoken when the video before this one could not be fetched.
                    ? Tr("Could not load the previous video.")
                    // Translators: Spoken when the video after this one could not be fetched, so playback stops.
                    : Tr("Could not load the next video."),
                pending.Direction < 0
                    // Translators: The short wording spoken when the previous video could not be fetched.
                    ? Tr("Previous video failed.")
                    // Translators: The short wording spoken when the next video could not be fetched.
                    : Tr("Next video failed."));
            return;
        }
        if (!MoveTo(pending.Session, pending.From, pending.To, ready))
            return;
        if (_settings.General.SpeakFileOnNavigation)
            _speech.Speak(ready.Item.Title, ready.Item.Title);
    }

    // ---- prefetching ----

    /// <summary>Starts resolving <paramref name="count"/> videos from <paramref name="start"/>, so they
    /// play without a wait when they are reached.</summary>
    /// <remarks>
    /// Both streams of each: the user chooses picture or sound only when they
    /// press the key, so both must be ready by then rather than one guessed at. The four-wide gate in
    /// <see cref="ResolveCache"/> keeps asking for two per row from flooding YouTube - the second simply
    /// queues behind the first - and a mode nobody plays costs one resolve that the bound soon evicts.
    /// </remarks>
    private void Prefetch(Session session, int start, int count)
    {
        if (session.IsCancelled)
            return;
        var end = Math.Min(session.Items.Count, Math.Max(0, start) + count);
        for (var index = Math.Max(0, start); index < end; index++)
        {
            var item = session.Items[index];
            // Only videos resolve to a stream; a playlist or channel row is browsed into, not played, so
            // there is nothing to make ready for it.
            if (item.ItemType is not YouTubeItemType.Video)
                continue;
            _cache.Prefetch(item.Url, item, false, session.VideoQuality, session.Token);
            _cache.Prefetch(item.Url, item, true, session.AudioQuality, session.Token);
        }
    }

    private Resolved? Ready(Session session, YouTubeResult item)
        => _cache.TryTake(
            _cache.Key(item.Url, Session.AudioFor(session.Mode), session.QualityFor(session.Mode)));

    /// <summary>Hands the next resolved stream to mpv while the current video is still playing. Resolution
    /// remains best-effort: a failed or late online request simply leaves the ordinary end-of-file path.</summary>
    private void PrepareSuccessor(Session session, int current, int next)
    {
        if ((!_settings.Audio.Crossfade && _settings.Audio.GaplessPlayback == GaplessPlaybackMode.Disabled)
            || next < 0 || next >= session.Items.Count)
            return;
        var item = session.Items[next];
        if (item.ItemType is not YouTubeItemType.Video)
            return;
        var audioOnly = Session.AudioFor(session.Mode);
        var task = _cache.Start(
            _cache.Key(item.Url, audioOnly, session.QualityFor(session.Mode)),
            item.Url, item, audioOnly, session.QualityFor(session.Mode), session.Token);
        _ = task.ContinueWith(finished =>
        {
            if (!finished.IsCompletedSuccessfully || finished.Result.Value is not Resolved ready)
                return;
            _dispatcher.Post(() => QueueSuccessor(session, current, ready, audioOnly));
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    private void QueueSuccessor(Session session, int current, Resolved ready, bool audioOnly)
    {
        if (!ReferenceEquals(_session, session)
            || CurrentWatchUrl is not string playing
            || session.IndexOf(playing) != current)
            return;
        _player.QueueSessionStream(
            playing, ready.Url, ready.Item.Title, ready.Item.Url, ready.AudioUrl, audioOnly);
    }

    /// <summary>Updates the session after mpv starts a preloaded video and begins preparing its successor.</summary>
    internal void AcceptQueuedAdvance()
    {
        if (_session is not Session session)
            return;
        var index = CurrentSessionIndex();
        if (index < 0)
            return;
        session.Selected = index;
        _activeChannelUrl = session.Items[index].ChannelUrl;
        var next = index + 1;
        if (next < session.Items.Count)
        {
            Prefetch(session, next, 1);
            PrepareSuccessor(session, index, next);
        }
    }

    private void RefreshQueuedSuccessor()
    {
        var index = CurrentSessionIndex();
        if (_session is Session session && index >= 0)
            PrepareSuccessor(session, index, index + 1);
    }

    /// <summary>A move to an adjacent video that is waiting on a resolve.</summary>
    private sealed record PendingMove(Session Session, int From, int To, string WatchUrl, int Direction);
}
