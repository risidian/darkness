# Design Doc: Dynamic Quest Visuals and Story Expansion

## 1. Overview
This design enables the `WorldScene` and `BattleScene` to dynamically reconfigure their visual appearance (backgrounds, colors, and NPC sprites) based on the current quest step. It also outlines the expansion of the "Revenge" arc from 3 beats to 9 beats.

## 2. Goals
- **Dynamic Environments:** Allow `WorldScene` to change from a shore to a castle or forest without creating new `.tscn` files.
- **Dynamic NPCs:** Support spawning different NPCs at different positions per quest step, with fallback from pre-rendered sprites to LPC-generated characters.
- **Automatic Refresh:** Ensure visuals update immediately when a quest step advances (e.g., after returning from combat).
- **Content Completion:** Build out JSON files for the remaining narrative beats (4-9).

## 3. Architecture

### 3.1 Core Models (`Darkness.Core`)
We will add a `Visuals` field to `QuestStep` to hold configuration for the environment and NPCs.

- **`VisualConfig`**:
    - `string? BackgroundKey`: Resource path or key for a background texture.
    - `string? GroundColor`: Hex color string for the main `ColorRect` fallback.
    - `string? WaterColor`: Hex color string for the `Water` `ColorRect`. If null, water is hidden.
    - `NpcConfig? Npc`: Configuration for the primary NPC in this step.

- **`NpcConfig`**:
    - `string Name`: Display name for dialogue.
    - `string? SpriteKey`: Path to a full sprite sheet (e.g., "bosses/Balgathor").
    - `float PositionX / PositionY`: World coordinates for the NPC.
    - `CharacterAppearance? Appearance`: LPC generation config if `SpriteKey` is absent or fails to load.

### 3.2 Service Updates
- **`IQuestService`**: No functional changes needed to the interface, but the `QuestStep` model it returns will now contain the visual data.

### 3.3 Scene Logic (`Darkness.Godot`)

#### WorldScene Refresh
- Add an `UpdateVisuals(QuestStep step)` method.
- **Backgrounds:** Update `Background` and `Water` `ColorRect` colors. If `BackgroundKey` is provided, load the texture into a new `TextureRect` (or update an existing one).
- **NPCs:** Move the `NPC` node to the specified coordinates. Use `LayeredSprite.SetupFullSheet` if `SpriteKey` is valid, otherwise use `LayeredSprite.SetupCharacter` with the LPC `Appearance`.
- **Trigger:** Call `UpdateVisuals` in `_Ready` and whenever returning to the scene with quest progress.

#### BattleScene Visual Swap
- Update `Initialize` to read `BackgroundKey` from `BattleArgs` or the current `QuestStep`.
- Update the `Background` `ColorRect` or load a background texture.

## 4. Narrative Expansion (Beats 4-9)
JSON files will be created in `assets/data/quests/` for:
- **Beat 4 (The Tavern):** `beat_4_the_tavern.json`
- **Beat 5 (Journey Begins):** `beat_5_journey_begins.json`
- **Beat 6 (The Knight):** `beat_6_the_knight.json`
- **Beat 7 (Araknos Demon):** `beat_7_araknos_demon.json`
- **Beat 8 (Undead Army):** `beat_8_undead_army.json`
- **Beat 9 (Kyarias):** `beat_9_kyarias_final.json`

## 5. Error Handling
- **Fallback Colors:** If a background image is missing, the `GroundColor` ensures the scene isn't just black.
- **LPC Fallback:** If a boss sprite sheet is missing, the LPC generator will create a placeholder character based on the `Appearance` block.

## 6. Testing Strategy
- **Visual Verification:** Manually advance through Beat 1 into Beat 2 and verify the "Old Man" disappears and the "Dark Warrior" / "Castle" visuals appear.
- **JSON Validation:** Ensure all new beat files are correctly parsed by `QuestSeeder`.
