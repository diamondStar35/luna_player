namespace LunaPlayer.Downloads;

/// <summary>A provider captures its immutable request; the queue owns scheduling and cancellation.</summary>
internal sealed record Job(string Title, Func<Action<Progress>, CancellationToken, Outcome> Run);
