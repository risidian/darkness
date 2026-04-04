# Design Spec: RuneScape-Style Inventory & Click Targeting

## 1. Overview
This spec details the transition of the inventory UI to a character-centric layout and the implementation of a modern click-to-target system in the battle scene.

## 2. RuneScape-Style Inventory Layout

### 2.1 UI Structure
- **Layout**: The `InventoryScene` will be updated from a single column to a 2-column layout using an `HSplitContainer` or side-by-side `PanelContainers`.
- **Character Panel (Left)**: 
    - A dedicated area for a `LayeredSprite` showing the player's current character.
    - Animation: `idle_down` or `idle_right`.
    - Real-time updates: As items are equipped/unequipped, the sprite will re-stitch and update its appearance.
- **Inventory Panel (Right)**:
    - The existing `ScrollContainer` and `ItemList`.
    - Displays all items in the character's backpack.

### 2.2 Interaction Logic
- **Equipping**: Tapping an item in the list will equip it.
- **Visual Sync**: After a successful equip, the character panel will refresh the `LayeredSprite` to reflect the new gear.

## 3. Battle Targeting System

### 3.1 Selection Logic
- **Interactive Enemies**: Each enemy wrapper in `BattleScene` will be updated to detect mouse/touch input.
- **Target Tracking**: The `BattleScene` will maintain a `_selectedEnemyIndex`.
- **Default State**: The first living enemy will be selected by default when the battle starts.

### 3.2 Visual Feedback (Target Highlighting)
- **StatusBar Update**: The `StatusBar` script will add a `SetHighlighted(bool highlighted)` method.
- **Highlight Effect**: When highlighted, the `NameLabel` will use a **Blue Outline** (`font_outline_color`) and a slightly increased `outline_size`.
- **Selection Change**: When a new enemy is tapped, the previous highlight is cleared and the new target is highlighted.

### 3.3 Skill Execution
- **Button Refactor**: Attack buttons will no longer be labeled "Attack 1/2/3" (targeting enemies). They will be renamed to "Strike", "Skill", "Item" or similar.
- **Execution**: Clicking an action button will perform that action on the **currently selected target**.

## 4. Technical Changes
- **`BattleScene.cs`**: Add targeting logic and input handling.
- **`StatusBar.cs`**: Add highlight visual states.
- **`InventoryScene.tscn`**: Redesign the layout to include the character preview.
- **`InventoryScene.cs`**: Wire up the `LayeredSprite` and sync appearance changes.
