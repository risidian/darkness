# Sprite Equipment & Quest System Redesign

**Date:** 2026-04-05
**Goal:** Make both systems data-driven, expandable, and maintainable without constant code changes.

---

## 1. Sprite Equipment System

### Problem

All equipment-to-sprite mappings live in hardcoded dictionaries inside `SpriteLayerCatalog.cs`. Adding new equipment requires editing multiple dictionaries, the `Item` model has no connection to sprites, and a legacy sprite system adds confusion.

### Design

#### New Models

**`EquipmentSprite`** — LiteDB collection replacing all dictionaries:

```csharp
public class EquipmentSprite
{
    public int Id { get; set; }
    public string Slot { get; set; }              // "Armor", "Weapon", "Shield", "Feet", "Legs", "Arms"
    public string DisplayName { get; set; }       // "Plate (Steel)"
    public string AssetPath { get; set; }         // "armor/plate/steel"
    public string FileNameTemplate { get; set; }  // "{action}.png" or "{action}/steel.png"
    public int ZOrder { get; set; }               // Armor=60, Weapon=140, etc.
    public string Gender { get; set; }            // "universal", "male", "female"
    public string? FallbackGender { get; set; }   // e.g., "male" when female variant missing
    public string TintHex { get; set; }           // "#FFFFFF" default
}
```

**`AppearanceOption`** — LiteDB collection replacing hair/skin/face/eye dictionaries:

```csharp
public class AppearanceOption
{
    public int Id { get; set; }
    public string Category { get; set; }          // "Hair", "Skin", "Face", "Eyes", "Head"
    public string DisplayName { get; set; }       // "Long", "Light"
    public string AssetPath { get; set; }         // "hair/long/adult"
    public string FileNameTemplate { get; set; }
    public string TintHex { get; set; }
    public int ZOrder { get; set; }
}
```

**`Item` gains equipment link:**

```csharp
public class Item
{
    // ... existing properties ...
    public string? EquipmentSlot { get; set; }    // "Armor", "Weapon", "Shield", etc.
    public int? EquipmentSpriteId { get; set; }   // FK to EquipmentSprite
}
```

#### How It Works

1. `SpriteLayerCatalog` becomes a thin wrapper over LiteDB queries. `GetStitchLayers(CharacterAppearance)` queries `EquipmentSprites` and `AppearanceOptions` by slot + display name.
2. Equipping an item resolves its sprite through `EquipmentSpriteId`.
3. Adding new equipment = insert a record in LiteDB (seeded from `sprite-catalog.json`).
4. Z-order values move into DB records.

#### Seed Data

A `sprite-catalog.json` file provides initial data, loaded into LiteDB at first run or when the seed version changes. This keeps the data human-readable and version-controllable.

#### What Gets Deleted

- All `Dictionary<string, string>` maps in SpriteLayerCatalog (ArmorFileMap, WeaponMaterialMap, FeetFileMap, LegsFileMap, etc.)
- `SpriteLayerDefinition` model
- `GetLayersForAppearance()` method (legacy system)
- Legacy sprite rendering path in `LayeredSprite`
- Individual layer sprites in `sprites/` directory (keep `sprites/monsters/` only)
- All hardcoded list properties (WeaponTypes, ShieldTypes, HairStyles, HairColors, SkinColors, etc.)

---

## 2. Quest System

### Problem

Quests live in a single monolithic `quests.json`. `QuestNode` mixes dialogue, combat, and location concerns into one flat structure. Branching uses string IDs with no validation. Conditions are limited to morality checks. WorldScene has hardcoded quest IDs and coordinate-based triggers.

### Design

#### File Structure

```
Darkness.Godot/assets/data/quests/
├── beat_1_the_awakening.json
├── beat_2_dark_warrior.json
├── beat_3_the_sorcerer.json
└── ... one file per quest chain
```

Each file contains a quest chain — a root quest and all its branching sub-nodes.

#### New Models

**`QuestChain`** — top-level container per JSON file:

```csharp
public class QuestChain
{
    public string Id { get; set; }                // "beat_1"
    public string Title { get; set; }             // "The Awakening"
    public bool IsMainStory { get; set; }
    public int SortOrder { get; set; }            // story progression sequence
    public List<string> Prerequisites { get; set; } // other chain IDs
    public List<QuestStep> Steps { get; set; }
}
```

**`QuestStep`** — typed step replacing overloaded `QuestNode`:

```csharp
public class QuestStep
{
    public string Id { get; set; }                // "beat_1_combat"
    public string Type { get; set; }              // "dialogue", "combat", "location", "branch"
    public string? NextStepId { get; set; }       // linear progression
    public DialogueData? Dialogue { get; set; }
    public CombatData? Combat { get; set; }
    public LocationTrigger? Location { get; set; }
    public BranchData? Branch { get; set; }
}
```

**`BranchData`** — explicit branching separated from dialogue:

```csharp
public class BranchData
{
    public List<BranchOption> Options { get; set; }
}

public class BranchOption
{
    public string Text { get; set; }
    public string NextStepId { get; set; }
    public int MoralityImpact { get; set; }
    public List<BranchCondition>? Conditions { get; set; }
}
```

**`BranchCondition`** — extensible condition system:

```csharp
public class BranchCondition
{
    public string Type { get; set; }    // "morality", "class", "has_item", "quest_completed"
    public string Operator { get; set; } // ">=", "<=", "==", "contains"
    public string Value { get; set; }
}
```

Adding a new condition type = adding one case to a condition evaluator method.

**`CombatData`** (`EnemySpawn` wraps the existing `Enemy` model with spawn-specific fields; `RewardData` wraps `Item` with quantity):

```csharp
public class CombatData
{
    public List<EnemySpawn> Enemies { get; set; }
    public string? BackgroundKey { get; set; }
    public int? SurvivalTurns { get; set; }
    public List<RewardData>? Rewards { get; set; }
}

public class EnemySpawn
{
    public string EnemyType { get; set; }     // references Enemy definitions
    public int Count { get; set; }
    public int? LevelOverride { get; set; }
}

public class RewardData
{
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public int? ExperiencePoints { get; set; }
}
```

**`LocationTrigger`**:

```csharp
public class LocationTrigger
{
    public string LocationKey { get; set; }   // "sandy_shore_east"
    public string? SceneKey { get; set; }
}
```

#### Quest State (LiteDB)

**`QuestState`** — per-character, per-chain progress:

```csharp
public class QuestState
{
    public int Id { get; set; }
    public int CharacterId { get; set; }
    public string ChainId { get; set; }
    public string CurrentStepId { get; set; }
    public string Status { get; set; }            // "available", "in_progress", "completed"
    public Dictionary<string, string> Flags { get; set; } // quest-specific state
}
```

Replaces `Character.CompletedQuestIds`. State is queryable — you can see where a player is mid-chain, not just whether they finished.

#### Runtime Services

**`QuestService`** changes:
- Startup: reads all JSON files from `quests/` → inserts into LiteDB `QuestChains` and `QuestSteps` collections
- `GetAvailableChains(Character)` — queries by prereqs + conditions
- `GetCurrentStep(Character, string chainId)` — resolves position in a chain
- `AdvanceStep(Character, string stepId, string? choiceId)` — progresses state, applies morality
- `IsMainStoryComplete(Character)` — checks if all main story chains are completed

**`ITriggerService`** — replaces coordinate checks:

```csharp
public interface ITriggerService
{
    QuestStep? CheckLocationTrigger(Character character, string locationKey);
    void RegisterTriggerZone(string locationKey, string sceneKey);
}
```

WorldScene calls `TriggerService.CheckLocationTrigger("sandy_shore_east")` when the player enters a zone.

#### Startup Flow

1. App starts → `QuestSeeder` reads all files from `assets/data/quests/`
2. Deserializes each into `QuestChain`
3. Upserts chains and steps into LiteDB collections
4. `QuestService` queries LiteDB for all runtime operations

---

## 3. Hardcoded Logic Cleanup

### WorldScene.cs

| Line | Current Code | Replacement |
|------|-------------|-------------|
| 50 | `CompleteQuest(..., "beat_1_stealth")` | `QuestService.AdvanceStep()` |
| 56 | `_pendingNextQuestId = "beat_1_sneak_combat"` | Step resolution via `AdvanceStep()` |
| 492 | `questToTrigger.Id == "beat_1_stealth"` | `QuestStep.Type == "location"` check |
| ~460 | `X > 1200` coordinate trigger | `TriggerService.CheckLocationTrigger()` |
| 510-516 | No end-game handling | `QuestService.IsMainStoryComplete()` check |

### BattleScene.cs

| Issue | Current | Replacement |
|-------|---------|-------------|
| Victory navigation | OK button → `NavigateToAsync("MainMenuPage")` | `AdvanceStep()` → navigate to WorldScene |
| No quest awareness | Victory/Defeat don't touch quest state | `BattleArgs` carries `QuestChainId` + `QuestStepId`; victory calls `AdvanceStep()` |

### StealthScene.cs

| Issue | Current | Replacement |
|-------|---------|-------------|
| No quest context | Returns "Success"/"Failure" strings | `StealthArgs` carries quest context; success calls `AdvanceStep()` |

### NavigationArgs Updates

**`BattleArgs`** gains:

```csharp
public string? QuestChainId { get; set; }
public string? QuestStepId { get; set; }
```

**`StealthArgs`** gains the same fields.

### End-Game

`QuestService.IsMainStoryComplete(Character)` returns true when all `IsMainStory` chains have `Status == "completed"`. WorldScene checks this after each step advancement and can trigger an ending scene/dialogue — driven by data, not hardcoded.

### What Gets Deleted

- Monolithic `quests.json`
- `QuestNode` model (replaced by `QuestChain` + `QuestStep`)
- `Character.CompletedQuestIds` property
- All hardcoded quest ID strings in WorldScene
- `X > 1200` coordinate trigger logic
- Multi-fallback quest resolution chain in `TriggerEncounter()`
- Victory → MainMenuPage navigation in BattleScene

---

## 4. XP & Leveling System

### Problem

The data model has `Character.Level`, `Character.Experience`, `Character.AttributePoints`, `Enemy.ExperienceReward`, and a `Level` model with `ExperienceRequired` — but none of it is wired up. Victory in combat awards nothing. Enemies define XP rewards that are never read. There is no level-up logic, no stat scaling, and no UI feedback.

### Design

#### Level Table (LiteDB)

The existing `Level` model is already the right shape. Seed it as a LiteDB collection from a `level-table.json`:

```json
[
  { "Value": 1, "ExperienceRequired": 0 },
  { "Value": 2, "ExperienceRequired": 100 },
  { "Value": 3, "ExperienceRequired": 250 },
  { "Value": 4, "ExperienceRequired": 500 },
  { "Value": 5, "ExperienceRequired": 900 }
]
```

Data-driven — adjusting the XP curve means editing the JSON, not code. New levels are just new rows.

#### ILevelingService

A single service owns all leveling logic:

```csharp
public interface ILevelingService
{
    /// Awards XP and returns level-up results (if any)
    LevelUpResult AwardExperience(Character character, int xp);

    /// Returns XP needed to reach next level
    int GetXpToNextLevel(Character character);

    /// Returns the level for a given total XP amount
    int GetLevelForXp(int totalXp);
}
```

**`LevelUpResult`** — returned after XP is awarded:

```csharp
public class LevelUpResult
{
    public int XpAwarded { get; set; }
    public int TotalXp { get; set; }
    public int PreviousLevel { get; set; }
    public int NewLevel { get; set; }
    public bool DidLevelUp => NewLevel > PreviousLevel;
    public int LevelsGained => NewLevel - PreviousLevel;
    public int AttributePointsAwarded { get; set; }
}
```

#### AwardExperience Flow

```
1. character.Experience += xp
2. Query Level table: what level corresponds to new total XP?
3. If new level > current level:
   a. character.Level = newLevel
   b. Award AttributePoints (configurable per level, default: 2 per level gained)
   c. Restore HP to max (level-up heal)
   d. Return LevelUpResult with DidLevelUp = true
4. Persist character to LiteDB
5. Return LevelUpResult
```

Stat increases are NOT automatic — the player spends `AttributePoints` manually (this is already the pattern with the existing `AttributePoints` property). This keeps leveling simple and gives the player agency.

#### Combat Victory Flow (Updated)

The full victory sequence becomes:

```
Enemy defeated
  → Sum all Enemy.ExperienceReward values
  → LevelingService.AwardExperience(character, totalXp)
  → Sum all Enemy.GoldReward values (for future use)
  → Display victory panel with XP gained, level-up notification if applicable
  → QuestService.AdvanceStep() (quest completion)
  → Navigate back to WorldScene
```

#### BattleScene Victory UI Changes

The victory panel shows:
- XP awarded (sum of enemy rewards)
- XP progress bar to next level
- "LEVEL UP!" notification if `LevelUpResult.DidLevelUp`
- Attribute points available (if leveled up)
- Quest rewards (from `CombatData.Rewards`)

#### What Gets Wired Up (Existing but Unused)

| Property | Current State | After |
|----------|--------------|-------|
| `Character.Level` | Set to 1 at creation, never changes | Updated by `LevelingService` on level-up |
| `Character.Experience` | Set to 0 at creation, never changes | Accumulated after each combat |
| `Character.AttributePoints` | Set to 5 at creation, never changes | Increased on level-up |
| `Enemy.ExperienceReward` | Defined on enemies, never read | Summed and awarded on victory |
| `Enemy.GoldReward` | Defined on enemies, never read | Summed and awarded on victory |
| `Level` model | Exists with no data | Seeded from `level-table.json`, queried for thresholds |
| StatusBar EXP type | Defined with yellow color, never used | Displayed during combat and on world screen |

#### What Gets Created

- `ILevelingService` interface and `LevelingService` implementation
- `LevelUpResult` model
- `level-table.json` seed file
- Level-up UI in BattleScene victory panel

#### What Does NOT Change

- `Character.AttributePoints` spending UI — assumed to already exist or be a separate feature
- Stat formulas in `CombatEngine` — these already work with current stat values
- Enemy stat definitions — `ExperienceReward` and `GoldReward` just start being read

---

## 5. Migration Strategy

Since the game is in testing only, this is a clean replacement, not a migration:

**Track A — Sprite System:**
1. Build `EquipmentSprite` and `AppearanceOption` models and LiteDB collections
2. Create `sprite-catalog.json` seed file and seeding service
3. Rewrite `SpriteLayerCatalog` to query LiteDB
4. Link `Item` model to `EquipmentSprite`
5. Delete legacy sprite system (dictionaries, `SpriteLayerDefinition`, old rendering path)

**Track B — Quest System:**
6. Build `QuestChain`, `QuestStep`, `QuestState` models
7. Split `quests.json` into per-chain files with new schema
8. Rewrite `QuestService` with `AdvanceStep()` flow and LiteDB persistence
9. Build `TriggerService`
10. Update `BattleScene`, `StealthScene`, `WorldScene` to pass quest context
11. Remove all hardcoded quest IDs, coordinate triggers, and victory→main menu navigation
12. Delete `Character.CompletedQuestIds`, old `QuestNode` model, monolithic `quests.json`

**Track C — XP & Leveling:**
13. Build `ILevelingService` and `LevelingService`
14. Create `level-table.json` seed file
15. Wire XP awarding into BattleScene victory flow
16. Add level-up UI to victory panel
17. Wire `Enemy.ExperienceReward` and `Enemy.GoldReward` reads

**Track D — Tests & Logging:**
18. Update all affected tests across sprites, quests, and leveling (see Section 6)
19. Add debug logging for JSON loading failures (see Section 7)

---

## 6. Testing Strategy

All new services and models must have unit tests. Tests use XUnit + Moq, matching the existing `Darkness.Tests` conventions.

### Sprite System Tests

**SpriteLayerCatalog (rewritten):**
- Query returns correct `StitchLayer` for each equipment slot
- Unknown display name returns empty/fallback gracefully
- Gender fallback works (female request falls back to male when no female variant)
- Z-order values are correct per slot
- Tint hex values are applied correctly

**EquipmentSprite seeding:**
- Seed file loads all records into LiteDB
- Duplicate seed runs don't create duplicate records (upsert)
- Missing or malformed seed file logs error and throws descriptive exception

**Item ↔ Sprite link:**
- Item with `EquipmentSpriteId` resolves to correct `EquipmentSprite`
- Item without `EquipmentSpriteId` (consumables etc.) returns null sprite

### Quest System Tests

**QuestService:**
- `GetAvailableChains` respects prerequisites (chain not available until prereqs completed)
- `GetAvailableChains` respects morality and other `BranchCondition` types
- `GetCurrentStep` returns correct position mid-chain
- `AdvanceStep` with linear step updates `CurrentStepId` correctly
- `AdvanceStep` with branch choice follows correct `NextStepId`
- `AdvanceStep` on final step sets chain `Status = "completed"`
- `AdvanceStep` applies morality impact from branch choices
- `IsMainStoryComplete` returns true only when all main story chains are completed

**BranchCondition evaluator:**
- Each condition type evaluates correctly: `morality`, `class`, `has_item`, `quest_completed`
- Each operator works: `>=`, `<=`, `==`, `contains`
- Missing or null condition list is treated as "no conditions" (always passes)

**Quest seeding:**
- All quest chain files deserialize correctly
- Missing quest directory logs error and returns empty collection
- Malformed JSON file logs error with filename and skips that file (doesn't crash startup)
- Duplicate chain IDs across files are detected and logged as warnings

**TriggerService:**
- `CheckLocationTrigger` returns correct quest step for registered location
- Returns null for unregistered location
- Returns null if quest prerequisites not met

**QuestState persistence:**
- State is created on first `AdvanceStep` for a chain
- State is updated (not duplicated) on subsequent steps
- State survives save/load cycle

### Leveling System Tests

**LevelingService:**
- `AwardExperience` increases `Character.Experience` correctly
- Level-up triggers when XP crosses threshold
- Multi-level-up works (enough XP to skip a level)
- `AttributePoints` awarded correctly per level gained
- HP restored to max on level-up
- `DidLevelUp` is false when XP doesn't cross threshold
- `GetXpToNextLevel` returns correct remaining XP
- `GetLevelForXp` returns correct level for edge cases (exact threshold, zero, max)

**Level table seeding:**
- Seed file loads and populates LiteDB collection
- Missing or malformed `level-table.json` logs error with details

### Integration-Style Tests

**Combat victory flow (mocked services):**
- Victory sums `ExperienceReward` from all defeated enemies
- Awards XP via `LevelingService`
- Calls `QuestService.AdvanceStep` with correct chain/step IDs
- Navigates to WorldScene (not MainMenuPage)

---

## 7. Debug Logging for JSON Loading

All JSON file loading (quest chains, sprite catalog seed, level table seed) must include structured debug logging for failures. Use `GD.Print` in Godot code and `ILogger` / console output in Core services.

### What Gets Logged

**File access failures:**
```
[QuestSeeder] ERROR: Quest directory not found: assets/data/quests/
[QuestSeeder] ERROR: Failed to read quest file: beat_1_the_awakening.json — FileNotFoundException: ...
[SpriteSeeder] ERROR: Seed file not found: assets/data/sprite-catalog.json
[LevelSeeder] ERROR: Seed file not found: assets/data/level-table.json
```

**Deserialization failures:**
```
[QuestSeeder] ERROR: Failed to parse quest file: beat_2_dark_warrior.json — JsonException: ... at line 42
[SpriteSeeder] ERROR: Failed to parse sprite-catalog.json — JsonException: ... at line 15
[LevelSeeder] ERROR: Failed to parse level-table.json — JsonException: ... at line 3
```

**Data validation warnings:**
```
[QuestSeeder] WARN: Duplicate chain ID 'beat_1' found in beat_1_copy.json — skipping
[QuestSeeder] WARN: Quest step 'beat_1_combat' references unknown NextStepId 'beat_1_missing'
[SpriteSeeder] WARN: EquipmentSprite 'Plate (Steel)' references asset path that may not exist: armor/plate/steel
[QuestSeeder] INFO: Loaded 7 quest chains with 22 steps from 7 files
[SpriteSeeder] INFO: Loaded 45 equipment sprites and 32 appearance options
[LevelSeeder] INFO: Loaded 20 level thresholds (max level: 20)
```

### Implementation Pattern

Each seeder follows the same pattern:

```csharp
public class QuestSeeder
{
    public void Seed(ILiteDatabase db, IFileSystemService fs)
    {
        var questDir = "assets/data/quests";
        if (!fs.DirectoryExists(questDir))
        {
            GD.PrintErr($"[QuestSeeder] ERROR: Quest directory not found: {questDir}");
            return;
        }

        var files = fs.GetFiles(questDir, "*.json");
        int chainCount = 0, stepCount = 0, errorCount = 0;

        foreach (var file in files)
        {
            try
            {
                var json = fs.ReadAllText(file);
                var chain = JsonSerializer.Deserialize<QuestChain>(json);
                // validate and upsert...
                chainCount++;
                stepCount += chain.Steps.Count;
            }
            catch (JsonException ex)
            {
                GD.PrintErr($"[QuestSeeder] ERROR: Failed to parse quest file: {Path.GetFileName(file)} — {ex.Message}");
                errorCount++;
            }
        }

        GD.Print($"[QuestSeeder] INFO: Loaded {chainCount} quest chains with {stepCount} steps from {files.Count} files ({errorCount} errors)");
    }
}
```

This pattern applies identically to `SpriteSeeder` and `LevelSeeder`. Failures are logged and skipped — a single bad file doesn't crash the game.
