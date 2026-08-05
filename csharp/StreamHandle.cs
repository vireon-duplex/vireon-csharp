namespace Vireon;

/// <summary>
/// A dedicated QUIC stream with its own delivery policy.
/// </summary>
public class StreamHandle : IDisposable
{
    private nint _handle;
    private volatile bool _disposed;

    internal StreamHandle(nint handle) { _handle = handle; }

    internal nint Handle
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _handle;
        }
    }

    /// <summary>Receive next message (blocking). Returns null when closed.</summary>
    public Task<Message?> RecvAsync() =>
        Task.Run<Message?>(() =>
        {
            var msg = new Native.VireonMessage();
            var rc = Native.vireon_stream_recv(Handle, ref msg);
            return rc switch
            {
                0 => Message.FromNative(ref msg),
                1 => null,
                _ => throw new VireonException("recv error"),
            };
        });

    /// <summary>Receive a batch of up to <paramref name="maxCount"/> messages.</summary>
    public Task<Message[]> RecvBatchAsync(int maxCount = 256) =>
        Task.Run(() =>
        {
            var batch = new Native.VireonMsgBatch();
            var rc = Native.vireon_stream_recv_batch(Handle, maxCount, ref batch);
            if (rc == -1)
            {
                throw new VireonException("recv_batch error");
            }
            if (rc == 1 || batch.Count == 0)
            {
                return Array.Empty<Message>();
            }
            return Message.FromBatch(ref batch);
        });

    /// <summary>Publish on this stream (blocking).</summary>
    public Task PublishAsync(string topic, byte[] payload) =>
        Task.Run(() => Error.Check(Native.vireon_stream_publish(
            Handle, topic, payload, (nuint)payload.Length)));

    /// <summary>Fire-and-forget publish on this stream.</summary>
    public void TryPublish(string topic, byte[] payload) =>
        Error.Check(Native.vireon_stream_try_publish(
            Handle, topic, payload, (nuint)payload.Length));

    /// <summary>The QUIC stream ID.</summary>
    public ulong StreamId() => Native.vireon_stream_id(Handle);

    /// <summary>Bytes buffered in this stream's send queue.</summary>
    public ulong PendingBytes() => Native.vireon_stream_pending_bytes(Handle);

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_handle != 0)
            {
                Native.vireon_stream_close(_handle);
                _handle = 0;
            }
        }
        GC.SuppressFinalize(this);
    }

    ~StreamHandle()
    {
        if (_handle != 0)
        {
            Native.vireon_stream_close(_handle);
            _handle = 0;
        }
    }
}
