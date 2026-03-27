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

## Project Conventions

- .NET 10 with `ImplicitUsings` and `Nullable` enabled across all projects
- App identifier: `com.risidian.darkness`
- Sprite assets in `/SpriteSheets/` (LPC-based character sprites)
- Design spec: `docs/superpowers/specs/2026-03-25-darkness-modernization-design.md`
