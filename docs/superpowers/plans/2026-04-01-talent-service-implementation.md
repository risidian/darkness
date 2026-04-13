# Talent Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the logic in `TalentService.cs` and verify it with unit tests.

**Architecture:**
- `TalentService` will interact with `LiteDatabase` to fetch `TalentTree` models.
- It will evaluate prerequisites for trees and nodes.
- It will modify `Character` models to unlock talents and deduct points.
- Stat bonuses from passives will be calculated based on unlocked talents.

**Tech Stack:**
- .NET 10
- LiteDB
- XUnit
- Moq

---

### Task 1: Basic TalentService Implementation - `GetAvailableTrees`

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`
- Create: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Write a failing test for `GetAvailableTrees`**

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Implement `GetAvailableTrees`**

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Commit**

---

### Task 2: Implement `CanPurchaseTalent` and `PurchaseTalent`

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`
- Modify: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Write a failing test for `PurchaseTalent`**

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Implement `CanPurchaseTalent` and `PurchaseTalent`**

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Commit**

---

### Task 3: Implement `ApplyTalentPassives`

**Files:**
- Modify: `Darkness.Core/Services/TalentService.cs`
- Modify: `Darkness.Tests/Services/TalentServiceTests.cs`

- [ ] **Step 1: Write a failing test for `ApplyTalentPassives`**

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Implement `ApplyTalentPassives`**

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Commit**
