# Design: Game Balance QA Audit

## 1. Goal
Identify the root causes of "unwinnable battles" and other game-breaking balance/logic bugs in the `Darkness.Core` engine using a tri-lens sub-agent audit and cross-critique.

## 2. Audit Roles (Sub-Agents)

### Agent 1: The Combat Math Specialist
*   **Focus Area:** `CombatEngine.cs`, `Enemy.cs`, `Skill.cs`, and `assets/data/skills.json`.
*   **Audit Task:** Perform a statistical audit of Hit Chances (D20 + Modifiers vs AC) and Damage-to-HP ratios.
*   **Objective:** Identify "Math Walls" where an enemy's Armor Class or Health Pool grows significantly faster than a player's maximum possible output.
*   **Tooling:** Will create a simulation script (`Darkness.Tests/Audit/CombatSim.cs`) to run 1,000 virtual rounds of Level X vs Enemy X.

### Agent 2: The Growth & Economy Auditor
*   **Focus Area:** `LevelingService.cs`, `TalentService.cs`, `Character.cs`, and `assets/data/level-table.json`, `talent-trees.json`.
*   **Audit Task:** Evaluate the attribute and talent point distribution across the Level 1-20 curve.
*   **Objective:** Check if "non-optimal" player builds (e.g., misallocated attribute points) lead to a mathematically impossible state. Check for "Accuracy Decay" where stat gains don't keep pace with enemy AC.
*   **Tooling:** Will analyze the JSON data-driven scaling and compare it against the Combat Specialist's "Math Wall" findings.

### Agent 3: The Logic & Integrity Guard
*   **Focus Area:** `QuestService.cs`, `ConditionEvaluator.cs`, `LocalDatabaseService.cs`, and `assets/data/quests/*.json`.
*   **Audit Task:** Audit the Quest/Dialogue branch logic and database state integrity.
*   **Objective:** Prevent "State Soft-locks." Ensure every quest step is reachable and has a valid exit path. Check for database race conditions in `LiteDB` usage during high-frequency updates.
*   **Tooling:** Will programmatically scan all quest JSON files for dead-end branches and unreachable dialogue steps.

## 3. The Cross-Critique Workflow

1.  **Phase 1: Independent Discovery:** Each agent performs a deep research dive and identifies 3-5 high-priority "Balance Risks."
2.  **Phase 2: The Critique Loop:**
    *   Agent 1 reviews Agent 2's Growth findings to see if Talent buffs mitigate "Math Walls."
    *   Agent 2 reviews Agent 3's Quest findings to see if "Choice Locks" prevent players from reaching XP-rich areas.
    *   Agent 3 reviews Agent 1's Combat findings to see if a "Combat Loss" correctly triggers a retry or quest-branch (to prevent soft-locks).
3.  **Phase 3: Synthesis:** Findings are consolidated into a "Balance Heatmap" for the final report.

## 4. Success Criteria
*   **Empirical Evidence:** Identification of specific level/enemy pairings where win probability drops below 15%.
*   **Reachability Map:** Confirmation that all 9 quest chains can be completed from start to finish.
*   **Scaling Validation:** Verification that "non-optimal" characters can still progress through the main story.
