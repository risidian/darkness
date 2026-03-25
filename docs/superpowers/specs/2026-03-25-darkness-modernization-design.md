# Design Spec: Darkness - .NET 10 Modernization & "Revenge" Arc

## 1. Overview
Modernize the legacy `Darkness` project into a platform-agnostic, cross-platform RPG ecosystem using **.NET 10**, **MAUI**, and **MonoGame**. This spec covers the transformation of the legacy codebase and the implementation of the first story arc, **"Revenge"**.

## 2. Technical Architecture

### 2.1 Project Structure
The solution will be split into three primary layers to ensure a clean separation of concerns and platform independence:

1.  **`Darkness.Core` (.NET 10 Class Library):**
    *   **The Brain:** Houses all shared business logic, game systems, and data models.
    *   **Systems:**
        *   Combat Engine (Speed-based turn order calculation, damage formulas, status checks).
        *   Leveling & XP System (Stat growth based on STR, DEX, CON, INT, WIS, CHA).
        *   `LocalDatabaseService`: SQLite-based persistence for accounts, characters, and inventory.
        *   `GameSession`: Tracks current logged-in user and active character state.
    *   **Data Models:** `User`, `Character`, `Item`, `Skill`, `Enemy`, `StatusEffect`.

2.  **`Darkness.MAUI` (.NET 10 MAUI App):**
    *   **The Shell:** Multi-targeted for **Android**, **iOS**, and **Windows**.
    *   **UI (XAML):** Account Creation, Character Selection, Inventory, Smith/Crafting, Settings, and Daily Login Bonus.
    *   **Platform Abstractions:** `IFileSystemService` for cross-platform file access.
    *   **Navigation:** Uses `AppShell` for seamless transitions between MAUI menus and the MonoGame world view.

3.  **`Darkness.Game` (MonoGame):**
    *   **The Engine:** A MonoGame view hosted within a MAUI `ContentPage`.
    *   **Features:**
        *   2D Belt-Scroller View for horizontal/vertical world exploration.
        *   Turn-Based Combat Screen with character/enemy animations.
        *   Resource Management: Loads assets from the `Darkness.MAUI` shared resources.

4.  **`Darkness.WebAPI` (.NET 10 Web API):**
    *   **The Hub:** A modernized backend using **EF Core 10**.
    *   **Role:** Currently provides basic user data synchronization; expansion path for future PvP/Co-op servers.

## 3. Data & RPG Systems

### 3.1 Character Stats
*   **Core (Input):** Strength (STR), Dexterity (DEX), Constitution (CON), Intelligence (INT), Wisdom (WIS), Charisma (CHA).
*   **Derived (Combat):** HP (CON), MP (WIS), Stamina (Endurance/CON), Speed (DEX), Accuracy, Evasion, Defense, Magic Defense.

### 3.2 Status & Resistances
*   **Resistances:** Fire, Ice, Lightning, Holy, Dark.
*   **Status Effects:** Poisoned, Bleeding, Stunned, Burning, Frozen, Fear.

### 3.3 Character Customization (LPC Tiered Approach)
*   **Tier A (Initial):** Simplified picker for Hair, Hair Color, Skin Color, Armor, and Weapon.
*   **Tier B (Expansion):** Advanced customization including Beards, Eyes, Bracers, Shoulders, Boots, and all LPC layers.

## 4. Gameplay Flow: "Revenge" Arc 1

### 4.1 Narrative Beats
1.  **Shore of Camelot:** Initial movement tutorial (Belt-scroller exploration).
2.  **The Tavern:** First interaction with NPC and hub tutorial.
3.  **First Combat:** Darius vs 3 Undead Dogs (Turn-based combat introduction).
4.  **The Dark Warrior:** Survival encounter (5 rounds) near the cliff face.
5.  **The Sorcerer:** Narrative boss fight and lore introduction.
6.  **Journey Begins:** Travel across the map encountering Goblins (Scale multi-target combat).
7.  **The Knight:** Meeting and fighting Tywin, who then joins the party (Multi-character combat).
8.  **Araknos Demon:** First joint-character boss battle.
9.  **Undead Army:** Final confrontation vs Kyarias and skeletal/zombie minions.

## 5. Technical Implementation Details

### 5.1 Local Database
*   **File:** `Darkness.db3` (SQLite).
*   **Tables:** `Users`, `Characters`, `Items`, `Skills`.
*   **Logic:** Asynchronous data access through `IUserService`.

### 5.2 Daily Login Bonus
*   **Logic:** Tracks the last login timestamp in `Darkness.db3`.
*   **Rewards:** Items (Health/Mana Pots, materials) added directly to character inventory.

## 6. Future Roadmap
1.  **Advanced Customization:** Enable all LPC sub-layers in the generator UI.
2.  **Open World Expansion:** Transition from Belt-Scroller to a tiled, free-roam open world.
3.  **PvP/Co-op Integration:** Expand `Darkness.WebAPI` to host real-time or asynchronous multiplayer sessions.

## 7. Tone & Aesthetic
*   **Style:** Dark Fantasy Anime.
*   **UI:** High-contrast, sharp lines, dark themes with dramatic combat animations.
*   **Assets:** Utilizing Universal LPC Sprite Sheet Character Generator assets with appropriate attribution (CC BY-SA 3.0).
