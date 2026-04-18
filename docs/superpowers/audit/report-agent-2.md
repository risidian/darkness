# Darkness: Reforged - Growth & Economy Auditor Findings (Agent 2)

## 1. Growth & Scaling Analysis

### Player Accuracy vs. Enemy AC ('Accuracy Decay')
*   **Accuracy Formula:** `Accuracy = 80 + Dexterity / 2 + Bonuses`.
*   **Enemy AC (Target AC) Formula:** `Target AC = 10 + ArmorClass + GetModifier(Dexterity)`.
*   **Early Game (Level 1):**
    *   Player (Knight): ~10 DEX -> 85 Accuracy.
    *   Enemy (Hell Hound Alpha): 4 Defense, 12 Speed (DEX) -> Target AC = 10 + 4 + 1 = 15.
    *   Hit Chance (d20 + Modifier + Accuracy >= AC):
        *   Modifier = (10-10)/2 = 0.
        *   d20 + 0 + 85 >= 15 -> d20 >= -70. (95% hit, cap is 1-20).
*   **Late Game (Level 15 - Kyarias Final):**
    *   Enemy (Kyarias): 20 Defense, 35 Speed (DEX) -> Target AC = 10 + 20 + 12 = 42.
    *   Player (Level 15, DEX build): ~40 DEX (Base 10 + 28 from level-up + talents) -> 100 Accuracy.
    *   Hit Chance:
        *   Modifier = (40-10)/2 = 15.
        *   d20 + 15 + 100 >= 42 -> d20 >= -73. (Still 95% hit).
*   **Analysis:** 'Accuracy Decay' does **not** occur in the current implementation. In fact, the player's base Accuracy (80) is so high relative to enemy AC scaling that the player almost always hits (95% chance due to d20+Accuracy vs AC logic) unless the enemy has massive AC (like Balgathor's 100). The `Accuracy` stat effectively makes the d20 roll redundant for hitting standard enemies.

### Build Viability (Strength-only vs Magic-only)
*   **Strength-only:**
    *   Primary Stat: Strength.
    *   Accuracy: Remains ~85-90 (limited DEX).
    *   Against Kyarias (AC 42): d20 + 0 + 85 >= 42 -> 95% hit.
    *   Damage: Scales well with Strength.
    *   Viability: **Highly Viable.** Heavy armor and high HP from Strength-adjacent stats (Constitution) make this very safe.
*   **Magic-only:**
    *   Primary Stat: Intelligence/Wisdom.
    *   Accuracy: Remains ~85-90 (limited DEX).
    *   Against Kyarias (AC 42): 95% hit.
    *   Damage: Scales with Intelligence.
    *   Viability: **Viable but Fragile.** Mana costs (Fireball: 1.5x damage, -10 accuracy) are manageable with Wisdom. Accuracy penalty on spells is negligible due to the high base Accuracy.

## 2. Key Audit Findings

| Audit Point | Status | Observation |
|-------------|--------|-------------|
| **Accuracy Decay** | Not Present | Base Accuracy (80) is too high; d20 rolls are irrelevant for hits. |
| **Enemy AC Scaling**| Low | Enemies gain AC slower than players gain stats. |
| **Strength Build** | Strong | High damage, high survivability, near-perfect hit rate. |
| **Magic Build** | Viable | High burst, accuracy penalties don't matter due to high base. |
| **Stat Overload** | Critical | Players receive 2 attribute points per level, potentially reaching 40+ in a single stat by level 15. |

## 3. Recommended Adjustments
1.  **Rebalance Accuracy:** Reduce base Accuracy from 80 to 0 or 10. The current 80 + Dex/2 formula guarantees a hit on almost everything.
2.  **Enemy AC Buff:** Increase enemy Defense/Dexterity scaling in later beats (7-9) to provide a challenge to high-level players.
3.  **Attribute Point Inflation:** Consider reducing attribute points per level or increasing the cost of higher stat tiers to prevent "god-tier" builds by mid-game.

---
*Audit performed by Gemini CLI Agent 2.*
