# Full Spritesheet, Stealth Mini-Game & Weapons Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full spritesheet generation and persistence, expand the weapon catalog from the generator repo, and build out the timing-based stealth mini-game for Beat 1.

**Architecture:** We will update the `Character` model to persist a 1536x2112 `FullSpriteSheet` (byte array). The `GodotSpriteCompositor` will generate this sheet during creation and equipment changes. `LayeredSprite` will prioritize the full sheet for rendering. The stealth mini-game will be a new Godot Scene that uses a `Tween`-animated bar for timing-based challenges.

**Tech Stack:** Godot 4.6.1 (.NET 10), LiteDB, Godot Tween API.

---

### Task 1: Update Model and Database

**Files:**
- Modify: `Darkness.Core/Models/Character.cs:50-55`

- [ ] **Step 1: Add the `FullSpriteSheet` property to the `Character` class**

```csharp
// Darkness.Core/Models/Character.cs
public byte[]? FullSpriteSheet { get; set; }
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Models/Character.cs
git commit -m "feat: add FullSpriteSheet property to Character model"
```

### Task 2: Migrate Full-Sized Layers and Catalog Expansion

**Files:**
- Create: `Darkness.Godot/assets/sprites/full/`
- Modify: `Darkness.Core/Services/SpriteLayerCatalog.cs`

- [ ] **Step 1: Create directories for full layers**

Run: `mkdir -p Darkness.Godot/assets/sprites/full/body Darkness.Godot/assets/sprites/full/weapons Darkness.Godot/assets/sprites/full/armor`

- [ ] **Step 2: Copy full-sized layers from generator repo**
- [ ] **Step 3: Update `SpriteLayerCatalog.cs` for new weapons and full paths**
- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/assets/sprites/full/ Darkness.Core/Services/SpriteLayerCatalog.cs
git commit -m "feat: migrate full-sized layers and expand weapon catalog"
```

### Task 3: Implement Full-Sheet Generation in CharacterGenScene

**Files:**
- Modify: `Darkness.Godot/src/UI/CharacterGenScene.cs`

- [ ] **Step 1: Update `OnCreatePressed` to generate and save the full 1536x2112 sheet**

```csharp
// Darkness.Godot/src/UI/CharacterGenScene.cs -> OnCreatePressed
// Use 1536x2112 instead of 576x256
var fullSheet = _compositor.CompositeLayers(streams, 1536, 2112);
character.FullSpriteSheet = fullSheet;
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/src/UI/CharacterGenScene.cs
git commit -m "feat: generate full 1536x2112 sheet during character creation"
```

### Task 4: Implement Equipment Updates in InventoryScene

**Files:**
- Modify: `Darkness.Godot/src/UI/InventoryScene.cs`

- [ ] **Step 1: Implement actual equipment change logic (Dagger, Bow, Staff)**
- [ ] **Step 2: Add full-sheet re-generation after equipment is changed**
- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/UI/InventoryScene.cs
git commit -m "feat: implement equipment updates and full-sheet re-generation"
```

### Task 5: Optimize LayeredSprite Rendering

**Files:**
- Modify: `Darkness.Godot/src/Game/LayeredSprite.cs`

- [ ] **Step 1: Update `SetupCharacter` to prioritize `Character.FullSpriteSheet` if it exists**

```csharp
// Darkness.Godot/src/Game/LayeredSprite.cs -> SetupCharacter
if (c.FullSpriteSheet != null)
{
    // Use the optimized single-sheet loading
    await SetupFromBytes(c.FullSpriteSheet);
    return;
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/src/Game/LayeredSprite.cs
git commit -m "perf: optimize LayeredSprite by using pre-composed FullSpriteSheet"
```

### Task 6: Implement Stealth Mini-Game Scene

**Files:**
- Create: `Darkness.Godot/scenes/StealthScene.tscn`
- Create: `Darkness.Godot/src/Game/StealthScene.cs`

- [ ] **Step 1: Create the UI layout (Progress bar, Detection bar, Timing slider)**
- [ ] **Step 2: Implement the timing logic and success/failure outcomes**
- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/scenes/StealthScene.tscn Darkness.Godot/src/Game/StealthScene.cs
git commit -m "feat: add timing-based stealth mini-game"
```

### Task 7: Integrate Stealth Scene into Story Flow

**Files:**
- Modify: `Darkness.Godot/assets/data/quests.json`
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Update `quests.json` to link the sneak choice to the StealthScene**
- [ ] **Step 2: Update navigation and outcome handling in `WorldScene.cs`**
- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/assets/data/quests.json Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: link stealth mini-game to the story path"
```
