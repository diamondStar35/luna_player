using System.Net;
using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic.Media;

/// <summary>The response owns both the network stream and its cancellation lifetime.</summary>
internal sealed class Response : IDisposable, IAsyncDisposable
{
    private readonly Session.Lease _lease;
    private readonly Stream _body;
    private int _disposed;
    internal Response(Session.Lease lease, Stream body)
    {
        _lease = lease; _body = body; Stream = new Reader(this);
    }
    internal Stream Stream { get; }
    internal long? Length => _lease.Response.Content.Headers.ContentLength;
    internal string? ContentType => _lease.Response.Content.Headers.ContentType?.MediaType;
    internal bool SupportsRanges => _lease.Response.Headers.AcceptRanges.Contains("bytes");
    internal HttpStatusCode StatusCode => _lease.Response.StatusCode;
    internal long? RangeStart => _lease.Response.Content.Headers.ContentRange?.From;
    internal string? SuggestedFileName
    {
        get
        {
            var name = _lease.Response.Content.Headers.ContentDisposition?.FileNameStar
                ?? _lease.Response.Content.Headers.ContentDisposition?.FileName;
            return name is null ? null : Path.GetFileName(name.Trim('"').Replace('\\', '/'));
        }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try { _lease.Timeout.Cancel(); _body.Dispose(); }
        finally { _lease.Dispose(); }
    }
    public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }

    private sealed class Reader(Response owner) : Stream
    {
        public override bool CanRead => Volatile.Read(ref owner._disposed) == 0 && owner._body.CanRead;
        public override bool CanSeek => owner._body.CanSeek;
        public override bool CanWrite => false;
        public override long Length => owner._body.Length;
        public override long Position { get => owner._body.Position; set => owner._body.Position = value; }
        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref owner._disposed) != 0, owner);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, owner._lease.Token);
            return await owner._body.ReadAsync(buffer, linked.Token).ConfigureAwait(false);
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override long Seek(long offset, SeekOrigin origin) => owner._body.Seek(offset, origin);
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) owner.Dispose(); base.Dispose(disposing); }
    }
}
