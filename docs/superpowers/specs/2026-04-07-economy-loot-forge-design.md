# Design Doc: Economy, Loot, and Forge Systems

## 1. Overview
This design expands the "Darkness: Reforged" core gameplay loop by implementing a functional economy (Gold), a data-driven loot system (Enemy Drops), an item hotbar for tactical combat, and a comprehensive Forge for crafting and upgrading gear. It also streamlines the Character Generation process to be class-driven.

## 2. Goals
- **Gold & Economy:** Award gold from combat, track it on the character, and allow spending at vendors.
- **Loot System:** Define fixed and random item drops for enemies and story encounters.
- **Combat Hotbar:** Provide a 5-slot tactical bar for quick-use items during combat.
- **The Forge:** Implement Crafting (creating items from materials), Upgrading (+1, +2 gear), and Infusion (adding effects).
- **UI Improvements:** Add character previews to Inventory and streamline Character Generation.

## 3. Data Models & Architecture

### 3.1 Character Model Updates (`Character.cs`)
- `int Gold`: Total currency.
- `string?[] Hotbar`: Fixed-size array (5) storing item IDs/Names mapped to slots.
- `RecalculateDerivedStats()`: Updated to handle weapon/armor stat modifiers.

### 3.2 Enemy & Loot Models
- **`Enemy` / `EnemySpawn`**:
    - `List<string> FixedDrops`: Names of items that always drop.
    - `List<LootEntry> RandomDrops`: Items with a decimal chance (0.0 to 1.0).
- **`LootEntry`**:
    - `string ItemName`
    - `float Chance`

### 3.3 Combat Data & Rewards
- **`CombatData`**: Expanded to include a `List<RewardData> EncounterRewards` for story-specific loot regardless of enemy types.

### 3.4 Forge & Crafting
- **`Recipe`**:
    - `string ResultItemName`
    - `Dictionary<string, int> RequiredMaterials`
    - `int GoldCost`

## 4. UI Components & Interaction

### 4.1 Character Generation (`CharacterGenScene`)
- **Removals:** Weapon and Armor dropdowns.
- **Logic:** Selecting a class updates the internal `Character` appearance object with that class's default gear.
- **Preview:** The `LayeredSprite` updates immediately to show the "Starting Kit" look.

### 4.2 Inventory UI (`InventoryScene`)
- **Gold Counter:** Displayed at top.
- **Character Preview:** Full-sized `LayeredSprite` on the right showing current equipment.
- **Hotbar Management:** "Map to Slot 1-5" button for consumable items.

### 4.3 Battle Hotbar (`BattleScene`)
- **The Bar:** 5 buttons at the bottom/top of the screen.
- **Interaction:** Clicking an item uses it immediately.
- **Turn Logic:** Using an item consumes the player's turn, triggering `ProcessNextTurn()` in the `TurnManager`.

### 4.4 The Forge (`ForgeScene`)
- **Crafting Tab:** List of recipes; consumes materials and gold.
- **Upgrade Tab:** Select an item + materials to increase its suffix (+1, +2) and base stats.
- **Infusion Tab:** Add elemental or status bonuses to gear using rare materials.

### 4.5 Vendors (`VendorScene`)
- Dual-list view (Buy/Sell).
- Prices calculated based on `Item.Value`.

## 5. Implementation Details

### 5.1 Gold Awarding (Fix)
The existing `BattleScene.Victory()` logic will be updated to:
1. Sum `GoldReward` from all enemies.
2. Add sum to `_session.CurrentCharacter.Gold`.
3. Call `_characterService.SaveCharacterAsync()`.

### 5.2 Loot Processing
New logic in `BattleScene` after victory:
1. Roll for `RandomDrops` for each enemy.
2. Collect all `FixedDrops`.
3. Collect `EncounterRewards`.
4. Add all found `Item` objects to `Character.Inventory`.

## 6. Testing Strategy
- **Regression Test:** `AwardExperience_AndGold_OnVictory` ensures both stats are saved correctly.
- **Unit Test:** `LootTable_RollsCorrectItems` verifies random drop percentages.
- **Unit Test:** `Forge_UpgradesItemStats` confirms +1 gear actually increases attack/defense.
- **Integration Test:** `UseItem_ConsumesTurn` ensures turn order advances after hotbar use.
