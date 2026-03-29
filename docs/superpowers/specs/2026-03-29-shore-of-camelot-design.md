# Design Spec: Shore of Camelot (Story Beat 1)

## 1. Overview
The "Shore of Camelot" is the first playable area in the "Revenge" story arc. It serves as a movement and interaction tutorial for the player before they encounter their first combat.

## 2. Scene Architecture

### 2.1 Component Structure
- **Playable Area:** A fixed 2D zone (e.g., 1280x720) representing the shoreline.
- **Boundaries:** 
  - **North:** Cliff face (Impassable).
  - **South:** Ocean/Water (Impassable).
  - **West:** Shoreline continuation (Impassable for tutorial).
  - **East:** Exit trigger (Triggers Story Beat 3 - First Combat).
- **Entities:**
  - **Player:** Loaded from the active session character.
  - **NPC (Old Man):** Located near the starting point to provide lore/instructions.

### 2.2 Interaction System
- **NPC Dialogue:** Triggered by proximity + Action Key (Space/Enter).
- **UI:** A floating or bottom-docked dialogue box with typewriter-style text.
- **Transition:** Reaching the East boundary triggers the "Heartbeat" pulse animation (implemented in `WorldScene`) and transitions the game state to `BattleScene` with Beat 3 enemies.

## 3. Technical Implementation

### 3.1 Data Flow
1. **DarknessGame** loads `WorldScene` at startup or story start.
2. **WorldScene** initializes boundaries and entities.
3. **Update Loop:**
   - Handle player input (WASD/Arrows).
   - Resolve collisions against boundaries.
   - Check proximity to NPC.
   - Check if Player rect intersects East boundary.
4. **Draw Loop:**
   - Render floor/background layers.
   - Render NPC and Player.
   - Render active UI (Dialogue/HUD).

### 3.2 Key Classes & Files
- `Darkness.Game/Scenes/WorldScene.cs`: Main scene logic.
- `Darkness.Core/Interfaces/ISessionService.cs`: Updated to include `CurrentCharacter`.
- `Darkness.Game/Entities/WorldEntity.cs`: (Optional) Base class for NPCs/Players in world.

## 4. Visual Style
- **Aesthetic:** High-contrast dark fantasy (matching the modernized design spec).
- **Assets:** Use LPC body/armor sprites for player and NPC. Placeholder colored rects for boundaries until tile assets are added.

## 5. Success Criteria
- Player can move in all 4 directions within the shore boundaries.
- Player can interact with the NPC to see a dialogue box.
- Moving to the east edge triggers the heartbeat pulse and starts the battle against 3 Undead Dogs.
