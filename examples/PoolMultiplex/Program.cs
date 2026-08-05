using Vireon;

string addr = Environment.GetEnvironmentVariable("VIREON_ADDR") ?? "127.0.0.1:4433";
const int N = 1000;

var builder = new ClientBuilder(addr)
    .TlsVerify(TlsVerify.DangerAcceptInvalid());

ClientPool pool = builder.ConnectPool(4);
// Subscriber must be on a separate connection — server no-echo design
// prevents a client from receiving its own publishes.
Client sub = new ClientBuilder(addr).TlsVerify(TlsVerify.DangerAcceptInvalid()).Connect();

var subscription = await sub.SubscribeAsync("pool.test");
await Task.Delay(300);

// ── Drain task (proper async) ────────────────────────────────────
int received = 0;

async Task DrainAsync()
{
    while (received < N)
    {
        var msg = await subscription.RecvAsync();
        if (msg == null) break;
        Interlocked.Increment(ref received);
    }
}

var drain = DrainAsync();

// ── Fire loop: tryPublish with backpressure retry ────────────────
byte[] payload = "x"u8.ToArray();
long start = System.Diagnostics.Stopwatch.GetTimestamp();
int sent = 0;
for (int i = 0; i < N; i++)
{
    while (true)
    {
        try
        {
            pool.TryPublish("pool.test", payload);
            sent++;
            break;
        }
        catch (VireonException)
        {
            await Task.Delay(TimeSpan.FromMicroseconds(500)); // yield on backpressure
        }
    }
}

await drain.WaitAsync(TimeSpan.FromSeconds(30));
long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - start;
double secs = elapsed / (double)System.Diagnostics.Stopwatch.Frequency;
int tput = secs > 0 ? (int)(received / secs) : 0;

Console.WriteLine($"pool_multiplex: received {received}/{N} messages | throughput: {tput} msg/s");

if (received != N)
{
    Console.Error.WriteLine($"FAIL: expected {N}, got {received}");
    Environment.Exit(1);
}
Console.WriteLine("PASS: all messages delivered via 4-connection pool");

subscription.Dispose();
sub.Dispose();
pool.Dispose();
