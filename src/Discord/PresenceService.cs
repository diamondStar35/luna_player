using LunaPlayer.Configuration.Models;
using LunaPlayer.Discord.Publishing;

namespace LunaPlayer.Discord;

/// <summary>Passes immutable app-thread snapshots to the publishing worker. Formatting, artwork
/// lookup and IPC run without accessing Player, mutable settings or wxWidgets.</summary>
internal sealed class PresenceService : IAsyncDisposable
{
    private sealed record Snapshot(PresenceOptions Options, PresenceMedia? Media, DateTimeOffset ObservedAt);
    private sealed record State(PresenceOptions Options, string? Identity, bool? Playing, double? Speed,
        SourceKind? Kind, bool? Video, string? Title, string? Artist, string? Chapter);
    private Snapshot _latest = new(default, null, default);
    private readonly PresenceText _text;
    private readonly ActivityBuilder _builder;
    private readonly Worker _worker;
    private readonly TimeProvider _time;

    internal PresenceService(PresenceText text, Func<IRpcClient>? factory = null, Timing? timing = null,
        IApplicationArtwork? artwork = null, TimeProvider? time = null)
    {
        _text = text;
        _time = time ?? TimeProvider.System;
        _builder = new(_time.GetUtcNow());
        _worker = new(Render, factory, timing, artwork: artwork, time: _time);
    }

    internal void Update(DiscordSettings settings, PresenceMedia? media)
    {
        Volatile.Write(ref _latest, new(PresenceOptions.Capture(settings), media, _time.GetUtcNow()));
        _worker.Wake();
    }

    private Publication Render(string? icon)
    {
        var snapshot = Volatile.Read(ref _latest);
        return new(snapshot.Options.Enabled,
            _builder.BuildSnapshot(snapshot.Options, snapshot.Media, _text, snapshot.ObservedAt, icon),
            new State(snapshot.Options, snapshot.Media?.Identity, snapshot.Media?.Playing, snapshot.Media?.Speed,
                snapshot.Media?.Kind, snapshot.Media?.Video, snapshot.Media?.Title, snapshot.Media?.Artist, snapshot.Media?.Chapter));
    }

    internal Task ShutdownAsync() => _worker.ShutdownAsync();
    public ValueTask DisposeAsync() => new(ShutdownAsync());
}
