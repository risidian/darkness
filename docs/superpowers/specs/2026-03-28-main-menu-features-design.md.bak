# Design Spec: Darkness - Main Menu Features

## 1. Overview
This document outlines the design for the main menu features of the Darkness game, as brainstormed and agreed upon. It details the implementation of several new game modes and screens, their underlying models, and the proposed order of implementation.

## 2. Technical Architecture

### 2.1 Main Menu Structure
The main menu will provide access to the following features, categorized by their implementation type:

#### 2.1.1 MVVM Views
These screens will be implemented as standard MAUI pages with ViewModels in the `Darkness.Core` project.

-   **Characters:** Allows players to view their created characters, including detailed stats, equipment, and inventory.
-   **Forge:** A crafting screen where players can create new items and upgrade existing ones using materials gathered from story mode, deathmatch, and daily logins.
-   **Settings:** Standard game settings, including audio controls (master, music, SFX), graphics options (resolution, display mode), and keybindings.
-   **Study:** The character progression screen. Players can allocate attribute points gained from leveling up and upgrade class-specific abilities.
-   **Allies:** A friend list for managing social connections. Players can view their friends, accept/decline friend requests, and initiate challenges.

#### 2.1.2 MonoGame Scenes
These game modes will be implemented as MonoGame scenes within the `Darkness.Game` project.

-   **Storyline:** The main narrative mode of the game, following the "Revenge" arc.
-   **PVP (Local Hot-seat):** A local multiplayer mode where two players can fight against each other on the same device.
-   **Deathmatch:** A single-player mode where players can fight against level-relevant bosses to earn materials for crafting and upgrades.

### 2.2 Data Models

#### 2.2.1 New Models
-   **`Ally`:** Represents a friend in the user's ally list.
    -   `AllyUserID` (int): The unique identifier of the allied user.
    -   `AllyUsername` (string): The username of the allied user.
    -   `Status` (string): The status of the friendship (e.g., "Pending", "Accepted").

-   **`DeathmatchEncounter`:** Defines a deathmatch battle.
    -   `EncounterID` (int): The unique identifier for the encounter.
    -   `RequiredPlayerLevel` (int): The minimum level a player must be to attempt the encounter.
    -   `Bosses` (List<Enemy>): A list of enemies that will be in the encounter.
    -   `PotentialRewards` (List<Item>): A list of possible items that can be rewarded upon completion.

#### 2.2.2 Existing Models
-   **`Enemy`:** Reused for bosses in Deathmatch mode.
-   **`Item`:** Reused for material rewards in Deathmatch mode.

## 3. Implementation Plan

### 3.1 Implementation Order
The features will be implemented in the following order to prioritize the core single-player experience:

1.  **Characters Screen (MVVM):** Essential for viewing player progress.
2.  **Study Screen (MVVM):** Core of the character progression system.
3.  **Forge Screen (MVVM):** Provides a use for collected materials.
4.  **Deathmatch Scene:** Adds a repeatable content loop for gathering materials.
5.  **Allies Screen (MVVM):** Introduces social features.
6.  **PVP (Local Hot-seat) Scene:** A basic implementation of player-vs-player combat.
7.  **Settings Screen (MVVM):** Important but not critical for the initial gameplay loop.

### 3.2 Future Roadmap
-   **Asynchronous PVP:** The local hot-seat PVP will be a stepping stone towards a more advanced asynchronous PVP system, which will be implemented in a future update. This will involve significant backend development.
-   **Siege & Training Mode:** These game modes are currently on the back burner and will be considered for future implementation.
