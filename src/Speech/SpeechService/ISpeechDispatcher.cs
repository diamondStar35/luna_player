using System;
using System.Threading.Tasks;

namespace PrismSharp.Speech
{
    /// <summary>
    /// A host-supplied hook for running speech work on a thread the host controls.
    /// </summary>
    /// <remarks>
    /// Prism context and backend handles are not thread-safe, so every operation must be funnelled
    /// onto one consistent thread. By default <see cref="ScreenReaderWorker"/> owns a private thread
    /// for that. A host that already has a suitable thread - a UI or game loop that owns the
    /// screen-reader context - implements this instead and passes it in, so speech work runs there
    /// rather than on a second thread.
    /// </remarks>
    public interface ISpeechDispatcher
    {
        /// <summary>Runs <paramref name="action"/> on the dispatcher's thread and returns its result.</summary>
        T Invoke<T>(Func<T> action);

        /// <summary>
        /// Queues <paramref name="action"/> on the dispatcher's thread without waiting for it.
        /// </summary>
        /// <remarks>
        /// The default implementation absorbs the blocking <see cref="Invoke{T}"/> on a thread-pool
        /// thread, which is correct but parks a pooled thread for the round trip. A host whose
        /// dispatcher has a native post or begin-invoke path should override this to use it.
        /// <para>
        /// Callers rely on this not blocking: it is what Prism's availability poll thread uses, and
        /// that thread cannot run another scan until the callback returns.
        /// </para>
        /// </remarks>
        void Post(Action action)
        {
            if (action == null)
                return;

            Task.Run(() =>
            {
                try
                {
                    Invoke(() =>
                    {
                        action();
                        return 0;
                    });
                }
                catch
                {
                }
            });
        }
    }
}
