# Data-Driven Random Encounter Rates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate hardcoded encounter rates and distances to the data-driven `encounters.json` configuration and centralize rolling logic in `EncounterService`.

**Architecture:** Update `EncounterTable` model to include `EncounterChance` and `EncounterDistance`. Enhance `EncounterService` with `RollForEncounter` to encapsulate the probability and threshold logic, then simplify `WorldScene.cs` to use this new service method.

**Tech Stack:** C#, .NET 10, LiteDB, Godot 4.6.1

---

### Task 1: Update Encounter Models

**Files:**
- Modify: `Darkness.Core\Models\EncounterTable.cs`

- [ ] **Step 1: Add Chance and Distance fields to EncounterTable**

```csharp
using System.Collections.Generic;

namespace Darkness.Core.Models;

public class EncounterTable
{
    public string BackgroundKey { get; set; } = string.Empty;
    public int EncounterChance { get; set; } = 5; // Default 5%
    public float EncounterDistance { get; set; } = 1000f; // Default 1000px
    public List<EncounterEntry> Encounters { get; set; } = new();
}
```

- [ ] **Step 2: Commit model changes**

```bash
git add Darkness.Core/Models/EncounterTable.cs
git commit -m "feat: add EncounterChance and EncounterDistance to EncounterTable model"
```

---

### Task 2: Update Encounter Service Interface

**Files:**
- Modify: `Darkness.Core\Interfaces\IEncounterService.cs`

- [ ] **Step 1: Add RollForEncounter to IEncounterService**

```csharp
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IEncounterService
{
    CombatData? GetRandomEncounter(string backgroundKey);
    CombatData? RollForEncounter(string backgroundKey, double distanceMoved);
}
```

- [ ] **Step 2: Commit interface changes**

```bash
git add Darkness.Core/Interfaces/IEncounterService.cs
git commit -m "feat: add RollForEncounter to IEncounterService"
```

---

### Task 3: Implement RollForEncounter in Service

**Files:**
- Modify: `Darkness.Core\Services\EncounterService.cs`

- [ ] **Step 1: Write the failing test in EncounterServiceTests.cs**

```csharp
    [Fact]
    public void RollForEncounter_RespectsDistanceThreshold()
    {
        // Arrange
        var table = new EncounterTable
        {
            BackgroundKey = "test_bg",
            EncounterChance = 100, // Always succeed if distance met
            EncounterDistance = 500f,
            Encounters = new List<EncounterEntry> { new EncounterEntry { Weight = 1, Combat = new CombatData() } }
        };
        _db.GetCollection<EncounterTable>("encounter_tables").Insert(table);

        // Act & Assert
        Assert.Null(_service.RollForEncounter("test_bg", 499)); // Below threshold
        Assert.NotNull(_service.RollForEncounter("test_bg", 501)); // Above threshold
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~EncounterServiceTests" -p:ParallelizeTestCollections=false`
Expected: FAIL (method not implemented)

- [ ] **Step 3: Implement RollForEncounter**

```csharp
    public CombatData? RollForEncounter(string backgroundKey, double distanceMoved)
    {
        var col = _db.GetCollection<EncounterTable>("encounter_tables");
        var table = col.FindOne(t => t.BackgroundKey == backgroundKey);

        if (table == null || table.Encounters.Count == 0 || distanceMoved < table.EncounterDistance)
        {
            return null;
        }

        int roll = _random.Next(1, 101);
        if (roll <= table.EncounterChance)
        {
            return GetRandomEncounter(backgroundKey);
        }

        return null;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~EncounterServiceTests" -p:ParallelizeTestCollections=false`
Expected: PASS

- [ ] **Step 5: Commit service implementation**

```bash
git add Darkness.Core/Services/EncounterService.cs Darkness.Tests/Services/EncounterServiceTests.cs
git commit -m "feat: implement RollForEncounter in EncounterService"
```

---

### Task 4: Update Seed Data

**Files:**
- Modify: `Darkness.Godot\assets\data\encounters.json`

- [ ] **Step 1: Update encounters.json with new fields**

```json
[
  {
    "BackgroundKey": "camelot_shore",
    "EncounterChance": 8,
    "EncounterDistance": 1000,
    "Encounters": [
      {
        "Weight": 50,
        "Combat": {
          "Enemies": [
            {
              "Name": "Stray Hound",
              "Level": 1,
              "MaxHP": 25,
              "CurrentHP": 25,
              "Attack": 5,
              "Defense": 2,
              "Speed": 10,
              "SpriteKey": "hound",
              "ExperienceReward": 15,
              "GoldReward": 1
            }
          ],
          "BackgroundKey": "camelot_shore_2"
        }
      },
      {
        "Weight": 25,
        "Combat": {
          "Enemies": [
            {
              "Name": "Shore Crab",
              "Level": 1,
              "MaxHP": 35,
              "CurrentHP": 35,
              "Attack": 6,
              "Defense": 5,
              "Speed": 5,
              "SpriteKey": "hound",
              "ExperienceReward": 20,
              "GoldReward": 2
            },
            {
              "Name": "Shore Crab",
              "Level": 1,
              "MaxHP": 35,
              "CurrentHP": 35,
              "Attack": 6,
              "Defense": 5,
              "Speed": 5,
              "SpriteKey": "hound",
              "ExperienceReward": 20,
              "GoldReward": 2
            }
          ],
          "BackgroundKey": "camelot_shore_2"
        }
      }
    ]
  }
]
```

- [ ] **Step 2: Commit seed data**

```bash
git add Darkness.Godot/assets/data/encounters.json
git commit -m "data: add EncounterChance and EncounterDistance to camelot_shore"
```

---

### Task 5: Simplify WorldScene Integration

**Files:**
- Modify: `Darkness.Godot\src\Game\WorldScene.cs`

- [ ] **Step 1: Remove hardcoded constants and use RollForEncounter**

```csharp
    // Remove these lines:
    // private const float EncounterDistanceThreshold = 1000f;
    // private const float EncounterChance = 0.08f; // 8% chance every 1000px

    private void CheckRandomEncounter(double delta)
    {
        float dist = _player.GlobalPosition.DistanceTo(_lastPlayerPosition);
        _distanceMovedSinceLastEncounter += dist;
        _lastPlayerPosition = _player.GlobalPosition;

        // Only roll if we have a background key to look up
        string? bgKey = _currentDialogueStep?.Visuals?.BackgroundKey;
        if (string.IsNullOrEmpty(bgKey)) return;

        var combat = _encounterService.RollForEncounter(bgKey, _distanceMovedSinceLastEncounter);
        if (combat != null)
        {
            _distanceMovedSinceLastEncounter = 0; // Reset only on success
            _ = StartRandomEncounter(combat);
        }
    }
```

- [ ] **Step 2: Build the solution to verify**

Run: `dotnet build Darkness.sln`
Expected: Build succeeded

- [ ] **Step 3: Commit WorldScene changes**

```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: delegate encounter rolling logic to EncounterService in WorldScene"
```

---

### Task 6: Final Verification

- [ ] **Step 1: Run all tests sequentially**

Run: `dotnet test Darkness.Tests -p:ParallelizeTestCollections=false`
Expected: All tests pass

- [ ] **Step 2: Verify Android build pipeline**

Run: `.\build-android.ps1`
Expected: APK generated successfully
