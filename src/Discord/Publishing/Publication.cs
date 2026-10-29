namespace LunaPlayer.Discord.Publishing;

internal sealed record Publication(bool Enabled, string? Activity, object? State = null);

internal sealed record Timing(TimeSpan Tick, TimeSpan Retry, TimeSpan ArtworkRefresh)
{
    internal static Timing Default { get; } = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(15));
}
