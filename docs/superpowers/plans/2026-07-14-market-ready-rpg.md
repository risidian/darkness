# Darkness: Reforged — Market-Ready Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Take Darkness: Reforged from ~65% completion to a shippable, market-ready cross-platform RPG with full Arc 1–2 story, complete loot loop, working Android export, and all six agent specialisations contributing production-quality deliverables.

**Architecture:** Darkness.Core holds all game logic (services, models, combat engine); Darkness.Godot is the rendering/input host; all content is data-driven via JSON seed files requiring zero code changes for most additions; Darkness.Tests enforces correctness via XUnit+Moq with mandatory regression tests on every bug fix.

**Tech Stack:** Godot 4.6.1 (.NET 10), C# 13, LiteDB 5.0.21, SkiaSharp 3.119.2, XUnit, Moq, ASP.NET Core 10 (WebAPI), CommunityToolkit.Mvvm 8.4.2, Newtonsoft.Json 13.0.3

## Global Constraints

- .NET 10, `ImplicitUsings`, `Nullable`, `TreatWarningsAsErrors` — zero warnings permitted
- All game logic in `Darkness.Core`. Godot project is rendering + input only. No business logic in `.cs` scene files.
- Services resolved via `Global.Services` only. No `new ServiceName()` anywhere.
- New game content (items, quests, skills, encounters, recipes) added via JSON seed files only — no code changes for content.
- LiteDB seeders use `DeleteAll()` + re-insert pattern (idempotent).
- Every bug fix MUST include a regression test (TDD: write failing test first).
- Full test run: `dotnet test Darkness.Tests -p:ParallelizeTestCollections=false` — must be green before every commit.
- Use `global::Godot.FileAccess` when `System.IO` is in scope.
- Use `using SystemJson = System.Text.Json` when `LiteDB` is in scope.
- App identifier: `com.risidian.darkness`
- Viewport: 1280×720, `gl_compatibility`, `canvas_items` stretch, pixel snapping enabled.
- Commit message format: `type(scope): description` — e.g. `fix(deathmatch): correct BattleArgs navigation key`

---

## PHASE 1 — Critical Bug Fixes
*Prerequisite for all other phases. These are crashes and broken systems.*

---

### Task 1: Fix DeathmatchScene Navigation Bug

**Owner:** Lead Systems Architect
**Files:**
- Modify: `Darkness.Godot/scenes/DeathmatchScene.cs`
- Test: `Darkness.Tests/Services/DeathmatchServiceTests.cs` (create)

**Context:** `DeathmatchScene.cs` currently passes key `"Encounter"` to navigation but `BattleScene.Initialize()` reads key `"Args"` expecting a `BattleArgs` typed object. The mismatch causes BattleScene to crash or load with null args on every Deathmatch entry.

**Interfaces:**
- Consumes: `INavigationService.NavigateToAsync(BattleArgs)` typed overload
- Consumes: `BattleArgs(CombatData combatData, string? questChainId, string? questStepId)`
- Produces: Working Deathmatch → BattleScene transition

- [ ] **Step 1: Write the failing regression test**

```csharp
// Darkness.Tests/Services/DeathmatchServiceTests.cs
public class DeathmatchServiceTests
{
    [Fact]
    public async Task GetEncountersAsync_ReturnsNonEmptyList()
    {
        var service = new DeathmatchService();
        var encounters = await service.GetEncountersAsync();
        Assert.NotEmpty(encounters);
    }

    [Fact]
    public async Task GetEncountersAsync_AllEncountersHaveEnemies()
    {
        var service = new DeathmatchService();
        var encounters = await service.GetEncountersAsync();
        Assert.All(encounters, e => Assert.NotEmpty(e.Enemies));
    }
}
```

- [ ] **Step 2: Run to document current state**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~DeathmatchServiceTests" -p:ParallelizeTestCollections=false
```

- [ ] **Step 3: Fix DeathmatchScene navigation — construct BattleArgs correctly**

In `DeathmatchScene.cs`, locate the navigation call and replace:
```csharp
// Before (broken):
await _navigationService.NavigateToAsync("BattlePage",
    new Dictionary<string, object> { { "Encounter", _selectedEncounter } });

// After (correct):
var combatData = new CombatData { Enemies = _selectedEncounter.Enemies.ToList() };
var args = new BattleArgs(combatData, questChainId: null, questStepId: null);
await _navigationService.NavigateToAsync(args);
```

- [ ] **Step 4: Run full test suite**

```bash
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false
```
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/scenes/DeathmatchScene.cs Darkness.Tests/Services/DeathmatchServiceTests.cs
git commit -m "fix(deathmatch): construct BattleArgs correctly to fix crash on deathmatch entry"
```

---

### Task 2: Fix DialogService.DisplayConfirmAsync Always Returns True

**Owner:** Lead Systems Architect
**Files:**
- Modify: `Darkness.Godot/Services/GodotDialogService.cs`
- Test: `Darkness.Tests/Services/DialogServiceContractTests.cs` (create)

**Context:** `DisplayConfirmAsync()` has `bool confirmed = true` hardcoded. Any destructive confirmation (delete character, etc.) always proceeds regardless of user clicking Cancel.

- [ ] **Step 1: Write interface contract test**

```csharp
// Darkness.Tests/Services/DialogServiceContractTests.cs
public class DialogServiceContractTests
{
    [Fact]
    public async Task DisplayConfirmAsync_WhenCancelled_ReturnsFalse()
    {
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.DisplayConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        var result = await mock.Object.DisplayConfirmAsync("Delete?", "Cannot be undone.");
        Assert.False(result);
    }

    [Fact]
    public async Task DisplayConfirmAsync_WhenConfirmed_ReturnsTrue()
    {
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.DisplayConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        var result = await mock.Object.DisplayConfirmAsync("Delete?", "Cannot be undone.");
        Assert.True(result);
    }
}
```

- [ ] **Step 2: Implement signal-based confirm**

```csharp
// GodotDialogService.cs - replace DisplayConfirmAsync
public async Task<bool> DisplayConfirmAsync(string title, string message)
{
    var tcs = new TaskCompletionSource<bool>();
    var dialog = new ConfirmationDialog();
    dialog.Title = title;
    dialog.DialogText = message;
    dialog.Confirmed += () => tcs.TrySetResult(true);
    dialog.Canceled += () => tcs.TrySetResult(false);
    _sceneRoot.AddChild(dialog);
    dialog.PopupCentered();
    var result = await tcs.Task;
    dialog.QueueFree();
    return result;
}
```

- [ ] **Step 3: Run full suite and commit**

```bash
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false
git add Darkness.Godot/Services/GodotDialogService.cs Darkness.Tests/Services/DialogServiceContractTests.cs
git commit -m "fix(dialog): implement signal-based DisplayConfirmAsync returning false on cancel"
```

---

### Task 3: Fix CraftingService Upgrade — Wire Material Check

**Owner:** Lead Systems Architect
**Files:**
- Modify: `Darkness.Core/Services/CraftingService.cs`
- Test: `Darkness.Tests/Services/CraftingServiceTests.cs` (add cases)

**Context:** `UpgradeItemAsync()` has a `// TODO: Check materials` comment. Material requirements silently ignored; only gold is checked.

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task UpgradeItemAsync_WhenRequiredMaterialMissing_ReturnsFalse()
{
    var character = CreateTestCharacter();
    var sword = new Item { Name = "Iron Sword", ItemType = "Weapon", Tier = 1, Value = 50 };
    character.Inventory.Add(sword);
    character.Gold = 1000; // Enough gold, but no Iron Ore

    var result = await _service.UpgradeItemAsync(character, sword,
        requiredMaterial: "Iron Ore", requiredQuantity: 1);

    Assert.False(result.Success);
    Assert.Equal(1, sword.Tier); // Unchanged
}

[Fact]
public async Task UpgradeItemAsync_WhenMaterialPresent_ConsumesAndUpgrades()
{
    var character = CreateTestCharacter();
    var sword = new Item { Name = "Iron Sword", ItemType = "Weapon", Tier = 1, Value = 50 };
    var ore = new Item { Name = "Iron Ore", ItemType = "Material", Quantity = 2 };
    character.Inventory.AddRange(new[] { sword, ore });
    character.Gold = 200;

    var result = await _service.UpgradeItemAsync(character, sword,
        requiredMaterial: "Iron Ore", requiredQuantity: 1);

    Assert.True(result.Success);
    Assert.Equal(2, sword.Tier);
    Assert.Equal(1, character.Inventory.First(i => i.Name == "Iron Ore").Quantity);
}
```

- [ ] **Step 2: Run to verify tests fail**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~CraftingServiceTests" -p:ParallelizeTestCollections=false
```

- [ ] **Step 3: Implement material check**

In `CraftingService.UpgradeItemAsync()`, replace the TODO comment:
```csharp
if (!string.IsNullOrEmpty(requiredMaterial) && requiredQuantity > 0)
{
    var mat = character.Inventory
        .FirstOrDefault(i => i.Name == requiredMaterial && i.Quantity >= requiredQuantity);
    if (mat == null)
        return new CraftResult { Success = false,
            Message = $"Missing {requiredQuantity}x {requiredMaterial}." };
    mat.Quantity -= requiredQuantity;
    if (mat.Quantity <= 0) character.Inventory.Remove(mat);
}
```

- [ ] **Step 4: Run full suite and commit**

```bash
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false
git add Darkness.Core/Services/CraftingService.cs Darkness.Tests/Services/CraftingServiceTests.cs
git commit -m "fix(crafting): implement material requirement check in UpgradeItemAsync (resolves TODO)"
```

---

### Task 4: Repo Hygiene — Remove Dead Files

**Owner:** Lead Systems Architect
**Files:**
- Delete: `Darkness.Game/` directory
- Delete: `Darkness.Godot/*.csproj.old*`
- Delete: root `check_dims.cs`, `output_test.txt`, `godot_out.txt`, `godot_output.txt`
- Delete: `Darkness.Tests/UnitTest1.cs`
- Modify: `Darkness.sln` — remove Darkness.Game reference

- [ ] **Step 1: Remove from solution and delete**

```bash
dotnet sln Darkness.sln remove Darkness.Game/Darkness.Game.csproj
rm -rf Darkness.Game/
rm -f Darkness.Godot/Darkness.Godot.csproj.old*
rm -f check_dims.cs output_test.txt godot_out.txt godot_output.txt
rm -f Darkness.Tests/UnitTest1.cs
```

- [ ] **Step 2: Verify build still passes**

```bash
dotnet build Darkness.sln
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "chore(repo): remove dead Darkness.Game project, stale .csproj backups, placeholder test, and empty root artifacts"
```

---

## PHASE 2 — Content Foundation
*All tasks in this phase are JSON-only. Zero code changes. Runs CONCURRENTLY with Phase 1.*

---

### Task 5: Expand Items — Weapons, Consumables & Materials

**Owner:** Narrative & World Designer
**Files:**
- Modify: `Darkness.Godot/assets/data/items.json`

**Item JSON schema (exact field names required by ItemSeeder):**
```json
{
  "Id": "item_iron_sword",
  "Name": "Iron Sword",
  "Description": "A standard iron sword. Reliable and affordable.",
  "ItemType": "Weapon",
  "WeaponType": "Sword",
  "EquipSlot": "MainHand",
  "Tier": 1,
  "Weight": 3.5,
  "Value": 80,
  "RequiredLevel": 1,
  "StatBonus": { "Attack": 6 }
}
```

- [ ] **Step 1: Add 8 weapons** — Iron Sword (Sword/MainHand), Iron Dagger (Dagger/MainHand), Hunting Bow (Bow/MainHand), Oak Staff (Wand/MainHand), Iron Axe (Axe/MainHand), Iron Mace (Mace/MainHand), Leather Knuckles (Unarmed/MainHand), Apprentice Wand (Wand/MainHand, T1 starter lower stats)

- [ ] **Step 2: Add 4 consumables** — Small Health Potion (RestoreHP 50), Large Health Potion (RestoreHP 150), Small Mana Potion (RestoreMP 30), Large Mana Potion (RestoreMP 80)

- [ ] **Step 3: Add 6 materials** — Iron Ore, Iron Scrap, Leather Scraps, Magic Essence, Dark Essence, Light Essence

- [ ] **Step 4: Validate and test**

```bash
python3 -c "import json; json.load(open('Darkness.Godot/assets/data/items.json')); print('Valid JSON')"
dotnet test Darkness.Tests --filter "FullyQualifiedName~ItemSeeder" -p:ParallelizeTestCollections=false
```

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/assets/data/items.json
git commit -m "content(items): expand from 6 to 24 items — weapons, consumables, essences, materials"
```

---

### Task 6: Expand Items — Armor & Shields

**Owner:** Narrative & World Designer
**Files:**
- Modify: `Darkness.Godot/assets/data/items.json`

- [ ] **Step 1: Add 8 armor pieces** — Leather Helm (Head/T1), Leather Chest (Chest/T1), Leather Legs (Legs/T1), Leather Boots (Feet/T1), Iron Helm (Head/T2), Iron Chest (Chest/T2), Iron Legs (Legs/T2), Iron Boots (Feet/T2)

- [ ] **Step 2: Add 3 shields** — Wooden Buckler (OffHand/T1 Defense+3), Iron Shield (OffHand/T2 Defense+7), Knight Shield (OffHand/T3 Defense+12)

- [ ] **Step 3: Validate, test, commit**

```bash
python3 -c "import json; json.load(open('Darkness.Godot/assets/data/items.json')); print('Valid JSON')"
dotnet test Darkness.Tests --filter "FullyQualifiedName~ItemSeeder" -p:ParallelizeTestCollections=false
git add Darkness.Godot/assets/data/items.json
git commit -m "content(items): add 11 armor and shield items across tiers 1-3"
```

---

### Task 7: Expand Recipes Database

**Owner:** Narrative & World Designer
**Files:**
- Modify: `Darkness.Godot/assets/data/recipes.json`

**Recipe JSON schema:**
```json
{
  "Id": "recipe_iron_sword",
  "Name": "Forge Iron Sword",
  "Description": "Craft a basic iron sword from ore and scraps.",
  "RequiredMaterials": [
    { "ItemName": "Iron Ore", "Quantity": 2 },
    { "ItemName": "Iron Scrap", "Quantity": 1 }
  ],
  "ResultItemName": "Iron Sword",
  "ResultQuantity": 1,
  "RequiredLevel": 1
}
```

- [ ] **Step 1: Add 12 recipes** — Iron Sword, Iron Dagger, Iron Axe, Iron Mace, Oak Staff, Hunting Bow, Leather Helm, Leather Chest, Leather Legs, Leather Boots, Wooden Buckler, Small Health Potion

- [ ] **Step 2: Validate, test, commit**

```bash
python3 -c "import json; json.load(open('Darkness.Godot/assets/data/recipes.json')); print('Valid JSON')"
dotnet test Darkness.Tests --filter "FullyQualifiedName~CraftingService" -p:ParallelizeTestCollections=false
git add Darkness.Godot/assets/data/recipes.json
git commit -m "content(recipes): expand from 2 to 12 recipes covering all craftable weapons and armor"
```

---

### Task 8: Expand Encounter Zones

**Owner:** Narrative & World Designer
**Files:**
- Modify: `Darkness.Godot/assets/data/encounters.json`

**Encounter JSON schema:**
```json
{
  "BackgroundKey": "forest",
  "EncounterChance": 15,
  "EncounterDistance": 700,
  "Entries": [
    {
      "Weight": 50,
      "Enemies": [
        { "Name": "Gray Wolf", "SpriteKey": "monsters/wolf", "Level": 3,
          "HP": 45, "Attack": 8, "Defense": 3, "ExperienceReward": 25 }
      ]
    }
  ]
}
```

- [ ] **Step 1: Add forest zone** — wolves (level 3-4), bandits (level 4-5), goblins (level 3)

- [ ] **Step 2: Add dark_castle zone** — skeleton warrior (level 9), wraith (level 10), undead knight (level 11)

- [ ] **Step 3: Add battlefield zone** — imperial soldier (level 6), crossbowman (level 7), mercenary (level 8)

- [ ] **Step 4: Add camelot_shore_2 zone** — shore crabs (level 4), pirates (level 5-6)

- [ ] **Step 5: Tune camelot_shore** — add 2 more enemy types, adjust weights

- [ ] **Step 6: Validate, test, commit**

```bash
python3 -c "import json; json.load(open('Darkness.Godot/assets/data/encounters.json')); print('Valid JSON')"
dotnet test Darkness.Tests --filter "FullyQualifiedName~EncounterService" -p:ParallelizeTestCollections=false
git add Darkness.Godot/assets/data/encounters.json
git commit -m "content(encounters): add 4 new encounter zones (forest, dark_castle, battlefield, camelot_shore_2)"
```

---

### Task 9: Expand Random Rewards

**Owner:** Narrative & World Designer
**Files:**
- Modify: `Darkness.Godot/assets/data/random-rewards.json`

- [ ] **Step 1: Expand to 20+ entries with tier-based distribution**

Add entries for: early-game (health potions, iron scraps, small gold), mid-game (weapons, armor pieces, materials), rare (essences, shields, large potions). Each entry should have a `Weight` field for probability.

- [ ] **Step 2: Validate, test, commit**

```bash
python3 -c "import json; json.load(open('Darkness.Godot/assets/data/random-rewards.json')); print('Valid JSON')"
dotnet test Darkness.Tests --filter "FullyQualifiedName~RewardService" -p:ParallelizeTestCollections=false
git add Darkness.Godot/assets/data/random-rewards.json
git commit -m "content(rewards): expand from 6 to 22 reward entries with tier-weighted distribution"
```

---

## PHASE 3 — Feature Completion

### Task 10: AllyService — LiteDB Persistence

**Owner:** Lead Systems Architect
**Files:**
- Modify: `Darkness.Core/Services/AllyService.cs`
- Test: `Darkness.Tests/Services/AllyServiceTests.cs` (create)

**Context:** Replaces hardcoded mock with LiteDB persistence against real `User` records via `IUserService`.

- [ ] **Step 1: Write failing tests**

```csharp
public class AllyServiceTests
{
    [Fact]
    public async Task SendAllyRequestAsync_ToExistingUser_CreatesPendingRequest()
    {
        // Setup in-memory DB, create target user, send request
        // Assert: GetAlliesForUserAsync returns 1 ally with Status="Pending"
    }

    [Fact]
    public async Task RespondToAllyRequestAsync_Accept_SetsStatusAccepted()
    {
        // Setup: send request, then accept
        // Assert: Status == "Accepted"
    }

    [Fact]
    public async Task SendAllyRequestAsync_ToNonExistentUser_ReturnsFalse()
    {
        // Assert: result.Success == false, "User not found" message
    }

    [Fact]
    public async Task SendAllyRequestAsync_DuplicateRequest_ReturnsFalse()
    {
        // Send same request twice, assert second fails
    }
}
```

- [ ] **Step 2: Run to verify failures**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~AllyServiceTests" -p:ParallelizeTestCollections=false
```

- [ ] **Step 3: Implement LiteDB-backed AllyService**

```csharp
public class AllyService : IAllyService
{
    private readonly LocalDatabaseService _db;
    private readonly IUserService _userService;

    public AllyService(LocalDatabaseService db, IUserService userService)
    { _db = db; _userService = userService; }

    public async Task<List<Ally>> GetAlliesForUserAsync(int userId)
    {
        var col = _db.Database.GetCollection<Ally>("allies");
        return await Task.FromResult(
            col.Find(a => a.UserId == userId || a.AllyUserId == userId).ToList());
    }

    public async Task<AllyResult> SendAllyRequestAsync(int requesterId, string targetUsername)
    {
        var target = await _userService.GetByUsernameAsync(targetUsername);
        if (target == null)
            return new AllyResult { Success = false, Message = "User not found." };

        var col = _db.Database.GetCollection<Ally>("allies");
        var existing = col.FindOne(a =>
            (a.UserId == requesterId && a.AllyUserId == target.Id) ||
            (a.UserId == target.Id && a.AllyUserId == requesterId));
        if (existing != null)
            return new AllyResult { Success = false, Message = "Request already exists." };

        col.Insert(new Ally
        {
            UserId = requesterId, AllyUserId = target.Id,
            AllyUsername = target.Username, Status = "Pending"
        });
        return new AllyResult { Success = true, Message = "Ally request sent." };
    }

    public async Task<AllyResult> RespondToAllyRequestAsync(int allyId, bool accept)
    {
        var col = _db.Database.GetCollection<Ally>("allies");
        var ally = col.FindById(allyId);
        if (ally == null) return new AllyResult { Success = false, Message = "Request not found." };
        ally.Status = accept ? "Accepted" : "Declined";
        col.Update(ally);
        return await Task.FromResult(new AllyResult { Success = true });
    }
}
```

- [ ] **Step 4: Run full suite and commit**

```bash
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false
git add Darkness.Core/Services/AllyService.cs Darkness.Tests/Services/AllyServiceTests.cs
git commit -m "feat(allies): replace mock AllyService with LiteDB-backed persistence against real User records"
```

---

### Task 11: DeathmatchService — Data-Driven with Seeder

**Owner:** Lead Systems Architect + Narrative & World Designer
**Files:**
- Modify: `Darkness.Core/Services/DeathmatchService.cs`
- Create: `Darkness.Godot/assets/data/deathmatch-encounters.json`
- Create: `Darkness.Core/Seeders/DeathmatchSeeder.cs`
- Modify: `Darkness.Godot/Global.cs` — register seeder
- Test: `Darkness.Tests/Services/DeathmatchServiceTests.cs` (expand)

- [ ] **Step 1: Create deathmatch-encounters.json** with 8 waves (Easy → Legendary), each with `Id`, `Title`, `Description`, `Difficulty`, `UnlockRequirement` (null or prior wave Id), and `Enemies[]`

- [ ] **Step 2: Write failing tests for seeded service**

```csharp
[Fact]
public async Task GetEncountersAsync_Returns8Waves_AfterSeeding()
{
    // Seed + call service, assert count == 8
}
[Fact]
public async Task GetEncountersAsync_Wave2_LockedForFreshCharacter()
{
    // Wave 2 requires wave 1 cleared, fresh char has none cleared
    // Assert wave2.IsUnlocked == false
}
```

- [ ] **Step 3: Implement DeathmatchSeeder** (mirror pattern from existing seeders: `DeleteAll()` + load JSON + insert)

- [ ] **Step 4: Implement LiteDB-backed DeathmatchService.GetEncountersAsync()** — loads from DB, checks unlock requirements

- [ ] **Step 5: Register DeathmatchSeeder in Global.cs startup**

- [ ] **Step 6: Run full suite and commit**

```bash
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false
git add -A
git commit -m "feat(deathmatch): replace hardcoded encounters with data-driven LiteDB-backed DeathmatchService + seeder"
```

---

### Task 12: Audio System Foundation

**Owner:** Audio & Atmosphere Engineer
**Files:**
- Create: `Darkness.Godot/scenes/autoload/AudioManager.tscn` + `AudioManager.cs`
- Modify: `Darkness.Core/Models/GameSettings.cs` — add `VolumeAmbience`
- Modify: `Darkness.Core/Services/SettingsService.cs` — persist new field
- Modify: `Darkness.Godot/scenes/SettingsScene.cs` — wire sliders to AudioManager
- Test: `Darkness.Tests/Services/SettingsServiceTests.cs` — add Ambience persistence test

- [ ] **Step 1: Add VolumeAmbience to GameSettings**

```csharp
public float VolumeAmbience { get; set; } = 1.0f;
```

- [ ] **Step 2: Write test for persistence**

```csharp
[Fact]
public async Task SaveSettingsAsync_VolumeAmbience_PersistsAndReloads()
{
    var service = new SettingsService(new FileSystemService());
    await service.SaveSettingsAsync(new GameSettings { VolumeAmbience = 0.7f });
    var loaded = await service.LoadSettingsAsync();
    Assert.Equal(0.7f, loaded.VolumeAmbience, precision: 2);
}
```

- [ ] **Step 3: Implement AudioManager autoload**

```csharp
public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; } = null!;
    private AudioStreamPlayer _musicPlayer = null!;
    private AudioStreamPlayer _ambiencePlayer = null!;

    public override void _Ready()
    {
        Instance = this;
        _musicPlayer = GetNode<AudioStreamPlayer>("MusicPlayer");
        _ambiencePlayer = GetNode<AudioStreamPlayer>("AmbiencePlayer");
    }

    public void PlayMusic(string resourcePath)
    {
        var stream = GD.Load<AudioStream>(resourcePath);
        _musicPlayer.Stream = stream;
        _musicPlayer.Play();
    }

    public void PlaySFX(string resourcePath)
    {
        var player = new AudioStreamPlayer();
        player.Bus = "SFX";
        player.Stream = GD.Load<AudioStream>(resourcePath);
        AddChild(player);
        player.Play();
        player.Finished += () => player.QueueFree();
    }

    public void SetBusVolume(string busName, float linearVolume)
    {
        int idx = AudioServer.GetBusIndex(busName);
        if (idx >= 0) AudioServer.SetBusVolumeDb(idx, Mathf.LinearToDb(linearVolume));
    }
}
```

- [ ] **Step 4: Wire SettingsScene sliders**

```csharp
_masterSlider.ValueChanged += v => AudioManager.Instance.SetBusVolume("Master", (float)v);
_musicSlider.ValueChanged += v => AudioManager.Instance.SetBusVolume("Music", (float)v);
_sfxSlider.ValueChanged += v => AudioManager.Instance.SetBusVolume("SFX", (float)v);
```

- [ ] **Step 5: Register AudioManager as Godot autoload in project.godot**

- [ ] **Step 6: Run suite and commit**

```bash
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false
git add -A
git commit -m "feat(audio): add AudioManager autoload with 4 audio buses, wire SettingsScene sliders to AudioServer"
```

---

### Task 13: Zone Music & Battle Music Transitions

**Owner:** Audio & Atmosphere Engineer
**Files:**
- Modify: `Darkness.Godot/scenes/WorldScene.cs`
- Modify: `Darkness.Godot/scenes/BattleScene.cs`
- Modify: `Darkness.Core/Models/BattleArgs.cs` — add `ReturnMusicPath`
- Create: `Darkness.Godot/assets/audio/music/` (placeholder .ogg files — commit directory with .gitkeep)

- [ ] **Step 1: Add ReturnMusicPath to BattleArgs**

```csharp
public string? ReturnMusicPath { get; init; }
```

- [ ] **Step 2: Add zone → music mapping in WorldScene**

```csharp
private static readonly Dictionary<string, string> ZoneMusicPaths = new()
{
    ["camelot_shore"]   = "res://assets/audio/music/camelot_shore.ogg",
    ["forest"]          = "res://assets/audio/music/forest_theme.ogg",
    ["dark_castle"]     = "res://assets/audio/music/dark_castle.ogg",
    ["battlefield"]     = "res://assets/audio/music/battlefield.ogg",
};
```

- [ ] **Step 3: Play zone music on zone change, pass ReturnMusicPath via BattleArgs**

- [ ] **Step 4: Play battle music in BattleScene._Ready(), restore zone music in Victory() and GameOver()**

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(audio): zone-based music transitions, battle music on combat entry with return-music passthrough via BattleArgs"
```

---

## PHASE 4 — Story Expansion (Arc 2)

### Task 14: Arc 2 — "The Castle of Horror" (Entry & Exploration)

**Owner:** Narrative & World Designer
**Files:**
- Create: `Darkness.Godot/assets/data/quests/beat_10_castle_horror_entry.json`
- Create: `Darkness.Godot/assets/data/quests/beat_11_castle_horror_depth.json`

**Context:** Arc 2 begins after `beat_9_kyarias_final` is completed. Story: players infiltrate the Castle of Horror to find the source of undead corruption. References `dark_castle` background + encounters already defined in Task 8.

- [ ] **Step 1: Write beat_10_castle_horror_entry.json** — 4+ steps: arrival dialogue → gate branch (combat force OR stealth side entrance) → convergence step → deeper discovery

- [ ] **Step 2: Write beat_11_castle_horror_depth.json** — 3+ steps: interior exploration → trap/puzzle encounter → discovery of corruption source (dialogue)

- [ ] **Step 3: Validate with QuestGraphAudit**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~QuestGraphAudit" -p:ParallelizeTestCollections=false
```
Expected: No broken step references or orphan steps.

- [ ] **Step 4: Run JSON round-trip test**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~QuestJsonRoundTrip" -p:ParallelizeTestCollections=false
```

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/assets/data/quests/beat_10_castle_horror_entry.json \
        Darkness.Godot/assets/data/quests/beat_11_castle_horror_depth.json
git commit -m "content(story): Arc 2 — Castle of Horror entry and interior exploration quest chains (beats 10-11)"
```

---

### Task 15: Arc 2 — Boss Encounter & Resolution

**Owner:** Narrative & World Designer
**Files:**
- Create: `Darkness.Godot/assets/data/quests/beat_12_castle_horror_boss.json`

**Context:** Final Arc 2 chain. Prerequisites: beat_11. Boss: Warlord of the Damned (high HP, tough). Resolution has morality-based branch outcomes.

- [ ] **Step 1: Write beat_12_castle_horror_boss.json** — 3 steps: pre-boss dialogue → boss combat (Warlord of the Damned, level 13, HP 250+) → post-battle resolution branch (spare/execute the corrupted soul — morality impact ±10)

- [ ] **Step 2: Validate and test**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~QuestGraphAudit" -p:ParallelizeTestCollections=false
dotnet test Darkness.Tests --filter "FullyQualifiedName~QuestJsonRoundTrip" -p:ParallelizeTestCollections=false
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/assets/data/quests/beat_12_castle_horror_boss.json
git commit -m "content(story): Arc 2 complete — Castle of Horror boss and morality-branched resolution (beat 12)"
```

---

## PHASE 5 — Platform & Polish

### Task 16: Fix Android Export — .sln Path Mismatch

**Owner:** Product Manager & Launch Specialist
**Files:**
- Modify: `build-android.ps1`
- Modify: `export_presets.cfg`

**Context:** The C# .sln path is hardcoded to `C:\Users\Mayce\Documents\GitHub\darkness\...` which doesn't match the actual repo at `C:\Users\mayce\source\repos\darkness\...`.

- [ ] **Step 1: Update build-android.ps1 to resolve path relative to script location**

```powershell
$repoRoot = Split-Path -Parent $PSScriptRoot
$godotSlnPath = Join-Path $repoRoot "Darkness.Godot\Darkness.Godot.sln"
if (-not (Test-Path $godotSlnPath)) {
    Write-Error "Cannot find Godot solution at: $godotSlnPath"
    exit 1
}
```

- [ ] **Step 2: Update export_presets.cfg** — replace hardcoded path with relative reference

- [ ] **Step 3: Verify path resolves**

```powershell
$repoRoot = "C:\Users\mayce\source\repos\darkness"
Test-Path (Join-Path $repoRoot "Darkness.Godot\Darkness.Godot.sln")
```
Expected: `True`

- [ ] **Step 4: Commit**

```bash
git add build-android.ps1 export_presets.cfg
git commit -m "fix(android): use repo-relative .sln path in build script and export presets"
```

---

### Task 17: Fix Android Export — Template SDK Mismatch

**Owner:** Product Manager & Launch Specialist
**Files:**
- Update: Android export template (Godot editor action)
- Modify: `export_presets.cfg` — confirm template version matches 4.6.1

**Context:** GodotApp.java fails with 17 errors because the Android template targets a different Godot API version than 4.6.1.

- [ ] **Step 1: Download correct Android template** for Godot 4.6.1 from `https://github.com/godotengine/godot/releases/tag/4.6.1-stable`

- [ ] **Step 2: Install template** via Godot Editor → Export → Manage Export Templates → Install From File

- [ ] **Step 3: Run full Android export**

```powershell
pwsh build-android.ps1
```
Expected: `bin/Darkness.apk` produced, zero Java compile errors.

- [ ] **Step 4: Commit export_presets if updated**

```bash
git add export_presets.cfg
git commit -m "fix(android): install Godot 4.6.1 Android export template to resolve GodotApp.java SDK mismatch"
```

---

### Task 18: App Icon & Storefront Assets

**Owner:** Product Manager + Asset & UI Pipeline Director
**Files:**
- Create: `Darkness.Godot/assets/icon/icon_1024.png`
- Create: `Darkness.Godot/assets/icon/android_foreground.png`
- Create: `Darkness.Godot/assets/icon/android_background.png`
- Modify: `project.godot`

- [ ] **Step 1: Create 1024×1024 app icon** — dark atmospheric design with game title/character silhouette

- [ ] **Step 2: Create Android adaptive icon layers** — foreground (character on transparent, 432×432 safe zone), background (dark gradient or solid)

- [ ] **Step 3: Register in project.godot**

```ini
[application]
config/icon="res://assets/icon/icon_1024.png"
```

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/assets/icon/ project.godot
git commit -m "feat(launch): add 1024x1024 app icon and Android adaptive icon layers"
```

---

### Task 19: Sprite Compositor Performance — Result Caching

**Owner:** QA Tester & Performance Profiler + Lead Systems Architect
**Files:**
- Modify: `Darkness.Core/Services/SkiaSharpSpriteCompositor.cs`
- Test: `Darkness.Tests/Services/SkiaSharpSpriteCompositorTests.cs` (add cache hit test)

**Context:** `SkiaSharpSpriteCompositor.cs` (21KB) recomposites on every call. Cache results by appearance hash to avoid redundant compositing on re-renders.

- [ ] **Step 1: Write cache hit test**

```csharp
[Fact]
public async Task CompositeAsync_CalledTwiceWithSameAppearance_SecondCallUsesCache()
{
    var compositor = CreateCompositor();
    var appearance = CreateTestAppearance();
    var sw1 = Stopwatch.StartNew();
    await compositor.CompositeAsync(appearance);
    sw1.Stop();

    var sw2 = Stopwatch.StartNew();
    await compositor.CompositeAsync(appearance);
    sw2.Stop();

    // Second call should be dramatically faster (cache hit)
    Assert.True(sw2.ElapsedMilliseconds < sw1.ElapsedMilliseconds / 2,
        $"Expected cache hit to be >2x faster. First: {sw1.ElapsedMilliseconds}ms, Second: {sw2.ElapsedMilliseconds}ms");
}
```

- [ ] **Step 2: Implement appearance-hash cache**

```csharp
private readonly ConcurrentDictionary<string, byte[]> _cache = new();

public async Task<byte[]> CompositeAsync(CharacterAppearance appearance)
{
    var key = appearance.GetCacheKey(); // implement: hash of all appearance fields
    if (_cache.TryGetValue(key, out var cached)) return cached;

    var result = await CompositeInternalAsync(appearance);
    _cache.TryAdd(key, result);
    return result;
}
```

- [ ] **Step 3: Implement GetCacheKey() on CharacterAppearance**

```csharp
public string GetCacheKey() =>
    $"{Gender}|{SkinOption}|{HairOption}|{FaceOption}|{EyeOption}|" +
    $"{ArmorName}|{WeaponName}|{ShieldName}|{FeetName}|{LegsName}|{ArmsName}";
```

- [ ] **Step 4: Run full suite and commit**

```bash
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false
git add Darkness.Core/Services/SkiaSharpSpriteCompositor.cs \
        Darkness.Core/Models/CharacterAppearance.cs \
        Darkness.Tests/Services/SkiaSharpSpriteCompositorTests.cs
git commit -m "perf(compositor): add appearance-hash result caching — eliminates redundant sprite sheet recomposition"
```

---

### Task 20: Localisation Foundation

**Owner:** Product Manager & Launch Specialist
**Files:**
- Create: `Darkness.Godot/assets/locale/en.po`
- Create: `Darkness.Godot/assets/locale/LOCALE_README.md`
- Modify: `project.godot`

- [ ] **Step 1: Create en.po with all UI string keys**

```po
# Darkness: Reforged — English (canonical)
msgid "ui_new_game"
msgstr "New Game"
msgid "ui_continue"
msgstr "Continue"
msgid "ui_settings"
msgstr "Settings"
msgid "ui_logout"
msgstr "Logout"
# ... all button labels, scene titles, error messages
```

- [ ] **Step 2: Register in project.godot**

```ini
[internationalization]
locale/translations=PackedStringArray("res://assets/locale/en.translation")
```

- [ ] **Step 3: Write LOCALE_README.md** — how to add a new language (copy en.po, translate msgstr values, register in project.godot)

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/assets/locale/ project.godot
git commit -m "feat(i18n): add English PO translation baseline and localisation pipeline documentation"
```

---

### Task 21: Launch Readiness — Final QA Sign-off

**Owner:** QA Tester & Performance Profiler + Product Manager
**Files:**
- Create: `docs/launch-readiness.md`

- [ ] **Step 1: Run complete test suite — must be 100% green**

```bash
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false --verbosity normal 2>&1 | tee docs/test-results.txt
```
Expected: All tests pass, zero failures, zero skipped.

- [ ] **Step 2: Build Android APK**

```powershell
pwsh build-android.ps1
```
Expected: `bin/Darkness.apk` produced, no errors. Record APK file size.

- [ ] **Step 3: Build Windows desktop export** via Godot editor → Export Windows → `bin/Darkness-Windows.zip`

- [ ] **Step 4: Write launch-readiness.md**

Document: test count + pass rate, APK size, Windows build size, performance baselines (FPS targets), platform sign-off checklist, known open items for post-launch.

- [ ] **Step 5: Tag release candidate**

```bash
git add docs/launch-readiness.md
git commit -m "docs(launch): launch readiness checklist, final QA sign-off, platform build verification"
git tag v1.0.0-rc1
```

---

## Execution Order & Parallelism

```
PARALLEL TRACK A              PARALLEL TRACK B
Phase 1 (Tasks 1-4)          Phase 2 (Tasks 5-9)
Bug fixes (code)              Content expansion (JSON-only)
     │                              │
     └──────────┬───────────────────┘
                │
          Phase 3 (Tasks 10-13)
          Feature completion
                │
          Phase 4 (Tasks 14-15)
          Arc 2 story content
                │
          Phase 5 (Tasks 16-21)
          Platform, polish, launch
```

**Concurrency rules:**
- **Tasks 1-4** and **Tasks 5-9** run in parallel — different file sets, zero conflicts.
- Within Phase 1: Tasks 1 and 2 are independent, can run in parallel.
- Within Phase 2: All 5 tasks are independent JSON files, can all run in parallel.
- Phase 3 tasks are sequential (each may reference state from the last).
- Phase 4 tasks are sequential (story beats build on each other).
- Phase 5 tasks are mostly independent (except Task 21 which requires all others complete).

---

## Progress Ledger

*Update after each reviewer approval. This file survives context compaction.*

```
Phase 1 — Bug Fixes
  Task 1:  [ ] DeathmatchScene navigation fix
  Task 2:  [ ] DialogService confirm always-true fix
  Task 3:  [ ] CraftingService upgrade material check
  Task 4:  [ ] Repo hygiene — dead files removed

Phase 2 — Content Foundation
  Task 5:  [ ] Items — weapons, consumables, materials (24 total)
  Task 6:  [ ] Items — armor and shields (11 total)
  Task 7:  [ ] Recipes — 12 craft recipes
  Task 8:  [ ] Encounters — 4 new zones
  Task 9:  [ ] Rewards — 22 entries with tier weighting

Phase 3 — Feature Completion
  Task 10: [ ] AllyService — LiteDB persistence
  Task 11: [ ] DeathmatchService — data-driven + seeder
  Task 12: [ ] Audio system foundation — AudioManager + buses
  Task 13: [ ] Zone music + battle music transitions

Phase 4 — Arc 2 Story
  Task 14: [ ] Arc 2 entry + exploration chains (beats 10-11)
  Task 15: [ ] Arc 2 boss + resolution (beat 12)

Phase 5 — Platform & Polish
  Task 16: [ ] Android .sln path fix
  Task 17: [ ] Android template SDK fix
  Task 18: [ ] App icon + storefront assets
  Task 19: [ ] Sprite compositor caching
  Task 20: [ ] Localisation foundation (en.po)
  Task 21: [ ] Launch readiness sign-off + v1.0.0-rc1 tag
```
