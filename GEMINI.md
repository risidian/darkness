# Darkness: Reforged — Project Guide

## 1. Project Overview
A cross-platform RPG built on **Godot 4.6.1 (.NET 10)** with a platform-agnostic core library.

### Core Projects
- **`Darkness.Core`**: Pure .NET 10 library — models, interfaces, services, ViewModels, combat engine, and LiteDB persistence. All game logic lives here.
- **`Darkness.Godot`**: Main game host. Godot C# project for rendering, UI, and input.
- **`Darkness.WebAPI`**: ASP.NET Core 10 backend with EF Core.
- **`Darkness.Tests`**: XUnit tests with Moq. References Darkness.Core.

### Build & Run
```bash
dotnet restore                                    # Restore all projects
dotnet build Darkness.sln                         # Build entire solution
dotnet build Darkness.Godot/Darkness.Godot.csproj # Build Godot project
dotnet run --project Darkness.WebAPI              # Run WebAPI backend
dotnet test Darkness.Tests                        # Run all tests
dotnet test Darkness.Tests --filter "FullyQualifiedName~TestMethodName"  # Single test
```

## 2. Architecture & Design Patterns

### Dependency Injection (DI)
- Uses `Microsoft.Extensions.DependencyInjection`.
- **Global Autoload (`Global.cs`)**: Initializes `ServiceProvider`, registers all services, and runs data seeders at startup.
- **Service Resolution**: Scenes resolve services via `Global.Services` in `_Ready()`.
- **Key services**: `IQuestService`, `ILevelingService`, `ITriggerService`, `ISpriteLayerCatalog`, `ICombatService`, `ICharacterService`, `ISessionService`, `INavigationService`.

### Data Architecture
The project uses a **data-driven architecture** with JSON seed files and LiteDB runtime storage:

- **Seed files** (human-readable JSON in `assets/data/`) define game content — equipment sprites, quest chains, level thresholds.
- **Seeders** (`SpriteSeeder`, `QuestSeeder`, `LevelSeeder`) load JSON into LiteDB collections at startup.
- **Services** query LiteDB at runtime for all lookups.
- **Adding new content** (equipment, quests, levels) requires only JSON edits — no code changes.

### Navigation System
- **`GodotNavigationService`**: Custom `INavigationService` using `GetTree().ChangeSceneToFile()`.
- **`BattleArgs`**: Carries `CombatData` + `QuestChainId`/`QuestStepId` for quest-aware combat.
- **`StealthArgs`**: Carries quest context for stealth scenes.

## 3. Sprite Composition Pipeline

Character appearance uses an 11+ layer system composited at runtime:

- **`EquipmentSprite`** (LiteDB collection): Maps equipment display names to asset paths, z-order, gender, and tint.
- **`AppearanceOption`** (LiteDB collection): Maps hair/skin/face/eye options to asset paths and tint hex values.
- **`SpriteLayerCatalog`**: Queries LiteDB to resolve `CharacterAppearance` → `List<StitchLayer>`.
- **`GodotSpriteCompositor`**: Composites PNG layers into 1536x2112 sprite sheets using Godot's `Image` class.
- **Seed file**: `assets/data/sprite-catalog.json` — equipment sprites, appearance options, class defaults.

To add new equipment: add an entry to `sprite-catalog.json` with slot, display name, asset path, z-order, and gender.

## 4. Quest System

Quest content is authored as per-chain JSON files and managed via LiteDB at runtime:

- **Quest chain files**: One JSON per chain in `assets/data/quests/` (e.g., `beat_1_the_awakening.json`).
- **`QuestChain`**: Container with id, title, prerequisites, sort order, and a list of `QuestStep`s.
- **`QuestStep`**: Typed step — `"dialogue"`, `"combat"`, `"location"`, or `"branch"`. Each type has its own data field (`Dialogue`, `Combat`, `Location`, `Branch`).
- **`BranchData`**: Explicit branching with `BranchOption`s containing text, next step ID, morality impact, and optional `BranchCondition`s.
- **`BranchCondition`**: Extensible conditions — `"morality"`, `"class"`, `"has_item"`, `"quest_completed"` with operators `>=`, `<=`, `==`, `contains`.
- **`QuestState`** (LiteDB): Per-character, per-chain progress tracking (current step, status, flags).
- **`QuestService.AdvanceStep()`**: Central method for all quest progression.
- **`TriggerService`**: Replaces coordinate-based triggers with data-driven location matching.
- **`ConditionEvaluator`**: Evaluates branch conditions against character state.

To add a new quest: create a JSON file in `assets/data/quests/` following the QuestChain schema.

## 5. XP & Leveling System

- **Level thresholds**: Defined in `assets/data/level-table.json` (20 levels, data-driven).
- **`LevelingService.AwardExperience()`**: Awards XP, checks level-up thresholds, grants attribute points (2 per level), restores HP on level-up.
- **`LevelUpResult`**: Returned after XP award — includes `DidLevelUp`, `LevelsGained`, `AttributePointsAwarded`.
- **Combat flow**: `BattleScene.Victory()` → sums `Enemy.ExperienceReward` → `LevelingService.AwardExperience()` → `QuestService.AdvanceStep()` → navigate to WorldScene.

## 6. Game Flow & Scenes

1. **`SplashScene`**: Initialization & database health check.
2. **`LoadUserScene`**: Login and user selection.
3. **`CreateUserScene`**: User creation.
4. **`CharactersScene`**: Character list with thumbnails.
5. **`MainMenuScene` (Hub)**: Central hub — Story, Deathmatch, PVP, Forge, Study, Allies, Settings.
6. **`WorldScene`**: World exploration with `TriggerService`-driven quest triggers. Handles dialogue, branch choices, and scene transitions.
7. **`BattleScene`**: Turn-based combat with XP awards and quest advancement on victory.
8. **`StealthScene`**: Timing-bar stealth minigame with quest context pass-through.

## 7. Build & Deployment

### Android (APK)
- **Tool**: `build-android.ps1` in the root.
- Auto-builds C# project, headless Godot export to `bin/Darkness.apk`.
- Auto-increments `version/code` in `export_presets.cfg`.
- **Requires**: JDK 17+ configured in Godot Editor Settings.

## 8. Implementation Notes
- **Touch Input**: `emulate_mouse_from_touch` enabled for Android button compatibility.
- **UI Layering**: Use `ScrollContainer` → `VBoxContainer` hierarchies. Set root `mouse_filter = Ignore` if buttons are unresponsive.
- **Audio Driver**: WASAPI with `enable_input = false` for Windows.
- **Namespaces**: Use `global::Godot.FileAccess` when `System.IO` is in scope. Use `using SystemJson = System.Text.Json` when `LiteDB` is in scope (both have `JsonSerializer`).
- **Data seeding**: All seeders use `DeleteAll()` + re-insert pattern (idempotent). Errors are logged but don't crash startup.

## 9. Workflow Requirements

- **Always use superpowers skills** for all work. Use brainstorming before creative/design work, TDD for implementation, systematic-debugging for bugs, and writing-plans for multi-step tasks. Never skip the skill workflow.
- **All tests must pass before committing.** Run `dotnet test Darkness.Tests` and verify all tests pass before every commit. Do not commit with failing tests. If a pre-existing test fails, investigate and fix it or explicitly flag it to the user — do not ignore it.

## 10. Project Conventions
- .NET 10 with `ImplicitUsings` and `Nullable` enabled.
- App identifier: `com.risidian.darkness`
- Sprite assets in `assets/sprites/full/` (LPC-based character sprites).
- Key dependencies: CommunityToolkit.Mvvm 8.4.2, LiteDB 5.0.21.
