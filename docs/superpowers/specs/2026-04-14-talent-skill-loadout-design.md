# Design: Unified Talent & Skill Loadout System

## Overview
This design unifies skill acquisition and loadout management by integrating class-based starting skills into the talent system and providing a UI for active skill slot configuration.

## Data Changes
- **`assets/data/talent-trees.json`**:
  - Introduce "Starting Nodes" for each class. These nodes will be marked as `AutomaticallyUnlocked: true` and will contain the 3 class-specific starter skills.
- **`Character` Model**:
  - Update to support a fixed-size `ActiveSkillSlots` (array/list of size 5).
  - Persist these selections in LiteDB to maintain state.

## Service Adjustments
- **`CharacterService` / `CharacterGen`**:
  - During character creation, automatically call `TalentService.PurchaseTalent()` (or an equivalent `UnlockStartingTalents()` method) for the chosen class's starting nodes.
- **`WeaponSkillService`**:
  - Refactor to remove hardcoded class-based skill checks.
  - Instead, the service will look at the `Character.ActiveSkillSlots` to determine combat availability.
- **`TalentService`**:
  - Manage the mapping between "Unlocked Talent Nodes" and "Skill Availability."

## UI/UX (Talent Menu)
- **Active Skill Configuration**:
  - Add an "Active Skills" panel to the Talent Menu.
  - Show the 5 available slots.
  - Display all unlocked skills (from talents) in a grid.
  - Users can click an unlocked skill to equip/swap into one of the 5 active slots.

## Data Flow
1. **Startup**: `QuestSeeder` and `SpriteSeeder` remain; `TalentSeeder` handles the new mandatory starter nodes.
2. **Character Creation**: User selects a class -> `CharacterGen` triggers starter talent unlock -> Skills appear in "Available" pool.
3. **Talent Menu**: Player views talent tree, purchases new nodes -> Skill is added to "Available" pool.
4. **Loadout Management**: Player toggles available skills into the 5 active slots.
5. **Combat**: `BattleScene` queries `WeaponSkillService` -> `WeaponSkillService` reads `Character.ActiveSkillSlots` -> combat interface is populated.

## Verification Strategy
- **Unit Tests**:
  - Verify starter talents are correctly granted at creation.
  - Verify swapping skills in active slots persists to LiteDB.
  - Verify `WeaponSkillService` returns skills based on active slots rather than class.
