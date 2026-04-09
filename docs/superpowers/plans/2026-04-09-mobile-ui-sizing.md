# Mobile UI Sizing Standardization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Standardize interactive UI elements (Buttons, Dropdowns, Text Inputs) to a minimum height of 80px and font size of 24 for mobile touch compatibility.

**Architecture:** Hybrid approach using a global Godot Theme for static nodes and a C# extension method for dynamic runtime elements.

**Tech Stack:** Godot 4.6.1, C# (.NET 10)

---

### Task 1: Create Global UI Theme

**Files:**
- Create: `Darkness.Godot/darkness_ui_theme.tres`
- Modify: `Darkness.Godot/project.godot`

- [ ] **Step 1: Create the theme resource file**
Create `Darkness.Godot/darkness_ui_theme.tres` with standard overrides for Button, OptionButton, and LineEdit.

```gdscript
[gd_resource type="Theme" format=3 uid="uid://darkness_theme"]

[sub_resource type="StyleBoxFlat" id="StyleBoxFlat_btn"]
content_margin_left = 12.0
content_margin_top = 10.0
content_margin_right = 12.0
content_margin_bottom = 10.0
bg_color = Color(0.2, 0.2, 0.2, 1)
corner_radius_top_left = 4
corner_radius_top_right = 4
corner_radius_bottom_right = 4
corner_radius_bottom_left = 4

[resource]
default_font_size = 24
Button/font_sizes/font_size = 24
Button/styles/normal = SubResource("StyleBoxFlat_btn")
LineEdit/font_sizes/font_size = 24
LineEdit/styles/normal = SubResource("StyleBoxFlat_btn")
OptionButton/font_sizes/font_size = 24
OptionButton/styles/normal = SubResource("StyleBoxFlat_btn")
PopupMenu/font_sizes/font_size = 24
```

- [ ] **Step 2: Register global theme in project settings**
Modify `Darkness.Godot/project.godot` to set the custom theme.

```gdscript
[gui]
theme/custom="res://darkness_ui_theme.tres"
```

- [ ] **Step 3: Commit**
```bash
git add Darkness.Godot/darkness_ui_theme.tres Darkness.Godot/project.godot
git commit -m "ui: add global mobile-friendly theme"
```

---

### Task 2: Implement UI Utility Helper

**Files:**
- Create: `Darkness.Godot/src/Core/UiUtils.cs`

- [ ] **Step 1: Create the helper class**
Implement the `ApplyMobileSizing` extension method.

```csharp
using Godot;

namespace Darkness.Godot.Core;

public static class UiUtils
{
    public const int MobileMinHeight = 80;
    public const int MobileFontSize = 24;

    public static void ApplyMobileSizing(this Control node)
    {
        if (node == null) return;
        
        // Ensure height is at least 80px
        var minSize = node.CustomMinimumSize;
        if (minSize.Y < MobileMinHeight)
        {
            node.CustomMinimumSize = new Vector2(minSize.X, MobileMinHeight);
        }

        // Apply font size overrides to common interactive nodes
        if (node is Button btn)
        {
            btn.AddThemeFontSizeOverride("font_size", MobileFontSize);
        }
        else if (node is LineEdit le)
        {
            le.AddThemeFontSizeOverride("font_size", MobileFontSize);
        }
        else if (node is OptionButton ob)
        {
            ob.AddThemeFontSizeOverride("font_size", MobileFontSize);
        }
    }
}
```

- [ ] **Step 2: Commit**
```bash
git add Darkness.Godot/src/Core/UiUtils.cs
git commit -m "ui: add UiUtils extension for mobile sizing"
```

---

### Task 3: Update Dialogue Choices in WorldScene

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Update UpdateDialogueUI to use helper**
Apply the sizing to dynamically created buttons.

```csharp
// Around line 535 in UpdateDialogueUI
foreach (var choice in _currentChoices)
{
    var btn = new Button { Text = choice.Text };
    btn.ApplyMobileSizing(); // Use the extension method
    btn.Pressed += () => OnChoiceSelected(choice);
    _choicesContainer.AddChild(btn);
}
```

- [ ] **Step 2: Commit**
```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "ui: apply mobile sizing to dynamic dialogue choices"
```

---

### Task 4: Refactor CharacterGenScene Sizing

**Files:**
- Modify: `Darkness.Godot/scenes/CharacterGenScene.tscn`

- [ ] **Step 1: Remove local overrides**
Find nodes like `NameEdit`, `ClassOption`, `SkinOption`, etc., and ensure they don't have hardcoded sizes or font sizes that conflict with the 80px theme. 

- [ ] **Step 2: Commit**
```bash
git add Darkness.Godot/scenes/CharacterGenScene.tscn
git commit -m "ui: standardize CharacterGenScene element heights"
```

---

### Task 5: Standardize Menu Buttons

**Files:**
- Modify: `Darkness.Godot/scenes/MainMenuScene.tscn`
- Modify: `Darkness.Godot/scenes/PauseMenu.tscn`
- Modify: `Darkness.Godot/scenes/SettingsScene.tscn`

- [ ] **Step 1: Update MainMenuScene**
Ensure buttons in the `GridContainer` scale correctly or have a minimum height of 80px.

- [ ] **Step 2: Update PauseMenu**
Ensure `ResumeButton`, `InventoryButton`, etc., match the 80px standard.

- [ ] **Step 3: Update SettingsScene**
Ensure `SaveButton` and `CancelButton` match the 80px standard.

- [ ] **Step 4: Commit**
```bash
git add Darkness.Godot/scenes/MainMenuScene.tscn Darkness.Godot/scenes/PauseMenu.tscn Darkness.Godot/scenes/SettingsScene.tscn
git commit -m "ui: finalize mobile sizing across all major menus"
```
