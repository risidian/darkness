# Talent Tree Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a WoW-style 3-column talent tree with class-specific filtering, exclusivity groups, and automatic layout.

**Architecture:** 
- **Core:** Data-driven logic in `TalentService` handles filtering and exclusivity.
- **Layout:** A `TalentLayoutHelper` calculates 2D coordinates for nodes based on prerequisite depth and branching.
- **UI:** Custom Godot controls for nodes and `Line2D` for connections, replacing the static `GridContainer`.

**Tech Stack:** Godot 4.6.1 (.NET 10), LiteDB, Microsoft.Extensions.DependencyInjection.

---

### Task 1: Model Updates

**Files:**
- Modify: `Darkness.Core/Models/TalentTree.cs`
- Modify: `Darkness.Core/Models/TalentNode.cs`

- [ ] **Step 1: Add RequiredClass, ExclusiveGroupId, and IsHidden to TalentTree.cs**

```csharp
public string? RequiredClass { get; set; }
public string? ExclusiveGroupId { get; set; }
public bool IsHidden { get; set; } = false;
```

- [ ] **Step 2: Add Row and Column to TalentNode.cs**

```csharp
public int Row { get; set; }
public int Column { get; set; }
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Models/TalentTree.cs Darkness.Core/Models/TalentNode.cs
git commit -m "feat: add layout and filtering properties to talent models"
```

---

### Task 2: Service Filtering Logic

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`
- Test: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Add failing test for class filtering**

```csharp
[Fact]
public void GetAvailableTrees_FiltersByClass() {
    var knight = new Character { Class = "Knight" };
    var mageTree = new TalentTree { Id = "mage_tree", RequiredClass = "Mage" };
    var knightTree = new TalentTree { Id = "knight_tree", RequiredClass = "Knight" };
    // ... mock DB setup ...
    var result = service.GetAvailableTrees(knight);
    Assert.Contains(result, t => t.Id == "knight_tree");
    Assert.DoesNotContain(result, t => t.Id == "mage_tree");
}
```

- [ ] **Step 2: Implement class filtering in TalentService.cs**

```csharp
public List<TalentTree> GetAvailableTrees(Character character)
{
    var allTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
    return allTrees.Where(tree => 
        (string.IsNullOrEmpty(tree.RequiredClass) || tree.RequiredClass == character.Class) &&
        IsTreeAvailable(character, tree)
    ).ToList();
}
```

- [ ] **Step 3: Add exclusivity group logic to IsTreeAvailable**

```csharp
if (!string.IsNullOrEmpty(tree.ExclusiveGroupId)) {
    var otherTreesInGroup = _db.GetCollection<TalentTree>("talent_trees")
        .Find(t => t.ExclusiveGroupId == tree.ExclusiveGroupId && t.Id != tree.Id);
    foreach (var other in otherTreesInGroup) {
        if (character.UnlockedTalentIds.Any(id => other.Nodes.Any(n => n.Id == id)))
            return false;
    }
}
```

- [ ] **Step 4: Update hidden tree logic in GetAvailableTrees**

```csharp
// Hidden trees only appear if IsHidden is false OR prerequisites are met
return allTrees.Where(tree => 
    (string.IsNullOrEmpty(tree.RequiredClass) || tree.RequiredClass == character.Class) &&
    (!tree.IsHidden || CheckPrerequisites(character, tree)) &&
    IsTreeAvailable(character, tree)
).ToList();
```

- [ ] **Step 5: Run tests and commit**

```bash
dotnet test Darkness.Tests --filter "TalentServiceTests"
git add Darkness.Core/Services/TalentService.cs
git commit -m "feat: implement class filtering and exclusivity for talent trees"
```

---

### Task 3: Layout Calculation Engine

**Files:**
- Create: `Darkness.Core/Services/TalentLayoutHelper.cs`
- Test: `Darkness.Tests/Services/TalentLayoutHelperTests.cs`

- [ ] **Step 1: Create TalentLayoutHelper.cs with CalculateLayout method**

```csharp
public static void CalculateLayout(List<TalentNode> nodes) {
    // 1. Assign Rows by depth
    // 2. Assign Columns (0, 1, 2)
    // 3. Resolve collisions
}
```

- [ ] **Step 2: Implement depth-based Row calculation**

- [ ] **Step 3: Implement Column alignment (parent-based)**

- [ ] **Step 4: Write tests for layout calculation**

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/TalentLayoutHelper.cs
git commit -m "feat: add TalentLayoutHelper for automatic node positioning"
```

---

### Task 4: Data Seeding Updates

**Files:**
- Modify: `Darkness.Godot/assets/data/talent-trees.json`

- [ ] **Step 1: Update JSON with RequiredClass and ExclusiveGroupId**

- [ ] **Step 2: Add realistic prerequisite chains (branches and convergences)**

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/assets/data/talent-trees.json
git commit -m "data: update talent trees with class restrictions and tiers"
```

---

### Task 5: UI Component Creation

**Files:**
- Create: `Darkness.Godot/scenes/UI/TalentNodeBox.tscn`
- Create: `Darkness.Godot/src/UI/TalentNodeBox.cs`

- [ ] **Step 1: Create TalentNodeBox scene with TextureButton and Labels**

- [ ] **Step 2: Implement TalentNodeBox.cs to update visual state (Locked/Unlocked/Available)**

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/scenes/UI/TalentNodeBox.tscn Darkness.Godot/src/UI/TalentNodeBox.cs
git commit -m "feat: create TalentNodeBox UI component"
```

---

### Task 6: TalentTreeScene Refactor

**Files:**
- Modify: `Darkness.Godot/src/UI/TalentTreeScene.cs`
- Modify: `Darkness.Godot/scenes/TalentTreeScene.tscn`

- [ ] **Step 1: Replace GridContainer with a generic Control (TreeCanvas)**

- [ ] **Step 2: Update LoadTrees to use TalentLayoutHelper**

- [ ] **Step 3: Implement line drawing with Line2D**

- [ ] **Step 4: Connect signals and verify**

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/src/UI/TalentTreeScene.cs Darkness.Godot/scenes/TalentTreeScene.tscn
git commit -m "feat: refactor TalentTreeScene with automatic grid layout and connections"
```
