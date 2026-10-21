using System.IO.Pipes;
using System.Text.Json;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;

namespace LunaPlayer.Application;

internal sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = $"Local\\{AppInfo.Identifier}.MainInstance";
    private const string PipeName = $"{AppInfo.Identifier}.OpenPaths";

    private readonly Lock _sync = new();
    private readonly Mutex? _mutex;
    private readonly bool _ownsMutex;
    private readonly bool _independent;
    private Thread? _listenerThread;
    private Action<IReadOnlyList<string>>? _receiver;
    private volatile bool _stopping;
    private bool _disposed;

    internal SingleInstanceService(bool independent = false)
    {
        if (independent)
        {
            _independent = true;
            IsPrimary = true;
            return;
        }

        try
        {
            _mutex = new Mutex(initiallyOwned: true, MutexName, out _ownsMutex);
            IsPrimary = _ownsMutex;
        }
        catch (UnauthorizedAccessException)
        {
            IsPrimary = true;
        }
        catch (IOException)
        {
            IsPrimary = true;
        }
    }

    internal bool IsPrimary { get; }

    internal void StartListening(Action<IReadOnlyList<string>> receiver)
    {
        Thread? listener = null;
        lock (_sync)
        {
            if (_disposed || !IsPrimary || _independent)
                return;
            _receiver = receiver;
            if (_listenerThread is null)
            {
                listener = new Thread(Listen)
                {
                    IsBackground = true,
                    Name = PipeName,
                };
                _listenerThread = listener;
            }
        }
        // Server construction belongs to this background thread. In particular, named-pipe setup never sits
        // between process entry and the first painted frame.
        listener?.Start();
    }

    /// <summary>Tries to hand a launch request to the primary instance. False means the caller must keep
    /// running and open the request itself rather than silently losing it.</summary>
    internal bool ForwardPaths(IEnumerable<string> paths)
    {
        var values = paths.Where(static path => !string.IsNullOrWhiteSpace(path)).ToArray();
        try
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.Out,
                PipeOptions.CurrentUserOnly);
            // An established instance connects immediately. The timeout only covers the uncommon race in
            // which another process acquired the mutex but has not painted its frame and started the server.
            pipe.Connect(500);
            WritePayload(pipe, values);
            return true;
        }
        catch (TimeoutException)
        {
            // The primary process did not finish opening its listener in time. The caller opens a new window.
        }
        catch (IOException)
        {
            // The pipe disappeared or rejected the payload. The caller opens a new window with the paths.
        }
        catch (UnauthorizedAccessException)
        {
            // Access to the other process's pipe was denied. Keep the requested files in this process.
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _stopping = true;
        lock (_sync)
        {
            _receiver = null;
        }
        // Release the single-instance lock first, before tearing anything else down. A relaunch is gated on
        // this mutex: freeing it up front lets the user reopen the player immediately, even while the rest of
        // this process is still shutting down (the media backend in particular takes a moment to close).
        if (_ownsMutex)
        {
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The mutex was not owned any more; disposal still has to release the remaining resources.
            }
        }
        _mutex?.Dispose();
        if (_listenerThread is not null)
        {
            TryWakeListener();
            _listenerThread = null;
        }
    }

    private void Listen()
    {
        while (!_stopping)
        {
            try
            {
                using var pipe = CreateServer();
                pipe.WaitForConnection();
                if (_stopping)
                    return;
                Deliver(ReadPayload(pipe));
            }
            catch (IOException) when (!_stopping)
            {
                // A client can disconnect while sending. The listener remains available for the next launch.
            }
            catch (UnauthorizedAccessException) when (!_stopping)
            {
                return;
            }
        }
    }

    private void Deliver(IReadOnlyList<string> paths)
    {
        Action<IReadOnlyList<string>>? receiver;
        lock (_sync)
        {
            if (_disposed)
                return;
            receiver = _receiver;
        }
        receiver?.Invoke(paths);
    }

    private static NamedPipeServerStream CreateServer() => new(
        PipeName,
        PipeDirection.In,
        1,
        PipeTransmissionMode.Byte,
        PipeOptions.CurrentUserOnly);

    private static void WritePayload(Stream stream, IReadOnlyList<string> paths)
    {
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        writer.WriteStartArray("paths");
        foreach (var path in paths)
            writer.WriteStringValue(path);
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
    }

    private static IReadOnlyList<string> ReadPayload(Stream stream)
    {
        try
        {
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Array)
                return [];
            var result = new List<string>();
            foreach (var path in paths.EnumerateArray())
            {
                if (path.ValueKind != JsonValueKind.String)
                    continue;
                var value = path.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(value);
            }
            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void TryWakeListener()
    {
        try
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.Out,
                PipeOptions.CurrentUserOnly);
            // Disposal must never wait for the listener. It is a background thread, so if it is between
            // pipe instances the process can simply finish without waking it.
            pipe.Connect(0);
        }
        catch (TimeoutException)
        {
            // The listener is between pipe instances; it is a background thread and needs no synchronous wake.
        }
        catch (IOException)
        {
            // The listener already closed its pipe, so there is nothing left to wake during disposal.
        }
    }
}
