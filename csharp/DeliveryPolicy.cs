namespace Vireon;

/// <summary>
/// Per-stream delivery semantics.
/// </summary>
public enum DeliveryPolicy
{
    /// <summary>Reliable, in-order delivery (default).</summary>
    ReliableOrdered = 0,
    /// <summary>Reliable but order not guaranteed.</summary>
    ReliableUnordered = 1,
    /// <summary>Drop oldest when behind (soft real-time).</summary>
    RealtimeDropOld = 2,
    /// <summary>Only latest value is kept.</summary>
    LatestOnly = 3,
}
