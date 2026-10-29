using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using LunaPlayer.Discord;
using LunaPlayer.Tests.Infrastructure;

namespace LunaPlayer.Tests.Discord;

[TestClass, TestCategory("Unit")]
public sealed class RpcTests
{
    private static NamedPipeServerStream Server(string name) => new(name, PipeDirection.InOut, 1,
        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

    private static async Task<(int Op, byte[] Body)> Read(Stream stream, CancellationToken token)
    {
        var header = new byte[8];
        await stream.ReadExactlyAsync(header, token);
        var bytes = new byte[BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4))];
        await stream.ReadExactlyAsync(bytes, token);
        return (BinaryPrimitives.ReadInt32LittleEndian(header), bytes);
    }

    private static async Task Write(Stream stream, int op, string json, CancellationToken token, bool fragmented = false)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var frame = new byte[8 + body.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame, op);
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4), body.Length);
        body.CopyTo(frame, 8);
        if (fragmented)
            foreach (var b in frame) await stream.WriteAsync(new byte[] { b }, token);
        else await stream.WriteAsync(frame, token);
    }

    private static async Task Handshake(NamedPipeServerStream server, CancellationToken token)
    {
        await server.WaitForConnectionAsync(token);
        var frame = await Read(server, token);
        Assert.AreEqual(0, frame.Op);
        using var hello = JsonDocument.Parse(frame.Body);
        Assert.AreEqual(1, hello.RootElement.GetProperty("v").GetInt32());
        Assert.AreEqual("123456789012345678", hello.RootElement.GetProperty("client_id").GetString());
        await Write(server, 1, "{\"cmd\":\"DISPATCH\",\"evt\":\"READY\"}", token, fragmented: true);
    }

    [TestMethod]
    public async Task FragmentedFramesPingsNoncesErrorsAndClearAreHandled()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var token = timeout.Token;
        var name = "luna-discord-test-" + Guid.NewGuid().ToString("N");
        await using var server = Server(name);
        var pong = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverTask = Task.Run(async () =>
        {
            await Handshake(server, token);
            // No SET_ACTIVITY is in flight: the reader must still answer this ping.
            await Write(server, 3, "{\"ping\":123}", token);
            var response = await Read(server, token);
            Assert.AreEqual(4, response.Op);
            Assert.AreEqual("{\"ping\":123}", Encoding.UTF8.GetString(response.Body));
            pong.SetResult();
            for (var i = 0; i < 3; i++)
            {
                var request = await Read(server, token);
                Assert.AreEqual(1, request.Op);
                using var document = JsonDocument.Parse(request.Body);
                var root = document.RootElement;
                Assert.AreEqual("SET_ACTIVITY", root.GetProperty("cmd").GetString());
                Assert.AreEqual(Environment.ProcessId, root.GetProperty("args").GetProperty("pid").GetInt32());
                if (i == 2) Assert.AreEqual(JsonValueKind.Null, root.GetProperty("args").GetProperty("activity").ValueKind);
                var nonce = root.GetProperty("nonce").GetString();
                // A stale or unsolicited frame cannot satisfy this request.
                await Write(server, 1, "{\"nonce\":\"wrong\",\"evt\":\"ERROR\"}", token);
                var evt = i == 0 ? "\"ERROR\"" : "null";
                await Write(server, 1, $"{{\"cmd\":\"SET_ACTIVITY\",\"evt\":{evt},\"nonce\":\"{nonce}\"}}", token, fragmented: true);
            }
        }, token);
        await using var client = new RpcClient(_ => name);
        Assert.IsTrue(await client.ConnectAsync("123456789012345678", token));
        await pong.Task.WaitAsync(token);
        Assert.IsFalse(await client.SetActivityAsync("{\"details\":\"test\"}", token));
        Assert.IsTrue(client.IsConnected);
        Assert.IsTrue(await client.SetActivityAsync("{\"details\":\"test\"}", token));
        Assert.IsTrue(await client.SetActivityAsync(null, token));
        await serverTask;
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(65537)]
    public async Task InvalidFrameSizesDisconnectWithoutAllocatingPayload(int length)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var name = "luna-discord-test-" + Guid.NewGuid().ToString("N");
        await using var server = Server(name);
        var sendBad = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverTask = Task.Run(async () =>
        {
            await Handshake(server, timeout.Token);
            await sendBad.Task.WaitAsync(timeout.Token);
            var header = new byte[8];
            BinaryPrimitives.WriteInt32LittleEndian(header, 1);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), length);
            await server.WriteAsync(header, timeout.Token);
        });
        await using var client = new RpcClient(_ => name);
        Assert.IsTrue(await client.ConnectAsync("123456789012345678", timeout.Token));
        sendBad.SetResult();
        await NativeHost.Until(() => !client.IsConnected);
        await serverTask;
    }

    [TestMethod]
    public async Task PendingCommandCanBeCancelledWithoutBlockingShutdown()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var name = "luna-discord-test-" + Guid.NewGuid().ToString("N");
        await using var server = Server(name);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverTask = Task.Run(async () =>
        {
            await Handshake(server, timeout.Token);
            await Read(server, timeout.Token);
            received.SetResult(); // Deliberately never acknowledge.
        });
        await using var client = new RpcClient(_ => name);
        Assert.IsTrue(await client.ConnectAsync("123456789012345678", timeout.Token));
        using var cancel = new CancellationTokenSource();
        var send = client.SetActivityAsync("{}", cancel.Token);
        await received.Task.WaitAsync(timeout.Token);
        cancel.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => send);
        Assert.IsFalse(client.IsConnected);
        await serverTask;
    }
}
