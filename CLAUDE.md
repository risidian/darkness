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

- **Darkness.Core** — Platform-agnostic business logic layer. Contains models (39+), services (21+), combat engine, and LiteDB persistence. All game logic lives here.
- **Darkness.Godot** — The main game host. A Godot 4.6.1 (.NET) project with 23 scenes, 5 platform services, and all rendering/UI/input.
- **Darkness.WebAPI** — ASP.NET Core 10 backend with EF Core (SQL Server). Currently has a placeholder ValuesController and two EF models (Characters, Users).
- **Darkness.Tests** — XUnit tests with Moq (165 tests). References Darkness.Core only. Five test files excluded (depend on removed MonoGame project).

### Key Patterns

- **DI** configured in `Darkness.Godot/src/Core/Global.cs`. All services are singletons. Five seeders run synchronously at startup.
- **Interface-driven**: Core defines 21 interfaces; Godot provides 5 platform implementations (`GodotNavigationService`, `GodotFileSystemService`, `GodotDispatcherService`, `GodotDialogService`, `GodotSpriteCompositor`).
- **Data-driven**: Game content (equipment, quests, level thresholds, skills, talent trees) defined in JSON seed files, loaded into LiteDB at startup. Adding content requires JSON edits only — no code changes.
- **Local data**: LiteDB via `LiteDB` package. Collections: characters, users, quest_chains, quest_states, skills, talent_trees, items, recipes, equipment_sprites, appearance_options.
- **D20 combat**: `CombatEngine` uses d20 hit rolls against AC, dice-based damage (XdY parser), initiative from DEX modifier, critical hits/misses.

### Data Flow

Godot Scenes → Services (in Core) → LiteDB

### Core Interfaces

`ICharacterService`, `IUserService`, `ISessionService`, `ISettingsService`, `ICombatService`, `IQuestService`, `ITriggerService`, `ILevelingService`, `ITalentService`, `IWeaponSkillService`, `ICraftingService`, `IRewardService`, `IDeathmatchService`, `IAllyService`, `INavigationService`, `ISpriteLayerCatalog`, `ISpriteCompositor`, `IFileSystemService`, `IDispatcherService`, `IDialogService`, `IGameEngineFactory`

### Sprite Composition Pipeline

Character appearance uses an 11+ layer system. `EquipmentSprite` and `AppearanceOption` records in LiteDB map display names to asset paths and z-ordering. `SpriteLayerCatalog` queries LiteDB to build `StitchLayer` lists. `GodotSpriteCompositor` composites PNG layers into 1536x2112 sprite sheets (6 LPC animation rows). Seed data in `Darkness.Godot/assets/data/sprite-catalog.json`.

### Quest System

9 quest chain JSON files in `Darkness.Godot/assets/data/quests/` (beat_1 through beat_9), seeded into LiteDB. `QuestChain` contains `QuestStep`s (typed: dialogue, combat, location, branch). `QuestState` in LiteDB tracks per-character progress. `QuestService.AdvanceStep()` drives all progression. `TriggerService` handles location-based triggers. `ConditionEvaluator` supports extensible branch conditions (morality, class, item, quest).

### XP & Leveling

Level thresholds in `Darkness.Godot/assets/data/level-table.json` (20 levels). `LevelingService.AwardExperience()` handles XP award, level-up detection, attribute points (2/level), talent points (1/even level), HP scaling, and HP restore. BattleScene awards XP on victory, then advances quest.

### Talent System

Talent trees in `Darkness.Godot/assets/data/talent-trees.json`, seeded into LiteDB. `TalentService` manages class-restricted and morality-gated trees with prerequisite nodes, exclusivity groups, and hidden trees. `TalentLayoutHelper` calculates spatial layout. Talents grant stat bonuses or skill unlocks via `TalentEffect`.

### Skills & Weapons

Skills in `Darkness.Godot/assets/data/skills.json`, seeded into LiteDB. `WeaponSkillService` resolves available skills — prioritizes `ActiveSkillSlots` (player-assigned hotbar), falls back to weapon-type matching. Skills have mana/stamina costs, damage dice, cooldowns, and optional talent requirements.

### Scene Flow

SplashScene → LoadUserScene → MainMenuScene (hub) → WorldScene, BattleScene, StealthScene, DeathmatchScene, CharactersScene, CharacterGenScene, InventoryScene, ForgeScene, SkillsScene, TalentTreeScene, StudyScene, AlliesScene, SettingsScene. `GodotNavigationService` handles fade transitions and `IInitializable` parameter injection. PauseMenu available in-game.

## Workflow Requirements

- **Always use superpowers skills** for all work. Use brainstorming before creative/design work, TDD for implementation, systematic-debugging for bugs, and writing-plans for multi-step tasks. Never skip the skill workflow.
- **All tests must pass before committing.** Run `dotnet test Darkness.Tests` and verify all tests pass before every commit. Do not commit with failing tests. If a pre-existing test fails, investigate and fix it or explicitly flag it to the user — do not ignore it.

## Project Conventions

- .NET 10 with `ImplicitUsings`, `Nullable`, and `TreatWarningsAsErrors` enabled.
- App identifier: `com.risidian.darkness`
- Sprite assets in `assets/sprites/full/` (LPC-based character sprites).
- Seed data in `Darkness.Godot/assets/data/` (sprite-catalog.json, skills.json, talent-trees.json, level-table.json, quests/*.json).
- Key dependencies: CommunityToolkit.Mvvm 8.4.2, LiteDB 5.0.21, Newtonsoft.Json 13.0.3 (Core); Microsoft.Extensions.DependencyInjection 9.0.2 (Godot); SkiaSharp 3.119.2 (Tests).
- Use `using SystemJson = System.Text.Json` when LiteDB is in scope (both have `JsonSerializer`).
- Rendering: `gl_compatibility` mode. Viewport: 1280x720, stretch mode `canvas_items`. Pixel snapping enabled.
- Seeders use `DeleteAll()` + re-insert pattern (idempotent). Errors logged but don't crash startup.
