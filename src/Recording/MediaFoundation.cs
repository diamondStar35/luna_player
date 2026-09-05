using NAudio.MediaFoundation;

namespace LunaPlayer.Recording;

/// <summary>Brings Media Foundation up once, on whichever thread first needs it.</summary>
///
/// <remarks>
/// Every encoder the player writes with is a Media Foundation one, and the library has to be started
/// before any of them is touched. Starting it more than once is harmless but shutting it down while
/// another recording is still writing is not, so it is started on demand and never shut down: it is
/// released when the process ends, which is the only moment nothing can still be using it.
/// </remarks>
internal static class MediaFoundation
{
    private static readonly Lock Sync = new();
    private static bool _started;

    /// <summary>Makes sure Media Foundation is up. Safe to call from any thread and as often as liked.
    /// </summary>
    internal static void Start()
    {
        lock (Sync)
        {
            if (_started)
                return;
            MediaFoundationApi.Startup();
            _started = true;
        }
    }
}
