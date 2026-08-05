namespace Vireon;

/// <summary>
/// Client certificate + key for mutual TLS authentication.
/// </summary>
public record ClientIdentity
{
    public string CertPath { get; init; }
    public string KeyPath { get; init; }

    public ClientIdentity(string certPath, string keyPath)
    {
        CertPath = certPath;
        KeyPath = keyPath;
    }
}
