# Talent Tree Redesign Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix a critical visibility bug for hidden talent trees, add icon support to talent nodes, and improve the robustness of talent stat bonuses.

**Architecture:** 
1.  Extend models to support icons and dedicated talent stat bonuses.
2.  Update `Character` stat properties to aggregate multiple bonus sources.
3.  Refactor `TalentService` to manage `TalentStatBonuses` instead of clearing the shared `StatBonuses` dictionary.
4.  Correct the boolean logic in `TalentService.GetAvailableTrees`.

**Tech Stack:** .NET 10, Godot 4.6.1 (C#), LiteDB.

---

### Task 1: Update Models

**Files:**
- Modify: `Darkness.Core/Models/TalentNode.cs`
- Modify: `Darkness.Core/Models/Character.cs`

- [ ] **Step 1: Add IconPath to TalentNode**
    Add `public string? IconPath { get; set; }` to `Darkness.Core/Models/TalentNode.cs`.

- [ ] **Step 2: Add TalentStatBonuses to Character**
    Add `public Dictionary<string, int> TalentStatBonuses { get; set; } = new();` to `Darkness.Core/Models/Character.cs`.

- [ ] **Step 3: Update Character stat properties**
    Update `Strength`, `Dexterity`, `Constitution`, `Intelligence`, `Wisdom`, and `Charisma` to include `TalentStatBonuses`.

```csharp
[BsonIgnore]
public int Strength
{
    get => BaseStrength + 
           (StatBonuses.TryGetValue("Strength", out var b) ? b : 0) +
           (TalentStatBonuses.TryGetValue("Strength", out var tb) ? tb : 0);
    set => BaseStrength = value;
}
```

- [ ] **Step 4: Commit model changes**

### Task 2: Fix TalentService Logic

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`

- [ ] **Step 1: Fix IsHidden logic in GetAvailableTrees**
    Change `return available && hasPoints;` to `return available || hasPoints;` within the `tree.IsHidden` block.

- [ ] **Step 2: Refactor ApplyTalentPassives**
    Update `ApplyTalentPassives` to use `character.TalentStatBonuses` instead of `character.StatBonuses`. Remove the `character.StatBonuses.Clear()` call.

```csharp
public void ApplyTalentPassives(Character character)
{
    var allTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
    var allNodes = allTrees.SelectMany(t => t.Nodes).ToList();

    character.TalentStatBonuses.Clear();

    foreach (var talentId in character.UnlockedTalentIds)
    {
        var node = allNodes.FirstOrDefault(n => n.Id == talentId);
        if (node?.Effect != null && !string.IsNullOrEmpty(node.Effect.Stat))
        {
            if (!character.TalentStatBonuses.ContainsKey(node.Effect.Stat))
            {
                character.TalentStatBonuses[node.Effect.Stat] = 0;
            }
            character.TalentStatBonuses[node.Effect.Stat] += node.Effect.Value;
        }
    }
    
    character.RecalculateDerivedStats();
}
```

- [ ] **Step 3: Commit service changes**

### Task 3: Update Godot UI

**Files:**
- Modify: `Darkness.Godot/src/UI/TalentNodeBox.cs`

- [ ] **Step 1: Update TalentNodeBox to use IconPath**
    In `UpdateUI()`, check `_node.IconPath`. If set, load the texture; otherwise, default to `res://icon.svg`.

```csharp
string iconPath = !string.IsNullOrEmpty(_node.IconPath) ? _node.IconPath : "res://icon.svg";
_iconButton.TextureNormal = GD.Load<Texture2D>(iconPath);
```

- [ ] **Step 2: Commit UI changes**

### Task 4: Regression Testing

**Files:**
- Modify: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Write regression test for IsHidden visibility**
    Add a test case where a hidden tree is available (met prerequisites) but has no points spent, and verify it is returned by `GetAvailableTrees`.

```csharp
[Fact]
public void GetAvailableTrees_ShouldShowHiddenTree_WhenPrereqsMetButNoPointsSpent()
{
    // Arrange
    var character = new Character { Level = 10, Class = "Knight" };
    var tree = new TalentTree 
    { 
        Id = "hidden_tree", 
        IsHidden = true, 
        RequiredClass = "Knight",
        Nodes = new List<TalentNode> { new TalentNode { Id = "node1" } }
    };
    _db.GetCollection<TalentTree>("talent_trees").Insert(tree);

    // Act
    var available = _talentService.GetAvailableTrees(character);

    // Assert
    Assert.Contains(available, t => t.Id == "hidden_tree");
}
```

- [ ] **Step 2: Run tests to verify failure (Red)**
    Run `dotnet test Darkness.Tests` and ensure the new test fails.

- [ ] **Step 3: Run tests to verify success (Green)**
    Ensure all tests pass.

- [ ] **Step 4: Final commit and verify**
