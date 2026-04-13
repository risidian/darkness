# Talent Tree Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a data-driven talent tree system where players earn 1 point every 2 levels to unlock passive stat bonuses and active skills, including hidden "Crusader" tier trees.

**Architecture:** Data-driven design using JSON seeding into LiteDB, managed by a `TalentService`. Passive effects are applied during `RecalculateDerivedStats`, and active skills are injected via `WeaponSkillService`.

**Tech Stack:** .NET 10, LiteDB, Godot 4.6.1 (C#), XUnit for testing.

---

### Task 1: Character Model & Core Talent Models

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`
- Create: `Darkness.Core/Models/TalentTree.cs`
- Create: `Darkness.Core/Models/TalentNode.cs`
- Create: `Darkness.Core/Models/TalentEffect.cs`

- [ ] **Step 1: Add talent fields to Character.cs**

```csharp
public int TalentPoints { get; set; } = 0;
public List<string> UnlockedTalentIds { get; set; } = new();
```

- [ ] **Step 2: Create TalentEffect.cs**

```csharp
namespace Darkness.Core.Models;
public class TalentEffect {
    public string? Stat { get; set; } // e.g. "Strength"
    public int Value { get; set; }
    public string? Skill { get; set; } // e.g. "Holy Strike"
}
```

- [ ] **Step 3: Create TalentNode.cs**

```csharp
namespace Darkness.Core.Models;
public class TalentNode {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointsRequired { get; set; } = 1;
    public string? PrerequisiteNodeId { get; set; }
    public TalentEffect Effect { get; set; } = new();
}
```

- [ ] **Step 4: Create TalentTree.cs**

```csharp
using System.Collections.Generic;
namespace Darkness.Core.Models;
public class TalentTree {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Tier { get; set; }
    public bool IsHidden { get; set; } = false;
    public Dictionary<string, int> Prerequisites { get; set; } = new(); // "Level", "Strength", "SpentInTree:knight_tree"
    public List<TalentNode> Nodes { get; set; } = new();
}
```

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Models/
git commit -m "feat: add character talent fields and core talent models"
```

---

### Task 2: Data Seeding & Service Interface

**Files:**
- Create: `Darkness.Godot/assets/data/talent-trees.json`
- Create: `Darkness.Core/Services/TalentSeeder.cs`
- Create: `Darkness.Core/Interfaces/ITalentService.cs`

- [ ] **Step 1: Create initial talent-trees.json with Knight/Holy Knight/Crusader**

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

- [ ] **Step 2: Create TalentSeeder.cs**

```csharp
using System.Text.Json;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;
public class TalentSeeder {
    private readonly LiteDatabase _db;
    public TalentSeeder(LiteDatabase db) { _db = db; }
    public void Seed(string jsonPath) {
        var collection = _db.GetCollection<TalentTree>("talent_trees");
        collection.DeleteAll();
        var json = File.ReadAllText(jsonPath);
        var trees = JsonSerializer.Deserialize<List<TalentTree>>(json);
        if (trees != null) collection.InsertBulk(trees);
    }
}
```

- [ ] **Step 3: Create ITalentService.cs**

```csharp
using Darkness.Core.Models;
namespace Darkness.Core.Interfaces;
public interface ITalentService {
    List<TalentTree> GetAvailableTrees(Character character);
    bool CanPurchaseTalent(Character character, string treeId, string nodeId);
    void PurchaseTalent(Character character, string treeId, string nodeId);
    void ApplyTalentPassives(Character character);
}
```

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "feat: add talent trees json, seeder, and service interface"
```

---

### Task 3: Talent Service Implementation & Testing (TDD)

**Files:**
- Create: `Darkness.Core/Services/TalentService.cs`
- Create: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Write failing test for purchasing talent**

```csharp
[Fact]
public void PurchaseTalent_AddsId_And_ReducesPoints() {
    var character = new Character { TalentPoints = 1 };
    var service = new TalentService(mockDb);
    service.PurchaseTalent(character, "knight_tree", "k1_str");
    Assert.Contains("k1_str", character.UnlockedTalentIds);
    Assert.Equal(0, character.TalentPoints);
}
```

- [ ] **Step 2: Implement TalentService logic**

```csharp
public void PurchaseTalent(Character character, string treeId, string nodeId) {
    if (CanPurchaseTalent(character, treeId, nodeId)) {
        character.UnlockedTalentIds.Add(nodeId);
        character.TalentPoints--;
    }
}
```

- [ ] **Step 3: Run tests and verify PASS**

Run: `dotnet test Darkness.Tests --filter "TalentServiceTests"`

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "feat: implement TalentService core logic and tests"
```

---

### Task 4: Leveling & Skill Integration

**Files:**
- Modify: `Darkness.Core/Services/LevelingService.cs`
- Modify: `Darkness.Core/Services/WeaponSkillService.cs`
- Modify: `Darkness.Core/Models/Character.cs`

- [ ] **Step 1: Update LevelingService to award points**

```csharp
// In AwardExperience:
if (newLevel > previousLevel) {
    // Award 1 point every 2 levels
    for (int i = previousLevel + 1; i <= newLevel; i++) {
        if (i % 2 == 0) character.TalentPoints++;
    }
}
```

- [ ] **Step 2: Update Character.RecalculateDerivedStats to apply passives**

```csharp
// Call ITalentService.ApplyTalentPassives(this) inside RecalculateDerivedStats
```

- [ ] **Step 3: Update WeaponSkillService to inject talent skills**

```csharp
// Inject skills matching character.UnlockedTalentIds that have a Skill name in their effect
```

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "feat: integrate talents with leveling, stats, and skills"
```

---

### Task 5: UI Implementation (Godot)

**Files:**
- Create: `Darkness.Godot/scenes/TalentTreeScene.tscn`
- Create: `Darkness.Godot/src/UI/TalentTreeScene.cs`
- Modify: `Darkness.Godot/src/UI/MainMenuScene.cs`

- [ ] **Step 1: Create TalentTreeScene UI with tabs and node grid**
- [ ] **Step 2: Add "Talents" button to MainMenuScene**
- [ ] **Step 3: Connect button to navigate to TalentTreeScene**
- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "feat: implement talent tree UI and main menu link"
```
