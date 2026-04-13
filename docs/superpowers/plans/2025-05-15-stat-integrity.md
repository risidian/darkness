# Talent & Stats System Refinement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unify stat bonus lookups and add the `Attack` property to ensure both gear and talents contribute to derived stats.

**Architecture:** Use a `GetTotalBonus` helper in `Character.cs` to sum `StatBonuses` (gear) and `TalentStatBonuses` (talents). Update `RecalculateDerivedStats` to use this helper and include the new `Attack` property.

**Tech Stack:** .NET 10, C# 13, XUnit

---

### Task 1: Update Character.cs Model

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`

- [ ] **Step 1: Add GetTotalBonus helper method**
```csharp
public int GetTotalBonus(string key) =>
    (StatBonuses.TryGetValue(key, out var b) ? b : 0) +
    (TalentStatBonuses.TryGetValue(key, out var tb) ? tb : 0);
```

- [ ] **Step 2: Add Attack property**
Add `public int Attack { get; set; }` to the derived stats section.

- [ ] **Step 3: Update RecalculateDerivedStats**
Update the method to use `GetTotalBonus` and include `Attack` and fix `Accuracy`.
`Accuracy` should only include its own bonus, not `Attack`'s bonus.
`Attack` should be `Strength * 2 + GetTotalBonus("Attack")`.
`ArmorClass` should use `GetTotalBonus("ArmorClass")`.

- [ ] **Step 4: Commit changes**
```bash
git add Darkness.Core/Models/Character.cs
git commit -m "feat: add GetTotalBonus helper and Attack property to Character"
```

### Task 2: Update CharacterStatTests.cs

**Files:**
- Modify: `Darkness.Tests/Models/CharacterStatTests.cs`

- [ ] **Step 1: Add test for summing gear and talent bonuses**
Add `RecalculateDerivedStats_SumsGearAndTalentBonuses`.

- [ ] **Step 2: Update existing tests to use "ArmorClass" instead of "Armor" if applicable**
The existing test `RecalculateDerivedStats_IncludesTalentArmor` uses "Armor", update it to "ArmorClass" to match the new lookup key.

- [ ] **Step 3: Run tests to verify**
Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~CharacterStatTests"`

- [ ] **Step 4: Commit changes**
```bash
git add Darkness.Tests/Models/CharacterStatTests.cs
git commit -m "test: add test for gear and talent bonus summation"
```

### Task 3: Final Verification

- [ ] **Step 1: Run all tests**
Run: `dotnet test Darkness.Tests`

- [ ] **Step 2: Final Commit**
```bash
git commit --amend -m "fix: unify stat bonus lookups and add attack property"
```
Wait, the user wanted a specific commit message: "fix: unify stat bonus lookups and add attack property".
I'll do that at the end.
