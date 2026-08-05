#!/usr/bin/env bash
# Build the C# SDK: Rust cdylib + .NET class library + examples.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
CSHARP_DIR="$(cd "$(dirname "$0")" && pwd)"
TARGET="$ROOT/target/x86_64-unknown-linux-gnu/release"

cd "$CSHARP_DIR"

echo "▶ Building Rust cdylib (libvireon_csharp.so)..."
cargo build --release --target x86_64-unknown-linux-gnu -p vireon-csharp

echo "▶ Building VireonSdk class library..."
dotnet build csharp/VireonSdk.csproj -c Release -o "$CSHARP_DIR/lib"

echo "▶ Building examples..."
for d in examples/*/; do
    proj=$(basename "$d")
    if [ -f "$d${proj}.csproj" ]; then
        echo "  → $proj"
        dotnet build "$d${proj}.csproj" -c Release -o "$CSHARP_DIR/bin"
    fi
done

echo
echo "✓ Build complete."
echo "  Native:  $TARGET/libvireon_csharp.so"
echo "  SDK:     $CSHARP_DIR/lib/VireonSdk.dll"
echo "  Examples: $CSHARP_DIR/bin/"
echo
echo "Run with:"
echo "  LD_LIBRARY_PATH=$TARGET VIREON_ADDR=127.0.0.1:4433 dotnet $CSHARP_DIR/bin/Quickstart.dll"
