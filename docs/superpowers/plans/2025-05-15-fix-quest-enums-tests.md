# Fix Quest Enums in Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix compilation errors in 8 test files by replacing string-based Type assignments with `QuestRewardType` and `QuestStepType` enum values.

**Architecture:** Surgical string replacement in test files, ensuring correct casing and enum namespace usage.

**Tech Stack:** C#, .NET 10, XUnit

---

### Task 1: Fix Darkness.Tests/Models/WorldFlagTests.cs

**Files:**
- Modify: `Darkness.Tests/Models/WorldFlagTests.cs`

- [ ] **Step 1: Replace string types with enums**
- [ ] **Step 2: Verify compilation**

### Task 2: Fix Darkness.Tests/Models/QuestModelTests.cs

**Files:**
- Modify: `Darkness.Tests/Models/QuestModelTests.cs`

- [ ] **Step 1: Replace string types with enums**
- [ ] **Step 2: Verify compilation**

### Task 3: Fix Darkness.Tests/Services/TriggerServiceTests.cs

**Files:**
- Modify: `Darkness.Tests/Services/TriggerServiceTests.cs`

- [ ] **Step 1: Replace string types with enums**
- [ ] **Step 2: Verify compilation**

### Task 4: Fix Darkness.Tests/Services/QuestServiceRobustnessTests.cs

**Files:**
- Modify: `Darkness.Tests/Services/QuestServiceRobustnessTests.cs`

- [ ] **Step 1: Replace string types with enums**
- [ ] **Step 2: Verify compilation**

### Task 5: Fix Darkness.Tests/Services/QuestSeederTests.cs

**Files:**
- Modify: `Darkness.Tests/Services/QuestSeederTests.cs`

- [ ] **Step 1: Replace string types with enums**
- [ ] **Step 2: Verify compilation**

### Task 6: Fix Darkness.Tests/Services/QuestServiceTests.cs

**Files:**
- Modify: `Darkness.Tests/Services/QuestServiceTests.cs`

- [ ] **Step 1: Replace string types with enums**
- [ ] **Step 2: Verify compilation**

### Task 7: Fix Darkness.Tests/Services/QuestExplorationTests.cs

**Files:**
- Modify: `Darkness.Tests/Services/QuestExplorationTests.cs`

- [ ] **Step 1: Replace string types with enums**
- [ ] **Step 2: Verify compilation**

### Task 8: Fix Darkness.Tests/Services/QuestProgressionTests.cs

**Files:**
- Modify: `Darkness.Tests/Services/QuestProgressionTests.cs`

- [ ] **Step 1: Replace string types with enums**
- [ ] **Step 2: Verify compilation**

### Task 9: Final Validation

- [ ] **Step 1: Run all tests to ensure no regressions**
Run: `dotnet test Darkness.Tests`
