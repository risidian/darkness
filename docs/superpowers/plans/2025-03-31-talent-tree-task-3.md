# Talent Tree Implementation Plan - Task 3

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the core logic of `TalentService` for purchasing talents and applying passive bonuses, with comprehensive unit tests.

**Architecture:** `TalentService` will interact with `LiteDatabase` to retrieve talent trees and nodes. It will update the `Character` object's `TalentPoints` and `UnlockedTalentIds`. Stat bonuses from passives will be applied directly to the character's base stats.

**Tech Stack:** C#, .NET 10, LiteDB, XUnit.

---

### Task 3.1: Implement `CanPurchaseTalent`

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`
- Test: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Write failing test for `CanPurchaseTalent`**

```csharp
    [Fact]
    public void CanPurchaseTalent_ReturnsFalse_WhenPointsInsufficient()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        var tree = new TalentTree 
        { 
            Id = "tree1", 
            Nodes = new List<TalentNode> { new TalentNode { Id = "node1", PointsRequired = 1 } } 
        };
        col.Insert(tree);
        
        var service = new TalentService(db);
        var character = new Character { TalentPoints = 0 };

        // Act
        var result = service.CanPurchaseTalent(character, "tree1", "node1");

        // Assert
        Assert.False(result);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~TalentServiceTests.CanPurchaseTalent_ReturnsFalse_WhenPointsInsufficient"`
Expected: FAIL (returns false because it's hardcoded to false, but we want to ensure it fails for the right reasons later)

- [ ] **Step 3: Implement `CanPurchaseTalent` minimal code**

```csharp
    public bool CanPurchaseTalent(Character character, string treeId, string nodeId)
    {
        var tree = _db.GetCollection<TalentTree>("talent_trees").FindById(treeId);
        if (tree == null) return false;

        var node = tree.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return false;

        if (character.TalentPoints < node.PointsRequired) return false;
        if (character.UnlockedTalentIds.Contains(nodeId)) return false;
        if (!IsTreeAvailable(character, tree)) return false;

        if (!string.IsNullOrEmpty(node.PrerequisiteNodeId) && !character.UnlockedTalentIds.Contains(node.PrerequisiteNodeId))
            return false;

        return true;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~TalentServiceTests.CanPurchaseTalent_ReturnsFalse_WhenPointsInsufficient"`
Expected: PASS

- [ ] **Step 5: Add more tests for `CanPurchaseTalent` (Prerequisites, Already Unlocked)**

- [ ] **Step 6: Commit**

```bash
git add Darkness.Core/Services/TalentService.cs Darkness.Tests/Services/TalentServiceTests.cs
git commit -m "feat: implement CanPurchaseTalent logic and tests"
```

### Task 3.2: Implement `PurchaseTalent`

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`
- Test: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Write failing test for `PurchaseTalent`**

```csharp
    [Fact]
    public void PurchaseTalent_DeductsPointsAndAddsToUnlocked()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        var tree = new TalentTree 
        { 
            Id = "tree1", 
            Nodes = new List<TalentNode> { new TalentNode { Id = "node1", PointsRequired = 1 } } 
        };
        col.Insert(tree);
        
        var service = new TalentService(db);
        var character = new Character { TalentPoints = 2 };

        // Act
        service.PurchaseTalent(character, "tree1", "node1");

        // Assert
        Assert.Equal(1, character.TalentPoints);
        Assert.Contains("node1", character.UnlockedTalentIds);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~TalentServiceTests.PurchaseTalent_DeductsPointsAndAddsToUnlocked"`
Expected: FAIL (NotImplementedException)

- [ ] **Step 3: Implement `PurchaseTalent` minimal code**

```csharp
    public void PurchaseTalent(Character character, string treeId, string nodeId)
    {
        var tree = _db.GetCollection<TalentTree>("talent_trees").FindById(treeId);
        if (tree == null) return;

        var node = tree.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return;

        if (CanPurchaseTalent(character, treeId, nodeId))
        {
            character.UnlockedTalentIds.Add(nodeId);
            character.TalentPoints -= node.PointsRequired;
        }
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~TalentServiceTests.PurchaseTalent_DeductsPointsAndAddsToUnlocked"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/TalentService.cs Darkness.Tests/Services/TalentServiceTests.cs
git commit -m "feat: implement PurchaseTalent logic and tests"
```

### Task 3.3: Implement `ApplyTalentPassives`

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`
- Test: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Write failing test for `ApplyTalentPassives`**

```csharp
    [Fact]
    public void ApplyTalentPassives_IncreasesStats()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        var tree = new TalentTree 
        { 
            Id = "tree1", 
            Nodes = new List<TalentNode> 
            { 
                new TalentNode { Id = "node1", Effect = new TalentEffect { Stat = "Strength", Value = 5 } } 
            } 
        };
        col.Insert(tree);
        
        var service = new TalentService(db);
        var character = new Character { Strength = 10, UnlockedTalentIds = new List<string> { "node1" } };

        // Act
        service.ApplyTalentPassives(character);

        // Assert
        Assert.Equal(15, character.Strength);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~TalentServiceTests.ApplyTalentPassives_IncreasesStats"`
Expected: FAIL (Strength remains 10)

- [ ] **Step 3: Implement `ApplyTalentPassives` minimal code**

```csharp
    public void ApplyTalentPassives(Character character)
    {
        var allTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
        foreach (var talentId in character.UnlockedTalentIds)
        {
            foreach (var tree in allTrees)
            {
                var node = tree.Nodes.FirstOrDefault(n => n.Id == talentId);
                if (node != null && !string.IsNullOrEmpty(node.Effect.Stat))
                {
                    ApplyEffect(character, node.Effect);
                }
            }
        }
        character.RecalculateDerivedStats();
    }

    private void ApplyEffect(Character character, TalentEffect effect)
    {
        var property = typeof(Character).GetProperty(effect.Stat);
        if (property != null && property.PropertyType == typeof(int))
        {
            int currentValue = (int)property.GetValue(character);
            property.SetValue(character, currentValue + effect.Value);
        }
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~TalentServiceTests.ApplyTalentPassives_IncreasesStats"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/TalentService.cs Darkness.Tests/Services/TalentServiceTests.cs
git commit -m "feat: implement ApplyTalentPassives logic and tests"
```

### Task 3.4: Final Verification

- [ ] **Step 1: Build project**

Run: `dotnet build`

- [ ] **Step 2: Run all TalentService tests**

Run: `dotnet test Darkness.Tests --filter "TalentServiceTests"`

- [ ] **Step 3: Commit final changes**

```bash
git commit -m "feat: complete Talent Tree Task 3 implementation"
```
