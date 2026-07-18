#!/usr/bin/env bash
#
# run-godot-tests.sh - Run the gdUnit4 C# test suite headlessly.
#
# Proves the Godot-layer test loop without CI:
#   dotnet test  ->  gdUnit4.test.adapter  ->  headless Godot  ->  gdUnit4 C# tests
#
# Idempotent. Safe to re-run.
#
# Environment:
#   GODOT_BIN   Path to a Godot 4.6.1 *mono* binary (headless-capable).
#               Defaults to the sandbox download location below; override to taste.
#
# Usage:
#   ./scripts/run-godot-tests.sh
#   GODOT_BIN=/path/to/Godot_mono ./scripts/run-godot-tests.sh
#
set -euo pipefail

# --- Resolve repo root (this script lives in <root>/scripts) ------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TEST_PROJ_DIR="$REPO_ROOT/Darkness.Godot.Tests"
TEST_CSPROJ="$TEST_PROJ_DIR/Darkness.Godot.Tests.csproj"

# --- Toolchain env ------------------------------------------------------------
export PATH="$HOME/.dotnet:$PATH"
# Godot mono headless has no ICU in this sandbox; invariant globalization avoids a crash.
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="${DOTNET_SYSTEM_GLOBALIZATION_INVARIANT:-1}"

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
# Building/importing a Godot project on a Windows drive mount fails (MSB3374 /
# timestamp errors) because drvfs cannot set file timestamps. Redirect the
# generated .godot cache to a real Linux tmp dir via a symlink. .godot is gitignored.
link_dotgodot() {
    local proj_dir="$1" tmp_dir="$2"
    mkdir -p "$tmp_dir"
    # (Re)create the symlink unless it already points somewhere valid.
    if [ -L "$proj_dir/.godot" ]; then
        return 0
    fi
    rm -rf "$proj_dir/.godot"
    ln -s "$tmp_dir" "$proj_dir/.godot"
    echo "linked $proj_dir/.godot -> $tmp_dir"
}
# Only apply the symlink workaround on drvfs (Windows) mounts.
case "$REPO_ROOT" in
    /mnt/*) link_dotgodot "$TEST_PROJ_DIR" "/tmp/darkness-tests-dotgodot" ;;
esac

# --- First-run import pass ----------------------------------------------------
# Godot must import the project's resources before tests can load scenes.
# The test project is tiny, so this is quick.
echo "== Importing test project resources =="
timeout 600 "$GODOT_BIN" --headless --path "$TEST_PROJ_DIR" --import >/dev/null 2>&1 || true

# --- Run the tests ------------------------------------------------------------
echo "== Running gdUnit4 tests headlessly =="
timeout 600 dotnet test "$TEST_CSPROJ" --settings "$TEST_PROJ_DIR/.runsettings"
status=$?

echo "== run-godot-tests.sh finished with exit code $status =="
exit $status
