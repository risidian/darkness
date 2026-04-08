# Economy, Loot, and Forge System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Gold, Loot, Combat Hotbar, and Forge systems while streamlining character creation.

**Architecture:** Data-driven approach using LiteDB and existing Godot UI patterns. Business logic resides in `Darkness.Core` with TDD-backed services.

**Tech Stack:** .NET 10, Godot 4.6.1, LiteDB, Moq, xUnit.

---

### Task 1: Core Data Model Updates

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`
- Modify: `Darkness.Core/Models/Enemy.cs`
- Modify: `Darkness.Core/Models/EnemySpawn.cs`
- Create: `Darkness.Core/Models/LootEntry.cs`
- Test: `Darkness.Tests/Models/CharacterTests.cs`

- [ ] **Step 1: Write the failing test for Gold and Hotbar**

```csharp
[Fact]
public void Character_InitializesWithZeroGoldAndEmptyHotbar()
{
    var character = new Character();
    Assert.Equal(0, character.Gold);
    Assert.NotNull(character.Hotbar);
    Assert.Equal(5, character.Hotbar.Length);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter CharacterTests`
Expected: FAIL (Gold and Hotbar properties missing)

- [ ] **Step 3: Update Character.cs**

```csharp
// Add these to Character class
public int Gold { get; set; } = 0;
public string?[] Hotbar { get; set; } = new string?[5];
```

- [ ] **Step 4: Update Enemy models and create LootEntry.cs**

```csharp
// Darkness.Core/Models/LootEntry.cs
namespace Darkness.Core.Models;
public class LootEntry {
    public string ItemName { get; set; } = string.Empty;
    public float Chance { get; set; } = 1.0f;
}

// Update Enemy.cs and EnemySpawn.cs to include:
public List<string> FixedDrops { get; set; } = new();
public List<LootEntry> RandomDrops { get; set; } = new();
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test Darkness.Tests --filter CharacterTests`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add Darkness.Core/Models/Character.cs Darkness.Core/Models/Enemy.cs Darkness.Core/Models/EnemySpawn.cs Darkness.Core/Models/LootEntry.cs Darkness.Tests/Models/CharacterTests.cs
git commit -m "feat: update core models for economy and loot"
```

---

### Task 2: Gold and Loot Awarding Logic

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`
- Test: `Darkness.Tests/Logic/BattleRewardTests.cs`

- [ ] **Step 1: Write the failing test for reward accumulation**

```csharp
[Fact]
public void CalculateRewards_SumsGoldAndLoot()
{
    // Arrange: Mock session and enemies with loot
    // Act: Process victory
    // Assert: Gold and items added to character
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter BattleRewardTests`
Expected: FAIL

- [ ] **Step 3: Update BattleScene.cs Victory method**

```csharp
// Inside Victory()
int totalGold = _enemies.Sum(e => e.GoldReward);
character.Gold += totalGold;

// Process Loot
foreach (var enemy in _enemies) {
    // Add FixedDrops
    // Roll for RandomDrops
}

victoryMsg += $"\n+{totalGold} Gold";
await _characterService.SaveCharacterAsync(character);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Darkness.Tests --filter BattleRewardTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "feat: implement gold and loot awarding in combat"
```

---

### Task 3: Streamlined Character Generation

**Files:**
- Modify: `Darkness.Godot/src/UI/CharacterGenScene.cs`
- Modify: `Darkness.Godot/scenes/CharacterGenScene.tscn`

- [ ] **Step 1: Remove Weapon/Armor dropdowns from UI**

Open `CharacterGenScene.tscn` and remove/hide `WeaponOption` and `ArmorOption` nodes.

- [ ] **Step 2: Update CharacterGenScene.cs logic**

```csharp
// Update OnClassChanged handler
private void OnClassChanged(int index) {
    string className = _classOption.GetItemText(index);
    var defaults = _catalog.GetDefaultAppearanceForClass(className);
    _character.WeaponType = defaults.WeaponType;
    _character.ArmorType = defaults.ArmorType;
    _character.ShieldType = defaults.ShieldType;
    UpdatePreview();
}
```

- [ ] **Step 3: Update Starter Inventory**

Ensure new characters start with 5 Health Potions and class gear.

- [ ] **Step 4: Verify UI updates correctly**

Manual verification: Changing class updates sprite preview with correct weapons/armor.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/src/UI/CharacterGenScene.cs Darkness.Godot/scenes/CharacterGenScene.tscn
git commit -m "ui: streamline character generation to be class-driven"
```

---

### Task 4: Inventory UI Overhaul (Preview & Gold)

**Files:**
- Modify: `Darkness.Godot/src/UI/InventoryScene.cs`
- Modify: `Darkness.Godot/scenes/InventoryScene.tscn`

- [ ] **Step 1: Add Gold label to UI**

Update `InventoryScene.tscn` to include a `GoldLabel`.

- [ ] **Step 2: Update InventoryScene.cs to show character preview**

```csharp
// Ensure _charSprite is updated on every equip/unequip
private async void UpdateCharacterPreview() {
    await _charSprite.SetupCharacter(_session.CurrentCharacter, _catalog, _fileSystem, _compositor);
    _charSprite.Play("idle_down");
}
```

- [ ] **Step 3: Add Hotbar Assignment UI**

Add "Map to Slot 1-5" buttons for items of type "Consumable".

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/src/UI/InventoryScene.cs Darkness.Godot/scenes/InventoryScene.tscn
git commit -m "ui: add character preview and gold display to inventory"
```

---

### Task 5: Battle Hotbar Implementation

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`
- Modify: `Darkness.Godot/scenes/BattleScene.tscn`

- [ ] **Step 1: Add Hotbar UI container to BattleScene.tscn**

Add an `HBoxContainer` named `HotbarContainer` with 5 `Button` nodes.

- [ ] **Step 2: Implement Hotbar Logic in BattleScene.cs**

```csharp
private void SetupHotbar() {
    for(int i=0; i<5; i++) {
        string itemName = character.Hotbar[i];
        // Find item in inventory, set icon/text
        // Connect Pressed signal to UseHotbarItem(i)
    }
}

private async void UseHotbarItem(int slot) {
    if (_isProcessingTurn) return;
    // Use item, remove from inventory if consumable
    // Consume turn:
    _turnManager.NextTurn();
    ProcessNextTurn();
}
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs Darkness.Godot/scenes/BattleScene.tscn
git commit -m "feat: implement battle hotbar and turn consumption"
```

---

### Task 6: The Forge (Crafting & Upgrading)

**Files:**
- Create: `Darkness.Core/Interfaces/ICraftingService.cs`
- Create: `Darkness.Core/Services/CraftingService.cs`
- Create: `Darkness.Godot/src/UI/ForgeScene.cs`
- Create: `Darkness.Godot/scenes/ForgeScene.tscn`

- [ ] **Step 1: Implement CraftingService logic**

Handle `CraftItem()`, `UpgradeItem()`, and `InfuseItem()`. Ensure stat calculations for +1, +2 are correct.

- [ ] **Step 2: Create Forge UI**

Implement Tabs for Craft, Upgrade, and Infusion.

- [ ] **Step 3: Run unit tests for crafting logic**

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/ Darkness.Godot/
git commit -m "feat: implement forge crafting and upgrade system"
```
