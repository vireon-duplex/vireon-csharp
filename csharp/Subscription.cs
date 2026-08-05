namespace Vireon;

/// <summary>
/// Subscription to a topic pattern.
/// <para>
/// Use <see cref="RecvAsync"/> for a single message or
/// <see cref="RecvBatchAsync"/> for up to 256 messages per FFI round-trip.
/// </para>
/// </summary>
public class Subscription : IDisposable
{
    private nint _handle;
    private volatile bool _disposed;

    internal Subscription(nint handle) { _handle = handle; }

    internal nint Handle
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _handle;
        }
    }

    /// <summary>
    /// Receive next message (blocking until one arrives).
    /// Returns null when the subscription is closed.
    /// </summary>
    public Task<Message?> RecvAsync() =>
        Task.Run<Message?>(() =>
        {
            var msg = new Native.VireonMessage();
            var rc = Native.vireon_sub_recv(Handle, ref msg);
            return rc switch
            {
                0 => Message.FromNative(ref msg),
                1 => null,
                _ => throw new VireonException("recv error"),
            };
        });

    /// <summary>
    /// Receive a batch of up to <paramref name="maxCount"/> messages.
    /// Blocks for the first, then drains available.
    /// </summary>
    public Task<Message[]> RecvBatchAsync(int maxCount = 256) =>
        Task.Run(() =>
        {
            var batch = new Native.VireonMsgBatch();
            var rc = Native.vireon_sub_recv_batch(Handle, maxCount, ref batch);
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

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_handle != 0)
            {
                Native.vireon_sub_close(_handle);
                _handle = 0;
            }
        }
        GC.SuppressFinalize(this);
    }

    ~Subscription()
    {
        if (_handle != 0)
        {
            Native.vireon_sub_close(_handle);
            _handle = 0;
        }
    }
}
