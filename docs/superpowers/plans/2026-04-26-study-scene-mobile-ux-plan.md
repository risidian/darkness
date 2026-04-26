# Study Scene Mobile UX Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the Study scene to be mobile-friendly with a side-by-side layout, larger touch targets, real-time derived stat updates, and unsaved changes protection.

**Architecture:** We will replace the current centered `VBoxContainer` in `StudyScene.tscn` with a full-screen `HBoxContainer` containing two `ScrollContainer`s (left for attributes, right for derived stats). We will add `-` (minus) buttons for each attribute and a Save button. In `StudyScene.cs`, we will track initial attribute values to support discarding unsaved changes and prompt the user if they try to navigate away without saving.

**Tech Stack:** Godot 4.6.1, C# (.NET 10)

---

### Task 1: Update the StudyScene UI Layout (TSCN)

**Files:**
- Modify: `Darkness.Godot/scenes/StudyScene.tscn`

- [ ] **Step 1: Replace the layout structure in TSCN**

Since modifying TSCN files via raw text can be fragile, we will replace the entire node structure while keeping the root and script reference. We'll change the root `VBoxContainer` to an `HBoxContainer` spanning the screen, add two `ScrollContainer`s, and update button minimum sizes.

Modify `Darkness.Godot/scenes/StudyScene.tscn` to contain this complete layout:

```ini
[gd_scene load_steps=2 format=3 uid="uid://study_scene_uid"]

[ext_resource type="Script" path="res://src/UI/StudyScene.cs" id="1_study"]

[node name="StudyScene" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1_study")

[node name="ColorRect" type="ColorRect" parent="."]
layout_mode = 1
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
color = Color(0.129412, 0.129412, 0.129412, 1)

[node name="MainLayout" type="HBoxContainer" parent="."]
layout_mode = 1
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
offset_left = 20.0
offset_top = 20.0
offset_right = -20.0
offset_bottom = -20.0
grow_horizontal = 2
grow_vertical = 2
theme_override_constants/separation = 40

[node name="LeftPanel" type="ScrollContainer" parent="MainLayout"]
layout_mode = 2
size_flags_horizontal = 3

[node name="VBoxContainer" type="VBoxContainer" parent="MainLayout/LeftPanel"]
layout_mode = 2
size_flags_horizontal = 3
theme_override_constants/separation = 20

[node name="CharName" type="Label" parent="MainLayout/LeftPanel/VBoxContainer"]
layout_mode = 2
theme_override_font_sizes/font_size = 48
text = "CHARACTER NAME"
horizontal_alignment = 1

[node name="Points" type="Label" parent="MainLayout/LeftPanel/VBoxContainer"]
layout_mode = 2
theme_override_colors/font_color = Color(1, 0.843137, 0, 1)
theme_override_font_sizes/font_size = 32
text = "Attribute Points: 0"
horizontal_alignment = 1

[node name="StatsGrid" type="VBoxContainer" parent="MainLayout/LeftPanel/VBoxContainer"]
layout_mode = 2
theme_override_constants/separation = 15

[node name="Strength" type="HBoxContainer" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid"]
layout_mode = 2

[node name="MinusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Strength"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "-"

[node name="Label" type="Label" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Strength"]
layout_mode = 2
size_flags_horizontal = 3
theme_override_font_sizes/font_size = 28
text = "Strength: 10"
horizontal_alignment = 1
vertical_alignment = 1

[node name="PlusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Strength"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "+"

[node name="Dexterity" type="HBoxContainer" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid"]
layout_mode = 2

[node name="MinusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Dexterity"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "-"

[node name="Label" type="Label" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Dexterity"]
layout_mode = 2
size_flags_horizontal = 3
theme_override_font_sizes/font_size = 28
text = "Dexterity: 10"
horizontal_alignment = 1
vertical_alignment = 1

[node name="PlusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Dexterity"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "+"

[node name="Constitution" type="HBoxContainer" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid"]
layout_mode = 2

[node name="MinusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Constitution"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "-"

[node name="Label" type="Label" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Constitution"]
layout_mode = 2
size_flags_horizontal = 3
theme_override_font_sizes/font_size = 28
text = "Constitution: 10"
horizontal_alignment = 1
vertical_alignment = 1

[node name="PlusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Constitution"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "+"

[node name="Intelligence" type="HBoxContainer" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid"]
layout_mode = 2

[node name="MinusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Intelligence"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "-"

[node name="Label" type="Label" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Intelligence"]
layout_mode = 2
size_flags_horizontal = 3
theme_override_font_sizes/font_size = 28
text = "Intelligence: 10"
horizontal_alignment = 1
vertical_alignment = 1

[node name="PlusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Intelligence"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "+"

[node name="Wisdom" type="HBoxContainer" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid"]
layout_mode = 2

[node name="MinusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Wisdom"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "-"

[node name="Label" type="Label" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Wisdom"]
layout_mode = 2
size_flags_horizontal = 3
theme_override_font_sizes/font_size = 28
text = "Wisdom: 10"
horizontal_alignment = 1
vertical_alignment = 1

[node name="PlusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Wisdom"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "+"

[node name="Charisma" type="HBoxContainer" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid"]
layout_mode = 2

[node name="MinusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Charisma"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "-"

[node name="Label" type="Label" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Charisma"]
layout_mode = 2
size_flags_horizontal = 3
theme_override_font_sizes/font_size = 28
text = "Charisma: 10"
horizontal_alignment = 1
vertical_alignment = 1

[node name="PlusButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/StatsGrid/Charisma"]
custom_minimum_size = Vector2(100, 100)
layout_mode = 2
theme_override_font_sizes/font_size = 40
text = "+"

[node name="ActionButtons" type="HBoxContainer" parent="MainLayout/LeftPanel/VBoxContainer"]
layout_mode = 2
theme_override_constants/separation = 20
alignment = 1

[node name="SaveButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/ActionButtons"]
custom_minimum_size = Vector2(200, 80)
layout_mode = 2
theme_override_font_sizes/font_size = 32
text = "SAVE"

[node name="BackButton" type="Button" parent="MainLayout/LeftPanel/VBoxContainer/ActionButtons"]
custom_minimum_size = Vector2(200, 80)
layout_mode = 2
theme_override_font_sizes/font_size = 32
text = "BACK"

[node name="RightPanel" type="ScrollContainer" parent="MainLayout"]
layout_mode = 2
size_flags_horizontal = 3

[node name="DerivedStatsList" type="VBoxContainer" parent="MainLayout/RightPanel"]
layout_mode = 2
size_flags_horizontal = 3
theme_override_constants/separation = 15

[node name="Header" type="Label" parent="MainLayout/RightPanel/DerivedStatsList"]
layout_mode = 2
theme_override_colors/font_color = Color(0.5, 0.8, 1, 1)
theme_override_font_sizes/font_size = 36
text = "Derived Stats"
horizontal_alignment = 1

[node name="MaxHP" type="Label" parent="MainLayout/RightPanel/DerivedStatsList"]
layout_mode = 2
theme_override_font_sizes/font_size = 28
text = "Max HP: 0"

[node name="MaxMana" type="Label" parent="MainLayout/RightPanel/DerivedStatsList"]
layout_mode = 2
theme_override_font_sizes/font_size = 28
text = "Max Mana: 0"

[node name="ArmorClass" type="Label" parent="MainLayout/RightPanel/DerivedStatsList"]
layout_mode = 2
theme_override_font_sizes/font_size = 28
text = "Armor Class: 0"

[node name="Evasion" type="Label" parent="MainLayout/RightPanel/DerivedStatsList"]
layout_mode = 2
theme_override_font_sizes/font_size = 28
text = "Evasion: 0"

[node name="Accuracy" type="Label" parent="MainLayout/RightPanel/DerivedStatsList"]
layout_mode = 2
theme_override_font_sizes/font_size = 28
text = "Accuracy: 0"

[node name="MeleeDamage" type="Label" parent="MainLayout/RightPanel/DerivedStatsList"]
layout_mode = 2
theme_override_font_sizes/font_size = 28
text = "Melee Damage: 0"

[node name="MagicDamage" type="Label" parent="MainLayout/RightPanel/DerivedStatsList"]
layout_mode = 2
theme_override_font_sizes/font_size = 28
text = "Magic Damage: 0"

[node name="UnsavedDialog" type="ConfirmationDialog" parent="."]
title = "Unsaved Changes"
initial_position = 2
size = Vector2i(400, 150)
ok_button_text = "Save"
cancel_button_text = "Cancel"
dialog_text = "You have unsaved attribute changes. Do you want to save them before leaving?"

[node name="DiscardButton" type="Button" parent="UnsavedDialog"]
offset_left = 8.0
offset_top = 8.0
offset_right = 392.0
offset_bottom = 101.0
text = "Discard"
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/scenes/StudyScene.tscn
git commit -m "ui: update Study scene layout for mobile (side-by-side)"
```

---

### Task 2: Implement State Tracking and Logic in StudyScene.cs

**Files:**
- Modify: `Darkness.Godot/src/UI/StudyScene.cs`

- [ ] **Step 1: Replace StudyScene.cs with state tracking logic**

We need to rewrite the script to track the initial state, handle minus buttons, update derived stats on the right side, and wire up the `ConfirmationDialog`. We will overwrite the entire file to ensure everything aligns with the new UI.

Modify `Darkness.Godot/src/UI/StudyScene.cs`:

```csharp
using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class StudyScene : Control
{
    private ICharacterService _characterService = null!;
    private ISessionService _session = null!;
    private INavigationService _navigation = null!;

    private Character? _character;
    private Label _charNameLabel = null!;
    private Label _pointsLabel = null!;
    private Button _saveButton = null!;
    private Button _backButton = null!;
    private ConfirmationDialog _unsavedDialog = null!;
    private Button _discardButton = null!;

    // Derived stat labels
    private Label _lblMaxHP = null!;
    private Label _lblMaxMana = null!;
    private Label _lblArmorClass = null!;
    private Label _lblEvasion = null!;
    private Label _lblAccuracy = null!;
    private Label _lblMeleeDamage = null!;
    private Label _lblMagicDamage = null!;

    // Initial state snapshot
    private int _initialPoints;
    private int _initialStr;
    private int _initialDex;
    private int _initialCon;
    private int _initialInt;
    private int _initialWis;
    private int _initialCha;

    private Dictionary<string, (Label Label, Button PlusBtn, Button MinusBtn)> _statControls = new();

    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        var global = GetNode<Global>("/root/Global");
        _characterService = global.Services!.GetRequiredService<ICharacterService>();
        _session = global.Services!.GetRequiredService<ISessionService>();
        _navigation = global.Services!.GetRequiredService<INavigationService>();

        _character = _session.CurrentCharacter;
        if (_character == null) return;

        SnapshotInitialState();

        _charNameLabel = GetNode<Label>("MainLayout/LeftPanel/VBoxContainer/CharName");
        _pointsLabel = GetNode<Label>("MainLayout/LeftPanel/VBoxContainer/Points");
        _saveButton = GetNode<Button>("MainLayout/LeftPanel/VBoxContainer/ActionButtons/SaveButton");
        _backButton = GetNode<Button>("MainLayout/LeftPanel/VBoxContainer/ActionButtons/BackButton");
        _unsavedDialog = GetNode<ConfirmationDialog>("UnsavedDialog");
        _discardButton = GetNode<Button>("UnsavedDialog/DiscardButton");

        _lblMaxHP = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/MaxHP");
        _lblMaxMana = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/MaxMana");
        _lblArmorClass = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/ArmorClass");
        _lblEvasion = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/Evasion");
        _lblAccuracy = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/Accuracy");
        _lblMeleeDamage = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/MeleeDamage");
        _lblMagicDamage = GetNode<Label>("MainLayout/RightPanel/DerivedStatsList/MagicDamage");

        SetupStatControl("Strength");
        SetupStatControl("Dexterity");
        SetupStatControl("Constitution");
        SetupStatControl("Intelligence");
        SetupStatControl("Wisdom");
        SetupStatControl("Charisma");

        _saveButton.Pressed += async () => await SaveChangesAsync();
        _backButton.Pressed += OnBackButtonPressed;
        
        _unsavedDialog.Confirmed += async () => {
            await SaveChangesAsync();
            _navigation.GoBackAsync();
        };
        
        // Add custom Discard button to the dialog
        _unsavedDialog.AddButton("Discard", true, "discard_action");
        _unsavedDialog.CustomAction += (actionName) => {
            if (actionName == "discard_action")
            {
                DiscardChanges();
                _unsavedDialog.Hide();
                _navigation.GoBackAsync();
            }
        };

        UpdateUI();
    }

    private void SnapshotInitialState()
    {
        if (_character == null) return;
        _initialPoints = _character.AttributePoints;
        _initialStr = _character.Strength;
        _initialDex = _character.Dexterity;
        _initialCon = _character.Constitution;
        _initialInt = _character.Intelligence;
        _initialWis = _character.Wisdom;
        _initialCha = _character.Charisma;
    }

    private bool HasUnsavedChanges()
    {
        return _character != null && _character.AttributePoints != _initialPoints;
    }

    private void SetupStatControl(string attr)
    {
        var label = GetNode<Label>($"MainLayout/LeftPanel/VBoxContainer/StatsGrid/{attr}/Label");
        var plusBtn = GetNode<Button>($"MainLayout/LeftPanel/VBoxContainer/StatsGrid/{attr}/PlusButton");
        var minusBtn = GetNode<Button>($"MainLayout/LeftPanel/VBoxContainer/StatsGrid/{attr}/MinusButton");
        
        plusBtn.Pressed += () => ModifyAttribute(attr, 1);
        minusBtn.Pressed += () => ModifyAttribute(attr, -1);
        
        _statControls[attr] = (label, plusBtn, minusBtn);
    }

    private void UpdateUI()
    {
        if (_character == null) return;

        _charNameLabel.Text = _character.Name;
        _pointsLabel.Text = $"Attribute Points: {_character.AttributePoints}";

        UpdateStatRow("Strength", _character.Strength, _initialStr);
        UpdateStatRow("Dexterity", _character.Dexterity, _initialDex);
        UpdateStatRow("Constitution", _character.Constitution, _initialCon);
        UpdateStatRow("Intelligence", _character.Intelligence, _initialInt);
        UpdateStatRow("Wisdom", _character.Wisdom, _initialWis);
        UpdateStatRow("Charisma", _character.Charisma, _initialCha);

        UpdateDerivedStats();

        _saveButton.Disabled = !HasUnsavedChanges();
    }

    private void UpdateStatRow(string attr, int value, int initialValue)
    {
        var controls = _statControls[attr];
        controls.Label.Text = $"{attr}: {value}";
        controls.PlusBtn.Disabled = _character!.AttributePoints <= 0;
        controls.MinusBtn.Disabled = value <= initialValue;
    }

    private void UpdateDerivedStats()
    {
        if (_character == null) return;
        _lblMaxHP.Text = $"Max HP: {_character.MaxHP}";
        _lblMaxMana.Text = $"Max Mana: {_character.MaxMana}";
        _lblArmorClass.Text = $"Armor Class: {_character.ArmorClass}";
        _lblEvasion.Text = $"Evasion: {_character.Evasion}";
        _lblAccuracy.Text = $"Accuracy: {_character.Accuracy}";
        // Character does not natively expose raw BaseMelee/Magic damage without weapon,
        // but we can estimate or show raw attribute scaling if desired.
        // For now we'll just show the raw attribute values for damage scaling to keep it simple,
        // or call combat engine calculations if needed. 
        // We'll approximate purely based on attributes as done in standard D20.
        _lblMeleeDamage.Text = $"Melee Dmg Mod: +{(_character.Strength - 10) / 2}";
        _lblMagicDamage.Text = $"Magic Dmg Mod: +{(_character.Intelligence - 10) / 2}";
    }

    private void ModifyAttribute(string attribute, int delta)
    {
        if (_character == null) return;

        // If delta is positive, we must have points.
        if (delta > 0 && _character.AttributePoints <= 0) return;

        int currentValue = 0;
        int initialValue = 0;
        switch (attribute)
        {
            case "Strength": currentValue = _character.Strength; initialValue = _initialStr; break;
            case "Dexterity": currentValue = _character.Dexterity; initialValue = _initialDex; break;
            case "Constitution": currentValue = _character.Constitution; initialValue = _initialCon; break;
            case "Intelligence": currentValue = _character.Intelligence; initialValue = _initialInt; break;
            case "Wisdom": currentValue = _character.Wisdom; initialValue = _initialWis; break;
            case "Charisma": currentValue = _character.Charisma; initialValue = _initialCha; break;
        }

        // If delta is negative, we can't go below initial value.
        if (delta < 0 && currentValue <= initialValue) return;

        switch (attribute)
        {
            case "Strength": _character.Strength += delta; break;
            case "Dexterity": _character.Dexterity += delta; break;
            case "Constitution": _character.Constitution += delta; break;
            case "Intelligence": _character.Intelligence += delta; break;
            case "Wisdom": _character.Wisdom += delta; break;
            case "Charisma": _character.Charisma += delta; break;
        }

        _character.AttributePoints -= delta;
        _character.RecalculateDerivedStats();
        UpdateUI();
    }

    private async Task SaveChangesAsync()
    {
        if (_character == null || !HasUnsavedChanges()) return;
        
        await Task.Run(() => _characterService.SaveCharacter(_character));
        SnapshotInitialState();
        UpdateUI();
    }

    private void DiscardChanges()
    {
        if (_character == null) return;
        _character.Strength = _initialStr;
        _character.Dexterity = _initialDex;
        _character.Constitution = _initialCon;
        _character.Intelligence = _initialInt;
        _character.Wisdom = _initialWis;
        _character.Charisma = _initialCha;
        _character.AttributePoints = _initialPoints;
        _character.RecalculateDerivedStats();
    }

    private void OnBackButtonPressed()
    {
        if (HasUnsavedChanges())
        {
            _unsavedDialog.PopupCentered();
        }
        else
        {
            _navigation.GoBackAsync();
        }
    }
}
```

- [ ] **Step 2: Verify Build and Commit**

Run: `dotnet build Darkness.Godot/Darkness.Godot.csproj`
Expected: Build succeeds.

```bash
git add Darkness.Godot/src/UI/StudyScene.cs
git commit -m "feat: implement state tracking and unsaved changes protection in Study scene"
```