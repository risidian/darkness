# Talent Tree Infrastructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the initial data structures, seeding mechanism, and service interface for the Talent Tree system.

**Architecture:**
-   `talent-trees.json`: Data-driven content for LiteDB, defining tiers, prerequisites, and nodes.
-   `TalentSeeder.cs`: Service to load JSON into LiteDB `talent_trees` collection using `DeleteAll()` and `InsertBulk()`.
-   `ITalentService.cs`: Core interface for talent-related logic (purchasing, availability, applying passives).

**Tech Stack:** .NET 10, LiteDB, System.Text.Json.

---

### Task 1: Create Talent Trees JSON Data

**Files:**
- Create: `Darkness.Godot/assets/data/talent-trees.json`

- [ ] **Step 1: Create `Darkness.Godot/assets/data/talent-trees.json`**

```json
[
  {
    "Id": "knight_tree",
    "Name": "Knight",
    "Tier": 1,
    "Prerequisites": { "Level": "1" },
    "Nodes": [
      {
        "Id": "k1_str", "Name": "Iron Grip", "Description": "+5 Strength",
        "Effect": { "Stat": "Strength", "Value": 5 }
      }
    ]
  },
  {
    "Id": "holy_knight_tree",
    "Name": "Holy Knight",
    "Tier": 2,
    "Prerequisites": { "Level": "20", "SpentInTree:knight_tree": "5" },
    "Nodes": [
      {
        "Id": "hk1_holy_strike", "Name": "Holy Strike", "Description": "Unlocks Holy Strike",
        "Effect": { "Skill": "Holy Strike" }
      }
    ]
  },
  {
    "Id": "crusader_tree",
    "Name": "Crusader",
    "Tier": 3, "IsHidden": true,
    "Prerequisites": { "Level": "40", "Strength": "20", "SpentInTree:holy_knight_tree": "20" },
    "Nodes": []
  }
]
```

- [ ] **Step 2: Commit JSON file**

```bash
git add Darkness.Godot/assets/data/talent-trees.json
git commit -m "feat: add talent trees json data"
```

### Task 2: Create Talent Seeder

**Files:**
- Create: `Darkness.Core/Services/TalentSeeder.cs`

- [ ] **Step 1: Create `Darkness.Core/Services/TalentSeeder.cs`**

```csharp
using System;
using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class TalentSeeder
{
    private readonly IFileSystemService _fileSystem;

    public TalentSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(LiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/talent-trees.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TalentSeeder] ERROR: Failed to read talent-trees.json — {ex.Message}");
            return;
        }

        List<TalentTree>? trees;
        try
        {
            trees = SystemJson.JsonSerializer.Deserialize<List<TalentTree>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            Console.WriteLine($"[TalentSeeder] ERROR: Failed to parse talent-trees.json — {ex.Message}");
            return;
        }

        if (trees == null || trees.Count == 0)
        {
            Console.WriteLine("[TalentSeeder] WARN: talent-trees.json is empty or null");
            return;
        }

        var col = db.GetCollection<TalentTree>("talent_trees");
        col.DeleteAll();
        col.InsertBulk(trees);
        col.EnsureIndex(t => t.Id);

        Console.WriteLine($"[TalentSeeder] INFO: Loaded {col.Count()} talent trees");
    }
}
```

- [ ] **Step 2: Commit seeder**

```bash
git add Darkness.Core/Services/TalentSeeder.cs
git commit -m "feat: add talent seeder"
```

### Task 3: Create Talent Service Interface

**Files:**
- Create: `Darkness.Core/Interfaces/ITalentService.cs`

- [ ] **Step 1: Create `Darkness.Core/Interfaces/ITalentService.cs`**

```csharp
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface ITalentService
{
    List<TalentTree> GetAvailableTrees(Character character);
    bool CanPurchaseTalent(Character character, string treeId, string nodeId);
    void PurchaseTalent(Character character, string treeId, string nodeId);
    void ApplyTalentPassives(Character character);
}
```

- [ ] **Step 2: Verify compilation**

Run: `dotnet build Darkness.Core/Darkness.Core.csproj`

- [ ] **Step 3: Commit interface**

```bash
git add Darkness.Core/Interfaces/ITalentService.cs
git commit -m "feat: add ITalentService interface"
```
