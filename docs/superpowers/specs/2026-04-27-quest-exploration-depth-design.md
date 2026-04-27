# Design Spec: Quest Exploration & Zone Depth

**Date:** 2026-04-27
**Topic:** Expanding quest structure for intermediate zones, hub-and-spoke exploration, and reward systems.

## 1. Overview
The goal is to move beyond the linear "Dialogue -> Combat" flow. We will introduce "Travel" zones where players can explore, interact with the environment (levers, puzzles), and collect "keys" (items or flags) to progress. This requires enhancements to the Quest System, a new World Flag system, and a Reward system.

## 2. Architecture & Data Models

### 2.1. Quest Rewards (`QuestReward`)
A new model to handle what the player receives upon completing a step or a chain.
- `Type`: "Item", "Experience", "Gold", "WorldFlag", "AttributePoint".
- `Value`: String/Int (e.g., "StoneEmblem", "500", "lever_pulled").

### 2.2. World Flags (`WorldFlag`)
A new LiteDB collection to track non-quest-specific state.
- `Key`: Unique identifier (e.g., `bridge_lowered`).
- `Value`: Boolean or String (usually boolean).

### 2.3. Updated `QuestStep`
Expand `QuestStep` to handle requirements and rewards:
- `Requirements`: List of `BranchCondition` (e.g., must have item X to trigger this step).
- `Rewards`: List of `QuestReward` granted when this step completes.
- `Type` additions: 
    - `"travel"`: An exploration-focused step where the player moves freely.
    - `"interact"`: Triggered by a "Trigger" zone with a specific `ActionId`.

### 2.4. `ZoneConfig` Enhancements
- `RequiredFlag`: Only show/enable this zone if a specific flag is set.
- `ActionId`: Map this to a specific `QuestStep` or a `WorldFlag` toggle.

## 3. Core Logic Changes

### 3.1. `QuestService` Updates
- **`AdvanceStep`**: Now checks `Requirements` before allowing a step to trigger.
- **`GrantRewards`**: Internal method to apply rewards (add items to inventory, set flags) when a step is completed.
- **`EvaluateStepAvailability`**: Checks if a step can be "entered" based on world state.

### 3.2. `WorldScene` Enhancements
- Support for "Non-Blocking" quest steps (Travel).
- Interactive zones that trigger `QuestService.AdvanceStep` with a specific `ActionId`.
- Visual updates to backgrounds/zones based on `WorldFlags` (e.g., if `bridge_lowered` is true, the "Block" zone on the bridge is removed).

## 4. Proposed Approaches

### Approach A: Step-Internal Rewards (Recommended)
Rewards are tied directly to the `QuestStep`. When you finish the combat or dialogue, the rewards are granted.
- **Pros:** Easy to author in JSON; clear correlation between action and reward.
- **Cons:** Less flexible if rewards depend on *how* you finished the step.

### Approach B: Global Reward Catalog
Quest steps point to a `RewardId` in a separate `rewards.json`.
- **Pros:** Reusable rewards; cleaner quest files.
- **Cons:** More files to manage; harder to see the "flow" at a glance.

**Recommendation:** **Approach A** for simplicity and readability, with **Approach B**'s logic used only for "Random Loot Tables" (which already exist in `random-rewards.json`).

## 5. Implementation Phases

1.  **Phase 1: Rewards & Flags.** Define `QuestReward` model and `WorldFlag` service/storage.
2.  **Phase 2: Quest Service Logic.** Update `QuestService` to handle requirements and grant rewards during `AdvanceStep`.
3.  **Phase 3: WorldScene Travel.** Implement the `"travel"` step type and dynamic zone behavior based on flags.
4.  **Phase 4: Content Authoring.** Update existing or create new JSON quest chains to demonstrate the "Hub & Spoke" pattern.

## 6. Testing Strategy
- **Unit Tests:** Verify `QuestService` grants rewards correctly and respects requirements.
- **Integration Tests:** Mock world flags and ensure `QuestStep` availability changes as expected.
- **Manual Testing:** Create a "Debug Quest" in `WorldScene` with a locked gate, a lever in a sub-zone, and a boss that only spawns after the lever is pulled.
