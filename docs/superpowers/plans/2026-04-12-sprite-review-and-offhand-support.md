# Sprite Review and Off-Hand Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Import missing LPC sprites, implement gender-aware armor filtering, and add off-hand weapon support (e.g. Wand + Dagger).

**Architecture:** 
- **Mirroring Logic:** Expand `GodotSpriteCompositor` to horizontally flip primary weapon sheets for use in the off-hand, since LPC doesn't provide left-hand sheets.
- **Data-Driven Filtering:** Update `SpriteLayerCatalog` to filter equipment by gender.
- **Model Expansion:** Add `OffHandType` to `CharacterAppearance`.

**Tech Stack:** Godot 4.6.1 (.NET 10), LiteDB, LPC Spritesheet System.

---

### Task 1: Model & Interface Expansion

**Files:**
- Modify: `Darkness.Core/Models/CharacterAppearance.cs`
- Modify: `Darkness.Core/Models/Character.cs`
- Modify: `Darkness.Core/Interfaces/ISpriteLayerCatalog.cs`

- [ ] **Step 1: Add OffHandType to CharacterAppearance**
- [ ] **Step 2: Add OffHandType to Character**
- [ ] **Step 3: Update ISpriteLayerCatalog interface**
- [ ] **Step 4: Commit**

---

### Task 2: Service Layer & Mirroring Logic

**Files:**
- Modify: `Darkness.Core/Services/SpriteLayerCatalog.cs`
- Modify: `Darkness.Godot/src/Services/GodotSpriteCompositor.cs`
- Modify: `Darkness.Core/Models/StitchLayer.cs`

- [ ] **Step 1: Add IsFlipped property to StitchLayer**
- [ ] **Step 2: Implement Mirroring in GodotSpriteCompositor**
- [ ] **Step 3: Update SpriteLayerCatalog filtering and OffHand support**
- [ ] **Step 4: Commit**

---

### Task 3: Data Migration & Sprite Import

**Files:**
- Modify: `Darkness.Godot/assets/data/sprite-catalog.json`

- [ ] **Step 1: Import Mace and Waraxe sprites**
- [ ] **Step 2: Update sprite-catalog.json with new weapons and gendered armor**
- [ ] **Step 3: Update ClassDefaults in sprite-catalog.json**
- [ ] **Step 4: Commit**

---

### Task 4: UI Update & Verification

**Files:**
- Modify: `Darkness.Godot/src/UI/CharacterGenScene.cs`

- [ ] **Step 1: Add Off-Hand Dropdown to UI**
- [ ] **Step 2: Add Gender Change Listener for dynamic filtering**
- [ ] **Step 3: Verify with Knight/Mage tests**
- [ ] **Step 4: Commit**

---

### Task 5: Regression Testing

**Files:**
- Modify: `Darkness.Tests/Services/SpriteLayerCatalogTests.cs`

- [ ] **Step 1: Add test for gender-filtered options**
- [ ] **Step 2: Add test for off-hand layer presence**
- [ ] **Step 3: Run all tests**
- [ ] **Step 4: Commit**
