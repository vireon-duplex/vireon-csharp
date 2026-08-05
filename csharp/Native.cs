using System.Runtime.InteropServices;

namespace Vireon;

/// <summary>
/// All P/Invoke declarations for the native vireon_csharp library.
/// </summary>
internal static class Native
{
    private const string Lib = "vireon_csharp";

    // ── Repr(C) structs ──────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    internal struct VireonMessage
    {
        public IntPtr Topic;       // *const c_char
        public IntPtr Payload;     // *const u8
        public nuint PayloadLen;
        public ulong Seq;
        public ulong StreamId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VireonMsgBatch
    {
        public IntPtr Msgs;        // *mut VireonMessage
        public nuint Count;
    }

    // ── Init + error ────────────────────────────────────────────────

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_init();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr vireon_last_error();

    // ── Message memory management ───────────────────────────────────

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void vireon_msg_free(ref VireonMessage msg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void vireon_batch_free(ref VireonMsgBatch batch);

    // ── Connect ─────────────────────────────────────────────────────

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint vireon_connect(
        string addr, int tlsMode, string? tlsPath, string? sni,
        ulong maxMsgSize, ulong subscriberBuffer, ulong cmdChannelCap,
        double idleTimeoutSecs,
        int reconnectEnabled, int reconnectMaxAttempts,
        double reconnectInitialSecs, double reconnectMaxSecs,
        string? identityCert, string? identityKey);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint vireon_pool_connect(
        string addr, int tlsMode, string? tlsPath, string? sni,
        ulong maxMsgSize, ulong subscriberBuffer, ulong cmdChannelCap,
        double idleTimeoutSecs,
        int reconnectEnabled, int reconnectMaxAttempts,
        double reconnectInitialSecs, double reconnectMaxSecs,
        string? identityCert, string? identityKey,
        int n);

    // ── Client ──────────────────────────────────────────────────────

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_client_close(nint handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_client_publish(
        nint handle, string topic, byte[] payload, nuint payloadLen);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_client_try_publish(
        nint handle, string topic, byte[] payload, nuint payloadLen);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint vireon_client_subscribe(
        nint handle, string pattern);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_client_unsubscribe(
        nint handle, string pattern);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint vireon_client_open_stream(
        nint handle, int policyOrdinal, string? topic);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint vireon_client_subscribe_group(
        nint handle, string topic, string group, string consumer);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_client_leave_group(
        nint handle, string topic, string group, string consumer);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_client_rpc(
        nint handle, string reqTopic, byte[] payload, nuint payloadLen,
        string replyTopic, double timeoutSecs, ref VireonMessage outMsg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_client_migrate(
        nint handle, string bindAddr);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong vireon_client_pending_bytes(nint handle);

    // ── Subscription ────────────────────────────────────────────────

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_sub_recv(
        nint handle, ref VireonMessage outMsg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_sub_recv_batch(
        nint handle, int maxCount, ref VireonMsgBatch outBatch);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void vireon_sub_close(nint handle);

    // ── GroupSubscription ───────────────────────────────────────────

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_group_sub_recv(
        nint handle, ref VireonMessage outMsg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_group_sub_recv_batch(
        nint handle, int maxCount, ref VireonMsgBatch outBatch);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void vireon_group_sub_close(nint handle);

    // ── StreamHandle ────────────────────────────────────────────────

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_stream_recv(
        nint handle, ref VireonMessage outMsg);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_stream_recv_batch(
        nint handle, int maxCount, ref VireonMsgBatch outBatch);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_stream_publish(
        nint handle, string topic, byte[] payload, nuint payloadLen);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_stream_try_publish(
        nint handle, string topic, byte[] payload, nuint payloadLen);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong vireon_stream_id(nint handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong vireon_stream_pending_bytes(nint handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void vireon_stream_close(nint handle);

    // ── ClientPool ──────────────────────────────────────────────────

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_pool_len(nint handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint vireon_pool_member(nint handle, int idx);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_pool_publish(
        nint handle, string topic, byte[] payload, nuint payloadLen);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_pool_try_publish(
        nint handle, string topic, byte[] payload, nuint payloadLen);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ulong vireon_pool_pending_bytes(nint handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int vireon_pool_close(nint handle);
}
