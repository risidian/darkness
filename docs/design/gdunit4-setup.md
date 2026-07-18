# GdUnit4 Scene-Test Harness — Setup & Workflow

Godot scene/integration tests for `Darkness.Godot` (C#), run **headless**. Complements the
"humble object" extraction pattern (logic lives in `Darkness.Core`, unit-tested by
`Darkness.Tests`); GdUnit4 covers what extraction can't — real scene wiring, node lifecycle,
input, rendering.

Ref: <https://github.com/godot-gdunit-labs/gdUnit4> · docs <https://godot-gdunit-labs.github.io/gdUnit4/latest/> · CI action <https://github.com/godot-gdunit-labs/gdUnit4-action>

## ✅ IMPLEMENTED — run it turnkey (no editor, no manual setup)
The harness is committed: addon vendored at `Darkness.Godot/addons/gdUnit4`, test project `Darkness.Godot.Tests/`, net10.0, plugin enabled in `project.godot`. To run the Godot-layer tests headless from a clean machine:

```bash
./scripts/run-godot-tests.sh     # gdUnit4 scene/unit tests
./scripts/smoke-boot.sh          # boots the real game headless; asserts DI + all seeders run
```

Both scripts **auto-download + cache Godot 4.6.1-mono on first run** via `scripts/fetch-godot.sh`
(~97MB → `~/.cache/darkness/godot`, NOT committed — the mono runtime is 213MB, over GitHub's
100MB limit). Override the engine with `GODOT_BIN=/path/to/godot_mono`. Linux/WSL auto-download;
on macOS/Windows set `GODOT_BIN` yourself (or run from the editor). The scripts also handle the
WSL drvfs quirks (`.godot`→`/tmp` symlink, invariant globalization).

CI (`.github/workflows/godot-tests.yml`, `gdUnit4-action`) is a **bonus** path, still manual-trigger
until validated — local is the primary loop now.

## Original setup notes (historical — already done above)
The addon + packages below are already installed/committed; kept for reference / re-provisioning.

## One-time setup (needs Godot 4.6.1-mono editor) — human step
1. **Install the addon:** Godot editor → AssetLib → search **"GdUnit4"** (asset id 1522) → install into `addons/gdUnit4/`. Enable it in Project Settings → Plugins. Commit `addons/gdUnit4/`.
2. **Create the C# scene-test project** — recommended as its own dir `Darkness.Godot/test/` compiled by the Godot project, OR a dedicated `Darkness.Godot.Tests.csproj`. Add these packages (verify latest on nuget.org):
   ```xml
   <ItemGroup>
     <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.0" />
     <PackageReference Include="gdUnit4.api" Version="5.1.0-rc1" />
     <PackageReference Include="gdUnit4.test.adapter" Version="3.0.0" />
     <PackageReference Include="gdUnit4.analyzers" Version="1.0.0">
       <PrivateAssets>none</PrivateAssets>
       <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
     </PackageReference>
   </ItemGroup>
   ```
   **⚠ .NET 10 caveat:** this repo is `net10.0`; gdUnit4.api docs target `net8.0`/`net9.0`. If the test project won't restore/build on net10, set the **test project** `<TargetFramework>net9.0</TargetFramework>` (it can still reference the net10 game assembly at runtime via Godot) or bump to a gdUnit4 version that lists net10 support. This is the first thing to confirm.
3. **`.runsettings`** (repo-relevant for headless): include `<Parameters>--headless</Parameters>` in the RunConfiguration TestRunParameters (see the CI section / gdUnit4 docs).
4. **First run to validate:** in-editor via the GdUnit4 inspector, then headless: `dotnet test <test.csproj> --settings .runsettings`. Once green locally, commit and flip the CI workflow trigger to `push`/`pull_request`.

## Writing a scene test (the pattern I'll use)
```csharp
using GdUnit4;
using static GdUnit4.Assertions;   // AssertThat, etc.

[TestSuite]
public class BattleSceneTests
{
    [TestCase]
    public void Party_IncludesDeclaredAllies()
    {
        // ISceneRunner runner = ISceneRunner.Load("res://scenes/BattleScene.tscn");
        // drive _Ready/initialize with BattleArgs, then assert party membership
        AssertThat(2 + 2).IsEqual(4); // placeholder until packages+addon are in
    }
}
```
GdUnit4 provides `ISceneRunner.Load("res://…")` to instantiate a scene, pump frames, simulate input, and assert node state — ideal for verifying the BattleScene party/ally wiring against the tested `PartyBattleController`.

## CI
`.github/workflows/godot-tests.yml` uses `godot-gdunit-labs/gdUnit4-action@v1` with `godot-net: true`, Godot **4.6.1**, pointed at the scene-test path. It is **`workflow_dispatch` (manual) for now** so it doesn't report red before the one-time setup lands. After the first local green run + addon commit, change its `on:` to `push`/`pull_request`.

## What this unlocks
Once the harness is live, the flagged **BattleScene multi-party/ally wiring** (rendering Darius/Tywin, current-turn input, ally AI) becomes CI-verifiable: I write the scene tests, CI runs them headless, and I iterate on the results — closing the one gap where I currently can't self-verify Godot-layer changes.
