namespace LunaPlayer.Media;

/// <summary>How far a background job has got, as it reports itself from the thread doing the work.</summary>
/// <param name="Value">The completed amount represented by the overall progress bar.</param>
/// <param name="Total">The overall amount, or zero while that is still being worked out.</param>
/// <param name="Name">The name of the file being dealt with right now, to show the user.</param>
/// <param name="Found">How many items were kept, for a job that filters as it goes. A changing value also
/// identifies a new sub-task whose individual progress bar begins again.</param>
/// <param name="ItemNumber">The one-based place of the current item in a batch, or zero when the job does
/// not expose individual items.</param>
/// <param name="ItemValue">How far the current item has got.</param>
/// <param name="ItemTotal">The current item's full amount, or zero when its progress is unknown.</param>
internal readonly record struct ProgressUpdate(
    int Value,
    int Total,
    string Name,
    int Found = 0,
    int ItemNumber = 0,
    int ItemValue = 0,
    int ItemTotal = 0);
