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

# Run all tests
dotnet test Darkness.Tests

# Run a single test
dotnet test Darkness.Tests --filter "FullyQualifiedName~TestMethodName"
```

## Architecture

This is **Darkness**, a cross-platform RPG built on .NET 10 with three active projects:

- **Darkness.Core** — Platform-agnostic business logic layer. Contains models, services, ViewModels, combat engine, story controller, and LiteDB persistence. All game logic lives here.
- **Darkness.Godot** — The main game host. A Godot 4.6.1 project using C# for rendering, UI, and input.
- **Darkness.WebAPI** — ASP.NET Core 10 backend with EF Core.
- **Darkness.Tests** — XUnit tests with Moq. References Darkness.Core.

### Key Patterns

- **MVVM** via `CommunityToolkit.Mvvm` — ViewModels in Core extend `ObservableObject`.
- **DI** configured in `Darkness.Godot/src/Core/Global.cs`. Seeders run at startup.
- **Interface-driven**: Core defines interfaces (`IQuestService`, `ILevelingService`, `ITriggerService`, `ISpriteLayerCatalog`, `ICombatService`, `ICharacterService`, `INavigationService`, `ISessionService`, `IFileSystemService`, `ISpriteCompositor`); Godot provides platform implementations.
- **Data-driven**: Game content (equipment, quests, level thresholds) defined in JSON seed files, loaded into LiteDB at startup. Adding content requires JSON edits only — no code changes.
- **Local data**: LiteDB via `LiteDB` package.

### Data Flow

Godot Scenes → ViewModels (in Core) → Services (in Core) → LiteDB

### Sprite Composition Pipeline

Character appearance uses an 11+ layer system. `EquipmentSprite` and `AppearanceOption` records in LiteDB map display names to asset paths and z-ordering. `SpriteLayerCatalog` queries LiteDB to build `StitchLayer` lists. `GodotSpriteCompositor` composites PNG layers into a single sprite sheet. Seed data in `assets/data/sprite-catalog.json`.

### Quest System

Per-chain JSON files in `assets/data/quests/`, seeded into LiteDB. `QuestChain` contains `QuestStep`s (typed: dialogue, combat, location, branch). `QuestState` in LiteDB tracks per-character progress. `QuestService.AdvanceStep()` drives all progression. `TriggerService` handles location-based triggers. `ConditionEvaluator` supports extensible branch conditions (morality, class, item, quest).

### XP & Leveling

Level thresholds in `assets/data/level-table.json`. `LevelingService.AwardExperience()` handles XP award, level-up detection, attribute points (2/level), HP restore. BattleScene awards XP on victory, then advances quest.

## Workflow Requirements

- **Always use superpowers skills** for all work. Use brainstorming before creative/design work, TDD for implementation, systematic-debugging for bugs, and writing-plans for multi-step tasks. Never skip the skill workflow.
- **All tests must pass before committing.** Run `dotnet test Darkness.Tests` and verify all tests pass before every commit. Do not commit with failing tests. If a pre-existing test fails, investigate and fix it or explicitly flag it to the user — do not ignore it.

## Project Conventions

- .NET 10 with `ImplicitUsings` and `Nullable` enabled.
- App identifier: `com.risidian.darkness`
- Sprite assets in `assets/sprites/full/` (LPC-based character sprites).
- Seed data in `assets/data/` (sprite-catalog.json, quests/*.json, level-table.json).
- Key dependencies: CommunityToolkit.Mvvm 8.4.2, LiteDB 5.0.21.
- Use `using SystemJson = System.Text.Json` when LiteDB is in scope (both have `JsonSerializer`).
