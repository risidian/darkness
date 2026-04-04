# Beat 2, 3 & Combat Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the next two main story beats (Dark Warrior and Sorcerer) and upgrade the combat animation system to support full LPC spritesheets and dynamic "lunge" attacks.

**Architecture:** We will enhance `ImageUtils` to detect full LPC spritesheets (1536x2112) and slice the complete animation set (Slash, Spellcast, etc.). `BattleScene` will be updated to prioritize these animations during combat and provide visual feedback via a Tween-based "lunge" for characters without specialized attack frames.

**Tech Stack:** Godot 4.6.1 (.NET 10), C#, Godot Tween API.

---

### Task 1: Asset Migration & Directory Setup

**Files:**
- Create: `Darkness.Godot/assets/sprites/bosses/`
- Copy: `SpriteSheets/*.png` to `Darkness.Godot/assets/sprites/bosses/`

- [x] **Step 1: Create the bosses directory**
- [x] **Step 2: Copy the boss spritesheets**
- [x] **Step 3: Verify files exist**
- [ ] **Step 4: Commit**

### Task 2: Enhance ImageUtils for Full LPC Support

**Files:**
- Modify: `Darkness.Godot/src/Core/ImageUtils.cs`

- [x] **Step 1: Update `CreateSpriteFrames` to support full LPC layouts**
- [x] **Step 2: Commit**

### Task 3: Support Single-Sheet Loading in LayeredSprite

**Files:**
- Modify: `Darkness.Godot/src/Game/LayeredSprite.cs`

- [x] **Step 1: Add `SetupFullSheet` method to bypass layer composition for bosses**
- [x] **Step 2: Commit**

### Task 4: Upgrade Combat Animations with Lunge and Selection Priority

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

- [x] **Step 1: Update `UpdateSprites` to load boss sheets correctly**
- [x] **Step 2: Update `ExecuteAttack` with Lunge and Priority Logic**
- [x] **Step 3: Commit**

### Task 5: Define Beat 2 & 3 in Quest Data

**Files:**
- Modify: `Darkness.Godot/assets/data/quests.json`

- [x] **xStep 1: Update Beat 2 and 3 with correct sprite paths and stats**
- [x] **Step 2: Commit**
