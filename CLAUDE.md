# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Restore all projects
dotnet restore

# Build entire solution
dotnet build Darkness.sln

# Build specific project
dotnet build Darkness.Core/Darkness.Core.csproj

# Run Godot project (requires Godot 4.6.1 with .NET)
dotnet build Darkness.Godot/Darkness.Godot.csproj

# Run WebAPI backend
dotnet run --project Darkness.WebAPI

# Run all tests (165 tests)
dotnet test Darkness.Tests

# Run a single test
dotnet test Darkness.Tests --filter "FullyQualifiedName~TestMethodName"

# Build Android APK
./build-android.ps1
```

## Architecture

This is **Darkness**, a cross-platform RPG built on .NET 10 with four projects:

- **Darkness.Core** — Platform-agnostic business logic layer. Contains models (45), interfaces (23), services (23+), combat engine, and LiteDB persistence. All game logic lives here.
- **Darkness.Godot** — The main game host. A Godot 4.6.1 (.NET) project with 23 scenes, 5 platform services, and all rendering/UI/input.
- **Darkness.WebAPI** — ASP.NET Core 10 backend with EF Core (SQL Server). Currently has a placeholder ValuesController and two EF models (Characters, Users).
- **Darkness.Tests** — XUnit tests with Moq (165 tests across 37 test files). References Darkness.Core only. Five test files excluded (depend on removed MonoGame project).

### Key Patterns

- **DI** configured in `Darkness.Godot/src/Core/Global.cs` using `Microsoft.Extensions.DependencyInjection`. All services are singletons. Ten seeders run synchronously at startup.
- **Interface-driven**: Core defines 23 interfaces; Godot provides 5 platform implementations (`GodotNavigationService`, `GodotFileSystemService`, `GodotDispatcherService`, `GodotDialogService`, `GodotSpriteCompositor`).
- **Data-driven**: Game content (equipment, quests, level thresholds, skills, talent trees, encounters, items, recipes, rewards) defined in JSON seed files, loaded into LiteDB at startup. Adding content requires JSON edits only — no code changes.
- **Local data**: LiteDB via `LiteDB` package. Collections: characters, users, quest_chains, quest_states, skills, talent_trees, items, recipes, equipment_sprites, appearance_options, encounter_tables, sheet_definitions.
- **D20 combat**: `CombatEngine` uses d20 hit rolls against AC, dice-based damage (XdY parser), initiative from DEX modifier, critical hit on natural 20, critical miss on natural 1. Supports Character vs Enemy, Enemy vs Character, and Character vs Character.

### Data Flow

Godot Scenes → Services (in Core) → LiteDB

### Core Interfaces

`ICharacterService`, `IUserService`, `ISessionService`, `ISettingsService`, `ICombatService`, `IQuestService`, `ITriggerService`, `IEncounterService`, `ILevelingService`, `ITalentService`, `IWeaponSkillService`, `ICraftingService`, `IRewardService`, `IDeathmatchService`, `IAllyService`, `INavigationService`, `ISheetDefinitionCatalog`, `ISpriteCompositor`, `IFileSystemService`, `IDispatcherService`, `IDialogService`, `IEquipmentService`, `LocalDatabaseService`

### Data Seeders

10 seeders total: `SheetDefinition`, `Appearance`, `Quest`, `Level`, `Talent`, `Skill`, `Recipe`, `Item`, `Reward`, `Encounter`. All use `DeleteAll()` + re-insert pattern (idempotent). Errors logged but don't crash startup.

### Sprite Composition Pipeline

Character appearance uses an 11+ layer system composited at runtime:

- **`SheetDefinition`** (LiteDB): Maps equipment names to z-order, animations, and variant-based asset paths for each layer.
- **`AppearanceOption`** (LiteDB): Maps hair/skin/face/eye options to asset paths, z-order, and tint hex values.
- **`SheetDefinitionCatalog`**: Resolves `CharacterAppearance` into `SheetDefinition` lists, handling gender-specific paths and tints.
- **`SkiaSharpSpriteCompositor`**: Composites PNG layers into 1536x2112 sprite sheets (6 LPC rows) using SkiaSharp.
- **Layer order**: Body → Head → Face → Eyes → Hair → Armor → Feet → Arms → Legs → Weapon → Shield → OffHand.
- **Seed files**: `assets/data/sheet_definitions/` (per-equipment JSON) and `assets/data/sprite-catalog.json` (AppearanceOptions).

### Navigation System

- **`GodotNavigationService`**: Custom `INavigationService` using `GetTree().ChangeSceneToFile()` with fade transitions and `IInitializable` parameter injection.
- **`BattleArgs`**: Carries `CombatData` + `QuestChainId`/`QuestStepId` for quest-aware combat.
- **`StealthArgs`**: Carries quest context for stealth scenes.

### Quest System

9 quest chain JSON files in `Darkness.Godot/assets/data/quests/` (beat_1 through beat_9), seeded into LiteDB. `QuestChain` contains `QuestStep`s (typed: dialogue, combat, location, branch). `QuestState` in LiteDB tracks per-character progress. `QuestService.AdvanceStep()` drives all progression. `TriggerService` handles location-based triggers. `ConditionEvaluator` supports extensible branch conditions (morality, class, item, quest).

### XP & Leveling

Level thresholds in `Darkness.Godot/assets/data/level-table.json` (20 levels). `LevelingService.AwardExperience()` handles XP award, level-up detection, attribute points (2/level), talent points (1/even level), HP scaling, and HP restore. `LevelUpResult` returned includes `DidLevelUp`, `LevelsGained`, `AttributePointsAwarded`, `TalentPointsAwarded`. BattleScene awards XP on victory, then advances quest.

### Talent System

Talent trees in `Darkness.Godot/assets/data/talent-trees.json`, seeded into LiteDB. `TalentService` manages class-restricted and morality-gated trees with prerequisite nodes, exclusivity groups, and hidden trees. `TalentLayoutHelper` calculates spatial layout. Talents grant stat bonuses or skill unlocks via `TalentEffect`. 1 talent point per even level.

### Skills & Weapons

Skills in `Darkness.Godot/assets/data/skills.json`, seeded into LiteDB. `WeaponSkillService` resolves available skills — prioritizes `ActiveSkillSlots` (player-assigned hotbar), falls back to weapon-type matching. Skills have mana/stamina costs, damage dice, cooldowns, and optional talent requirements. Generates inline skills for weapon types (Wand, Bow, Sword, Shield, Unarmed).

### Random Encounter System

Random world encounters are data-driven and triggered by movement distance.

- **`EncounterTable`**: Maps a `BackgroundKey` to weighted `EncounterEntry`s, with `EncounterChance` (percentage) and `EncounterDistance` (pixels).
- **`EncounterService.RollForEncounter()`**: Tracks distance moved, rolls against area's chance, selects weighted mob on success.
- **"Heartbeat" Transition**: Ghost enemy spawns near player, camera performs 4 zoom pulses before transitioning to `BattleScene`.
- **Seed file**: `assets/data/encounters.json`.

### Scene Flow

SplashScene → LoadUserScene → MainMenuScene (hub) → WorldScene, BattleScene, StealthScene, DeathmatchScene, CharactersScene, CharacterGenScene, InventoryScene, ForgeScene, SkillsScene, TalentTreeScene, StudyScene, AlliesScene, SettingsScene. `CreateUserScene` for new user creation. PauseMenu available in-game.

## Workflow Requirements

- **Always use superpowers skills** for all work. Use brainstorming before creative/design work, TDD for implementation, systematic-debugging for bugs, and writing-plans for multi-step tasks. Never skip the skill workflow.
- **Regression Testing**: Every bug fix MUST include a new regression test in `Darkness.Tests` to prevent recurrence.
- **Continuous Validation**: Run `dotnet test Darkness.Tests` after every code change to ensure no regressions.
- **All tests must pass before committing.** Verify all tests pass before every commit. Do not commit with failing tests. If a pre-existing test fails, investigate and fix it or explicitly flag it to the user — do not ignore it.

## Implementation Notes

- **Touch Input**: `emulate_mouse_from_touch` enabled for Android button compatibility.
- **UI Layering**: Use `ScrollContainer` → `VBoxContainer` hierarchies. Set root `mouse_filter = Ignore` if buttons are unresponsive.
- **Audio Driver**: WASAPI with `enable_input = false` for Windows.
- **Namespaces**: Use `global::Godot.FileAccess` when `System.IO` is in scope. Use `using SystemJson = System.Text.Json` when `LiteDB` is in scope (both have `JsonSerializer`).
- **Camera Centering**: `WorldScene` uses a static `Camera2D` centered at `(640, 360)` (scene root) to maintain static backgrounds while allowing encounter zoom effects.

## Project Conventions

- .NET 10 with `ImplicitUsings`, `Nullable`, and `TreatWarningsAsErrors` enabled.
- App identifier: `com.risidian.darkness`
- Sprite assets in `assets/sprites/full/` (LPC-based character sprites).
- Seed data in `Darkness.Godot/assets/data/` (sprite-catalog.json, skills.json, talent-trees.json, level-table.json, encounters.json, quests/*.json, recipes.json, items.json, random-rewards.json, login-calendar.json, sheet_definitions/*.json).
- Key dependencies: CommunityToolkit.Mvvm 8.4.2, LiteDB 5.0.21, Newtonsoft.Json 13.0.3 (Core); Microsoft.Extensions.DependencyInjection 9.0.2 (Godot); SkiaSharp 3.119.2 (Tests).
- Rendering: `gl_compatibility` mode. Viewport: 1280x720, stretch mode `canvas_items`. Pixel snapping enabled.
