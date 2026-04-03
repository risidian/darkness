# Design Doc: Unified Quest & Navigation System

## 1. Overview
This design standardizes how the game transitions between exploration (`WorldScene`), combat (`BattleScene`), and UI menus. It replaces hardcoded scene logic with a data-driven **Quest Graph** and a type-safe **Navigation System** to support branching paths and side-quests.

## 2. Goals
- **Branching Quests:** Support Main Story and Side Quests with prerequisites.
- **Zero Duplication:** Centralize encounter setup so `BattleScene` doesn't need to know *why* it's starting.
- **Expandability:** Add new content by adding data nodes rather than modifying scene code.
- **Type Safety:** Replace string-based dictionaries in navigation with strongly-typed parameters.

## 3. Architecture

### 3.1 Core Models (`Darkness.Core`)
- **`QuestNode`**: Represents a single story beat or side quest.
    - `string Id`: Unique identifier.
    - `string Title`: Display name.
    - `bool IsMainStory`: Flag for UI filtering.
    - `List<string> Prerequisites`: List of `QuestNode` IDs required to unlock this.
    - `EncounterData Encounter`: Details for the battle/event.
- **`EncounterData`**:
    - `List<Enemy> Enemies`: The combatants.
    - `int? SurvivalTurns`: For "survive for X turns" objectives.
    - `string BackgroundPath`: Specific arena for the battle.
    - `List<Item> Rewards`: Loot dropped on victory.

### 3.2 Services
- **`IQuestService` (New)**: 
    - `List<QuestNode> GetAvailableQuests(Character character)`: Filtered list based on progress.
    - `QuestNode GetQuestById(string id)`: Fetch specific data.
    - `void CompleteQuest(Character character, string questId)`: Updates character progress in the DB.
- **`INavigationService` (Update)**:
    - Updated to accept a generic parameter: `Task NavigateToAsync<T>(string route, T parameters) where T : NavigationArgs`.

### 3.3 Godot Implementation (`Darkness.Godot`)
- **`Routes.cs`**: A static class containing all scene path constants (e.g., `Routes.World`, `Routes.Battle`).
- **`BaseScene<T>`**: An abstract base class for scenes that implements `IInitializable` with type-safe arguments.

## 4. Data Flow
1. **WorldScene**: Player hits a trigger (e.g., enters a cave).
2. **Trigger Logic**: Calls `QuestService.GetEncounterForLocation("CaveEntry")`.
3. **Navigation**: `NavigationService.NavigateToAsync(Routes.Battle, new BattleArgs { Encounter = data })`.
4. **BattleScene**: Receives `BattleArgs` via `Initialize()`, sets up sprites/HP bars automatically.
5. **Completion**: Battle finishes, `QuestService.CompleteQuest()` is called, and player returns to `WorldScene`.

## 5. Error Handling
- **Missing Scene**: `NavigationService` will alert and return to `MainMenu` if a route is invalid.
- **Invalid Data**: `BattleScene` will fall back to a "Training" encounter if no data is passed, preventing crashes.

## 6. Testing Strategy
- **Unit Tests (`Darkness.Tests`)**: 
    - Verify `QuestService` correctly filters nodes based on prerequisites.
    - Verify `CombatEngine` correctly processes `EncounterData`.
- **Integration Tests**: 
    - Mock `INavigationService` to ensure `WorldScene` passes correct parameters on trigger.
