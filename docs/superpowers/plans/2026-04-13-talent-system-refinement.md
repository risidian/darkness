# Talent & Stats System Refinement Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure all talent bonuses are functional in the game engine and improve the UI feedback loop for a better player experience.

**Architecture:** 
- **Stats:** Update `Character.cs` to aggregate all bonus sources (Generic, Talent, and future Gear) into derived stats.
- **UI:** Enhance `TalentService` to provide metadata about why talents are locked, surfacing this via tooltips.
- **Layout:** Relax column constraints in `TalentLayoutHelper` to support wider tree designs.

**Tech Stack:** Godot 4.6.1 (.NET 10), LiteDB, XUnit.

---

### Task 1: Stat Integrity (Backend)

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`
- Test: `Darkness.Tests/Models/CharacterStatTests.cs`

- [ ] **Step 1: Write a failing test for secondary talent bonuses**
Ensure that if a character has a "Armor" bonus in `TalentStatBonuses`, it is reflected in `ArmorClass`.

```csharp
[Fact]
public void RecalculateDerivedStats_IncludesTalentArmor() {
    var character = new Character { BaseConstitution = 10 };
    character.TalentStatBonuses["Armor"] = 5;
    character.RecalculateDerivedStats();
    Assert.Equal(10, character.ArmorClass); // Base(10/2) + 5
}
```

- [ ] **Step 2: Update Character.cs to include all talent bonuses in derived stats**
Modify `RecalculateDerivedStats` and properties like `ArmorClass`, `Attack`, `Accuracy` to look at the `TalentStatBonuses` dictionary.

- [ ] **Step 3: Run tests and verify**

- [ ] **Step 4: Commit**

---

### Task 2: UI Feedback & Tooltips

**Files:**
- Modify: `Darkness.Core/Interfaces/ITalentService.cs`
- Modify: `Darkness.Core/Services/TalentService.cs`
- Modify: `Darkness.Godot/src/UI/TalentNodeBox.cs`

- [ ] **Step 1: Create a `TalentPurchaseResult` struct to hold failure reasons**
Instead of returning `bool`, return an object that contains `bool CanPurchase` and `string? Reason`.

- [ ] **Step 2: Update `CanPurchaseTalent` logic to populate reasons**
(e.g., "Requires Level 20", "Requires 5 points in Knight Tree", "Locked by Exclusive Group").

- [ ] **Step 3: Update `TalentNodeBox.cs` to display the reason in the tooltip**

- [ ] **Step 4: Verify UI and Commit**

---

### Task 3: Visual Pathing & Legend

**Files:**
- Modify: `Darkness.Godot/src/UI/TalentTreeScene.cs`
- Modify: `Darkness.Godot/scenes/TalentTreeScene.tscn`

- [ ] **Step 1: Update Line2D color logic**
Use a "Dashed" or "Dim" color for paths that are not yet available (prereqs not met).

- [ ] **Step 2: Add a Legend UI panel**
Add a small Panel in the corner of the TalentTreeScene explaining colors: Green (Unlocked), Yellow (Available), Grey (Locked).

- [ ] **Step 3: Verify and Commit**
