# Session Summary — 2026-03-30

## Fixes Completed

### 1. App crash on launch (Windows) — EXIT CODE 0x80008087
**Root cause:** `MonoGame.Framework.WindowsDX 3.8.2.1105` has a transitive dependency on `Microsoft.NETCore.App/2.1.30`, which deployed .NET Core 2.1 host binaries (`hostfxr.dll`, `hostpolicy.dll`) into the output directory. These ancient host binaries couldn't initialize the .NET 10 CLR, and `coreclr.dll` was never deployed.

**Fix:** Added exclusions in `Darkness.Game/Darkness.Game.csproj` to suppress the stale packages:
- `Microsoft.NETCore.App 2.1.30`
- `Microsoft.NETCore.DotNetAppHost 2.1.30`
- `Microsoft.NETCore.DotNetHostPolicy 2.1.30`
- `Microsoft.NETCore.DotNetHostResolver 2.1.30`

**Files changed:** `Darkness.Game/Darkness.Game.csproj`

---

### 2. Shell navigation errors — "Global routes cannot be the only page on the stack"
**Root cause:** Pages registered via `Routing.RegisterRoute()` (global routes) were navigated to with `///` absolute routing, which only works for `ShellContent` items defined in AppShell.xaml.

Only `MainPage` and `LoadUserPage` are ShellContent routes. All others (GamePage, CharacterGenPage, CharactersPage, DeathmatchPage, StudyPage, ForgePage, AlliesPage, SettingsPage) are global routes that must use relative navigation.

**Fix:** Changed `///GamePage` → `GamePage`, `///CharacterGenPage` → `CharacterGenPage`, etc. across all ViewModels and tests.

**Files changed:**
- `Darkness.Core/ViewModels/MainViewModel.cs`
- `Darkness.Core/ViewModels/DeathmatchViewModel.cs`
- `Darkness.Core/ViewModels/CreateUserViewModel.cs`
- `Darkness.Core/ViewModels/LoadUserViewModel.cs`
- `Darkness.Core/ViewModels/CharactersViewModel.cs`
- `Darkness.Tests/ViewModels/NavigationRegressionTests.cs`
- `Darkness.Tests/ViewModels/MainViewModelTests.cs`
- `Darkness.Tests/ViewModels/CreateUserViewModelTests.cs`
- `Darkness.Tests/ViewModels/LoadUserViewModelTests.cs`

---

### 3. MonoGame never initialized on Windows — race condition in handler
**Root cause:** `MonoGameHostHandler.UpdateGame()` set `_game` but never called `InitializeGame()`. MAUI connects the native SwapChainPanel first (`ConnectHandler`), then binds properties (`UpdateGame`). By the time `UpdateGame` ran, `ConnectHandler` had already executed with `_game` still null, so `InitializeGame` was never invoked. Result: `_isInitialized` stayed false, `Tick()` was never called.

**Fix:** Added a check in `UpdateGame()` to call `InitializeGame()` immediately when `PlatformView` is already connected.

**Files changed:** `Darkness.MAUI/Platforms/Windows/Handlers/MonoGameHostHandler.cs`

---

### 4. ContentLoadException spam every frame
**Root cause:** A missing `font.xnb` content file was attempted via `content.Load<SpriteFont>("font")` every frame since `LoadContent()` is called from `Tick()`. The catch block swallowed the exception but it fired 60 times per second.

**Fix:** Added `_fontLoadAttempted` boolean guard so each scene only tries to load the font once.

**Files changed:**
- `Darkness.Game/Scenes/WorldScene.cs`
- `Darkness.Game/Scenes/BattleScene.cs`
- `Darkness.Game/Scenes/DeathmatchScene.cs`
- `Darkness.Game/Scenes/PvpScene.cs`

---

### 5. Windows black screen — MonoGame rendering to hidden window
**Root cause:** `MonoGame.Framework.WindowsDX 3.8.2.1105`'s `GraphicsDeviceManager` has no `SwapChainPanel` property. The handler's reflection-based binding failed silently. MonoGame created its own internal WinForms window and rendered there, while the MAUI SwapChainPanel stayed empty/black.

**Fix (partial):** Implemented DXGI swap chain interop bridge:
1. Added SharpDX NuGet packages (`SharpDX`, `SharpDX.DXGI`, `SharpDX.Direct3D11` v4.0.1) to MAUI project for Windows target.
2. Rewrote `MonoGameHostHandler` to extract MonoGame's internal D3D11 device via reflection, create a composition swap chain via `IDXGIFactory2.CreateSwapChainForComposition()`, bind it to the SwapChainPanel via `ISwapChainPanelNative` COM interop, and replace MonoGame's internal `_swapChain` and `_renderTargetView`.
3. Added explicit `GraphicsDevice.Present()` call in `DarknessGame.Tick()` since the custom game loop bypasses MonoGame's normal `BeginDraw/EndDraw` pipeline.
4. Bound the new render target to the D3D11 device context output merger.

**Status: INCOMPLETE** — The bridge completes without errors in debug output, but the screen is still black. The swap chain is created, bound to the panel, and MonoGame's internals are patched, but rendered content doesn't appear. Likely cause: MonoGame rebinds its own render target each frame internally, overriding our replacement. The fix may require intercepting MonoGame's per-frame render target setup.

**Files changed:**
- `Darkness.MAUI/Platforms/Windows/Handlers/MonoGameHostHandler.cs` (full rewrite)
- `Darkness.MAUI/Darkness.MAUI.csproj` (SharpDX package references)
- `Darkness.Game/DarknessGame.cs` (added `GraphicsDevice.Present()` in `Tick()`)

---

### 6. Android sprite preview generation flooding main thread
**Root cause:** `CharacterGenViewModel` constructor sets 13 appearance properties, each triggering `UpdatePreviewAsync().FireAndForget()`. On Android with slow asset loading, this flooded the main thread (skipping 90+ frames) and made the app unresponsive.

**Fix:** Added 250ms debounce via `CancellationTokenSource`. All property change handlers now call `SchedulePreviewUpdate()` which cancels any pending generation and waits 250ms before executing. Rapid-fire changes coalesce into a single preview generation.

**Files changed:** `Darkness.Core/ViewModels/CharacterGenViewModel.cs`

---

### 7. Android crash when opening GamePage — MonoGame NullReferenceException
**Root cause:** `MonoGame.Framework.Android` requires hosting inside an `AndroidGameActivity` with proper Activity context. Creating `new DarknessGame()` from a MAUI page causes `AndroidGamePlatform..ctor()` to throw `NullReferenceException`.

**Fix:** Wrapped `DarknessGame` creation in `GamePage.OnAppearing()` with try-catch. When it fails, the page falls back to a MAUI-rendered world view.

**Files changed:** `Darkness.MAUI/Pages/GamePage.xaml.cs`

---

### 8. Android GamePage black screen — fallback world view
**Root cause:** Since MonoGame can't initialize on Android (see #7), the MonoGameHost control has no game and renders nothing.

**Fix:** Added a MAUI XAML fallback world view visible when `IsEngineUnavailable` is true. Shows sandy background, water strip, colored NPC/player squares, and "Shore of Camelot" label. The MonoGameHost is hidden when unavailable.

**Files changed:**
- `Darkness.MAUI/Pages/GamePage.xaml`
- `Darkness.Core/ViewModels/GamePageViewModel.cs` (added `IsEngineAvailable` / `IsEngineUnavailable` properties)

---

## Remaining Issues

### HIGH PRIORITY

#### Windows GamePage still black
The DXGI swap chain bridge completes successfully (all debug log steps pass), but rendered content doesn't appear in the SwapChainPanel. Likely causes to investigate:
- MonoGame may rebind its own render target each frame, overriding our replacement. Need to hook into MonoGame's per-frame `ApplyRenderTargets()` or patch it every frame before Draw.
- The swap chain may need `Present()` called with specific sync intervals for composition swap chains.
- MonoGame's `SpriteBatch` may cache the render target reference from initialization time.

**Next steps:**
1. Add debug logging in `Tick()` to verify `GraphicsDevice.Clear()` and `SpriteBatch` operations target the correct render target.
2. Try re-binding the composition swap chain's render target to the D3D context at the START of each `Tick()` call, not just at initialization.
3. Consider intercepting `GraphicsDevice.Present()` to ensure it presents to the composition swap chain.
4. As a fallback, consider rendering MonoGame to a `RenderTarget2D`, reading pixels, and displaying via SkiaSharp `SKCanvasView`.

#### Android MonoGame integration
MonoGame on Android requires `AndroidGameActivity` hosting. Proper integration would need:
1. A custom `AndroidGameView` or embed MonoGame's `AndroidSurfaceView` inside the MAUI layout.
2. Or use the same fallback approach (MAUI XAML world view) as a permanent solution for Android, with MonoGame only on Windows/desktop.

### LOW PRIORITY

#### Android sprite layer loading errors
`[SpritePreview] FAILED: sprites/body/light.png - Specified method is not supported` — `OpenAppPackageFileAsync` throws on some Android configurations. The compositing still succeeds (sprites are loaded via a fallback path), so this is cosmetic noise. Could be improved by:
1. Using `Android.Content.Res.AssetManager` directly on Android.
2. Or silencing the error logs when the fallback succeeds.

#### Missing font content file
No `font.xnb` exists for MonoGame's `ContentManager`. All scenes gracefully handle the missing font (text just doesn't render). To fix properly:
1. Create a `font.spritefont` definition in `Darkness.Game/Content/`.
2. Build it with the MonoGame Content Pipeline tool to produce `font.xnb`.
3. Include it in the app package.

---

## Test Status
All 112 tests pass after all changes.
