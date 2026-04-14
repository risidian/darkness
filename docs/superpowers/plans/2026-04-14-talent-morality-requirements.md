# Talent Morality Requirements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate morality checks into the unlock requirements for Knight Tier 3 talents (Crusader and Oathbreaker).

**Architecture:** Extend `TalentService.IsTreeAvailable` to evaluate morality conditions based on character morality.

**Tech Stack:** C#, .NET 10, LiteDB, JSON (talent-trees.json).

---

### Task 1: Update `TalentService` to support Morality

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`

- [ ] **Step 1: Update `IsTreeAvailable` to check Morality**
Add check for `RequiredMorality` in `TalentTree` model (which we need to add to the class) and evaluate it against character morality.

Wait, first I need to add `RequiredMorality` to `TalentTree.cs`.

### Task 2: Update TalentTree Model

**Files:**
- Modify: `Darkness.Core/Models/TalentTree.cs`

- [ ] **Step 1: Add `RequiredMorality`**

```csharp
public int? RequiredMorality { get; set; } // Positive for Crusader, Negative for Oathbreaker
```

### Task 3: Update `TalentService.cs`

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`

- [ ] **Step 1: Add check in `IsTreeAvailable`**

```csharp
if (tree.RequiredMorality.HasValue)
{
    if (tree.RequiredMorality.Value > 0 && character.Morality < 0)
         return TalentPurchaseResult.Failed("Requires positive morality");
    if (tree.RequiredMorality.Value < 0 && character.Morality > 0)
         return TalentPurchaseResult.Failed("Requires negative morality");
}
```

### Task 4: Update `talent-trees.json`

**Files:**
- Modify: `Darkness.Godot/assets/data/talent-trees.json`

- [ ] **Step 1: Add `RequiredMorality` to Crusader and Oathbreaker (renamed from Kingsguard?)**
Actually, the user said "the other third level option for knight will be oath breaker". So I'll rename `kingsguard_tree` to `oathbreaker_tree`.

- [ ] **Step 2: Update Crusader tree**
Set `RequiredMorality: 1`.

- [ ] **Step 3: Update Oathbreaker tree**
Set `RequiredMorality: -1`.

---
