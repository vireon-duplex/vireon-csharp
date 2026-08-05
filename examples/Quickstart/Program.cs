using Vireon;

string addr = Environment.GetEnvironmentVariable("VIREON_ADDR") ?? "127.0.0.1:4433";

Client connect() => new ClientBuilder(addr)
    .TlsVerify(TlsVerify.DangerAcceptInvalid())
    .Connect();

Client sub = connect();
Client pub = connect();

// ── 1. Default-channel pub/sub ──────────────────────────────────
var subscription = await sub.SubscribeAsync("sensor.*");
await Task.Delay(200);

await pub.PublishAsync("sensor.temp", "42C"u8.ToArray());
var msg = await subscription.RecvAsync() ?? throw new InvalidOperationException("no message received");
Console.WriteLine($"  pub/sub: {msg.Topic} = {System.Text.Encoding.UTF8.GetString(msg.Payload)}");

// ── 2. All delivery policies ─────────────────────────────────────
var policies = new (DeliveryPolicy p, string name)[] {
    (DeliveryPolicy.ReliableOrdered,    "RELIABLE_ORDERED"),
    (DeliveryPolicy.ReliableUnordered,  "RELIABLE_UNORDERED"),
    (DeliveryPolicy.RealtimeDropOld,    "REALTIME_DROP_OLD"),
    (DeliveryPolicy.LatestOnly,         "LATEST_ONLY"),
};

foreach (var (policy, name) in policies)
{
    string topic = $"test.{name}";
    var stream = await sub.OpenStreamAsync(policy, topic);
    await Task.Delay(200);
    await pub.PublishAsync(topic, System.Text.Encoding.UTF8.GetBytes($"data-{name}"));
    var m = await stream.RecvAsync() ?? throw new InvalidOperationException($"no message on stream {name}");
    Console.WriteLine($"  {name}: topic={m.Topic} payload={System.Text.Encoding.UTF8.GetString(m.Payload)} streamId={m.StreamId}");
    stream.Dispose();
}

Console.WriteLine();
Console.WriteLine("all 5 delivery policies verified");

sub.Dispose();
pub.Dispose();
