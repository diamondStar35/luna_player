using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using PrismSharp.Native;

namespace PrismSharp
{
    /// <summary>
    /// Receives a single Prism log message.
    /// </summary>
    /// <param name="level">Severity of the message.</param>
    /// <param name="source">Component that produced the message.</param>
    /// <param name="message">The message text.</param>
    /// <remarks>
    /// Invoked on Prism's internal logging thread, never concurrently with itself. Implementations
    /// must synchronize any shared state they touch, should do as little work as possible, and must
    /// not call back into <see cref="Log"/> - doing so is undefined behaviour.
    /// </remarks>
    public delegate void LogCallback(LogLevel level, string source, string message);

    /// <summary>
    /// Prism's diagnostic logging subsystem.
    /// </summary>
    /// <remarks>
    /// Logging is process-wide rather than per-context, so every member here may be called before a
    /// <see cref="Context"/> exists and after one is disposed. Messages are handed to a background
    /// thread, so logging never blocks the caller and never interferes with synthesis. With no
    /// handler installed, messages are discarded and logging costs almost nothing.
    /// <para>
    /// When <c>PRISM_LOG</c> is set to <c>trace</c>, <c>debug</c>, <c>info</c>, <c>warn</c>,
    /// <c>error</c> or <c>none</c>, Prism installs its own standard-error handler during the first
    /// <see cref="Context"/> construction. Installing a handler here afterwards replaces it.
    /// </para>
    /// </remarks>
    public static unsafe class Log
    {
        private static readonly Lock Sync = new();

        // Prism warns that a message already in flight may still reach a replaced handler, so these
        // are rooted for the lifetime of the process rather than freed on replacement.
        private static readonly List<GCHandle> Retained = [];

        /// <summary>
        /// Installs the handler that receives Prism diagnostics, replacing any previous one.
        /// </summary>
        /// <param name="handler">The handler to install, or <see langword="null"/> to stop delivery.</param>
        /// <remarks>
        /// Safe to call at any time, including concurrently with logging on other threads. The
        /// replacement takes effect for messages delivered after the call; a message already in
        /// flight may still reach the previous handler, so state it depends on must stay valid until
        /// the application knows no delivery is in progress.
        /// </remarks>
        public static void Install(LogCallback? handler)
        {
            lock (Sync)
            {
                var native = default(PrismLogHandler);
                if (handler != null)
                {
                    var box = GCHandle.Alloc(handler);
                    Retained.Add(box);
                    native.fn = &OnLog;
                    native.userdata = (void*)GCHandle.ToIntPtr(box);
                }

                Methods.prism_set_log_handler(native);
            }
        }

        /// <summary>
        /// Sets the minimum severity delivered to the handler.
        /// </summary>
        /// <param name="level">
        /// Minimum severity; messages below it are discarded before being queued.
        /// <see cref="LogLevel.None"/> suppresses everything.
        /// </param>
        /// <returns>The threshold that was in effect before the call.</returns>
        /// <remarks>
        /// The threshold is applied on the thread producing a message, before queuing, so raising it
        /// immediately reduces the work done for suppressed messages. The threshold and the installed
        /// handler are independent.
        /// </remarks>
        public static LogLevel SetLevel(LogLevel level) => Methods.prism_set_log_level(level);

        /// <summary>
        /// Emits a message through Prism's logger, so application diagnostics share the handler and
        /// ordering of the library's own.
        /// </summary>
        /// <param name="level">Severity of the message.</param>
        /// <param name="source">Component producing the message. Must not be empty.</param>
        /// <param name="message">The message text.</param>
        /// <remarks>
        /// Returns without queuing anything if the severity is below the current threshold. Otherwise
        /// the message is copied onto an internal queue; the handler is not invoked directly and this
        /// never blocks waiting for delivery. The queue has a fixed capacity, and messages submitted
        /// while it is full are dropped and reported later as a single warning from source
        /// <c>prism</c>.
        /// </remarks>
        public static void Write(LogLevel level, string source, string message)
        {
            if (string.IsNullOrEmpty(source) || message == null)
                return;

            Methods.prism_log(level, source, message);
        }

        /// <summary>
        /// Blocks until every message queued before this call has reached the handler.
        /// </summary>
        /// <remarks>
        /// Useful before replacing a handler or shutting down. A flush is never dropped, even when the
        /// queue is otherwise full. Must not be called from inside a log handler.
        /// </remarks>
        public static void Flush() => Methods.prism_log_flush();

        /// <summary>
        /// Drains pending messages, stops Prism's logging thread and joins it.
        /// </summary>
        /// <remarks>
        /// Not ordinarily necessary, since the logger is torn down at process exit. There is no way to
        /// restart the logging thread afterwards, so call this only when certain the process will not
        /// need logging again - for example immediately before unloading the library. Must not be
        /// called from inside a log handler, and no other thread may be logging concurrently.
        /// </remarks>
        public static void Shutdown() => Methods.prism_log_shutdown();

        // Runs on Prism's logging thread. An exception escaping into native code is undefined
        // behaviour, so nothing is allowed out.
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnLog(void* userdata, LogLevel level, byte* source, byte* message)
        {
            try
            {
                if (userdata == null)
                    return;

                var handle = GCHandle.FromIntPtr((nint)userdata);
                if (!handle.IsAllocated || handle.Target is not LogCallback callback)
                    return;

                // Both strings are valid only for the duration of this call.
                callback(level, Methods.String(source) ?? string.Empty, Methods.String(message) ?? string.Empty);
            }
            catch
            {
            }
        }
    }
}
