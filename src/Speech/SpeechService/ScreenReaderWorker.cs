using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PrismSharp.Speech.Playback;
using PrismSharp.Speech.ScreenReaders;

namespace PrismSharp.Speech
{
    /// <summary>
    /// Thread-confinement shim guaranteeing every <see cref="IScreenReader"/> operation runs on one
    /// consistent thread, which Prism requires because backend handles are not thread-safe.
    /// </summary>
    /// <remarks>
    /// With no dispatcher the worker owns a private background thread. Given an
    /// <see cref="ISpeechDispatcher"/> it defers to the host's thread instead.
    /// </remarks>
    public sealed class ScreenReaderWorker : IDisposable
    {
        private readonly IMode _mode;
        private bool _disposed;

        /// <param name="screenReader">The reader to confine. Required.</param>
        /// <param name="player">
        /// Sink for synthesized PCM when a backend speaks to memory, or null to disable that path.
        /// </param>
        /// <param name="dispatcher">
        /// A host thread to run speech work on, or null to let the worker own a private thread.
        /// </param>
        public ScreenReaderWorker(IScreenReader screenReader, IPlayer? player = null, ISpeechDispatcher? dispatcher = null)
        {
            if (screenReader == null)
                throw new ArgumentNullException(nameof(screenReader));

            _mode = dispatcher == null
                ? new ThreadMode(screenReader, player)
                : new DispatcherMode(screenReader, player, dispatcher);
        }

        /// <summary>
        /// Runs an operation on the reader thread and returns its result, blocking until it finishes.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="operation">The work to run against the confined reader.</param>
        /// <returns>Whatever <paramref name="operation"/> returned.</returns>
        /// <remarks>
        /// Exceptions thrown by the operation surface here. Calling from the reader thread itself runs
        /// inline rather than deadlocking. Never call this from a Prism-owned callback thread - use
        /// <see cref="Post"/>, which does not block.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The worker has been disposed.</exception>
        public T Invoke<T>(Func<IScreenReader, T> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _mode.Invoke(operation);
        }

        /// <summary>
        /// Queues work on the reader thread without waiting for it. Use this from threads that must
        /// not block - notably Prism's availability poll thread, which cannot scan again until the
        /// callback returns and would deadlock against a blocking <see cref="Invoke{T}"/>.
        /// </summary>
        public void Post(Action<IScreenReader> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);

            if (_disposed)
                return;

            try
            {
                _mode.Post(operation);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// Closes the reader on its own thread and shuts the worker down, joining the private thread
        /// when the worker owns one.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _mode.Dispose();
        }

        private interface IMode : IDisposable
        {
            T Invoke<T>(Func<IScreenReader, T> operation);
            void Post(Action<IScreenReader> operation);
        }

        private sealed class DispatcherMode : IMode
        {
            private readonly IScreenReader _screenReader;
            private readonly ISpeechDispatcher _dispatcher;
            private bool _disposed;

            public DispatcherMode(IScreenReader screenReader, IPlayer? player, ISpeechDispatcher dispatcher)
            {
                _screenReader = screenReader;
                _dispatcher = dispatcher;
                _dispatcher.Invoke(() =>
                {
                    _screenReader.BindPlayer(player);
                    return 0;
                });
            }

            public T Invoke<T>(Func<IScreenReader, T> operation)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DispatcherMode));

                return _dispatcher.Invoke(() => operation(_screenReader));
            }

            public void Post(Action<IScreenReader> operation)
            {
                if (_disposed)
                    return;

                // ISpeechDispatcher.Post is non-blocking by contract; a host whose dispatcher can do
                // better than the default thread-pool hop overrides it.
                _dispatcher.Post(() =>
                {
                    if (!_disposed)
                        operation(_screenReader);
                });
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                try
                {
                    _dispatcher.Invoke(() =>
                    {
                        try
                        {
                            _screenReader.Close();
                        }
                        catch
                        {
                        }

                        return 0;
                    });
                }
                catch
                {
                }
            }
        }

        private sealed class ThreadMode : IMode
        {
            private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
            private readonly ManualResetEventSlim _startupReady = new ManualResetEventSlim(false);
            private readonly Thread _thread;
            private readonly IScreenReader _screenReader;
            private readonly IPlayer? _player;
            private Exception? _startupError;
            private int _threadId;
            private bool _disposed;

            public ThreadMode(IScreenReader screenReader, IPlayer? player)
            {
                _screenReader = screenReader;
                _player = player;
                _thread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "PrismSharp.ScreenReaderWorker"
                };
                _thread.Start();
                _startupReady.Wait();
                if (_startupError != null)
                {
                    Dispose();
                    throw new InvalidOperationException("Failed to initialize speech worker.", _startupError);
                }
            }

            public T Invoke<T>(Func<IScreenReader, T> operation)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ThreadMode));

                if (Thread.CurrentThread.ManagedThreadId == _threadId)
                    return operation(_screenReader);

                var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    _queue.Add(() =>
                    {
                        try
                        {
                            completion.TrySetResult(operation(_screenReader));
                        }
                        catch (Exception ex)
                        {
                            completion.TrySetException(ex);
                        }
                    });
                }
                catch (InvalidOperationException ex)
                {
                    throw new ObjectDisposedException(nameof(ThreadMode), ex.Message);
                }

                return completion.Task.GetAwaiter().GetResult();
            }

            public void Post(Action<IScreenReader> operation)
            {
                if (_disposed)
                    return;

                try
                {
                    _queue.Add(() =>
                    {
                        try
                        {
                            operation(_screenReader);
                        }
                        catch
                        {
                        }
                    });
                }
                catch (InvalidOperationException)
                {
                    // The queue stopped accepting work; the worker is shutting down.
                }
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _queue.CompleteAdding();
                _thread.Join();
                _startupReady.Dispose();
                _queue.Dispose();
            }

            private void Run()
            {
                _threadId = Thread.CurrentThread.ManagedThreadId;
                try
                {
                    _screenReader.BindPlayer(_player);
                }
                catch (Exception ex)
                {
                    _startupError = ex;
                }
                finally
                {
                    _startupReady.Set();
                }

                if (_startupError != null)
                {
                    CloseReaderQuietly();
                    return;
                }

                foreach (var work in _queue.GetConsumingEnumerable())
                    work();

                CloseReaderQuietly();
            }

            private void CloseReaderQuietly()
            {
                try
                {
                    _screenReader.Close();
                }
                catch
                {
                }
            }
        }
    }
}
