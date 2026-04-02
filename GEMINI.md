# Darkness: Reforged — Project Status (Godot Migration)

## 1. Project Overview
The project has been successfully migrated from a fragmented **.NET MAUI + MonoGame** architecture to a unified **Godot 4.6.1 (.NET 10)** host application. 

### Core Projects
- **`Darkness.Core`**: Pure .NET 10 library containing all models, interfaces, and business logic (Combat, Session, Database). Shared across platforms.
- **`Darkness.Godot`**: The main game entry point. A Godot C# project that handles rendering, UI, and input.
- **`Darkness.MAUI.bak`**: (Archived) Previous frontend project.

## 2. Architecture & Design Patterns

### Dependency Injection (DI)
- Uses `Microsoft.Extensions.DependencyInjection`.
- **Global Autoload (`Global.cs`)**: Initializes the `ServiceProvider` on startup and manages service lifetimes.
- **Service Resolution**: UI and Game nodes resolve services via `Global.Services` during their `_Ready()` lifecycle.

### Navigation System
- **`GodotNavigationService`**: Custom implementation of `INavigationService`.
- **Pattern**: Uses `GetTree().ChangeSceneToFile()` with **Explicit Parameter Injection**.
- **`IInitializable`**: Interface for scenes to receive complex data (Encounters, Snapshots) during transitions.

### Game Loop & Assets
- **Sprite Generation**: Uses `GodotSpriteCompositor` (Godot-native) to generate 576x256 LPC-compliant sprite sheets at runtime using `Image` alpha blending.
- **Animation**: `ImageUtils.CreateSpriteFrames()` slices dynamic sheets into Godot `SpriteFrames` for `AnimatedSprite2D`.
- **FileSystem**: `GodotFileSystemService` uses `ResourceLoader.Load<Texture2D>()` to safely load remapped `res://` assets on Android, ensuring format conversion to PNG-compatible RGBA8.

## 3. Game Flow & Scenes
The current navigation flow is strictly enforced:
1. **`SplashScene`**: Initial initialization & Database health check.
2. **`LoadUserScene`**: Login and user selection.
3. **`CreateUserScene`**: Gated user creation (requires user entry to proceed).
4. **`CharactersScene`**: Dynamic list of characters with thumbnails.
5. **`MainMenuScene` (Hub)**: Central hub for all game modes (**Story**, **Deathmatch**, **PVP**, **Forge**, **Study**, **Allies**, **Settings**).
6. **`WorldScene`**: Functional "Sandy Shore" world with animated player/NPC and interaction triggers.
7. **`BattleScene`**: Turn-based combat with animated combatants and health tracking.

## 4. Build & Deployment

### Android (APK)
- **Tool**: PowerShell script `build-android.ps1` in the root.
- **Features**:
    - Auto-builds the C# project.
    - Headless Godot export to `bin/Darkness.apk`.
    - **Auto-Versioning**: Increments `version/code` in `export_presets.cfg` every run.
- **Requirements**: JDK 17+ (OpenJDK recommended) configured in Godot Editor Settings.

## 5. Implementation Notes & Troubleshooting
- **Touch Input**: Enabled `emulate_mouse_from_touch` in Project Settings to ensure UI button compatibility on Android.
- **UI Layering**: High-density screens are wrapped in `ScrollContainer` -> `VBoxContainer` hierarchies. Ensure root nodes use `mouse_filter = Ignore` if buttons are unresponsive.
- **Audio Driver**: Explicitly set to **WASAPI** in `project.godot` with `enable_input = false` to mitigate Windows driver initialization errors.
- **Namespaces**: Use `global::Godot.FileAccess` when using `System.IO` in the same file to avoid ambiguous references.

## 6. Next Steps
- Implement PVP mode logic.
- Expand `WorldScene` with more NPCs and StoryController triggers.
- Finalize `InventoryScene` item usage/equipping logic.
