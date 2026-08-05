namespace Vireon;

/// <summary>
/// TLS verification strategy for the QUIC handshake.
/// </summary>
public abstract record TlsVerify
{
    internal abstract int Mode { get; }
    internal abstract string? Path { get; }

    /// <summary>
    /// Trust-on-first-use (default). The first certificate seen for a host
    /// becomes the trusted one; subsequent changes raise an error.
    /// </summary>
    public static TlsVerify Tofu() => new TofuMode();
    private sealed record TofuMode : TlsVerify
    {
        internal override int Mode => 0;
        internal override string? Path => null;
    }

    /// <summary>
    /// Accept any certificate — development only, no verification.
    /// </summary>
    public static TlsVerify DangerAcceptInvalid() => new DangerMode();
    private sealed record DangerMode : TlsVerify
    {
        internal override int Mode => 1;
        internal override string? Path => null;
    }

    /// <summary>
    /// Verify against a CA bundle file (PEM or DER).
    /// </summary>
    public static TlsVerify Strict(string caPath) => new StrictMode(caPath);
    private sealed record StrictMode(string CaPath) : TlsVerify
    {
        internal override int Mode => 2;
        internal override string? Path => CaPath;
    }

    /// <summary>
    /// Pin a single DER-encoded certificate.
    /// </summary>
    public static TlsVerify Pinned(string certDerPath) => new PinnedMode(certDerPath);
    private sealed record PinnedMode(string CertDerPath) : TlsVerify
    {
        internal override int Mode => 3;
        internal override string? Path => CertDerPath;
    }
}
