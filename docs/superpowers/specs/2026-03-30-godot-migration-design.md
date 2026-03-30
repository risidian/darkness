# Specification: Godot C# (.NET 10) Migration — "Darkness: Reforged"

## 1. Objective
Replace the fragmented .NET MAUI + MonoGame architecture with a unified, scalable, and testable **Godot 4.3 (.NET 10)** host application. This will resolve ongoing platform-integration issues (Windows black screen, Android host crashes) and provide a professional-grade game engine for future expansion.

## 2. Goals
- **Unified Rendering:** Use Godot's 2D renderer for both UI (menus) and Game Scenes (World/Battle).
- **Testability:** Maintain 100% testability for `Darkness.Core` logic and introduce Scene-level testing in Godot.
- **Mobile First:** Ensure native Touch support and Android lifecycle stability.
- **Framework Preservation:** Reuse all existing business logic from `Darkness.Core` (Combat, Session, User/Character management).

## 3. Architecture

### 3.1 Project Structure
The solution will be restructured into:
- **`Darkness.Core`**: Pure .NET Class Library. Contains Models, Interfaces, and Services. **No Godot or MAUI dependencies.**
- **`Darkness.Godot`**: The Godot project folder.
  - `src/`: C# scripts for Godot nodes.
  - `scenes/`: `.tscn` files for UI and Game levels.
  - `assets/`: Textures, fonts, and sounds (moved from MAUI/MonoGame Resources).

### 3.2 Dependency Injection (DI)
We will use `Microsoft.Extensions.DependencyInjection` to maintain the senior-engineer "Clean Architecture" standards.
- **Global Autoload**: A `Global.cs` node (Autoload) will initialize the `ServiceCollection` on `_Ready()`.
- **Service Access**: Nodes will resolve services via the `Global` node's `ServiceProvider`.

```csharp
// Example Scene Script
public partial class BattleScene : Node2D {
    [Inject] private ICombatService _combat; // Resolved in _Ready() via Global
}
```

### 3.3 Navigation & Data Passing
A `GodotNavigationService` will replace MAUI Shell.
- **Pattern**: Explicit Parameter Injection (Static Factory / Init method).
- **Benefit**: Scenes are decoupled from global state, making them unit-testable in isolation.

```csharp
// Passing an Encounter to Battle
NavigationService.NavigateTo("BattleScene", new Dictionary<string, object> { 
    { "Encounter", currentEncounter } 
});
```

## 4. Feature Migration Mapping

| MAUI Page / ViewModel | Godot Scene / Script | Status |
| :--- | :--- | :--- |
| `LoadUserPage` | `LoadUserScene.tscn` | UI + User Selection |
| `CreateUserPage` | `CreateUserScene.tscn` | UI + Form Validation |
| `CharactersPage` | `CharactersScene.tscn` | Character List + Selection |
| `CharacterGenPage` | `CharacterGenScene.tscn` | Live Sprite Preview + Appearance Logic |
| `GamePage` (World) | `GameScene.tscn` | World Map + NPC Dialogue + Encounter Triggers |
| `BattlePage` | `BattleScene.tscn` | Turn-based Combat UI + Combat Log |
| `DeathmatchPage` | `DeathmatchScene.tscn` | Wave-based Survival |
| `ForgePage` | `ForgeScene.tscn` | Crafting / Recipe Management |
| `AlliesPage` | `AlliesScene.tscn` | Social Management |
| `SettingsPage` | `SettingsScene.tscn` | Configuration |

## 5. Input & Interaction
- **Godot Input Map**: Define actions like `move_up`, `interact`, `attack_1`.
- **Bindings**: Keyboards (WASD), Gamepads, and Touch (Android).
- **Touch Emulation**: Godot's "Emulate Mouse From Touch" will be used for menu navigation.
- **World Interaction**: Custom touch handling for world movement and targeting.

## 6. Mobile & Android Strategy
- **`IAdService`**: Defined in `Darkness.Core`. Godot-native implementation using community AdMob plugins. Mock implementation for Windows testing.
- **Save Games**: Use Godot's `user://` pathing to ensure data persistence across Windows and Android internal storage.
- **Scaling**: **Canvas Items** stretch mode with **Expand** aspect ratio for multi-resolution support (Phones vs. Tablets).

## 7. Verification Plan
1. **Unit Tests**: Retain and run all `Darkness.Core` tests.
2. **Scene Tests**: Verify that `BattleScene.Initialize()` correctly populates characters and enemies without crashing.
3. **Smoke Test**: Launch the game, create a user, create a character, and trigger a battle on both Windows and Android.
