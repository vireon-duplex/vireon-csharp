using System.Runtime.InteropServices;

namespace Vireon;

/// <summary>
/// Error checking helper. Native FFI returns:
///   0  = success
///  -1  = error (call vireon_last_error to get message)
///   1  = closed/timeout (not an error — caller handles)
/// </summary>
internal static class Error
{
    /// <summary>
    /// Check a return code; throw VireonException if negative.
    /// </summary>
    internal static void Check(int rc)
    {
        if (rc < 0)
        {
            throw new VireonException(FetchMessage());
        }
    }

    /// <summary>
    /// Check a handle returned from connect/subscribe/openStream.
    /// Handle 0 means error. Throw with last error message.
    /// </summary>
    internal static nint CheckHandle(nint handle)
    {
        if (handle == 0)
        {
            throw new VireonException(FetchMessage());
        }
        return handle;
    }

    private static string FetchMessage()
    {
        var ptr = Native.vireon_last_error();
        if (ptr == IntPtr.Zero)
        {
            return "unknown error";
        }
        return Marshal.PtrToStringAnsi(ptr) ?? "unknown error";
    }
}
