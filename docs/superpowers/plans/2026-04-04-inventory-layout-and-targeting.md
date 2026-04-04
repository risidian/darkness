# Inventory Layout & Click Targeting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a RuneScape-style character-preview inventory and a click-to-target system in the battle scene with visual highlights.

**Architecture:** We will redesign the `InventoryScene` to include a `LayeredSprite` for character previews that updates when equipment changes. The `BattleScene` will be updated to handle input events on enemy nodes, tracking a selected index and highlighting the target's `StatusBar` using a blue outline effect.

**Tech Stack:** Godot 4.6.1 (.NET 10), Godot UI/Input system.

---

### Task 1: Add Targeting Highlight to StatusBar

**Files:**
- Modify: `Darkness.Godot/src/Game/StatusBar.cs`

- [ ] **Step 1: Implement `SetHighlighted` method**

```csharp
// Darkness.Godot/src/Game/StatusBar.cs

public void SetHighlighted(bool highlighted)
{
    if (highlighted)
    {
        _nameLabel.AddThemeColorOverride("font_outline_color", Colors.RoyalBlue);
        _nameLabel.AddThemeConstantOverride("outline_size", 8);
    }
    else
    {
        _nameLabel.RemoveThemeColorOverride("font_outline_color");
        _nameLabel.RemoveThemeConstantOverride("outline_size");
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/src/Game/StatusBar.cs
git commit -m "feat: add SetHighlighted support to StatusBar"
```

### Task 2: Implement Click Targeting in BattleScene

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

- [ ] **Step 1: Add `_selectedEnemyIndex` field and `OnEnemyTapped` handler**

```csharp
// Darkness.Godot/src/Game/BattleScene.cs

private int _selectedEnemyIndex = 0;

private void OnEnemyTapped(int index)
{
    if (index >= _enemies.Count) return;
    
    // Clear previous highlight
    if (_selectedEnemyIndex < _enemyHealthBars.Count)
        _enemyHealthBars[_selectedEnemyIndex].SetHighlighted(false);
        
    _selectedEnemyIndex = index;
    
    // Set new highlight
    if (_selectedEnemyIndex < _enemyHealthBars.Count)
        _enemyHealthBars[_selectedEnemyIndex].SetHighlighted(true);
        
    GD.Print($"[Battle] Target selected: {_enemies[_selectedEnemyIndex].Name}");
}
```

- [ ] **Step 2: Update `UpdateSprites` to add input detection**

```csharp
// Inside UpdateSprites loop for enemies
int index = i; // Local copy for closure
spriteContainer.MouseFilter = Control.MouseFilterEnum.Stop;
spriteContainer.GuiInput += (@event) => {
    if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        OnEnemyTapped(index);
    else if (@event is InputEventScreenTouch touch && touch.Pressed)
        OnEnemyTapped(index);
};

// Default selection for the first living enemy
if (i == 0) _enemyHealthBars[0].SetHighlighted(true);
```

- [ ] **Step 3: Update `ExecuteAttack` to use `_selectedEnemyIndex` and refactor buttons**

```csharp
// Darkness.Godot/src/Game/BattleScene.cs -> ExecuteAttack
// Pass the action type instead of enemy index
private async void ExecuteAttack(string skillType)
{
    // ... logic to use _selectedEnemyIndex ...
    int actualIndex = _selectedEnemyIndex;
    // Ensure index is still valid (enemy might have died)
    if (actualIndex >= _enemies.Count) actualIndex = 0;
    if (_enemies.Count == 0) return;
    
    // ... existing attack logic using actualIndex ...
}
```

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "feat: implement click-to-target system in BattleScene"
```

### Task 3: Redesign Inventory UI with Character Preview

**Files:**
- Modify: `Darkness.Godot/scenes/InventoryScene.tscn`
- Modify: `Darkness.Godot/src/UI/InventoryScene.cs`

- [ ] **Step 1: Update `.tscn` layout to include an `HSplitContainer`**

```text
HSplitContainer
  PreviewArea (Left)
    SpritePreview (TextureRect or Control)
  BackpackArea (Right)
    ScrollContainer -> ItemList
```

- [ ] **Step 2: Update `InventoryScene.cs` to manage the character preview**

```csharp
// Darkness.Godot/src/UI/InventoryScene.cs

private LayeredSprite _charSprite = null!;

// Inside _Ready
var previewContainer = GetNode<Control>("MarginContainer/VBoxContainer/HSplitContainer/PreviewArea");
var layeredSpriteScene = GD.Load<PackedScene>("res://scenes/LayeredSprite.tscn");
_charSprite = layeredSpriteScene.Instantiate<LayeredSprite>();
previewContainer.AddChild(_charSprite);
_charSprite.Position = new Vector2(100, 100);
_charSprite.Scale = new Vector2(4, 4);

UpdateCharacterPreview();

// Method to update visual
private async void UpdateCharacterPreview()
{
    if (_session.CurrentCharacter != null)
    {
        await _charSprite.SetupCharacter(_session.CurrentCharacter, _catalog, _fileSystem);
        _charSprite.Play("idle_down");
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/scenes/InventoryScene.tscn Darkness.Godot/src/UI/InventoryScene.cs
git commit -m "feat: redesign inventory with RuneScape-style character preview"
```
