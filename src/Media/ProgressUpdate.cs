namespace LunaPlayer.Media;

/// <summary>How far a job working through a set of files has got, as it reports itself from the thread doing
/// the work.</summary>
/// <param name="Value">How many files have been dealt with, counting from one.</param>
/// <param name="Total">How many there are in all, or zero while that is still being worked out.</param>
/// <param name="Name">The name of the file being dealt with right now, to show the user.</param>
/// <param name="Found">How many of those dealt with were kept, for a job that filters as it goes.</param>
internal readonly record struct ProgressUpdate(int Value, int Total, string Name, int Found = 0);
