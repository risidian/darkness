# Passive vs. Active Talent Distinction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Distinguish between passive and active talents, update models/data, and adjust UI for better clarity.

**Architecture:** 
- Add `IsPassive` boolean to `TalentNode` model.
- Migrate JSON data to include `IsPassive` flag.
- Update `TalentNodeBox` UI to show visual distinction.
- Update `SkillsScene` to filter out passive talents.

**Tech Stack:** Godot 4.6.1 (.NET 10), C#.

---

### Task 1: Data Model Update

**Files:**
- Modify: `Darkness.Core/Models/TalentNode.cs`

- [ ] **Step 1: Add `IsPassive` property**

```csharp
public bool IsPassive { get; set; } = false;
```

### Task 2: Data Migration

**Files:**
- Modify: `Darkness.Godot/assets/data/talent-trees.json`

- [ ] **Step 1: Add `"IsPassive": true/false` to all nodes**
Review `talent-trees.json` and mark stat-boosting talents as `true` and skill-unlocking talents as `false`.

### Task 3: UI Updates (TalentNodeBox)

**Files:**
- Modify: `Darkness.Godot/src/UI/TalentNodeBox.cs`

- [ ] **Step 1: Update visual rendering based on `IsPassive`**
In `Setup(TalentNode node)`, set the Panel background color.

```csharp
var style = new StyleBoxFlat();
style.BgColor = node.IsPassive ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.4f, 0.2f, 0.4f);
this.AddThemeStyleboxOverride("panel", style);
```

### Task 4: UI Updates (Skills Menu)

**Files:**
- Modify: `Darkness.Godot/src/UI/SkillsScene.cs`

- [ ] **Step 1: Filter passive talents**
Ensure `GetAvailableSkills` or the scene itself filters out nodes with `IsPassive == true` so they don't appear in the "Equip" list.

```csharp
// Inside LoadSkills in SkillsScene.cs
var availableSkills = _weaponSkillService.GetAvailableSkills(character)
    .Where(s => !s.IsPassive).ToList();
```

---
