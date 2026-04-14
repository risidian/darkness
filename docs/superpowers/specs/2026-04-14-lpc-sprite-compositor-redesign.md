# LPC Sprite Compositor Redesign

**Date:** 2026-04-14
**Status:** Approved
**Approach:** Port-and-Adapt from Universal LPC Spritesheet Character Generator

## Problem

The current sprite compositor produces weapon geometry that doesn't match during animations. Weapons appear misaligned during slash, thrust, and other attack animations because:

1. Weapons are treated as 1-2 flat layers composited identically across all animation rows
2. No per-animation z-ordering — a weapon can't be behind the body in one animation and in front in another
3. No oversize frame support — attack animations need 192x192 frames for proper weapon swing arcs but are squeezed into 64x64
4. Sheet format is undersized (1536x2112, 6 animations) vs the LPC standard (832x3456, 54 animation rows)
5. Several catalog bugs: Mace undefined, Waraxe z-order collision with Head, Bow hardcoded to walk pose

The reference implementation (Universal LPC Spritesheet Character Generator) solves all of these with a multi-layer, per-animation sheet definition system.

## Decisions

- **Full parity** with the reference LPC generator's format (832x3456, 54 animation rows, oversize frames)
- **Adopt reference JSON format** — individual sheet definition files per item in `sheet_definitions/` directory
- **Clean break** — no backward compatibility with old format, existing characters get regenerated
- **Compositing moves to Core** — SkiaSharp-based compositor in Darkness.Core, testable without Godot
- **Class defaults first** — 6 weapons + 2 shields for initial implementation, architecture supports easy expansion

## Section 1: Sheet Format & Constants

Output spritesheet adopts the Universal LPC format exactly.

**Dimensions:** 832 x 3456 pixels (13 columns x 54 rows, 64x64 frames)

**Row Layout:**

| Rows | Animation | Frames | Directions |
|------|-----------|--------|------------|
| 0-3 | spellcast | 7 | N/W/S/E |
| 4-7 | thrust | 8 | N/W/S/E |
| 8-11 | walk | 9 | N/W/S/E |
| 12-15 | slash | 6 | N/W/S/E |
| 16-19 | shoot | 13 | N/W/S/E |
| 20 | hurt | 6 | 1 row |
| 21 | climb | 6 | 1 row |
| 22-25 | idle | 3 | N/W/S/E |
| 26-29 | jump | 6 | N/W/S/E |
| 30-33 | sit | 15 | N/W/S/E |
| 34-37 | emote | 15 | N/W/S/E |
| 38-41 | run | 8 | N/W/S/E |
| 42-45 | combat_idle | 3 | N/W/S/E |
| 46-48 | backslash | 12 | 3 rows |
| 49-51 | halfslash | 6 | 3 rows |

**Oversize Region:** Appended below the standard 3456px sheet at a fixed Y offset of 3456px. Uses 192x192 frames. The set of oversize animations is fixed in `SheetConstants` (matching the reference): `slash_oversize`, `slash_reverse_oversize`, `thrust_oversize`. Each occupies 4 direction rows (N/W/S/E). All characters get the same oversize region layout regardless of equipment — layers without oversize assets simply produce empty frames in that region.

**`SheetConstants` static class in Core:**
- `FRAME_SIZE = 64`
- `OVERSIZE_FRAME_SIZE = 192`
- `SHEET_WIDTH = 832`
- `SHEET_HEIGHT = 3456`
- `COLUMNS = 13`
- `ROWS = 54`
- Animation-to-row mapping dictionary
- Frame count per animation dictionary

## Section 2: Sheet Definition Data Model

Replaces the flat `EquipmentSprite` model with a multi-layer definition system matching the reference's JSON format.

**Directory structure:**

```
assets/data/sheet_definitions/
  weapons/
    sword/
      weapon_sword_arming.json
      weapon_sword_dagger.json
    blunt/
      weapon_blunt_mace.json
      weapon_blunt_waraxe.json
    ranged/
      weapon_ranged_recurve_bow.json
    magic/
      weapon_magic_wand.json
  shields/
    shield_spartan.json
    shield_crusader.json
```

**JSON schema per item:**

```json
{
  "name": "Arming Sword",
  "slot": "Weapon",
  "layer_1": {
    "zPos": 140,
    "male": "weapons/sword/arming/",
    "female": "weapons/sword/arming/"
  },
  "layer_2": {
    "zPos": 9,
    "male": "weapons/sword/arming/universal_behind/",
    "female": "weapons/sword/arming/universal_behind/"
  },
  "layer_3": {
    "custom_animation": "slash_oversize",
    "zPos": -1,
    "male": "weapons/sword/arming/attack_slash/behind/",
    "female": "weapons/sword/arming/attack_slash/behind/"
  },
  "layer_4": {
    "custom_animation": "slash_oversize",
    "zPos": 150,
    "male": "weapons/sword/arming/attack_slash/",
    "female": "weapons/sword/arming/attack_slash/"
  },
  "variants": ["steel", "iron", "gold"],
  "animations": ["walk", "hurt", "slash_oversize", "thrust_oversize"],
  "preview_row": 10,
  "preview_column": 1
}
```

**New Core models:**

- **`SheetDefinition`** — Top-level: name, slot, layers dictionary, variants list, animations list, preview metadata
- **`SheetLayer`** — Per-layer: zPos, custom_animation (nullable), body-type-to-path dictionary
- **`SheetConstants`** — Static: frame sizes, sheet dimensions, animation row mappings, frame counts

**Removals:**
- `EquipmentSprite` model — replaced by `SheetDefinition`
- `sprite-catalog.json` equipment section — replaced by individual sheet definition files
- `AppearanceOption` stays but gets expanded with multi-layer support for body parts that need it (most body parts are single-layer so they stay simple)

**Seeder changes:**
- New `SheetDefinitionSeeder` scans `assets/data/sheet_definitions/` recursively, loads all JSON files, inserts into `sheet_definitions` LiteDB collection
- Old `SpriteSeeder` removed
- `appearance_options` collection stays for simple single-layer body parts (hair, eyes, face, skin)

## Section 3: Compositor Architecture

Compositing moves from `GodotSpriteCompositor` (Godot Image API) to `Darkness.Core` using SkiaSharp.

**New class: `SkiaSharpSpriteCompositor` in Darkness.Core/Services/**

**New dependency:** Add `SkiaSharp` package to `Darkness.Core.csproj` (already used in Darkness.Tests, proven to work in the project).

**Compositing pipeline (mirrors reference renderer.js):**

1. **Resolve Layers** — For each equipped item, load its SheetDefinition, collect all layers. For each body part, collect appearance layers. Result: flat list of (path, zPos, customAnimation, bodyType) tuples.

2. **Group by Animation** — Standard animations: layers without custom_animation field. Custom animations: layers grouped by custom_animation name (e.g., "slash_oversize").

3. **Render Standard Sheet (832x3456)** — For each animation row: sort applicable layers by zPos ascending (lower drawn first = behind). For each layer: resolve asset path for body type + animation + variant, load PNG as SKBitmap, draw onto output canvas at correct (x, y), apply tint if specified.

4. **Render Oversize Region (appended below standard sheet)** — For each custom animation group: extract source frames from standard sheet (64x64), center in oversize canvas (192x192) with offset = (192-64)/2 = 64px. Sort custom animation layers by zPos. Draw custom layers at oversize frame positions. Append completed oversize rows to output.

5. **Export** — Encode final SKBitmap as PNG bytes.

**Z-ordering per animation (the key fix):**

The reference sorts layers by zPos per animation render pass, not globally. A weapon layer at zPos=140 for walk can be at zPos=-1 for slash (behind the body). This works because each `SheetLayer` with a `custom_animation` only applies to that specific animation's render pass, while standard layers apply to all non-custom animations.

**Asset path resolution:**

```
Base: assets/sprites/full/
Layer path: weapons/sword/arming/         (from SheetLayer for body type)
Animation: walk/                          (current animation)
Variant: steel.png                        (selected variant)
Full: assets/sprites/full/weapons/sword/arming/walk/steel.png
```

Fallback chain:
1. Exact path: `{layerPath}/{animation}/{variant}.png`
2. Attack prefix: `{layerPath}/attack_{animation}/{variant}.png`
3. Skip layer (log warning, don't crash)

**Updated `ISpriteCompositor` interface:**

```csharp
Task<byte[]> CompositeFullSheet(
    IReadOnlyList<SheetDefinition> definitions,
    CharacterAppearance appearance,
    IFileSystemService fileSystem);

Task<byte[]> CompositePreviewFrame(
    IReadOnlyList<SheetDefinition> definitions,
    CharacterAppearance appearance,
    IFileSystemService fileSystem);

byte[] ExtractFrame(byte[] spriteSheetPng,
    string animation, int frameIndex, int direction);
```

**Removals:**
- `GodotSpriteCompositor` — replaced entirely (or becomes thin wrapper delegating to Core)
- `StitchLayer` model — replaced by `SheetLayer` within `SheetDefinition`
- `CompositeLayers(IReadOnlyList<Stream>, int, int)` — the old low-level byte stream method is removed; all compositing goes through the new `SheetDefinition`-based methods
- bg/fg path hack in `SpriteLayerCatalog.AddEquipmentLayer()`

## Section 4: SpriteLayerCatalog Refactor

**Renamed: `SpriteLayerCatalog` -> `SheetDefinitionCatalog`**

**Updated interface `ISheetDefinitionCatalog`:**

```csharp
List<SheetDefinition> GetSheetDefinitions(CharacterAppearance appearance);
CharacterAppearance GetDefaultAppearanceForClass(string className);
List<string> GetOptionNames(string category, string gender);
SheetDefinition? GetSheetDefinitionByName(string slot, string displayName);
```

**`GetSheetDefinitions()` logic:**

1. Body — Load body SheetDefinition (always present, tinted with skin color)
2. Head, Face, Eyes, Hair — Load from `appearance_options` (simple single-layer items wrapped in minimal SheetDefinition)
3. Equipment (Armor, Feet, Legs, Arms) — Query `sheet_definitions` LiteDB collection
4. Weapon — Query `sheet_definitions`, returns full multi-layer definition
5. Shield — Same as weapon
6. OffHand — Same as weapon, flagged as flipped

**Hacks removed:**
- bg/fg path split logic — multi-layer is the native format
- Male mage robes redirect — becomes a proper sheet definition entry (tabard variant)
- Gender resolution — uses body-type-to-path dictionary in SheetLayer instead of string replacement

**Body type resolution:**
- `"male"` -> looks up `male` key in SheetLayer paths
- `"female"` -> looks up `female` key in SheetLayer paths
- Falls back to `male` if a body type key is missing
- Extensible: when more body types are added, the data model already supports it

**OffHand handling:**
`isFlipped` flag attached to `SheetDefinition` rather than `StitchLayer`. Compositor applies horizontal flip during compositing pass.

## Section 5: Godot Integration & Animation Playback

**New `LpcAnimationHelper` class in Core:**

```csharp
Rect GetFrameRect(string animation, int direction, int frameIndex);
Rect GetOversizeFrameRect(string customAnimation, int direction,
    int frameIndex, int oversizeYOffset);
int GetFrameCount(string animation);
bool IsOversize(string animation);
```

**Sprite loading:**
- Characters store composited PNG in LiteDB (same as now, larger sheet)
- On load, PNG converted to Godot Image -> ImageTexture
- `LpcAnimationHelper` provides frame rect lookups by animation name

**Scene updates:**

- **BattleScene** — Replace hardcoded row indices with `LpcAnimationHelper` queries. For oversize attacks, switch sprite RegionRect to 192x192 region with position offset of -64px on each axis to keep character centered.
- **WorldScene** — Walk/idle animations use named lookups instead of magic row numbers.
- **CharacterGenScene / CharactersScene** — Preview frame extraction uses updated `ExtractFrame` with animation name.
- **StealthScene, DeathmatchScene** — Same pattern: replace hardcoded rows with helper lookups.

**Animation name mapping:**
Scenes use string animation names (`"walk"`, `"slash"`, `"combat_idle"`) rather than magic row numbers. `SheetConstants` is the single source of truth for name-to-row translation. If a scene requests an animation without oversize frames, it falls back to 64x64.

**No changes to:** `GodotNavigationService`, `GodotFileSystemService`, DI registration pattern in `Global.cs`.

## Section 6: Migration & Testing Strategy

### Migration

**Data migration:**
1. Remove `sprite-catalog.json` equipment entries
2. Create 8 individual sheet definition JSON files for class defaults: Arming Sword (Steel/Iron/Gold), Dagger (Steel), Waraxe, Mace, Mage Wand, Recurve Bow, Spartan Shield, Crusader Shield
3. Keep `appearance_options` in `sprite-catalog.json` (hair, eyes, face, skin, body)
4. Copy corresponding weapon/shield sprite PNGs from the reference repo into `assets/sprites/full/`

**LiteDB migration:**
- `SpriteSeeder` replaced by `SheetDefinitionSeeder` + simplified `AppearanceSeeder`
- On startup, `sheet_definitions` collection cleared and reseeded (idempotent)
- Old `equipment_sprites` collection dropped
- Existing character appearance data references display names ("Arming Sword (Steel)") which resolve against new `sheet_definitions` collection — saved characters work without data wipe

**Code removal:**
- `EquipmentSprite` model
- `StitchLayer` model
- `GodotSpriteCompositor` (replaced by Core compositor or thin wrapper)
- `SpriteSeeder` (replaced by `SheetDefinitionSeeder`)
- bg/fg path hack in `SpriteLayerCatalog`
- Male mage robes hardcoded redirect

### Testing

**Unit tests for `SkiaSharpSpriteCompositor`:**
- Correct sheet dimensions (832x3456)
- Layer z-ordering per animation (weapon behind body in walk, in front during slash)
- Oversize frame generation (192x192 centered correctly)
- Tinting applied correctly
- Flip for off-hand items
- Fallback chain when assets missing

**Unit tests for `SheetDefinitionCatalog`:**
- All class defaults resolve to valid definitions
- Multi-layer weapons return correct layer count
- Body type path resolution
- Gender fallback behavior

**Unit tests for `LpcAnimationHelper`:**
- Frame rect calculation for every animation type
- Oversize rect calculation
- Frame counts match constants

**Integration tests:**
- Full pipeline: load definitions -> composite -> verify output PNG dimensions and non-empty pixels at expected weapon positions
- Seeder: load sheet definition JSONs -> verify LiteDB population

**Visual validation (manual):**
- Generate sheets for each class default and visually inspect weapon positioning across all animation rows
- Automated tests verify structure; visual inspection verifies aesthetics

**Existing tests:**
- `SpriteLayerCatalogTests` -> rewritten for `SheetDefinitionCatalog`
- `SpriteCompositorTests` -> rewritten for `SkiaSharpSpriteCompositor`
- All other tests unaffected and must still pass
