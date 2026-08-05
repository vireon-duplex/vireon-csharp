using Vireon;

// Bench — throughput benchmark with three modes: stream, broadcast, group.
//
// Uses RecvBatchAsync() to amortise P/Invoke overhead — each FFI round-trip
// drains up to 256 messages instead of one.
//
// Usage:
//   quic-server --config vireon.conf &
//   VIREON_ADDR=127.0.0.1:4433 Bench [mode] [size] [count]
//
// mode: stream (default) | broadcast | group
// size: payload bytes (default 1024)
// count: number of messages (default 5000)

string addr = Environment.GetEnvironmentVariable("VIREON_ADDR") ?? "127.0.0.1:4433";
const int BATCH_SIZE = 256;

string mode = args.Length > 0 ? args[0] : "stream";
int size = args.Length > 1 ? int.Parse(args[1]) : 1024;
int count = args.Length > 2 ? int.Parse(args[2]) : 5000;

Console.WriteLine($"bench: mode={mode} size={size}B count={count}");

Client connect() => new ClientBuilder(addr)
    .TlsVerify(TlsVerify.DangerAcceptInvalid())
    .SubscriberBuffer(65536)
    .Connect();

Client sub = connect();
Client pub = connect();

byte[] payload = new byte[size];
for (int i = 0; i < size; i++) payload[i] = (byte)(i & 0xFF);

// Shared counter captured by the drain task.
int received = 0;

async Task BatchDrain(Func<int, Task<Message[]>> recvBatch)
{
    while (received < count)
    {
        Message[] batch;
        try
        {
            batch = await recvBatch(BATCH_SIZE);
        }
        catch
        {
            break;
        }
        if (batch == null || batch.Length == 0) break;
        Interlocked.Add(ref received, batch.Length);
    }
}

void RunFireLoop(Client p, string topic, byte[] pl, int cnt)
{
    int sent = 0;
    while (sent < cnt)
    {
        try
        {
            p.TryPublish(topic, pl);
            sent++;
        }
        catch (VireonException)
        {
            Thread.Sleep(TimeSpan.FromMicroseconds(500)); // backpressure yield
        }
    }
}

long benchStart;

switch (mode)
{
    case "stream":
    {
        var stream = await sub.OpenStreamAsync(DeliveryPolicy.ReliableOrdered, "bench.stream");
        await Task.Delay(300);
        var drain = BatchDrain(stream.RecvBatchAsync);
        benchStart = System.Diagnostics.Stopwatch.GetTimestamp();
        RunFireLoop(pub, "bench.stream", payload, count);
        await drain.WaitAsync(TimeSpan.FromSeconds(30));
        break;
    }
    case "broadcast":
    {
        var s = await sub.SubscribeAsync("bench.broadcast");
        await Task.Delay(300);
        var drain = BatchDrain(s.RecvBatchAsync);
        benchStart = System.Diagnostics.Stopwatch.GetTimestamp();
        RunFireLoop(pub, "bench.broadcast", payload, count);
        await drain.WaitAsync(TimeSpan.FromSeconds(30));
        break;
    }
    case "group":
    {
        var gs = await sub.SubscribeGroupAsync("bench.group", "workers", "c0");
        await Task.Delay(300);
        var drain = BatchDrain(gs.RecvBatchAsync);
        benchStart = System.Diagnostics.Stopwatch.GetTimestamp();
        RunFireLoop(pub, "bench.group", payload, count);
        await drain.WaitAsync(TimeSpan.FromSeconds(30));
        break;
    }
    default:
        Console.Error.WriteLine($"Unknown mode: {mode}");
        Environment.Exit(1);
        return;
}

long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - benchStart;
double secs = elapsed / (double)System.Diagnostics.Stopwatch.Frequency;
double mib = received * (long)size / (1024.0 * 1024.0);
double mibPerSec = secs > 0 ? mib / secs : 0;
Console.WriteLine($"bench: mode={mode} received={received}/{count}  {mib:F1} MiB");
Console.WriteLine($"throughput: {(secs > 0 ? received / secs : 0):F0} msg/s  {mibPerSec:F1} MiB/s");
