# Design Spec: Full Spritesheet Generation, Stealth Mini-Game & Expanded Weapons

## 1. Overview
This spec covers three major enhancements to the *Darkness: Reforged* engine:
1.  **Full Spritesheet Engine**: Transitioning the character system to generate and persist full 1536x2112 LPC-compliant spritesheets during character creation and equipment updates.
2.  **Expanded Weaponry**: Migrating a diverse set of full-sized weapon layers (Daggers, Bows, Staffs, Shields) from the LPC generator repository.
3.  **Stealth Mini-Game ("The Shadow Path")**: Implementing a timing-based mini-game for the sneak path in Beat 1.

## 2. Full Spritesheet Engine & Data Persistence

### 2.1 Model Changes
- **`Character.cs`**: Add a `byte[]? FullSpriteSheet` property. This will store the complete PNG data of the 1536x2112 composite.
- **`CharacterSnapshot.cs`**: No changes. The snapshot will remain a condensed preview to keep login/loading performant.

### 2.2 Composition Workflow
- **`GodotSpriteCompositor.cs`**: Update `CompositeLayers` to default to 1536x2112 if requested.
- **`CharacterGenScene.cs`**: 
    - During the live preview, continue using a cropped frame for performance.
    - When the user clicks "Create," perform a full composition of all layers at 1536x2112 and save the result to the character's `FullSpriteSheet` property.
- **`InventoryScene.cs`**: 
    - When equipment is changed, trigger a background task to re-composite the full sheet.
    - Update the `LayeredSprite` in the world/battle scenes immediately after the database save.

### 2.3 Rendering Optimization
- **`LayeredSprite.cs`**: 
    - Add logic to check for `Character.FullSpriteSheet`.
    - If present, load the entire sheet as the `Body` layer and disable other layers. This significantly reduces draw calls and texture swaps.

## 3. Expanded Weapon & Layer Migration

### 3.1 Layer Refresh
- Replace current 576x256 PNGs in `Darkness.Godot/sprites/` with full-sized 1536x2112 counterparts from the `Universal-LPC-Spritesheet-Character-Generator` repository.
- **Categories**: Body, Armor, Head, Hair, Eyes, Face, Legs, Feet, Arms.

### 3.2 New Weapons
- **Daggers**: `spritesheets/weapon/sword/dagger/`
- **Bows**: `spritesheets/weapon/ranged/bow/` (or crossbow)
- **Mage Staffs/Wands**: `spritesheets/weapon/magic/`
- **Shields**: `spritesheets/shield/heater/`
- **SpriteLayerCatalog.cs**: Update to include these new categories and map them to their full-sized resource paths.

## 4. Stealth Mini-Game ("The Shadow Path")

### 4.1 Concept
A timing-based mini-game that replaces the immediate "Dialogue -> Combat" flow for the sneak path.

### 4.2 UI Components
- **StealthBar**: A horizontal bar representing the current "Safe Zone" and "Detection Level."
- **Marker**: A moving indicator that oscillates across the bar.
- **Progress Counter**: Tracks successful "steps" (taps in the green zone).

### 4.3 Gameplay Logic
- **Success Criteria**: 5 consecutive successful taps in the green zone.
- **Failure Criteria**: 3 misses or filling the "Detection" bar (time-based).
- **Outcomes**:
    - **Success**: Bypass `beat_1_sneak_combat`. Transition to `beat_2` (Balgathor). Apply "Sneak Attack" buff (1.5x damage for first turn).
    - **Failure**: Triggers `beat_1_sneak_combat` (Creek Monster) with a "Surprised" debuff (-10% Evasion).

## 5. Quest Data Updates
- **`quests.json`**: Update `beat_1` choices to point to the new `StealthScene` (to be created) instead of directly to dialogue/combat.
- **`beat_1_sneak_combat`**: Update the "Creek Monster" stats to be a significant threat for low-level players.
