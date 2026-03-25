# Darkness - .NET 10 Modernization & "Revenge" Arc Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a platform-agnostic turn-based RPG using .NET 10 MAUI and MonoGame, implementing the first story arc "Revenge".

**Architecture:** A three-layer project structure: `Darkness.Core` for shared logic and data, `Darkness.MAUI` for the cross-platform UI and shell, and `Darkness.Game` for the MonoGame engine hosted within MAUI.

**Tech Stack:** .NET 10, MAUI, MonoGame, SQLite (sqlite-net-pcl), Newtonsoft.Json.

---

### Task 1: Core Models & SQLite Persistence

**Files:**
- Create: `Darkness.Core/Models/Item.cs`
- Create: `Darkness.Core/Models/Skill.cs`
- Create: `Darkness.Core/Models/Enemy.cs`
- Create: `Darkness.Core/Models/StatusEffect.cs`
- Modify: `Darkness.Core/Models/Character.cs` (add detailed stats)
- Modify: `Darkness.Core/Services/UserService.cs` (update initialization)

- [ ] **Step 1: Add detailed stats to Character model**
Update `Darkness.Core/Models/Character.cs` to include STR, DEX, CON, INT, WIS, CHA and derived stats like CurrentHP, MaxHP, Stamina, etc.

- [ ] **Step 2: Create supporting combat models**
Implement `Item.cs`, `Skill.cs`, `Enemy.cs`, and `StatusEffect.cs` in `Darkness.Core/Models/`.

- [ ] **Step 3: Update SQLite schema in UserService**
Ensure `InitializeAsync` in `UserService.cs` creates tables for all new models.

- [ ] **Step 4: Commit**
`git add Darkness.Core/; git commit -m "feat: core models and persistence schema"`

---

### Task 2: MAUI Identity & Account Flow

**Files:**
- Create: `Darkness.MAUI/Pages/CreateUserPage.xaml`
- Create: `Darkness.MAUI/Pages/CreateUserPage.xaml.cs`
- Create: `Darkness.MAUI/Pages/LoadUserPage.xaml`
- Create: `Darkness.MAUI/Pages/LoadUserPage.xaml.cs`
- Modify: `Darkness.MAUI/AppShell.xaml`

- [ ] **Step 1: Create Account Creation UI**
Port logic from `CreateUsername.axml` to `CreateUserPage.xaml` using MAUI Entry and Button controls.

- [ ] **Step 2: Create Login UI**
Port logic from `LoadUsername.axml` to `LoadUserPage.xaml`.

- [ ] **Step 3: Implement Navigation logic**
Update `AppShell.xaml` to register routes for these pages and handle initial redirection if no user exists.

- [ ] **Step 4: Commit**
`git add Darkness.MAUI/; git commit -m "feat: account creation and login UI"`

---

### Task 3: Simple Character Generation (Tier A)

**Files:**
- Create: `Darkness.MAUI/Pages/CharacterGenPage.xaml`
- Create: `Darkness.MAUI/Pages/CharacterGenPage.xaml.cs`
- Create: `Darkness.Core/Services/CharacterService.cs`

- [ ] **Step 1: Implement CharacterService**
Add logic to create and save a new character tied to a User ID.

- [ ] **Step 2: Create Character Creation UI**
Implement a picker-based UI for Hair, Skin Color, and starting Class (STR/DEX/INT bias).

- [ ] **Step 3: Commit**
`git add Darkness.Core/ Darkness.MAUI/; git commit -m "feat: simple character generation UI"`

---

### Task 4: Combat Engine Logic

**Files:**
- Create: `Darkness.Core/Logic/CombatEngine.cs`
- Create: `Darkness.Core/Interfaces/ICombatService.cs`

- [ ] **Step 1: Implement Turn Order logic**
Write a function that calculates initiative based on DEX + Speed + Random variance.

- [ ] **Step 2: Implement Damage Calculation**
Add logic for Physical Attack (STR) and Magic Attack (INT) checks against Defense/MDef.

- [ ] **Step 3: Implement Status Effect checks**
Add logic for checking resistance vs status application (e.g., Bleed, Stun).

- [ ] **Step 4: Commit**
`git add Darkness.Core/; git commit -m "feat: core combat engine logic"`

---

### Task 5: MonoGame Integration & 2D World View

**Files:**
- Create: `Darkness.MAUI/Views/MonoGameHost.xaml`
- Create: `Darkness.MAUI/Views/MonoGameHost.xaml.cs`
- Create: `Darkness.Game/DarknessGame.cs`
- Create: `Darkness.Game/Scenes/WorldScene.cs`

- [ ] **Step 1: Setup MonoGame View Wrapper**
Create a custom MAUI view that hosts a MonoGame `Game` instance.

- [ ] **Step 2: Implement Belt-Scroller movement**
In `WorldScene.cs`, implement 4-way movement on a scrolling background texture.

- [ ] **Step 3: Commit**
`git add Darkness.MAUI/ Darkness.Game/; git commit -m "feat: monogame integration and world view"`

---

### Task 6: Turn-Based Battle Implementation (Arc 1: 1-3)

**Files:**
- Create: `Darkness.Game/Scenes/BattleScene.cs`
- Modify: `Darkness.MAUI/MauiProgram.cs` (inject combat service)

- [ ] **Step 1: Create Battle Screen UI**
Implement the turn-order bar and the action menu (Attack, Skill, Item, Flee) in the MonoGame view.

- [ ] **Step 2: Implement "Undead Dog" Encounter**
Script the first encounter (3 enemies vs 1 player).

- [ ] **Step 3: Commit**
`git add Darkness.Game/; git commit -m "feat: battle scene and first encounter implementation"`

---

### Task 7: "Revenge" Narrative Scripts (Arc 1: 4-10)

**Files:**
- Create: `Darkness.Core/Logic/StoryController.cs`
- Modify: `Darkness.Game/Scenes/BattleScene.cs`

- [ ] **Step 1: Implement Survival Battle Logic**
Add support for battles that end after a set number of rounds (Dark Warrior fight).

- [ ] **Step 2: Implement Party Member logic**
Add Tywin to the combat scene when he joins (Multi-character support).

- [ ] **Step 3: Implement Arc 1 Final Boss**
Script the Kyarias fight with skeletal/zombie spawns.

- [ ] **Step 4: Commit**
`git add Darkness.Core/ Darkness.Game/; git commit -m "feat: revenge arc narrative scripts and multi-party combat"`

---

### Task 8: Daily Login Bonus System

**Files:**
- Create: `Darkness.Core/Services/RewardService.cs`
- Modify: `Darkness.MAUI/MainPage.xaml`

- [ ] **Step 1: Implement Daily Tracking**
Logic to check the last login date and award an item.

- [ ] **Step 2: Display Reward UI**
Add a popup or section on the MainPage to show the reward received.

- [ ] **Step 3: Commit**
`git add Darkness.Core/ Darkness.MAUI/; git commit -m "feat: daily login bonus system"`
