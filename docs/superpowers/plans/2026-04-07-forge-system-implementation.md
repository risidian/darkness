# Forge Crafting & Upgrade System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a comprehensive Forge system in the Core library and Godot UI, enabling item crafting from recipes, tiered item upgrades, and elemental infusions.

**Architecture:** 
1. **Core:** Extend `Item` model, update `ICraftingService` interface, and implement logic in `CraftingService` with TDD.
2. **Persistence:** Add `RecipeSeeder` and JSON seed data in `assets/data/`.
3. **UI:** Create a three-tabbed `ForgeScene` in Godot using `TabContainer` and `ItemList`.

**Tech Stack:** .NET 10, Godot 4.6.1, LiteDB, xUnit, Moq.

---

### Task 1: Core Data Model Extensions

**Files:**
- Modify: `Darkness.Core/Models/Item.cs`
- Modify: `Darkness.Core/Models/Recipe.cs` (Verify current state)

- [ ] **Step 1: Update Item.cs with Tier and Infusion**

```csharp
// Darkness.Core/Models/Item.cs
public int Tier { get; set; } = 0; // 0 = normal, 1 = +1, 2 = +2, etc.
public string? Infusion { get; set; } // Elemental/Bonus effect (e.g., "Fire", "Life Steal")
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Models/Item.cs
git commit -m "feat: add Tier and Infusion properties to Item model"
```

---

### Task 2: Crafting Service Interface & TDD Setup

**Files:**
- Modify: `Darkness.Core/Interfaces/ICraftingService.cs`
- Create: `Darkness.Tests/Services/CraftingServiceTests.cs`

- [ ] **Step 1: Update ICraftingService interface**

```csharp
// Darkness.Core/Interfaces/ICraftingService.cs
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface ICraftingService
    {
        Task<List<Recipe>> GetAvailableRecipesAsync();
        Task<bool> CraftItemAsync(Character character, Recipe recipe);
        Task<bool> UpgradeItemAsync(Character character, Item item, List<Item> materials, int gold);
        Task<bool> InfuseItemAsync(Character character, Item item, Item essence);
    }
}
```

- [ ] **Step 2: Write failing tests for CraftingService**

```csharp
// Darkness.Tests/Services/CraftingServiceTests.cs
using Xunit;
using Darkness.Core.Models;
using Darkness.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Tests.Services
{
    public class CraftingServiceTests
    {
        [Fact]
        public async Task CraftItem_Fails_If_Insufficient_Materials()
        {
            var service = new CraftingService();
            var character = new Character { Gold = 100 };
            var recipe = new Recipe 
            { 
                Name = "Iron Sword",
                Materials = new Dictionary<string, int> { { "Iron Ore", 5 } },
                Result = new Item { Name = "Iron Sword" }
            };

            var result = await service.CraftItemAsync(character, recipe);
            Assert.False(result);
        }

        [Fact]
        public async Task UpgradeItem_Increases_Tier_And_Stats()
        {
            var service = new CraftingService();
            var character = new Character { Gold = 1000 };
            var item = new Item { Name = "Steel Sword", AttackBonus = 10, Tier = 0 };
            
            var result = await service.UpgradeItemAsync(character, item, new List<Item>(), 500);
            
            Assert.True(result);
            Assert.Equal(1, item.Tier);
            Assert.True(item.AttackBonus > 10);
        }
    }
}
```

- [ ] **Step 3: Run tests to verify failure**

Run: `dotnet test Darkness.Tests --filter CraftingServiceTests`
Expected: FAIL (Methods missing or not implemented correctly)

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/Interfaces/ICraftingService.cs Darkness.Tests/Services/CraftingServiceTests.cs
git commit -m "test: add failing tests for crafting and upgrade logic"
```

---

### Task 3: Crafting Service Implementation

**Files:**
- Modify: `Darkness.Core/Services/CraftingService.cs`

- [ ] **Step 1: Implement CraftItemAsync**

Check materials in character inventory, consume them and gold, add result.

```csharp
public async Task<bool> CraftItemAsync(Character character, Recipe recipe)
{
    if (character == null || recipe == null) return false;
    
    // Check materials
    foreach (var material in recipe.Materials)
    {
        var count = character.Inventory.Count(i => i.Name == material.Key);
        if (count < material.Value) return false;
    }

    // Check gold (assuming recipes have a GoldCost property, add to Recipe.cs if needed)
    // For now, assume 0 or add property to Recipe
    
    // Consume materials
    foreach (var material in recipe.Materials)
    {
        for (int i = 0; i < material.Value; i++)
        {
            var itemToRemove = character.Inventory.First(item => item.Name == material.Key);
            character.Inventory.Remove(itemToRemove);
        }
    }

    // Add result
    character.Inventory.Add(new Item 
    { 
        Name = recipe.Result.Name,
        Description = recipe.Result.Description,
        Type = recipe.Result.Type,
        AttackBonus = recipe.Result.AttackBonus,
        DefenseBonus = recipe.Result.DefenseBonus,
        Value = recipe.Result.Value
    });
    
    return true;
}
```

- [ ] **Step 2: Implement UpgradeItemAsync**

```csharp
public async Task<bool> UpgradeItemAsync(Character character, Item item, List<Item> materials, int gold)
{
    if (character.Gold < gold) return false;
    
    character.Gold -= gold;
    item.Tier++;
    if (item.Type == "Weapon") item.AttackBonus += 2 * item.Tier;
    if (item.Type == "Armor") item.DefenseBonus += 2 * item.Tier;
    
    return true;
}
```

- [ ] **Step 3: Implement InfuseItemAsync**

```csharp
public async Task<bool> InfuseItemAsync(Character character, Item item, Item essence)
{
    if (!character.Inventory.Contains(essence)) return false;
    
    item.Infusion = essence.Name.Replace(" Essence", "");
    character.Inventory.Remove(essence);
    return true;
}
```

- [ ] **Step 4: Run tests to verify PASS**

Run: `dotnet test Darkness.Tests --filter CraftingServiceTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/CraftingService.cs
git commit -m "feat: implement crafting and upgrade logic in CraftingService"
```

---

### Task 4: UI Implementation in Godot

**Files:**
- Create: `Darkness.Godot/scenes/UI/ForgeScene.tscn`
- Create: `Darkness.Godot/src/UI/ForgeScene.cs`
- Modify: `Darkness.Godot/src/Core/Global.cs` (Already registered, but verify)

- [ ] **Step 1: Create ForgeScene.tscn**

Use a `TabContainer` with three tabs: "Craft", "Upgrade", "Infusion".
Include `ItemList` for recipes and inventory, and `Button` for actions.

- [ ] **Step 2: Implement ForgeScene.cs logic**

- [ ] **Step 3: Connect UI to CraftingService**

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/scenes/UI/ForgeScene.tscn Darkness.Godot/src/UI/ForgeScene.cs
git commit -m "ui: implement Forge UI with Craft, Upgrade, and Infusion tabs"
```

---

### Task 5: Final Verification & Wiring

- [ ] **Step 1: Add a "Forge" button to MainMenuScene.tscn**
- [ ] **Step 2: Wire up navigation to ForgeScene**
- [ ] **Step 3: Final build check**

Run: `dotnet build Darkness.sln`
Expected: SUCCESS

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "feat: wire up Forge system to MainMenu and finalize implementation"
```
