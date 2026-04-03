# Status Bar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a reusable floating status bar (HP, Mana, etc.) for both characters and enemies in combat and world scenes.

**Architecture:** A standalone `Control` scene (`StatusBar.tscn`) with a script (`StatusBar.cs`) that handles layout, coloring, and value updates. It uses `ProgressBar` for visual feedback.

**Tech Stack:** Godot 4.3 (C#), .NET 10.

---

### Task 1: Create the StatusBar Scene

**Files:**
- Create: `Darkness.Godot/scenes/StatusBar.tscn`

- [ ] **Step 1: Create the .tscn file**

```gdscript
[gd_scene load_steps=2 format=3 uid="uid://status_bar_id"]

[ext_resource type="Script" path="res://src/Game/StatusBar.cs" id="1_status"]

[node name="StatusBar" type="VBoxContainer"]
custom_minimum_size = Vector2(160, 40)
offset_right = 160.0
offset_bottom = 40.0
theme_override_constants/separation = 0
alignment = 1
script = ExtResource("1_status")

[node name="NameLabel" type="Label" parent="."]
layout_mode = 2
theme_override_colors/font_outline_color = Color(0, 0, 0, 1)
theme_override_constants/outline_size = 4
theme_override_font_sizes/font_size = 14
text = "Unit Name"
horizontal_alignment = 1

[node name="BarWrapper" type="Control" parent="."]
custom_minimum_size = Vector2(0, 16)
layout_mode = 2

[node name="ProgressBar" type="ProgressBar" parent="BarWrapper"]
custom_minimum_size = Vector2(0, 16)
layout_mode = 1
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
value = 50.0
show_percentage = false

[node name="ValueLabel" type="Label" parent="BarWrapper"]
layout_mode = 1
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
theme_override_colors/font_outline_color = Color(0, 0, 0, 1)
theme_override_constants/outline_size = 2
theme_override_font_sizes/font_size = 12
text = "50 / 100"
horizontal_alignment = 1
vertical_alignment = 1
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/scenes/StatusBar.tscn
git commit -m "feat: add StatusBar scene definition"
```

---

### Task 2: Implement StatusBar Logic

**Files:**
- Create: `Darkness.Godot/src/Game/StatusBar.cs`

- [ ] **Step 1: Create the C# script**

```csharp
using Godot;
using System;

namespace Darkness.Godot.Game;

public enum StatusType { HP, Mana, Stamina, EXP }

public partial class StatusBar : VBoxContainer
{
    private Label _nameLabel = null!;
    private ProgressBar _progressBar = null!;
    private Label _valueLabel = null!;

    [Export] public StatusType Type = StatusType.HP;
    [Export] public Color CustomColor = Colors.White;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("NameLabel");
        _progressBar = GetNode<ProgressBar>("BarWrapper/ProgressBar");
        _valueLabel = GetNode<Label>("BarWrapper/ValueLabel");
        
        UpdateTheme();
    }

    private void UpdateTheme()
    {
        var style = new StyleBoxFlat();
        style.SetCornerRadiusAll(2);
        
        Color barColor = Type switch
        {
            StatusType.HP => new Color(0.7f, 0.1f, 0.1f), // Dark Red
            StatusType.Mana => new Color(0.1f, 0.1f, 0.7f), // Dark Blue
            StatusType.Stamina => new Color(0.1f, 0.6f, 0.1f), // Green
            StatusType.EXP => new Color(0.7f, 0.7f, 0.1f), // Yellow
            _ => CustomColor
        };

        style.BgColor = barColor;
        _progressBar.AddThemeStyleboxOverride("fill", style);
    }

    public void Setup(string unitName, int current, int max, StatusType type = StatusType.HP)
    {
        Type = type;
        if (_nameLabel == null) _Ready(); // Ensure nodes are ready if called early
        
        _nameLabel.Text = unitName;
        UpdateValue(current, max);
        UpdateTheme();
    }

    public void UpdateValue(int current, int max)
    {
        if (_progressBar == null) _Ready();
        
        _progressBar.MaxValue = max;
        _progressBar.Value = current;
        _valueLabel.Text = $"{current} / {max}";

        // Simple color pulse if low health
        if (Type == StatusType.HP && (float)current / max < 0.25f)
        {
            _nameLabel.SelfModulate = Colors.Red;
        }
        else
        {
            _nameLabel.SelfModulate = Colors.White;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/src/Game/StatusBar.cs
git commit -m "feat: implement StatusBar logic and themes"
```

---

### Task 3: Integrate into BattleScene (Sprites)

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

- [ ] **Step 1: Update fields and UpdateSprites method**

```csharp
// Around line 30, add:
private List<StatusBar> _partyHealthBars = new();
private List<StatusBar> _enemyHealthBars = new();

// In UpdateSprites method, replace loops starting around line 100:
foreach (var character in _party)
{
    var wrapper = new VBoxContainer { CustomMinimumSize = new Vector2(200, 160) };
    _partyContainer.AddChild(wrapper);

    // Add Status Bar
    var statusBarScene = GD.Load<PackedScene>("res://scenes/StatusBar.tscn");
    var hpBar = statusBarScene.Instantiate<StatusBar>();
    wrapper.AddChild(hpBar);
    hpBar.Setup(character.Name, character.CurrentHP, character.MaxHP, StatusType.HP);
    _partyHealthBars.Add(hpBar);

    // Sprite Area
    var spriteContainer = new Control { CustomMinimumSize = new Vector2(200, 120) };
    wrapper.AddChild(spriteContainer);
    
    var sprite = layeredSpriteScene.Instantiate<LayeredSprite>();
    spriteContainer.AddChild(sprite);
    sprite.Position = new Vector2(100, 60);
    sprite.Scale = new Vector2(2.5f, 2.5f);
    _partySprites.Add(sprite);

    await sprite.SetupCharacter(character, _catalog, _fileSystem);
    sprite.Play("idle_right");
}

foreach (var enemy in _enemies)
{
    var wrapper = new VBoxContainer { CustomMinimumSize = new Vector2(200, 160) };
    _enemyContainer.AddChild(wrapper);

    // Add Status Bar
    var statusBarScene = GD.Load<PackedScene>("res://scenes/StatusBar.tscn");
    var hpBar = statusBarScene.Instantiate<StatusBar>();
    wrapper.AddChild(hpBar);
    hpBar.Setup(enemy.Name, enemy.CurrentHP, enemy.MaxHP, StatusType.HP);
    _enemyHealthBars.Add(hpBar);

    // Sprite Area
    var spriteContainer = new Control { CustomMinimumSize = new Vector2(200, 120) };
    wrapper.AddChild(spriteContainer);

    var sprite = layeredSpriteScene.Instantiate<LayeredSprite>();
    spriteContainer.AddChild(sprite);
    sprite.Position = new Vector2(100, 60);
    _enemySprites.Add(sprite);
    // ... rest of monster setup ...
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "feat: add StatusBars to BattleScene layout"
```

---

### Task 4: Update Health Bars during Combat

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

- [ ] **Step 1: Update ExecuteAttack to refresh bars**

```csharp
// In ExecuteAttack, after health is deducted:
target.CurrentHP -= damage;
if (enemyIndex < _enemyHealthBars.Count)
    _enemyHealthBars[enemyIndex].UpdateValue(target.CurrentHP, target.MaxHP);

// And for the counter-attack:
attacker.CurrentHP -= enemyDamage;
if (_partyHealthBars.Count > 0)
    _partyHealthBars[0].UpdateValue(attacker.CurrentHP, attacker.MaxHP);
```

- [ ] **Step 2: Run verification**
1. Build the project.
2. Enter a battle.
3. Attack an enemy.
4. Verify the red health bar decreases and the text "X / Y" updates.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "feat: update health bars during combat turns"
```
