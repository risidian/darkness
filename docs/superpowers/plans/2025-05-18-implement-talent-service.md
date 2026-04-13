# TalentService Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the core logic for purchasing talents and applying their passive stat boosts.

**Architecture:** Use `LiteDB` for talent tree data access. Updates the `Character` model directly and triggers derived stat recalculation.

**Tech Stack:** .NET 10, LiteDB, XUnit.

---

### Task 1: Update TalentService Implementation

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`

- [ ] **Step 1: Implement `PurchaseTalent`**
    ```csharp
    public void PurchaseTalent(Character character, string treeId, string nodeId)
    {
        if (CanPurchaseTalent(character, treeId, nodeId))
        {
            var tree = _db.GetCollection<TalentTree>("talent_trees").FindById(treeId);
            var node = tree.Nodes.First(n => n.Id == nodeId);
            
            character.UnlockedTalentIds.Add(nodeId);
            character.TalentPoints -= node.PointsRequired;
        }
    }
    ```

- [ ] **Step 2: Implement `ApplyTalentPassives`**
    ```csharp
    public void ApplyTalentPassives(Character character)
    {
        var allTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
        var allNodes = allTrees.SelectMany(t => t.Nodes).ToList();

        foreach (var talentId in character.UnlockedTalentIds)
        {
            var node = allNodes.FirstOrDefault(n => n.Id == talentId);
            if (node?.Effect != null && !string.IsNullOrEmpty(node.Effect.Stat))
            {
                switch (node.Effect.Stat)
                {
                    case "Strength": character.Strength += node.Effect.Value; break;
                    case "Dexterity": character.Dexterity += node.Effect.Value; break;
                    case "Constitution": character.Constitution += node.Effect.Value; break;
                    case "Intelligence": character.Intelligence += node.Effect.Value; break;
                    case "Wisdom": character.Wisdom += node.Effect.Value; break;
                    case "Charisma": character.Charisma += node.Effect.Value; break;
                }
            }
        }
        
        character.RecalculateDerivedStats();
    }
    ```

- [ ] **Step 3: Run build to verify compilation**
    Run: `dotnet build Darkness.sln`
    Expected: Build succeeds.

---

### Task 2: Add Regression Tests

**Files:**
- Modify: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Add `PurchaseTalent_DeductsPoint_And_AddsId` test**
    ```csharp
    [Fact]
    public void PurchaseTalent_DeductsPoint_And_AddsId()
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
        var character = new Character { TalentPoints = 1, Level = 1 };

        // Act
        service.PurchaseTalent(character, "tree1", "node1");

        // Assert
        Assert.Contains("node1", character.UnlockedTalentIds);
        Assert.Equal(0, character.TalentPoints);
    }
    ```

- [ ] **Step 2: Add `ApplyTalentPassives_AddsStats_And_UpdatesDerived` test**
    ```csharp
    [Fact]
    public void ApplyTalentPassives_AddsStats_And_UpdatesDerived()
    {
        // Arrange
        using var db = new LiteDatabase(new MemoryStream());
        var col = db.GetCollection<TalentTree>("talent_trees");
        var tree = new TalentTree 
        { 
            Id = "tree1", 
            Nodes = new List<TalentNode> 
            { 
                new TalentNode 
                { 
                    Id = "node1", 
                    Effect = new TalentEffect { Stat = "Strength", Value = 5 } 
                } 
            } 
        };
        col.Insert(tree);
        
        var service = new TalentService(db);
        var character = new Character 
        { 
            Strength = 10, 
            Constitution = 10,
            UnlockedTalentIds = new List<string> { "node1" } 
        };
        character.RecalculateDerivedStats(); 

        // Act
        service.ApplyTalentPassives(character);

        // Assert
        Assert.Equal(15, character.Strength);
    }
    ```

- [ ] **Step 3: Run tests and verify they pass**
    Run: `dotnet test Darkness.Tests --filter "TalentServiceTests"`
    Expected: All tests pass (including existing ones).

---

### Task 3: Commit Changes

- [ ] **Step 1: Commit the implementation**
    ```bash
    git add Darkness.Core/Services/TalentService.cs Darkness.Tests/Services/TalentServiceTests.cs
    git commit -m "feat: complete TalentService core logic and tests"
    ```
