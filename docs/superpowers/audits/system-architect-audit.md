# System Architect Audit: Darkness (MAUI + MonoGame)
**Status: CRITICAL ARCHITECTURAL DEBT DETECTED**

## 1. Reflection-Based Platform Bridge (Brittle & Dangerous)
The `MonoGameHostHandler` implementation across Windows and Android is an "architectural accident waiting to happen." It relies heavily on reflection (`GetField`, `GetMethod`, `Invoke`) to bypass access modifiers and manually trigger the MonoGame lifecycle (`Initialize`, `LoadContent`, `Tick`).
- **Risk:** This will break with any minor version update of MonoGame or the underlying .NET runtime. It's a complete bypass of type safety and standard lifecycle management.
- **Fix:** Implement a proper interface-based bridge or use a more robust hosting library that doesn't require "reaching into" private fields of the `GraphicsDeviceManager`.

## 2. Threading & Frame Timing (Stutter & Race Conditions)
The Android rendering loop in `MonoGameHostHandler.cs` is implemented using `async void` and `Task.Delay(16)`. 
- **The Sin:** `Task.Delay` is not a high-precision timer. This results in inconsistent frame timing (jitter), which is unacceptable for a game. 
- **Concurrency Risk:** Calling `Tick` from an arbitrary task thread while the UI might be interacting with the `Game` instance via MAUI properties is a race condition.
- **Fix:** Use a platform-native rendering callback (e.g., `Choreographer` on Android, `CompositionTarget.Rendering` on Windows) to sync with the display's VSync.

## 3. Memory Leaks & Resource Lifecycle
Disposable resources are being created but never disposed.
- **Example:** `BattleScene.cs` creates a new `Texture2D` (`_pixel`) in `LoadContent` every time a battle starts. This texture is never disposed. Since `DarknessGame` creates new `BattleScene` instances frequently, this is a slow-motion memory leak that will eventually crash the app on mobile devices with limited RAM.
- **DI Abuse:** `BattleScene` manually instantiates `CombatEngine` (`new CombatEngine()`), ignoring the DI container. This makes unit testing impossible and creates redundant object allocations.

## 4. Scalability: Hardcoded "Story Beats"
The `StoryController` uses a massive `switch(beat)` statement with hardcoded enemy stats and party members.
- **Architectural Smell:** This is a "God Object" in the making. As the game grows, this file will become unmaintainable.
- **Fix:** Move game data (encounters, stats, dialogue) into external JSON or SQLite data files.

## 5. Dangerous Exception Handling
`LocalDatabaseService.cs` contains empty `catch (Exception)` blocks.
- **Impact:** Critical IO failures will be silently swallowed, leaving the application in an undefined state. Debugging database corruption will be a nightmare.
- **Fix:** Log exceptions at a minimum, or implement a proper retry/error-reporting strategy.
