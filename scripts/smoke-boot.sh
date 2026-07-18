#!/usr/bin/env bash
#
# smoke-boot.sh - Headless game-boot smoke test for Darkness.Godot.
#
# Boots the REAL Darkness.Godot project headless so we can verify at RUNTIME
# that the Global autoload's DI init + all data seeders run against the authored
# content without crashing.
#
#   dotnet build  ->  headless --import (assets)  ->  headless boot (--quit-after)
#   ->  grep the boot log for the seeding-complete + DataValidator markers and
#       for known failure signatures.
#
# Exits non-zero if the game fails to boot/seed. Idempotent; safe to re-run.
# The expensive asset --import pass is skipped when the .godot cache already
# looks populated (override with FORCE_IMPORT=1).
#
# Environment:
#   GODOT_BIN      Path to a Godot 4.6.1 *mono* binary (headless-capable).
#                  Defaults to the sandbox download location below.
#   FORCE_IMPORT   Set to 1 to force the (slow, ~4700-sprite) re-import pass.
#   QUIT_AFTER     Frames to run before auto-quitting (default 1200). The
#                  seeding Task is async, so allow time for it to finish.
#
# Usage:
#   ./scripts/smoke-boot.sh
#   GODOT_BIN=/path/to/Godot_mono ./scripts/smoke-boot.sh
#
set -uo pipefail

# --- Resolve repo root (this script lives in <root>/scripts) ------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
GAME_DIR="$REPO_ROOT/Darkness.Godot"
GAME_CSPROJ="$GAME_DIR/Darkness.Godot.csproj"

# --- Toolchain env ------------------------------------------------------------
export PATH="$HOME/.dotnet:$PATH"
# Godot mono headless has no ICU in this sandbox; invariant globalization avoids a crash.
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="${DOTNET_SYSTEM_GLOBALIZATION_INVARIANT:-1}"

QUIT_AFTER="${QUIT_AFTER:-1200}"
BOOT_LOG="$(mktemp -t darkness-smoke-boot.XXXXXX.log)"

# --- Godot binary -------------------------------------------------------------
export GODOT_BIN="${GODOT_BIN:-/tmp/godot-mono/Godot_v4.6.1-stable_mono_linux_x86_64/Godot_v4.6.1-stable_mono_linux.x86_64}"
if [ ! -x "$GODOT_BIN" ]; then
    echo "ERROR: GODOT_BIN is not an executable file: $GODOT_BIN" >&2
    echo "       Set GODOT_BIN to a Godot 4.6.1 mono binary and re-run." >&2
    exit 1
fi
echo "GODOT_BIN = $GODOT_BIN"
"$GODOT_BIN" --headless --version

# --- .godot cache off the drvfs (/mnt/c) mount -------------------------------
# Importing/booting a Godot project on a Windows drive mount fails (drvfs cannot
# set file timestamps). Redirect the generated .godot cache to a real Linux tmp
# dir via a symlink. .godot is gitignored.
DOTGODOT_TMP="/tmp/darkness-dotgodot"
link_dotgodot() {
    mkdir -p "$DOTGODOT_TMP"
    if [ -L "$GAME_DIR/.godot" ]; then
        return 0
    fi
    rm -rf "$GAME_DIR/.godot"
    ln -s "$DOTGODOT_TMP" "$GAME_DIR/.godot"
    echo "linked $GAME_DIR/.godot -> $DOTGODOT_TMP"
}
case "$REPO_ROOT" in
    /mnt/*) link_dotgodot ;;
esac

# --- Build --------------------------------------------------------------------
echo "== Building Darkness.Godot =="
if ! dotnet build "$GAME_CSPROJ"; then
    echo "SMOKE-BOOT FAILED: build error." >&2
    exit 1
fi

# --- Asset import (slow; cached) ---------------------------------------------
# The full project has ~4700 sprites; a cold import can take many minutes.
# Skip it when the cache already looks populated unless FORCE_IMPORT=1.
IMPORT_SENTINEL="$GAME_DIR/.godot/global_script_class_cache.cfg"
if [ "${FORCE_IMPORT:-0}" = "1" ] || [ ! -f "$IMPORT_SENTINEL" ]; then
    echo "== Importing project assets (this can be slow) =="
    "$GODOT_BIN" --headless --path "$GAME_DIR" --import >/dev/null 2>&1 || true
else
    echo "== Skipping asset import (cache present; set FORCE_IMPORT=1 to force) =="
fi

# --- Boot headless ------------------------------------------------------------
echo "== Booting game headless (--quit-after $QUIT_AFTER) =="
"$GODOT_BIN" --headless --path "$GAME_DIR" --quit-after "$QUIT_AFTER" >"$BOOT_LOG" 2>&1 || true
echo "Boot log: $BOOT_LOG"

# --- Assess -------------------------------------------------------------------
# Success requires BOTH the seeding-complete marker AND the DataValidator
# completion marker (DataValidator throws on bad data, faulting the seeding
# Task silently in a headless boot, so the completion marker is the real signal).
SEED_OK='\[Global\] Data seeding complete\.'
VALIDATE_OK='\[DataValidator\] Validation complete\.'

# Failure signatures — scoped to boot/seed logic, NOT benign headless render noise.
FAIL_SIGS='\[Global\] Critical error|\[Splash\] CRITICAL|Data validation failed|\[DataValidator\] ERROR|\[ArcSeeder\] ERROR|\[LocalizationSeeder\] ERROR|\[QuestSeeder\] ERROR|\[SheetDefinitionSeeder\] ERROR|Seeder\] ERROR|Unhandled [Ee]xception|LiteException|System\.Exception'

status=0

if grep -qE "$SEED_OK" "$BOOT_LOG"; then
    echo "PASS: found seeding-complete marker."
else
    echo "FAIL: missing '[Global] Data seeding complete.' marker." >&2
    status=1
fi

if grep -qE "$VALIDATE_OK" "$BOOT_LOG"; then
    echo "PASS: DataValidator ran to completion."
else
    echo "FAIL: missing '[DataValidator] Validation complete.' marker." >&2
    status=1
fi

if grep -qE "$FAIL_SIGS" "$BOOT_LOG"; then
    echo "FAIL: boot/seed error signature(s) detected:" >&2
    grep -nE "$FAIL_SIGS" "$BOOT_LOG" >&2
    status=1
fi

echo
echo "== Key boot log lines =="
grep -nE '\[Global\]|\[DataValidator\]|Seeder\]|\[Splash\]' "$BOOT_LOG" || true

echo
if [ "$status" -eq 0 ]; then
    echo "== smoke-boot.sh PASSED =="
else
    echo "== smoke-boot.sh FAILED (see $BOOT_LOG) ==" >&2
fi
exit $status
