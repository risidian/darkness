# Design Doc: Scene Transition System

## 1. Overview
This design implements a smooth visual transition (fade-to-black) between scene changes in Godot. It replaces the jarring "instant cut" with a cinematic fade that masks the loading process.

## 2. Goals
- **Smooth Navigation:** Provide a consistent fade-out/fade-in effect for all scene transitions.
- **Persistent Overlay:** Use a global canvas layer that remains active regardless of the current scene.
- **Future-Proofing:** Infrastructure should support more complex loading screens (Approach C) later.

## 3. Architecture

### 3.1 Components

#### `TransitionLayer.tscn / .cs`
- A `CanvasLayer` with `layer = 128` (ensuring it stays on top of all UI).
- A `ColorRect` covering the full screen (`anchors_preset = 15`).
- Initial state: `modulate.a = 0` (invisible).
- Methods:
    - `Task FadeOut(float duration)`: Tweens alpha from 0 to 1.
    - `Task FadeIn(float duration)`: Tweens alpha from 1 to 0.

#### `Global.cs`
- Will instantiate and add the `TransitionLayer` to the root viewport on startup.
- Provides a public property or method to access the `TransitionLayer`.

#### `GodotNavigationService.cs`
- Modified to orchestrate the fade:
    1. `await Global.Transition.FadeOut()`
    2. `GetTree().ChangeSceneToFile()`
    3. `await process_frame` (allow new scene to initialize)
    4. `await Global.Transition.FadeIn()`

### 3.2 Data Flow
1. A scene calls `_navigation.NavigateToAsync(...)`.
2. Navigation service requests a `FadeOut` from the global transition controller.
3. Once opaque, the scene is swapped.
4. Navigation service waits for the new scene to be ready.
5. Navigation service requests a `FadeIn`.

## 4. Future Expansion (Approach C)
The `TransitionLayer` can be updated later to include:
- A `TextureRect` for background art.
- A `ProgressBar` for async loading progress.
- Narrative text or tips displayed during the black-out phase.

## 5. Testing Strategy
- **Manual Verification:** Navigate between `MainMenu`, `WorldScene`, and `BattleScene`. Verify that the screen fades to black before the swap and fades back in after.
- **Timing Check:** Ensure the "fade in" doesn't start until the new scene's `Initialize()` and `_Ready()` methods have had a chance to run (masked by the `process_frame` wait).
