# Quest Exploration & Zone Depth - Task 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create models and database registration for quest rewards and world flags.

**Architecture:** Add new models for `QuestReward` and `WorldFlag` to `Darkness.Core.Models`. Update existing models `QuestStep` and `ZoneConfig` to include new fields for rewards, requirements, and flag-based visibility. Register `WorldFlag` in `LocalDatabaseService`.

**Tech Stack:** .NET 10, LiteDB.

---

### Task 1.1: Create Models

**Files:**
- Create: `Darkness.Core/Models/QuestReward.cs`
- Create: `Darkness.Core/Models/WorldFlag.cs`

- [ ] **Step 1: Create `Darkness.Core/Models/QuestReward.cs`**
- [ ] **Step 2: Create `Darkness.Core/Models/WorldFlag.cs`**

### Task 1.2: Update Existing Models

**Files:**
- Modify: `Darkness.Core/Models/QuestStep.cs`
- Modify: `Darkness.Core/Models/ZoneConfig.cs`

- [ ] **Step 1: Add `Requirements` and `Rewards` to `QuestStep.cs`**
- [ ] **Step 2: Add `RequiredFlag` and `ForbiddenFlag` to `ZoneConfig.cs`**

### Task 1.3: Update Database Registration

**Files:**
- Modify: `Darkness.Core/Data/LocalDatabaseService.cs`

- [ ] **Step 1: Register `WorldFlag` entity mapping**

### Task 1.4: Verification

- [ ] **Step 1: Build the solution**
- [ ] **Step 2: Add a test for WorldFlag database registration**
