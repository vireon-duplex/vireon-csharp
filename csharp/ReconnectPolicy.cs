namespace Vireon;

/// <summary>
/// Automatic reconnection policy with exponential backoff.
/// </summary>
public record ReconnectPolicy
{
    /// <summary>Max reconnect attempts (0 = disabled).</summary>
    public int MaxAttempts { get; init; } = 0;
    /// <summary>Initial backoff in seconds.</summary>
    public double InitialBackoffSecs { get; init; } = 0.5;
    /// <summary>Maximum backoff cap in seconds.</summary>
    public double MaxBackoffSecs { get; init; } = 10.0;

    /// <summary>Disable reconnection (fail immediately on disconnect).</summary>
    public static ReconnectPolicy Disabled() => new()
    {
        MaxAttempts = 0,
    };

    /// <summary>
    /// Exponential backoff: starts at <paramref name="initialSecs"/>,
    /// doubles each attempt up to <paramref name="maxSecs"/>, for at most
    /// <paramref name="maxAttempts"/> attempts. Subscriptions are restored.
    /// </summary>
    public static ReconnectPolicy Exponential(
        int maxAttempts, double initialSecs = 0.5, double maxSecs = 10.0) => new()
    {
        MaxAttempts = maxAttempts,
        InitialBackoffSecs = initialSecs,
        MaxBackoffSecs = maxSecs,
    };
}
