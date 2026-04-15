# Hard-Baked Sprite Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a "Hard-Baked" persistence model for the sprite engine. CharacterGen and Inventory will generate a single `FullSpriteSheet` PNG saved to LiteDB. World and Battle scenes will render only this baked texture.

**Architecture:** Refactor `LayeredSprite` from a multi-node system to a single `BakedSprite` system. Ensure all animations (including 192px oversize attacks) are correctly mapped and aligned in the bake step.

**Tech Stack:** .NET 10, LiteDB, SkiaSharp, Godot 4.6.1 (C#)

---

### Task 1: Core Model & Deserialization Fixes

**Files:**
- Modify: `Darkness.Core/Models/SheetLayer.cs`
- Modify: `Darkness.Core/Models/SheetDefinition.cs`
- Create: `Darkness.Core/Models/ClassDefault.cs`
- Modify: `Darkness.Core/Services/AppearanceSeeder.cs`

- [ ] **Step 1: Fix JSON Field Mappings**
Decorate `SheetLayer` and `SheetDefinition` with `JsonPropertyName` to ensure `custom_animation`, `preview_row`, and `preview_column` load correctly from the JSON seed files.
```csharp
using System.Text.Json.Serialization;
// ... add [JsonPropertyName("custom_animation")] etc.
```

- [ ] **Step 2: Implement ClassDefault Model**
Create the `ClassDefault` model to store the data-driven starter gear for each class (Knight, Mage, etc.).
```csharp
namespace Darkness.Core.Models;
public class ClassDefault {
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ArmorType { get; set; } = "None";
    public string WeaponType { get; set; } = "None";
    public string ShieldType { get; set; } = "None";
    public string? OffHandType { get; set; } = "None";
    public string Head { get; set; } = "Human Male";
    public string Face { get; set; } = "Default";
    public string Feet { get; set; } = "None";
    public string Arms { get; set; } = "None";
    public string Legs { get; set; } = "None";
    public string SkinColor { get; set; } = "Light";
    public string HairStyle { get; set; } = "None";
    public string HairColor { get; set; } = "None";
    public string Eyes { get; set; } = "Default";
}
```

- [ ] **Step 3: Update Seeder**
Update `Darkness.Core/Services/AppearanceSeeder.cs` to load the `ClassDefaults` section of `sprite-catalog.json` into a new LiteDB collection.

- [ ] **Step 4: Commit**
```bash
git add Darkness.Core/Models/SheetLayer.cs Darkness.Core/Models/SheetDefinition.cs Darkness.Core/Models/ClassDefault.cs Darkness.Core/Services/AppearanceSeeder.cs
git commit -m "feat: fix JSON field mappings and add ClassDefault model"
```

---

### Task 2: Compositor Centering & Caching

**Files:**
- Modify: `Darkness.Core/Services/SkiaSharpSpriteCompositor.cs`

- [ ] **Step 1: Implement Bitmap Caching**
Add an `SKBitmap` cache to the compositor in `CompositeFullSheet` and `CompositePreviewFrame`.

- [ ] **Step 2: Correct Oversize Frame Alignment**
Modify the oversize rendering loop in `CompositeFullSheet` to center the 64x64 character body at `(64, 64)` within the 192x192 frame.

- [ ] **Step 3: Comprehensive Path Resolution**
Enhance the path resolution in `SkiaSharpSpriteCompositor.cs` to check for gendered folders (e.g., `armor/leather/male/walk/steel.png`) before falling back to ungendered paths. Use a list of candidate paths and load the first one that exists.

- [ ] **Step 4: Commit**
```bash
git add Darkness.Core/Services/SkiaSharpSpriteCompositor.cs
git commit -m "feat: implement bitmap caching and fix oversize frame alignment in compositor"
```

---

### Task 3: Godot LayeredSprite Scene Refactor

**Files:**
- Modify: `Darkness.Godot/scenes/LayeredSprite.tscn`
- Modify: `Darkness.Godot/src/Game/LayeredSprite.cs`
- Modify: `Darkness.Godot/src/Core/ImageUtils.cs`

- [ ] **Step 1: Remove Layer Nodes**
Modify `Darkness.Godot/scenes/LayeredSprite.tscn` to remove all children nodes of `LayeredSprite` and add a single `AnimatedSprite2D` named `BakedSprite` with `centered = false`.

- [ ] **Step 2: Update Setup Logic**
Update `Darkness.Godot/src/Game/LayeredSprite.cs` to use `_bakedSprite` and remove all logic related to managing multiple `_layers`. Ensure it bakes if `FullSpriteSheet` is empty and a `compositor` is provided. Update `Play` to offset position for oversize frames correctly.

- [ ] **Step 3: Fix Row Mapping for Hurt/Shoot**
Update `ImageUtils.CreateSpriteFrames` in `Darkness.Godot/src/Core/ImageUtils.cs` to handle single-row animations correctly. For row 20 (`hurt`), it should create a single animation, not try to find 4 directions.

- [ ] **Step 4: Commit**
```bash
git add Darkness.Godot/scenes/LayeredSprite.tscn Darkness.Godot/src/Game/LayeredSprite.cs Darkness.Godot/src/Core/ImageUtils.cs
git commit -m "feat: refactor LayeredSprite to use a single baked sprite node"
```

---

### Task 4: Scene Flow Integration

**Files:**
- Modify: `Darkness.Godot/src/UI/CharacterGenScene.cs`
- Modify: `Darkness.Godot/src/UI/InventoryScene.cs`
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Await Bake on Character Creation**
In `Darkness.Godot/src/UI/CharacterGenScene.cs`, ensure `CompositeFullSheet` is awaited during `OnCreatePressed` before saving and navigating.

- [ ] **Step 2: Atomic Equipment Swap**
In `Darkness.Godot/src/UI/InventoryScene.cs`, ensure `RegenerateFullSheet()` is fully awaited and the character is saved to LiteDB when equipping items.

- [ ] **Step 3: NPC Sheet Caching**
Update `Darkness.Godot/src/Game/WorldScene.cs` to bake NPC sheets once per encounter and cache the texture.

- [ ] **Step 4: Commit**
```bash
git add Darkness.Godot/src/UI/CharacterGenScene.cs Darkness.Godot/src/UI/InventoryScene.cs Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: ensure full sprite sheet is baked and saved in scenes"
```

---

### Task 5: Verification & Regression

- [ ] **Step 1: Run Animation Tests**
Run `dotnet test Darkness.Tests` to ensure the new row mapping doesn't break existing combat logic.

- [ ] **Step 2: Visual Smoke Test**
Run the `SpriteSheetGenerator` (if applicable) or run the game and verify in-game that the character and weapon are perfectly aligned and rendered as a single sheet.