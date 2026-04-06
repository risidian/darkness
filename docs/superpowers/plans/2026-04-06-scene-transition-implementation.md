# Scene Transition System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a cinematic fade-to-black transition between all scene changes.

**Architecture:** Create a persistent `TransitionLayer` UI that lives in the `Global` autoload. Update `GodotNavigationService` to orchestrate fading out before swapping scenes and fading in after the new scene is ready.

**Tech Stack:** .NET 10, Godot 4.6.1 (Tweens, CanvasLayer).

---

### Task 1: Create TransitionLayer Component

**Files:**
- Create: `Darkness.Godot/scenes/TransitionLayer.tscn`
- Create: `Darkness.Godot/src/UI/TransitionLayer.cs`

- [ ] **Step 1: Create TransitionLayer.cs**

```csharp
using Godot;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class TransitionLayer : CanvasLayer
{
    private ColorRect _overlay = null!;

    public override void _Ready()
    {
        Layer = 128; // Ensure it's on top of everything
        _overlay = new ColorRect
        {
            Color = Colors.Black,
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _overlay.Modulate = new Color(1, 1, 1, 0); // Start transparent
        AddChild(_overlay);
    }

    public async Task FadeOut(float duration = 0.5f)
    {
        _overlay.MouseFilter = Control.MouseFilterEnum.Stop; // Block input during fade
        var tween = CreateTween();
        tween.TweenProperty(_overlay, "modulate:a", 1.0f, duration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
        
        await ToSignal(tween, "finished");
    }

    public async Task FadeIn(float duration = 0.5f)
    {
        var tween = CreateTween();
        tween.TweenProperty(_overlay, "modulate:a", 0.0f, duration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.In);
        
        await ToSignal(tween, "finished");
        _overlay.MouseFilter = Control.MouseFilterEnum.Ignore; // Allow input again
    }
}
```

- [ ] **Step 2: Create TransitionLayer.tscn**

```xml
[gd_scene load_steps=2 format=3 uid="uid://transition_layer_uid"]

[ext_resource type="Script" path="res://src/UI/TransitionLayer.cs" id="1_trans"]

[node name="TransitionLayer" type="CanvasLayer"]
script = ExtResource("1_trans")
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/UI/TransitionLayer.cs Darkness.Godot/scenes/TransitionLayer.tscn
git commit -m "feat: add TransitionLayer component for scene fades"
```

---

### Task 2: Integrate TransitionLayer into Global

**Files:**
- Modify: `Darkness.Godot/src/Core/Global.cs`

- [ ] **Step 1: Instantiate and expose TransitionLayer**

Add a field and property to `Global.cs`:
```csharp
    public TransitionLayer Transition { get; private set; } = null!;
```

Update `_Ready()` to add it to the scene tree:
```csharp
        // ... after DI setup ...
        Transition = new TransitionLayer();
        AddChild(Transition);
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/src/Core/Global.cs
git commit -m "feat: initialize TransitionLayer in Global autoload"
```

---

### Task 3: Update Navigation Service

**Files:**
- Modify: `Darkness.Godot/src/Services/GodotNavigationService.cs`

- [ ] **Step 1: Orchestrate fade in NavigateToAsync**

Update `NavigateToAsync` (both overloads):
```csharp
    public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        GD.Print($"[Navigation] Navigating to route: {route}");
        
        // 1. Fade Out
        await _global.Transition.FadeOut();

        string sceneName = route.Replace("Page", "Scene");
        string path = $"res://scenes/{sceneName}.tscn";

        // 2. Swap Scene
        var error = _global.GetTree().ChangeSceneToFile(path);
        if (error != Error.Ok)
        {
            await _global.Transition.FadeIn(); // Fade back in so user can see the error
            // ... existing error handling ...
            return;
        }

        // Wait for next frame so current scene is really the new one
        await _global.ToSignal(_global.GetTree(), "process_frame");

        // 3. Handle parameter injection
        var root = _global.GetTree().CurrentScene;
        if (root is IInitializable initializable && parameters != null)
        {
            initializable.Initialize(parameters);
        }

        // 4. Fade In
        await _global.Transition.FadeIn();
    }
```

- [ ] **Step 2: Update Generic NavigateToAsync**

Apply the same `FadeOut` / `FadeIn` logic to `public async Task NavigateToAsync<T>(string route, T parameters)`.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/Services/GodotNavigationService.cs
git commit -m "feat: use cinematic fades in GodotNavigationService"
```

---

### Task 4: Verification

- [ ] **Step 1: Build Check**
Run `dotnet build Darkness.Godot/Darkness.Godot.csproj`

- [ ] **Step 2: Manual Playthrough**
Navigate from Login -> Character Gen -> Main Menu -> World.
Verify smooth black fades between every transition.
Confirm input is blocked during the fade (cannot click buttons while screen is black).
