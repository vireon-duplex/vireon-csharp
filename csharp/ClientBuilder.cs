namespace Vireon;

/// <summary>
/// Fluent builder for <see cref="Client"/>.
///
/// <code>
/// var client = new ClientBuilder("127.0.0.1:4433")
///     .TlsVerify(TlsVerify.DangerAcceptInvalid())
///     .Connect();
/// </code>
///
/// Call <see cref="Connect"/> for a blocking connect, or
/// <see cref="ConnectAsync"/> for async. The builder is reusable.
/// </summary>
public class ClientBuilder
{
    private readonly string _addr;
    private TlsVerify _tls = null!;
    private string? _sni;
    private ulong _maxMsgSize = 1024 * 1024;
    private ulong _subscriberBuffer = 65536;
    private ulong _cmdChannelCap = 1024;
    private double _idleTimeoutSecs = 60.0;
    private ReconnectPolicy _reconnect = ReconnectPolicy.Disabled();
    private ClientIdentity? _identity;

    public ClientBuilder(string addr)
    {
        _addr = addr;
        _tls = global::Vireon.TlsVerify.Tofu();
    }

    public ClientBuilder TlsVerify(Vireon.TlsVerify v) { _tls = v; return this; }
    public ClientBuilder Sni(string sni) { _sni = sni; return this; }
    public ClientBuilder ClientIdentity(ClientIdentity id) { _identity = id; return this; }
    public ClientBuilder Reconnect(ReconnectPolicy p) { _reconnect = p; return this; }
    public ClientBuilder MaxMessageSize(ulong n) { _maxMsgSize = n; return this; }
    public ClientBuilder SubscriberBuffer(ulong n) { _subscriberBuffer = n; return this; }
    public ClientBuilder CmdChannelCap(ulong n) { _cmdChannelCap = n; return this; }
    public ClientBuilder MaxIdleTimeout(double secs) { _idleTimeoutSecs = secs; return this; }

    // ── package-private getters for ClientPool ─────────────────────

    internal string Addr => _addr;
    internal int TlsMode => _tls.Mode;
    internal string? TlsPath => _tls.Path;
    internal string? SniValue => _sni;
    internal ulong MaxMsgSize => _maxMsgSize;
    internal ulong SubscriberBufferValue => _subscriberBuffer;
    internal ulong CmdChannelCapValue => _cmdChannelCap;
    internal double IdleTimeoutSecs => _idleTimeoutSecs;
    internal int ReconnectEnabled => _reconnect.MaxAttempts > 0 ? 1 : 0;
    internal int ReconnectMaxAttempts => _reconnect.MaxAttempts;
    internal double ReconnectInitialSecs => _reconnect.InitialBackoffSecs;
    internal double ReconnectMaxSecs => _reconnect.MaxBackoffSecs;
    internal string? IdentityCert => _identity?.CertPath;
    internal string? IdentityKey => _identity?.KeyPath;

    /// <summary>
    /// Establish the QUIC connection (blocking).
    /// </summary>
    public Client Connect()
    {
        VireonInit.Ensure();
        var handle = Native.vireon_connect(
            _addr, _tls.Mode, _tls.Path, _sni,
            _maxMsgSize, _subscriberBuffer, _cmdChannelCap, _idleTimeoutSecs,
            ReconnectEnabled, _reconnect.MaxAttempts,
            _reconnect.InitialBackoffSecs, _reconnect.MaxBackoffSecs,
            _identity?.CertPath, _identity?.KeyPath);
        return new Client(Error.CheckHandle(handle));
    }

    /// <summary>
    /// Async connect — runs on a ThreadPool thread.
    /// </summary>
    public Task<Client> ConnectAsync() => Task.Run(Connect);

    /// <summary>
    /// Connect a pool of N clients sharing the same config.
    /// </summary>
    public ClientPool ConnectPool(int n)
    {
        VireonInit.Ensure();
        var handle = Native.vireon_pool_connect(
            _addr, _tls.Mode, _tls.Path, _sni,
            _maxMsgSize, _subscriberBuffer, _cmdChannelCap, _idleTimeoutSecs,
            ReconnectEnabled, _reconnect.MaxAttempts,
            _reconnect.InitialBackoffSecs, _reconnect.MaxBackoffSecs,
            _identity?.CertPath, _identity?.KeyPath,
            n);
        return new ClientPool(Error.CheckHandle(handle));
    }

    public Task<ClientPool> ConnectPoolAsync(int n) => Task.Run(() => ConnectPool(n));
}
