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
- **DI** configured in `Darkness.Godot/src/Core/Global.cs`.
- **Interface-driven**: Core defines interfaces (`IUserService`, `ICharacterService`, `ICombatService`, `INavigationService`, `IDialogService`, `ISessionService`, `IRewardService`, `IFileSystemService`, `ISpriteCompositor`); Godot provides platform implementations.
- **Local data**: LiteDB via `LiteDB` package.

### Data Flow

Godot Scenes → ViewModels (in Core) → Services (in Core) → LiteDB

### Sprite Composition Pipeline

Character appearance uses an 11+ layer system (body, head, face, eyes, hair, armor, feet, arms, legs, weapon). `SpriteLayerCatalog` maps display names to file paths and defines z-ordering. `GodotSpriteCompositor` uses Godot's `Image` class to composite PNG layers into a single sprite sheet with proper alpha blending.

## Project Conventions

- .NET 10 with `ImplicitUsings` and `Nullable` enabled.
- App identifier: `com.risidian.darkness`
- Sprite assets in `/sprites/` (LPC-based character sprites).
- Key dependencies: CommunityToolkit.Mvvm 8.4.2, LiteDB 5.0.21.
