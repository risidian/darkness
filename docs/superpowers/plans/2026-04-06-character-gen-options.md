# Character Generation Options Update Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hide equipment-related appearance options and labels in the Character Generation scene.

**Architecture:** We will set `Visible = false` via `.Hide()` on the OptionButton and Label nodes in `_Ready()` in `CharacterGenScene.cs`, preserving the underlying logic while cleaning up the UI.

**Tech Stack:** Godot 4.6, C#, .NET 10

---

### Task 1: Hide Class-Determined Options in CharacterGenScene

**Files:**
- Modify: `Darkness.Godot/src/UI/CharacterGenScene.cs`

- [ ] **Step 1: Implement the UI hiding logic**

Modify `Darkness.Godot/src/UI/CharacterGenScene.cs` in the `_Ready()` method, right after `SetupOptions();`.

```csharp
        // Hide class-determined equipment options
        GetNode<Label>(container + "LegsLabel").Hide();
        _legsOption.Hide();
        GetNode<Label>(container + "FeetLabel").Hide();
        _feetOption.Hide();
        GetNode<Label>(container + "ArmsLabel").Hide();
        _armsOption.Hide();
        GetNode<Label>(container + "ArmorLabel").Hide();
        _armorOption.Hide();
        GetNode<Label>(container + "WeaponLabel").Hide();
        _weaponOption.Hide();
        GetNode<Label>(container + "ShieldLabel").Hide();
        _shieldOption.Hide();
```

*(Note: Godot UI node testing is excluded from automated test suites because it tests Godot scene logic. Manual verification will suffice.)*

- [ ] **Step 2: Build to verify compilation**

Run:
```bash
dotnet build Darkness.Godot/Darkness.Godot.csproj
```
Expected: Build succeeds with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/UI/CharacterGenScene.cs
git commit -m "feat(ui): hide class-determined equipment options in character creator"
```
