# Design Spec: Mobile-Friendly UI Sizing Standardization

Standardize the sizing of interactive UI elements (Buttons, Dropdowns, Text Inputs) across the game to ensure they are touch-friendly for mobile devices, targeting a minimum height of 80px and font size of 24.

## 1. Problem Statement
Many UI elements in the current Godot project are sized inconsistently, with some buttons as small as 70px or using font sizes that are difficult to read on mobile screens. Specifically, the Character Generation dropdowns and dynamic Dialogue Choice buttons need standardized sizing to ensure a high-quality mobile experience.

## 2. Goals & Success Criteria
- **Global Consistency:** All standard interactive elements (Buttons, OptionButtons, LineEdits) have a minimum height of 80px.
- **Readability:** All standard UI text has a minimum font size of 24.
- **Maintainability:** UI sizing is controlled through a central Godot Theme resource where possible.
- **Runtime Support:** Dynamically created UI elements (like dialogue choices) automatically apply the standardized sizing.

## 3. Architecture & Components

### 3.1 Global Godot Theme (`darkness_ui_theme.tres`)
A new Theme resource will be created and set as the **Project Default Theme** in `project.godot`.

**Theme Overrides:**
- **Button:**
    - `content_margin_top`: 10
    - `content_margin_bottom`: 10
    - `font_size`: 24
- **OptionButton:**
    - `content_margin_top`: 10
    - `content_margin_bottom`: 10
    - `font_size`: 24
- **LineEdit:**
    - `content_margin_top`: 10
    - `content_margin_bottom`: 10
    - `font_size`: 24
- **PopupMenu (Dropdown List):**
    - `item_start_padding`: 10
    - `item_end_padding`: 10
    - `font_size`: 24

### 3.2 UI Utility Helper (`src/Core/UiUtils.cs`)
A static utility class providing extension methods for runtime UI nodes.

```csharp
namespace Darkness.Godot.Core;

public static class UiUtils
{
    public const int MobileMinHeight = 80;
    public const int MobileFontSize = 24;

    public static void ApplyMobileSizing(this Control node)
    {
        node.CustomMinimumSize = new Vector2(node.CustomMinimumSize.X, MobileMinHeight);
        
        if (node is Button btn)
        {
            btn.AddThemeFontSizeOverride("font_size", MobileFontSize);
        }
        else if (node is LineEdit le)
        {
            le.AddThemeFontSizeOverride("font_size", MobileFontSize);
        }
    }
}
```

### 3.3 Target Scenes for Refactoring
The following scenes will be updated to remove local overrides that conflict with the new 80px global standard:
- `CharacterGenScene.tscn`: Remove manual `custom_minimum_size` on OptionButtons/LineEdits.
- `WorldScene.tscn`: Update TopMenu and TopRightMenu buttons.
- `MainMenuScene.tscn`: Ensure GridContainer buttons scale correctly.
- `PauseMenu.tscn`: Ensure vertical list buttons match the 80px standard.
- `SettingsScene.tscn`: Standardize Save/Cancel buttons.

## 4. Implementation Details

### 4.1 Dialogue Choice Buttons
In `WorldScene.cs`, the `UpdateDialogueUI` method will be updated to use the helper:

```csharp
foreach (var choice in _currentChoices)
{
    var btn = new Button { Text = choice.Text };
    btn.ApplyMobileSizing(); // Extension method call
    btn.Pressed += () => OnChoiceSelected(choice);
    _choicesContainer.AddChild(btn);
}
```

### 4.2 Project Settings Update
`project.godot` will be modified to include:
```gdscript
[gui]
theme/custom="res://darkness_ui_theme.tres"
```

## 5. Testing & Validation
- **Manual Verification:** Open all core scenes in the Godot editor to ensure layout integrity.
- **Mobile Emulation:** Run the project with "Emulate Touch From Mouse" enabled.
- **Dynamic Check:** Trigger dialogue with choices in `WorldScene` to confirm button sizing.
- **Character Gen Check:** Ensure all dropdowns in `CharacterGenScene` are large and readable.
