# Task 2: Fix and Integrate Talent System

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Correct data types in `talent-trees.json`, refine `TalentSeeder` for robustness, implement a skeletal `TalentService`, and integrate both into the application's global dependency injection and initialization.

**Architecture:** Use data-driven seeding via LiteDB and DI registration for runtime service availability.

**Tech Stack:** .NET 10, Godot 4.6.1, LiteDB, Microsoft.Extensions.DependencyInjection.

---

### Task 1: Fix `talent-trees.json` Data Types

**Files:**
- Modify: `Darkness.Godot/assets/data/talent-trees.json`

- [ ] **Step 1: Convert prerequisite values from strings to integers**

```json
[
  {
    "Id": "knight_tree",
    "Name": "Knight",
    "Tier": 1,
    "Prerequisites": { "Level": 1 },
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
    "Prerequisites": { "Level": 20, "SpentInTree:knight_tree": 5 },
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
    "Prerequisites": { "Level": 40, "Strength": 20, "SpentInTree:holy_knight_tree": 20 },
    "Nodes": []
  }
]
```

### Task 2: Refine `TalentSeeder.cs`

**Files:**
- Modify: `Darkness.Core/Services/TalentSeeder.cs`

- [ ] **Step 1: Update error reporting and deserialization options to match `QuestSeeder` pattern**

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
        const string jsonPath = "assets/data/talent-trees.json";
        
        if (!_fileSystem.FileExists(jsonPath))
        {
            Console.Error.WriteLine($"[TalentSeeder] ERROR: File not found: {jsonPath}");
            return;
        }

        string json;
        try
        {
            json = _fileSystem.ReadAllText(jsonPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[TalentSeeder] ERROR: Failed to read talent-trees.json — {ex.Message}");
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
            Console.Error.WriteLine($"[TalentSeeder] ERROR: Failed to parse talent-trees.json — {ex.Message}");
            return;
        }

        if (trees == null || trees.Count == 0)
        {
            Console.Error.WriteLine("[TalentSeeder] WARN: talent-trees.json is empty or null");
            return;
        }

        var col = db.GetCollection<TalentTree>("talent_trees");
        col.DeleteAll();
        col.InsertBulk(trees);
        col.EnsureIndex(t => t.Id);

        Console.Error.WriteLine($"[TalentSeeder] INFO: Loaded {col.Count()} talent trees");
    }
}
```

### Task 3: Create Skeletal `TalentService.cs`

**Files:**
- Create: `Darkness.Core/Services/TalentService.cs`

- [ ] **Step 1: Implement `ITalentService` with empty bodies**

```csharp
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System;
using System.Collections.Generic;

namespace Darkness.Core.Services;

public class TalentService : ITalentService
{
    private readonly LiteDatabase _db;

    public TalentService(LiteDatabase db)
    {
        _db = db;
    }

    public List<TalentTree> GetAvailableTrees(Character character)
    {
        return new List<TalentTree>();
    }

    public bool CanPurchaseTalent(Character character, string treeId, string nodeId)
    {
        return false;
    }

    public void PurchaseTalent(Character character, string treeId, string nodeId)
    {
        throw new NotImplementedException();
    }

    public void ApplyTalentPassives(Character character)
    {
        // Skeletal implementation
    }
}
```

### Task 4: Integrate into `Global.cs`

**Files:**
- Modify: `Darkness.Godot/src/Core/Global.cs`

- [ ] **Step 1: Add `ITalentService` registration and call `TalentSeeder.Seed`**

```csharp
// ... in DI registration block
services.AddSingleton<ITalentService, TalentService>();
// ... in seeding block
new SpriteSeeder(fs).Seed(db);
new QuestSeeder(fs).Seed(db);
new LevelSeeder(fs).Seed(db);
new TalentSeeder(fs).Seed(db); // Add this line
```

### Task 5: Verification

- [ ] **Step 1: Build the project to verify no compilation errors**
Run: `dotnet build Darkness.sln`

- [ ] **Step 2: Commit changes**
Commit message: "fix: correct talent JSON and integrate TalentSeeder into Global.cs"
