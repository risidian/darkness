# Senior QA Audit: Darkness (RPG Mechanics & Design)
**Status: UNSTABLE & UNBALANCED**

## 1. Combat Math: The "Defense Cliff"
The damage formula `(totalAttack * 2) - defender.Defense` is fundamentally flawed for a balanced RPG.
- **Problem:** Damage scales linearly with attack but is subtracted by defense. This creates a "defense cliff" where an enemy with slightly higher defense than your attack power takes ZERO damage, while an enemy with slightly lower defense takes massive damage. 
- **Risk:** This makes balancing impossible. A single level-up or equipment change could turn a "boss" into a "minor nuisance" or vice versa.
- **Fix:** Use a ratio-based or diminishing returns formula (e.g., `Attack * (Attack / (Attack + Defense))`).

## 2. Input Spamming & State Transitions
`BattleScene.Update` checks `Keyboard.GetState().IsKeyDown(Keys.D1)`.
- **The Bug:** If a player holds down '1', the attack logic will trigger *every frame* until the turn order advances. While the current turn logic resets `_isPlayerTurn`, there is no debouncing or "key pressed" logic.
- **Race Condition:** If `BattleEnded` is invoked and the scene doesn't swap *immediately*, multiple "BattleEnded" events could be fired from a single key press.
- **Fix:** Implement an input manager that tracks `JustPressed` states, not just `IsDown`.

## 3. Enemy "Turn Warp" (AI Logic)
`NextParticipant` calls `DetermineTurn` recursively if it's an enemy's turn. 
- **The Issue:** All enemy turns happen *instantly* in one frame. The player has no time to see what damage was dealt or what actions were taken.
- **QA Concern:** This makes it impossible to track battle flow. If three enemies attack, the log just updates to the last one's message instantly.
- **Fix:** Implement a turn delay or animation state machine to allow players to process battle events.

## 4. Hardcoded & Magic Data
- **Enemy Stats:** Hardcoded directly in `StoryController.cs`. No ability to tweak balancing without a full recompilation.
- **Screen Layout:** Hardcoded `Vector2` positions (e.g., `50, 400`) assume a specific aspect ratio. 
- **The Result:** On any screen other than the developer's, the UI will likely be misaligned, overlapping, or cut off. 

## 5. Missing Mechanics & Edge Cases
- **Luck/Crit:** Hardcoded to 5%. Stats like "Luck" exist in models but are ignored in the engine.
- **Invincibility:** `IsInvincible` is a binary flag. If a player gets stuck in a battle with an invincible enemy (like the Dark Warrior in Beat 4) and there's no "survival turn" logic, the game is soft-locked.
- **Status Effects:** Application is purely random vs. wisdom, ignoring the power of the source. This leads to frustrating RNG "stuns" that feel unfair.
