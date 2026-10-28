using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Equalizer;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI.Equalizer;
using LunaPlayer.YouTube;

namespace LunaPlayer.UI.Contracts;
internal readonly record struct FileSelection(string Path, string Directory);
internal readonly record struct UiOperation(bool Success, string Error = "");

internal interface IProgressView : IDisposable
{
    /// <summary>Shows how far the job has got, as a percentage from nought to a hundred.</summary>
    /// <remarks>
    /// A percentage rather than a count, because a job can change what it is counting part way through - the
    /// folder scan counts files it has sized up and then files it has looked at, two different totals - and a
    /// bar told raw counts has no way to know that. It is also what a screen reader reads out.
    /// </remarks>
    void Update(int percent, string message);

    /// <summary>Moves a bar that has no figure behind it. Called on every tick of a job that cannot say how
    /// far through it is, so the window still shows that something is happening.</summary>
    void Pulse();

    /// <summary>Whether the user has pressed Cancel. A plain flag rather than something reported back out
    /// of Update, so a job with nothing new to report can still be cancelled.</summary>
    bool Cancelled { get; }
}
