# Vireon C# SDK

C# binding for the Vireon QUIC-native pub/sub runtime, via a C ABI cdylib
+ P/Invoke.

The CLR has no stable QUIC support (`System.Net.Quic` is experimental in
.NET 8/9), so this binding wraps the Rust SDK via a thin C ABI layer —
the most mature FFI mechanism in .NET.

## Architecture

| Layer | Description |
|---|---|
| **Rust cdylib** (`libvireon_csharp.so`) | `extern "C"` functions with `vireon_` prefix. Global tokio runtime. `block_on()` bridges async→sync. |
| **C# class library** (`VireonSdk.dll`) | `[DllImport]` P/Invoke declarations + high-level `async` wrappers via `Task.Run()`. `IDisposable` for all handles. |

## Prerequisites

- Rust 1.85+ (workspace pins `x86_64-unknown-linux-gnu`)
- .NET 8 SDK (`sudo apt install dotnet-sdk-8.0`)

## Build

```bash
bash build.sh
```

This produces:
- `csharp/lib/VireonSdk.dll` — .NET class library
- `examples/bin/*.dll` — compiled examples

## Quickstart

```csharp
using Vireon;

var client = new ClientBuilder("127.0.0.1:4433")
    .TlsVerify(TlsVerify.DangerAcceptInvalid())
    .Connect();

var sub = await client.SubscribeAsync("sensor.*");
await Task.Delay(200);

await client.PublishAsync("sensor.temp", "42C"u8.ToArray());
var msg = await sub.RecvAsync();
Console.WriteLine($"{msg?.Topic}: {Encoding.UTF8.GetString(msg?.Payload ?? Array.Empty<byte>())}");

client.Dispose();
```

## Examples

| Example | Description |
|---|---|
| **Quickstart** | Basic pub/sub + all delivery policies |
| **Ordering** | Verify in-order delivery (500 frames) |
| **ConsumerGroup** | 3-member round-robin distribution |
| **HolCongestion** | Head-of-line blocking isolation (5 streams) |
| **PoolMultiplex** | 4-connection pool, 1000 messages |
| **Bench** | Throughput benchmark (stream/broadcast/group) |

### Run an example

```bash

# Run Quickstart
VIREON_ADDR=127.0.0.1:4433 \
dotnet examples/bin/Quickstart.dll
```

## API

### Client

| Method | Description |
|---|---|
| `PublishAsync(topic, payload)` | Publish on default channel |
| `TryPublish(topic, payload)` | Fire-and-forget publish |
| `SubscribeAsync(pattern)` | Subscribe to topic pattern |
| `UnsubscribeAsync(pattern)` | Remove subscription |
| `OpenStreamAsync(policy, topic?)` | Open dedicated stream |
| `SubscribeGroupAsync(topic, group, consumer)` | Join consumer group |
| `LeaveGroupAsync(topic, group, consumer)` | Leave consumer group |
| `RpcAsync(reqTopic, payload, replyTopic, timeout)` | Request/reply RPC |
| `MigrateAsync(bindAddr)` | Trigger connection migration |
| `PendingBytes()` | Transport buffered bytes |

### ClientBuilder

Fluent builder with methods: `TlsVerify`, `Sni`, `ClientIdentity`,
`Reconnect`, `MaxMessageSize`, `SubscriberBuffer`, `CmdChannelCap`,
`MaxIdleTimeout`.

### Delivery Policies

| Policy | Description |
|---|---|
| `ReliableOrdered` | In-order delivery (default) |
| `ReliableUnordered` | Reliable, order not guaranteed |
| `RealtimeDropOld` | Drop oldest when behind |
| `LatestOnly` | Only latest value kept |

### TLS Verification

| Factory | Description |
|---|---|
| `TlsVerify.Tofu()` | Trust-on-first-use (default) |
| `TlsVerify.DangerAcceptInvalid()` | No verification (dev only) |
| `TlsVerify.Strict(caPath)` | Verify against CA bundle |
| `TlsVerify.Pinned(certDerPath)` | Pin single certificate |

## RecvBatch optimization

`Subscription.RecvBatchAsync(maxCount)` drains up to 256 messages per FFI
round-trip — 2-6× throughput improvement for pub/sub modes compared to
single-message `RecvAsync()`.

## Server no-echo design

The server filters `conn_idx` on fan-out — a client never receives its own
publishes. Tests and examples use **two connections** (sub + pub).
