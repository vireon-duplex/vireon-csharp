namespace Vireon;

/// <summary>
/// Consumer group subscription — messages are load-balanced across
/// consumers in the same group.
/// </summary>
public class GroupSubscription : IDisposable
{
    private nint _handle;
    private volatile bool _disposed;

    internal GroupSubscription(nint handle) { _handle = handle; }

    internal nint Handle
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _handle;
        }
    }

    /// <summary>
    /// Receive next message (blocking). Returns null when closed.
    /// </summary>
    public Task<Message?> RecvAsync() =>
        Task.Run<Message?>(() =>
        {
            var msg = new Native.VireonMessage();
            var rc = Native.vireon_group_sub_recv(Handle, ref msg);
            return rc switch
            {
                0 => Message.FromNative(ref msg),
                1 => null,
                _ => throw new VireonException("recv error"),
            };
        });

    /// <summary>
    /// Receive a batch of up to <paramref name="maxCount"/> messages.
    /// </summary>
    public Task<Message[]> RecvBatchAsync(int maxCount = 256) =>
        Task.Run(() =>
        {
            var batch = new Native.VireonMsgBatch();
            var rc = Native.vireon_group_sub_recv_batch(Handle, maxCount, ref batch);
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
                Native.vireon_group_sub_close(_handle);
                _handle = 0;
            }
        }
        GC.SuppressFinalize(this);
    }

    ~GroupSubscription()
    {
        if (_handle != 0)
        {
            Native.vireon_group_sub_close(_handle);
            _handle = 0;
        }
    }
}
