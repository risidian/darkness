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

# Run MAUI app (Windows)
dotnet build Darkness.MAUI -f net10.0-windows10.0.19041.0

# Run WebAPI backend
dotnet run --project Darkness.WebAPI

# Run all tests
dotnet test Darkness.Tests

# Run a single test
dotnet test Darkness.Tests --filter "FullyQualifiedName~TestMethodName"
```

## Architecture

This is **Darkness**, a cross-platform RPG built on .NET 10 with four projects:

- **Darkness.Core** — Platform-agnostic business logic layer. Contains models, services, ViewModels, combat engine, story controller, and SQLite persistence (`LocalDatabaseService`). All game logic lives here.
- **Darkness.MAUI** — UI shell targeting Android (`net10.0-android`) and Windows (`net10.0-windows10.0.19041.0`). XAML pages for menus/screens, platform service implementations (`MauiFileSystemService`, `MauiNavigationService`, `MauiDialogService`).
- **Darkness.Game** — MonoGame 2D engine embedded within MAUI. Contains `WorldScene` (belt-scroller exploration) and `BattleScene` (turn-based combat rendering).
- **Darkness.WebAPI** — ASP.NET Core 10 backend with EF Core (SQL Server). Currently minimal; expansion path for multiplayer.
- **Darkness.Tests** — XUnit tests with Moq. References only Darkness.Core.

### Key Patterns

- **MVVM** via `CommunityToolkit.Mvvm` — ViewModels in Core extend `ObservableObject`
- **DI** configured in `Darkness.MAUI/MauiProgram.cs` — services are singletons, pages/ViewModels are transient
- **Interface-driven**: Core defines interfaces (`IUserService`, `ICharacterService`, `ICombatService`, `INavigationService`, `IDialogService`, `ISessionService`, `IRewardService`, `IFileSystemService`); MAUI provides platform implementations
- **Local data**: SQLite via `sqlite-net-pcl`, database at `FileSystem.AppDataDirectory/Darkness.db3`
- **Remote data**: EF Core `DarknessContext` in WebAPI (SQL Server)

### Data Flow

MAUI Pages → ViewModels (in Core) → Services (in Core) → SQLite / MonoGame scenes

Navigation between MAUI pages and MonoGame views goes through `AppShell` and `INavigationService`.

## RPG Systems

- **Stats**: STR, DEX, CON, INT, WIS, CHA → derived HP, MP, Stamina, Speed, Accuracy, Evasion, Defense, MagicDefense
- **Combat**: Turn-based, speed-based initiative (`CombatEngine` in Core/Logic)
- **Story**: "Revenge" arc scripted in `StoryController` (Core/Logic)
- **Status effects**: Poisoned, Bleeding, Stunned, Burning, Frozen, Fear
- **Resistances**: Fire, Ice, Lightning, Holy, Dark

### MonoGame ↔ MAUI Integration

`DarknessGame` (MonoGame) is hosted inside MAUI's `GamePage`. Scene switching between `WorldScene` (exploration) and `BattleScene` (combat) is controlled via `_isBattleActive` flag in `DarknessGame`. The `BattleEnded` event signals MAUI to navigate back.

### Sprite Composition Pipeline

Character appearance uses an 11+ layer system (body, head, face, eyes, hair, armor, feet, arms, legs, weapon). `SpriteLayerCatalog` maps display names to file paths and defines z-ordering. `SpriteCompositor` uses SkiaSharp to composite PNG layers into a single sprite sheet, with frame extraction and scaling support. Preview generation is async and triggered on any appearance property change in `CharacterGenViewModel`.

### Navigation Routes

Shell routes registered in `AppShell`: `LoadUserPage`, `CreateUserPage`, `CharacterGenPage`, `GamePage`, `BattlePage`, `MainPage`. Navigation uses `Shell.Current.GoToAsync()` via `MauiNavigationService`.

### Combat Formulas

- **Turn order**: DEX + Speed + d10 (random)
- **Damage**: (Attacker × 2) − Defense; 5% crit chance at 1.5× multiplier
- **Status effect application**: d100 roll > target Wisdom = effect applied
- **Survival encounters**: N-turn endurance via `BattleScene.SurvivalTurns` (e.g., story beat 4: Dark Warrior, 5-round survival)

### Character Classes & Stat Presets

Warrior (STR 15, CON 14), Knight, Rogue, Mage (INT 15), Cleric — each with preset stat distributions and default appearance. Base stats range 8–18; derived: HP = CON×10, MP = WIS×5, Stamina = CON×5.

## Project Conventions

- .NET 10 with `ImplicitUsings` and `Nullable` enabled across all projects
- App identifier: `com.risidian.darkness`
- Sprite assets in `/SpriteSheets/` (LPC-based character sprites)
- Design spec: `docs/superpowers/specs/2026-03-25-darkness-modernization-design.md`
- Key dependencies: CommunityToolkit.Mvvm 8.4.2, sqlite-net-pcl 1.9.172, SkiaSharp 3.116.1, MonoGame.Framework.Portable 3.7.1.189
