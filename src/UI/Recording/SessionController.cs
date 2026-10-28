using LunaPlayer.Application;
using LunaPlayer.Recording;
namespace LunaPlayer.UI.Recording;

internal sealed class SessionController(RecordingEngine engine, IApplicationDispatcher dispatcher)
{
    internal void Start(RecordingOptions options, IReadOnlyList<RecordingSource> sources,
        Action<RecordingOutcome, IReadOnlyList<string>> completed)
    {
        _ = Task.Run(() =>
        {
            try
            {
                var outcome = engine.Start(options, sources, out var failures);
                dispatcher.Post(() => completed(outcome, failures));
            }
            catch (Exception failure)
            {
                dispatcher.Post(() => completed(RecordingOutcome.Failed(RecordingFailure.Failed, failure.Message), []));
            }
        });
    }
    internal void Stop(Action<RecordingOutcome> completed)
    {
        _ = Task.Run(() =>
        {
            try { var outcome = engine.Stop(); dispatcher.Post(() => completed(outcome)); }
            catch (Exception failure)
            { dispatcher.Post(() => completed(RecordingOutcome.Failed(RecordingFailure.Failed, failure.Message))); }
        });
    }
}
