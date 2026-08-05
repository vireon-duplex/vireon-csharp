using Vireon;

string addr = Environment.GetEnvironmentVariable("VIREON_ADDR") ?? "127.0.0.1:4433";
const int HEAVY_COUNT = 2000;

Client connect() => new ClientBuilder(addr)
    .TlsVerify(TlsVerify.DangerAcceptInvalid())
    .Connect();

Client sub = connect();
Client pub = connect();

// Open 5 dedicated streams
var streams = new StreamHandle[5];
for (int i = 0; i < 5; i++)
{
    string topic = $"hol.stream{i}";
    streams[i] = await sub.OpenStreamAsync(DeliveryPolicy.ReliableOrdered, topic);
}
await Task.Delay(300);

int[] counts = new int[5];
int heavyReceived = 0;

// ── Drain tasks for each stream (proper async) ───────────────────
async Task DrainAsync(StreamHandle stream, int idx)
{
    while (true)
    {
        var msg = await stream.RecvAsync();
        if (msg == null) break;
        Interlocked.Increment(ref counts[idx]);
        if (idx == 0) Interlocked.Increment(ref heavyReceived);
    }
}

var drainers = new Task[5];
for (int i = 0; i < 5; i++)
{
    drainers[i] = DrainAsync(streams[i], i);
}

// ── Flood the heavy stream (stream 0) ────────────────────────────
byte[] payload = new byte[1024];
for (int i = 0; i < HEAVY_COUNT; i++)
{
    // Interleave light publishes every 100 heavy frames
    if (i % 100 == 0 && i > 0)
    {
        for (int s = 1; s < 5; s++)
        {
            await pub.PublishAsync($"hol.stream{s}", payload);
        }
    }
    try
    {
        pub.TryPublish("hol.stream0", payload);
    }
    catch (VireonException)
    {
        await Task.Delay(TimeSpan.FromMicroseconds(500)); // yield on backpressure
    }
}

// Give drain threads time to catch up
await Task.Delay(1000);

int heavy = counts[0];
int lightest = int.MaxValue;
for (int s = 1; s < 5; s++) lightest = Math.Min(lightest, counts[s]);
int total = 0;
for (int s = 0; s < 5; s++) total += counts[s];

Console.WriteLine($"hol_congestion: heavy={heavy}msgs lightest={(lightest == int.MaxValue ? 0 : lightest)}msgs total={total}msgs");

// Light streams must have received messages despite heavy congestion
for (int s = 1; s < 5; s++)
{
    if (counts[s] == 0)
    {
        Console.Error.WriteLine($"FAIL: light stream {s} got 0 messages (HOL blocked)");
        Environment.Exit(1);
    }
}
Console.WriteLine("PASS: HOL isolation verified — light streams delivered during congestion");

foreach (var s in streams) s.Dispose();
sub.Dispose();
pub.Dispose();
