using System.Runtime.InteropServices;

namespace Vireon;

/// <summary>
/// A received message. Topic + payload are copied to managed memory
/// at construction time; the native allocation is freed immediately.
/// </summary>
public record Message
{
    public string Topic { get; init; } = "";
    public byte[] Payload { get; init; } = Array.Empty<byte>();
    public ulong Seq { get; init; }
    public ulong StreamId { get; init; }

    internal static Message FromNative(ref Native.VireonMessage m)
    {
        string topic = "";
        byte[] payload = Array.Empty<byte>();

        if (m.Topic != IntPtr.Zero)
        {
            topic = Marshal.PtrToStringAnsi(m.Topic) ?? "";
        }

        if (m.Payload != IntPtr.Zero && m.PayloadLen > 0)
        {
            payload = new byte[m.PayloadLen];
            Marshal.Copy(m.Payload, payload, 0, (int)m.PayloadLen);
        }

        var msg = new Message
        {
            Topic = topic,
            Payload = payload,
            Seq = m.Seq,
            StreamId = m.StreamId,
        };

        // Free native memory immediately after copy.
        Native.vireon_msg_free(ref m);
        return msg;
    }

    internal static Message[] FromBatch(ref Native.VireonMsgBatch batch)
    {
        if (batch.Count == 0 || batch.Msgs == IntPtr.Zero)
        {
            return Array.Empty<Message>();
        }

        var result = new Message[(int)batch.Count];
        var structSize = Marshal.SizeOf<Native.VireonMessage>();
        for (int i = 0; i < (int)batch.Count; i++)
        {
            var ptr = batch.Msgs + i * structSize;
            result[i] = Marshal.PtrToStructure<Native.VireonMessage>(ptr) is { } m
                ? FromNativeNoFree(ref m) // FromNative frees; batch_free will handle array
                : new Message();
        }

        // Free the array + each message.
        Native.vireon_batch_free(ref batch);
        return result;
    }

    // Copies without freeing — used inside FromBatch where batch_free does the freeing.
    private static Message FromNativeNoFree(ref Native.VireonMessage m)
    {
        string topic = "";
        byte[] payload = Array.Empty<byte>();

        if (m.Topic != IntPtr.Zero)
        {
            topic = Marshal.PtrToStringAnsi(m.Topic) ?? "";
        }

        if (m.Payload != IntPtr.Zero && m.PayloadLen > 0)
        {
            payload = new byte[m.PayloadLen];
            Marshal.Copy(m.Payload, payload, 0, (int)m.PayloadLen);
        }

        var msg = new Message
        {
            Topic = topic,
            Payload = payload,
            Seq = m.Seq,
            StreamId = m.StreamId,
        };
        return msg;
    }
}
