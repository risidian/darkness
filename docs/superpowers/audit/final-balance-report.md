# Darkness: Reforged - Final Game Balance & Logic Audit

## 1. Executive Summary
The game balance audit reveals a critical disconnect between the **Data Model** and the **Combat Engine**, resulting in unintended "Math Walls" and a high risk of player soft-locks in late-game content. While the character progression system inflates stats rapidly, the combat engine fails to utilize the primary accuracy stat for players, making high-defense enemies disproportionately difficult.

---

## 2. Reconciled Findings

### 2.1 The Accuracy Paradox (Agent 1 vs. Agent 2)
*   **The Conflict:** Agent 1 reported a ~15% drop in hit rate from level 1 to level 20. Agent 2 claimed "Accuracy Decay" was impossible because base accuracy is 80.
*   **Reconciliation:** Agent 2 analyzed the `Character.Accuracy` property, which indeed defaults to `80 + Dex/2`. However, **Agent 1 is correct** regarding actual gameplay. The `CombatEngine.cs` implementation for players passes `0` as the accuracy value to the damage calculator.
*   **Impact:** Players rely solely on their Strength/Intelligence modifier and d20 roll to hit. The `Accuracy` stat (and Dexterity's contribution to it) is effectively a "ghost stat" for the player, while enemies use it correctly. This creates a hidden math wall as enemy Defense scales.

### 2.2 Quest Logic & Soft-locks (Agent 3)
*   **Finding:** Agent 3 identified that `QuestStep` JSON files lack explicit failure paths.
*   **Intersection with "Unwinnable Battles":** In `BattleScene.cs`, a defeat results in the "Continue" and "Main Menu" (OK) buttons being hidden, leaving only the "Retry" button. 
*   **Impact:** If a player reaches a "Math Wall" (e.g., Kyarias at Level 20 with high Defense), and their build or current level makes the hit chance too low to win, they are **soft-locked**. They cannot exit the battle to grind or respec; they can only retry the impossible fight.

### 2.3 Stat Inflation vs. Math Walls
*   **Observation:** Agent 2 noted "Stat Inflation" (2 points per level). 
*   **Intersection:** Because `Accuracy` is ignored, players only gain +1 to hit for every 4 points of Dexterity (via the modifier), whereas the intent was likely +1 per 2 points (via the Accuracy property). 
*   *Correction:* Actually, the modifier is `(Stat-10)/2`, so +1 per 2 points. However, because the base 80 is missing, the player starts with a massive deficit compared to the intended design.

---

## 3. Prioritized Fix Map

| Priority | Category | Action Item | Responsibility |
| :--- | :--- | :--- | :--- |
| **CRITICAL** | **Bug** | Update `CombatEngine.cs` to pass `attacker.Accuracy` for Characters. | Engineering |
| **CRITICAL** | **UX** | Enable the "OK" (Main Menu) button on Defeat in `BattleScene.cs` to prevent soft-locks. | UI/UX |
| **HIGH** | **Balance** | Reduce base Accuracy from 80 to 20-30 once the engine fix is applied (to prevent 95% guaranteed hits). | Design |
| **MEDIUM** | **Feature** | Implement `FailureStepId` in `QuestStep` model and JSON to allow "failing forward" in specific story beats. | Design/Eng |
| **MEDIUM** | **Balance** | Increase late-game Enemy AC scaling to compensate for fixed Player Accuracy. | Design |

---

## 4. Conclusion
The current balance issues are primarily rooted in a code-level omission rather than a systemic design failure. Fixing the `Accuracy` stat usage will immediately resolve the "Math Walls" observed by Agent 1, but must be paired with a reduction in base values to avoid making the game trivial.

*Report synthesized by Gemini CLI Agent.*
