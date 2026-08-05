using System.Buffers.Binary;
using Vireon;

string addr = Environment.GetEnvironmentVariable("VIREON_ADDR") ?? "127.0.0.1:4433";
const int N = 500;

Client connect() => new ClientBuilder(addr)
    .TlsVerify(TlsVerify.DangerAcceptInvalid())
    .Connect();

Client sub = connect();
Client pub = connect();

var stream = await sub.OpenStreamAsync(DeliveryPolicy.ReliableOrdered, "ordering.test");
await Task.Delay(300);

// ── Drain: receives and verifies ordering ────────────────────────
int received = 0, gaps = 0, duplicates = 0;
long lastSeq = -1;

async Task DrainAsync()
{
    while (received < N)
    {
        var msg = await stream.RecvAsync();
        if (msg == null) break;
        long seq = (long)msg.Seq;
        if (seq <= lastSeq) duplicates++;
        if (seq > lastSeq + 1 && lastSeq >= 0) gaps++;
        lastSeq = seq;
        received++;
    }
}

var drain = DrainAsync();

// ── Fire loop: publishes N frames as fast as possible ────────────
byte[] payload = new byte[256];
for (int i = 0; i < N; i++)
{
    BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), i);
    try
    {
        pub.TryPublish("ordering.test", payload);
    }
    catch (VireonException)
    {
        await Task.Delay(1); // backpressure — yield briefly
    }
}

await drain.WaitAsync(TimeSpan.FromSeconds(30));

Console.WriteLine($"ordering: received: {received}/{N}");
Console.WriteLine($"  gaps: {gaps}");
Console.WriteLine($"  duplicates: {duplicates}");

if (received != N)
{
    Console.Error.WriteLine($"FAIL: expected {N}, got {received}");
    Environment.Exit(1);
}
if (gaps > 0 || duplicates > 0)
{
    Console.Error.WriteLine($"FAIL: gaps={gaps} duplicates={duplicates}");
    Environment.Exit(1);
}
Console.WriteLine("PASS: all frames delivered in order, no gaps, no duplicates");

stream.Dispose();
sub.Dispose();
pub.Dispose();
