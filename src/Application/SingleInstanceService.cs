using System.IO.Pipes;
using System.Text.Json;

namespace LunaPlayer.Application;

internal sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "Local\\LunaPlayer.MainInstance";
    private const string PipeName = "LunaPlayer.OpenPaths";

    private readonly Mutex? _mutex;
    private readonly bool _ownsMutex;
    private Thread? _listenerThread;
    private Action<IReadOnlyList<string>>? _receiver;
    private volatile bool _stopping;
    private bool _disposed;

    internal SingleInstanceService()
    {
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
        if (!IsPrimary || _listenerThread is not null)
            return;
        _receiver = receiver;
        _listenerThread = new Thread(Listen)
        {
            IsBackground = true,
            Name = "LunaPlayer.OpenPaths",
        };
        _listenerThread.Start();
    }

    internal void ForwardPaths(IEnumerable<string> paths)
    {
        var values = paths.Where(static path => !string.IsNullOrWhiteSpace(path)).ToArray();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(
                    ".",
                    PipeName,
                    PipeDirection.Out,
                    PipeOptions.CurrentUserOnly);
                pipe.Connect(100);
                WritePayload(pipe, values);
                return;
            }
            catch (TimeoutException)
            {
            }
            catch (IOException)
            {
            }
            Thread.Sleep(50);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _stopping = true;
        if (_listenerThread is not null)
        {
            TryWakeListener();
            _listenerThread.Join();
            _listenerThread = null;
        }
        if (_ownsMutex)
        {
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }
        _mutex?.Dispose();
    }

    private void Listen()
    {
        while (!_stopping)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.CurrentUserOnly);
                pipe.WaitForConnection();
                if (_stopping)
                    return;
                _receiver?.Invoke(ReadPayload(pipe));
            }
            catch (IOException) when (!_stopping)
            {
            }
            catch (UnauthorizedAccessException) when (!_stopping)
            {
            }
        }
    }

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
            pipe.Connect(100);
        }
        catch (TimeoutException)
        {
        }
        catch (IOException)
        {
        }
    }
}
