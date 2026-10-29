using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;

namespace LunaPlayer.Discord;

internal interface IRpcClient : IAsyncDisposable
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync(string clientId, CancellationToken token);
    Task<bool> SetActivityAsync(string? activity, CancellationToken token);
}

/// <summary>Local Discord IPC only: no account token, SDK, native DLL, or Python runtime.
/// A continuous reader answers pings even when the activity has not changed.</summary>
internal sealed class RpcClient : IRpcClient
{
    private const int MaxFrameBytes = 64 * 1024;
    private readonly Func<int, string> _pipeName;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private Stream? _stream;
    private Task? _reader;
    private Pending? _pending;
    private volatile bool _connected;
    private sealed record Pending(string Nonce, TaskCompletionSource<bool> Reply);

    internal RpcClient(Func<int, string>? pipeName = null) => _pipeName = pipeName ?? (i => $"discord-ipc-{i}");
    public bool IsConnected => _connected;

    public async Task<bool> ConnectAsync(string clientId, CancellationToken token)
    {
        for (var i = 0; i < 10; i++)
        {
            token.ThrowIfCancellationRequested();
            var pipe = new NamedPipeClientStream(".", _pipeName(i), PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await pipe.ConnectAsync(100, token).ConfigureAwait(false);
                _stream = pipe;
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                deadline.CancelAfter(TimeSpan.FromSeconds(2));
                using var hello = new MemoryStream();
                using (var json = new Utf8JsonWriter(hello))
                {
                    json.WriteStartObject();
                    json.WriteNumber("v", 1);
                    json.WriteString("client_id", clientId);
                    json.WriteEndObject();
                }
                await WriteAsync(0, hello.ToArray(), deadline.Token).ConfigureAwait(false);
                while (true)
                {
                    var (op, body) = await ReadAsync(deadline.Token).ConfigureAwait(false);
                    if (op == 3) { await WriteAsync(4, body, deadline.Token).ConfigureAwait(false); continue; }
                    if (op != 1) throw new IOException("Discord did not accept the handshake.");
                    using var document = JsonDocument.Parse(body);
                    var root = document.RootElement;
                    if (Field(root, "cmd") != "DISPATCH" || Field(root, "evt") != "READY")
                        throw new IOException("Discord application handshake was rejected.");
                    _connected = true;
                    _reader = ReadLoopAsync();
                    return true;
                }
            }
            catch (Exception e) when (e is IOException or TimeoutException or OperationCanceledException or JsonException or UnauthorizedAccessException)
            {
                pipe.Dispose();
                _stream = null;
                token.ThrowIfCancellationRequested();
            }
        }
        return false;
    }

    // Single caller (the publishing worker). The reader can concurrently write only PONG frames.
    public async Task<bool> SetActivityAsync(string? activity, CancellationToken token)
    {
        if (!IsConnected) throw new IOException("Discord is disconnected.");
        var pending = new Pending(Guid.NewGuid().ToString("N"), new(TaskCreationOptions.RunContinuationsAsynchronously));
        Volatile.Write(ref _pending, pending);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            using var stream = new MemoryStream();
            using (var json = new Utf8JsonWriter(stream))
            {
                json.WriteStartObject();
                json.WriteString("cmd", "SET_ACTIVITY");
                json.WriteStartObject("args");
                json.WriteNumber("pid", Environment.ProcessId);
                json.WritePropertyName("activity");
                if (activity is null) json.WriteNullValue(); else json.WriteRawValue(activity);
                json.WriteEndObject();
                json.WriteString("nonce", pending.Nonce);
                json.WriteEndObject();
            }
            await WriteAsync(1, stream.ToArray(), deadline.Token).ConfigureAwait(false);
            return await pending.Reply.Task.WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch
        {
            _connected = false;
            throw;
        }
        finally { Interlocked.CompareExchange(ref _pending, null, pending); }
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                var (op, body) = await ReadAsync(_lifetime.Token).ConfigureAwait(false);
                if (op == 3) { await WriteAsync(4, body, _lifetime.Token).ConfigureAwait(false); continue; }
                if (op == 4) continue;
                if (op != 1) throw new IOException("Discord closed the IPC connection.");
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                var pending = Volatile.Read(ref _pending);
                if (pending is null || Field(root, "nonce") != pending.Nonce) continue;
                // A validation rejection is not a broken pipe. Keep the connection and report failure.
                pending.Reply.TrySetResult(Field(root, "evt") != "ERROR");
            }
        }
        catch (Exception e) when (e is IOException or OperationCanceledException or JsonException or ObjectDisposedException)
        {
            _connected = false;
            Volatile.Read(ref _pending)?.Reply.TrySetException(new IOException("Discord IPC connection ended.", e));
        }
    }

    private static string? Field(JsonElement root, string name)
        => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private async Task<(int Op, byte[] Body)> ReadAsync(CancellationToken token)
    {
        var stream = _stream ?? throw new IOException("Discord IPC is not open.");
        var header = new byte[8];
        await stream.ReadExactlyAsync(header, token).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));
        if (length < 0 || length > MaxFrameBytes) throw new IOException("Invalid Discord IPC frame size.");
        var body = new byte[length];
        await stream.ReadExactlyAsync(body, token).ConfigureAwait(false);
        return (BinaryPrimitives.ReadInt32LittleEndian(header), body);
    }

    private async Task WriteAsync(int op, byte[] body, CancellationToken token)
    {
        await _writeLock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var stream = _stream ?? throw new IOException("Discord IPC is not open.");
            var frame = new byte[8 + body.Length];
            BinaryPrimitives.WriteInt32LittleEndian(frame, op);
            BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4), body.Length);
            body.CopyTo(frame, 8);
            await stream.WriteAsync(frame, token).ConfigureAwait(false);
        }
        finally { _writeLock.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        _connected = false;
        _lifetime.Cancel();
        _stream?.Dispose();
        if (_reader is not null) await _reader.ConfigureAwait(false);
        _lifetime.Dispose();
        _writeLock.Dispose();
    }
}
