namespace Vireon;

/// <summary>
/// Pool of N QUIC connections for publish-side multiplexing.
/// Publishes round-robin across members; try_publish fails over across members.
/// </summary>
public class ClientPool : IDisposable
{
    private nint _handle;
    private volatile bool _disposed;

    internal ClientPool(nint handle)
    {
        _handle = handle;
    }

    internal nint Handle
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _handle;
        }
    }

    /// <summary>Number of connections in the pool.</summary>
    public int Len() => Native.vireon_pool_len(Handle);

    /// <summary>Get pool member by index (caller must Dispose).</summary>
    public Client Member(int idx)
    {
        var h = Native.vireon_pool_member(Handle, idx);
        return new Client(Error.CheckHandle(h));
    }

    /// <summary>Publish via round-robin (blocking).</summary>
    public Task PublishAsync(string topic, byte[] payload) =>
        Task.Run(() => Error.Check(Native.vireon_pool_publish(
            Handle, topic, payload, (nuint)payload.Length)));

    /// <summary>Fire-and-forget publish via round-robin.</summary>
    public void TryPublish(string topic, byte[] payload) =>
        Error.Check(Native.vireon_pool_try_publish(
            Handle, topic, payload, (nuint)payload.Length));

    /// <summary>Total pending bytes across all pool members.</summary>
    public ulong PendingBytes() => Native.vireon_pool_pending_bytes(Handle);

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_handle != 0)
            {
                _ = Native.vireon_pool_close(_handle);
                _handle = 0;
            }
        }
        GC.SuppressFinalize(this);
    }

    ~ClientPool()
    {
        if (_handle != 0)
        {
            _ = Native.vireon_pool_close(_handle);
            _handle = 0;
        }
    }
}
