# Design Spec: MonoGame Host Control for .NET MAUI

## 1. Overview
Integrate the MonoGame engine into the .NET MAUI application by creating a custom control (`MonoGameHost`). This control will provide a native rendering surface for `DarknessGame` on both Windows (WinUI) and Android, bridging the gap between MAUI's UI framework and MonoGame's game loop.

## 2. Architecture

### 2.1 Cross-Platform Control (`Darkness.MAUI/Controls/MonoGameHost.cs`)
- Inherits from `Microsoft.Maui.Controls.View`.
- **Properties:**
  - `Game`: A `BindableProperty` of type `Microsoft.Xna.Framework.Game`. This allows the `GamePage` to bind its `DarknessGame` instance to the view.
- **Purpose:** Acts as the MAUI-side representative of the game viewport.

### 2.2 Handler Pattern (`Darkness.MAUI/Handlers/MonoGameHostHandler.cs`)
- Uses the MAUI Handler pattern to map the cross-platform `MonoGameHost` to native platform views.
- Defines a `Mapper` that handles property updates (e.g., when the `Game` property changes).

### 2.3 Platform-Specific Implementations

#### Windows (`Platforms/Windows/Handlers/MonoGameHostHandler.cs`)
- **Native View:** `Microsoft.UI.Xaml.Controls.SwapChainPanel`.
- **Integration:** Initializes the MonoGame `GraphicsDevice` to target the `SwapChainPanel`. 
- **Loop:** Hooks into the `CompositionTarget.Rendering` event or a dedicated thread to drive the MonoGame `Tick()`.

#### Android (`Platforms/Android/Handlers/MonoGameHostHandler.cs`)
- **Native View:** `Microsoft.Xna.Framework.AndroidGameView`.
- **Integration:** Wraps the standard MonoGame Android view logic.

## 3. Lifecycle Management
- **Initialization:** The native view is created when the handler is attached. The `DarknessGame.Run()` method is triggered.
- **Cleanup:** Implements `IDisposable`. When the `GamePage` is navigated away from or destroyed, the MonoGame instance is properly disposed of to release GPU resources.
- **Resizing:** The MonoGame viewport updates its back buffer size when the MAUI control's dimensions change.

## 4. Proposed Usage
```xml
<controls:MonoGameHost 
    Game="{Binding GameInstance}"
    HorizontalOptions="Fill"
    VerticalOptions="Fill" />
```

## 5. Success Criteria
- `DarknessGame` renders its "Shore of Camelot" scene within the `GamePage`.
- Input (Keyboard/Touch) correctly flows into the MonoGame logic.
- No memory leaks or GPU resource hangs when navigating between pages.
