#!/usr/bin/env bash
# Resolve a Godot 4.6.1-mono headless binary, auto-downloading + caching on first use.
# Prints the resolved binary path to STDOUT; all logs go to STDERR.
# The download is cached OUTSIDE the repo (Godot mono is ~213MB — never commit it).
#
#   GODOT_BIN        if set to an executable, used as-is (no download)
#   GODOT_CACHE_DIR  cache location (default: ~/.cache/darkness/godot)
set -euo pipefail

VERSION="4.6.1-stable"
CACHE_DIR="${GODOT_CACHE_DIR:-$HOME/.cache/darkness/godot}"

# 1) Explicit override wins.
if [ -n "${GODOT_BIN:-}" ] && [ -x "${GODOT_BIN}" ]; then echo "$GODOT_BIN"; exit 0; fi

OS="$(uname -s)"
if [ "$OS" != "Linux" ]; then
  echo "fetch-godot: auto-download supports Linux only (found $OS). Set GODOT_BIN to a Godot ${VERSION} mono binary." >&2
  exit 1
fi

DIRNAME="Godot_v${VERSION}_mono_linux_x86_64"
BINNAME="Godot_v${VERSION}_mono_linux.x86_64"
CACHED_BIN="$CACHE_DIR/$DIRNAME/$BINNAME"

# 2) Already cached?
if [ -x "$CACHED_BIN" ]; then echo "$CACHED_BIN"; exit 0; fi

# 3) Reuse a prior sandbox download if present (avoids re-downloading).
LEGACY_BIN="/tmp/godot-mono/$DIRNAME/$BINNAME"
if [ -x "$LEGACY_BIN" ]; then echo "$LEGACY_BIN"; exit 0; fi

# 4) Download + extract to the cache.
ZIP="$CACHE_DIR/${DIRNAME}.zip"
URL="https://github.com/godotengine/godot-builds/releases/download/${VERSION}/Godot_v${VERSION}_mono_linux_x86_64.zip"
mkdir -p "$CACHE_DIR"
echo "fetch-godot: downloading Godot ${VERSION} mono (~97MB, one-time) ..." >&2
curl -fSL --retry 3 -o "$ZIP" "$URL" >&2
echo "fetch-godot: extracting ..." >&2
python3 -c "import zipfile; zipfile.ZipFile('$ZIP').extractall('$CACHE_DIR')"   # 'unzip' isn't always installed
chmod +x "$CACHED_BIN" 2>/dev/null || true
rm -f "$ZIP"
[ -x "$CACHED_BIN" ] || { echo "fetch-godot: binary not found after extract: $CACHED_BIN" >&2; exit 1; }
echo "$CACHED_BIN"
