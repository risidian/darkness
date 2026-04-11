# Sprite Review, Gender Filtering, and Off-Hand Weapon Support Design

**Date:** 2026-04-12
**Status:** Approved

## 1. Overview
This spec outlines the systematic review and import of missing LPC sprites (Mace, Axe), the implementation of gender-aware armor filtering, and the expansion of the sprite composition system to support off-hand weapons (e.g., a Mage's secondary dagger).

## 2. Goals
- **Sprite Completeness:** Import missing Mace and Waraxe sprites from the local LPC generator repository.
- **Gender Consistency:** Ensure armor selection is filtered by character gender.
- **Off-Hand Support:** Expand `CharacterAppearance` and `GodotSpriteCompositor` to support a dedicated off-hand weapon slot.
- **Starting Equipment:** Update class defaults to match the requested archetypes (Knight, Rogue, Mage, Cleric, Warrior).

## 3. Architecture & Implementation

### 3.1 Sprite Imports
The following assets will be imported from `C:\Users\Mayce\Documents\GitHub\Universal-LPC-Spritesheet-Character-Generator\spritesheets\`:
- **Mace:** `weapon/blunt/mace/` (walk, hurt, thrust, attack_slash)
- **Waraxe:** `weapon/blunt/waraxe/` (walk, hurt, attack_slash)
- **Armor Sync:** Verify all current armor in `Darkness.Godot/assets/sprites/full/` has both `male/` and `female/` subdirectories where applicable.

### 3.2 Model & Data Expansion
- **`CharacterAppearance.cs`**: Add `public string? OffHandType { get; set; } = "None";`
- **`sprite-catalog.json`**:
    - Add entries for "Mace" and "Waraxe" in the `Weapon` slot.
    - Update `Armor` entries to use `"Gender": "gendered"` and ensure paths resolve correctly in `SpriteLayerCatalog.cs`.
    - Update `ClassDefaults` for all 5 classes.
    - **Mage Robes:** Map to `torso/clothes/robe/female` for females and `torso/jacket/tabard/male` for males to provide a consistent "clothed caster" look.

### 3.3 Off-Hand Weapon Rendering
LPC does not provide separate left-hand weapon sheets. To support dual-wielding (e.g., Wand + Dagger):
- **Mirroring Technique:** `GodotSpriteCompositor` will implement a mirroring transformation for off-hand weapons.
    - The entire weapon sheet is flipped horizontally.
    - The East (right-facing) and West (left-facing) rows are swapped to maintain correct directional logic.
- **Z-Ordering:** Off-hand weapons will be rendered at `ZOrder: 135` (between Body 10 and Primary Weapon 140/Shield 130).

### 3.4 Gender Filtering
- **`ISpriteLayerCatalog.GetOptionNames`**: Update signature to `List<string> GetOptionNames(string category, string gender);`.
- **Filtering Logic:** Only return `EquipmentSprite` entries where `Gender == "universal"` or `Gender == "gendered"` or `Gender == gender`.

## 4. Class Defaults
| Class | Primary Weapon | Off-Hand | Shield | Armor |
| :--- | :--- | :--- | :--- | :--- |
| **Knight** | Arming Sword (Steel) | None | Spartan | Plate (Steel) |
| **Rogue** | Dagger (Steel) | None | None | Leather (Black) |
| **Mage** | Mage Wand | Dagger (Steel) | None | Mage Robes (Blue) |
| **Cleric** | Mace | None | Crusader | Longsleeve (White) |
| **Warrior** | Waraxe | None | None | Plate (Steel) |

## 5. Verification Plan
- **Unit Tests:** Update `SpriteLayerCatalogTests` to verify gender filtering and new weapon slots.
- **Visual Check:** Launch `CharacterGenScene` to verify that selecting "Male" filters out female-only armor and that the Mage correctly displays both Wand and Dagger.
- **Regression:** Ensure existing animations (Walk, Slash, Spellcast) still work correctly for all new weapon types.
