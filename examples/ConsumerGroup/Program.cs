using Vireon;

string addr = Environment.GetEnvironmentVariable("VIREON_ADDR") ?? "127.0.0.1:4433";
const int N = 30;

Client connect() => new ClientBuilder(addr)
    .TlsVerify(TlsVerify.DangerAcceptInvalid())
    .Connect();

Client pub = connect();
Client c0 = connect();
Client c1 = connect();
Client c2 = connect();

var g0 = await c0.SubscribeGroupAsync("task.jobs", "workers", "c0");
var g1 = await c1.SubscribeGroupAsync("task.jobs", "workers", "c1");
var g2 = await c2.SubscribeGroupAsync("task.jobs", "workers", "c2");
await Task.Delay(500);

// ── Drain tasks ───────────────────────────────────────────────────
int[] counts = new int[3];
int total = 0;
var subs = new[] { g0, g1, g2 };

async Task DrainAsync(GroupSubscription s, int idx)
{
    while (true)
    {
        var msg = await s.RecvAsync();
        if (msg == null) break;
        Interlocked.Increment(ref counts[idx]);
        Interlocked.Increment(ref total);
    }
}

for (int i = 0; i < 3; i++)
{
    var idx = i;
    _ = Task.Run(() => DrainAsync(subs[idx], idx));
}

// ── Publish N jobs ────────────────────────────────────────────────
byte[] payload = "job"u8.ToArray();
for (int i = 0; i < N; i++)
{
    await pub.PublishAsync("task.jobs", payload);
}

// ── Wait for all N deliveries ─────────────────────────────────────
var sw = System.Diagnostics.Stopwatch.StartNew();
while (total < N && sw.Elapsed < TimeSpan.FromSeconds(10))
    await Task.Delay(50);

Console.WriteLine($"consumer_group: delivered: {total}/{N}");
Console.WriteLine($"  balance: g0={counts[0]}msgs g1={counts[1]}msgs g2={counts[2]}msgs");

if (total != N)
{
    Console.Error.WriteLine($"FAIL: expected {N}, got {total}");
    Environment.Exit(1);
}
for (int i = 0; i < 3; i++)
{
    if (counts[i] == 0)
    {
        Console.Error.WriteLine($"FAIL: member c{i} got 0 messages");
        Environment.Exit(1);
    }
}
Console.WriteLine("PASS: all jobs distributed, all members received work");

// Exit immediately — drainer tasks are blocked on recv() and will be
// cleaned up when the process exits.
Environment.Exit(0);
