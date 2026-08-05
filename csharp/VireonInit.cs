namespace Vireon;

/// <summary>
/// Lazy one-time initialization of the native tokio runtime.
/// The first P/Invoke automatically loads the library; this ensures
/// vireon_init() is called exactly once before any other API.
/// </summary>
internal static class VireonInit
{
    private static readonly Lazy<int> _init = new(() =>
    {
        return Native.vireon_init();
    });

    internal static void Ensure()
    {
        _ = _init.Value;
    }
}
