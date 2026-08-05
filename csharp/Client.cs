namespace Vireon;

/// <summary>
/// A handle to a Vireon QUIC connection.
///
/// All async methods wrap blocking FFI calls in <see cref="Task.Run"/>
/// to avoid blocking the caller's thread.
///
/// Construct via <see cref="ClientBuilder.Connect"/>.
/// </summary>
public class Client : IDisposable
{
    private nint _handle;
    private volatile bool _disposed;

    internal Client(nint handle)
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

    // ── Publish ─────────────────────────────────────────────────────

    /// <summary>Publish <paramref name="payload"/> to <paramref name="topic"/>.</summary>
    public Task PublishAsync(string topic, byte[] payload) =>
        Task.Run(() => Error.Check(Native.vireon_client_publish(
            Handle, topic, payload, (nuint)payload.Length)));

    /// <summary>Fire-and-forget publish — returns immediately.</summary>
    public void TryPublish(string topic, byte[] payload) =>
        Error.Check(Native.vireon_client_try_publish(
            Handle, topic, payload, (nuint)payload.Length));

    // ── Subscribe ───────────────────────────────────────────────────

    /// <summary>Subscribe to a topic pattern (<c>*</c> = single-segment wildcard).</summary>
    public Task<Subscription> SubscribeAsync(string pattern) =>
        Task.Run(() =>
        {
            var h = Error.CheckHandle(Native.vireon_client_subscribe(Handle, pattern));
            return new Subscription(h);
        });

    /// <summary>Remove a previously-registered subscription.</summary>
    public Task UnsubscribeAsync(string pattern) =>
        Task.Run(() => Error.Check(Native.vireon_client_unsubscribe(Handle, pattern)));

    // ── Stream ──────────────────────────────────────────────────────

    /// <summary>
    /// Open a dedicated QUIC stream with its own delivery policy.
    /// </summary>
    /// <param name="policy">delivery policy for this stream</param>
    /// <param name="topic">optional topic scope (null = catch-all)</param>
    public Task<StreamHandle> OpenStreamAsync(DeliveryPolicy policy, string? topic = null) =>
        Task.Run(() =>
        {
            var h = Error.CheckHandle(Native.vireon_client_open_stream(
                Handle, (int)policy, topic));
            return new StreamHandle(h);
        });

    // ── Consumer Group ──────────────────────────────────────────────

    /// <summary>Join a consumer group on <paramref name="topic"/> as <paramref name="consumer"/>.</summary>
    public Task<GroupSubscription> SubscribeGroupAsync(
        string topic, string group, string consumer) =>
        Task.Run(() =>
        {
            var h = Error.CheckHandle(Native.vireon_client_subscribe_group(
                Handle, topic, group, consumer));
            return new GroupSubscription(h);
        });

    /// <summary>Leave a consumer group.</summary>
    public Task LeaveGroupAsync(string topic, string group, string consumer) =>
        Task.Run(() => Error.Check(Native.vireon_client_leave_group(
            Handle, topic, group, consumer)));

    // ── RPC ─────────────────────────────────────────────────────────

    /// <summary>
    /// Request/reply RPC over pub/sub.
    /// </summary>
    public Task<Message> RpcAsync(
        string reqTopic, byte[] payload, string replyTopic, double timeoutSecs) =>
        Task.Run(() =>
        {
            var msg = new Native.VireonMessage();
            Error.Check(Native.vireon_client_rpc(
                Handle, reqTopic, payload, (nuint)payload.Length,
                replyTopic, timeoutSecs, ref msg));
            return Message.FromNative(ref msg);
        });

    // ── Migration ───────────────────────────────────────────────────

    /// <summary>Trigger QUIC connection migration by rebinding the UDP socket.</summary>
    public Task MigrateAsync(string bindAddr) =>
        Task.Run(() => Error.Check(Native.vireon_client_migrate(Handle, bindAddr)));

    // ── Metrics ─────────────────────────────────────────────────────

    /// <summary>Total bytes buffered in transport awaiting flow-control window.</summary>
    public ulong PendingBytes() => Native.vireon_client_pending_bytes(Handle);

    // ── Dispose ─────────────────────────────────────────────────────

    /// <summary>Close the connection and reclaim native resources.</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_handle != 0)
            {
                _ = Native.vireon_client_close(_handle);
                _handle = 0;
            }
        }
        GC.SuppressFinalize(this);
    }

    ~Client()
    {
        if (_handle != 0)
        {
            _ = Native.vireon_client_close(_handle);
            _handle = 0;
        }
    }
}
