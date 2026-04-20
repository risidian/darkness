# Daily Rewards & Encumbrance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the broken login reward system, implement a 12-month sequential login calendar, establish a master item catalog for weight management, and implement character encumbrance logic.

**Architecture:** We will create a new `ItemSeeder` to load a master item catalog from JSON and a `RewardSeeder` to load weighted random reward pools and monthly calendar lists into LiteDB. The `RewardService` will be updated to fetch from these collections, verify character capacity (`Strength * 20`), and correctly persist reward additions to the character's inventory.

**Tech Stack:** .NET 10, LiteDB, JSON.

---

### Task 1: Data Model Updates (Weight & Capacity)

**Files:**
- Modify: `Darkness.Core/Models/Item.cs`
- Modify: `Darkness.Core/Models/Character.cs`

- [ ] **Step 1: Update Item model to include Weight and Quantity**
Add `Weight` (int) and `Quantity` (int, default 1) to the `Item` class.

```csharp
namespace Darkness.Core.Models
{
    public class Item
    {
        // Existing fields...
        public int Weight { get; set; }
        public int Quantity { get; set; } = 1;
        // ... rest of fields
    }
}
```

- [ ] **Step 2: Update Character model with Capacity logic**
Add `TotalWeight` and `CarryCapacity` computed properties. Update `RecalculateDerivedStats` if needed.

```csharp
// Darkness.Core/Models/Character.cs

[BsonIgnore]
public int TotalWeight => Inventory?.Sum(i => i.Weight * i.Quantity) ?? 0;

[BsonIgnore]
public int CarryCapacity => Strength * 20;
```

- [ ] **Step 3: Verify and Commit**
Run `dotnet build Darkness.sln` to ensure no breaking changes in model usage.

---

### Task 2: Master Item Catalog & Reward Pools (JSON)

**Files:**
- Create: `Darkness.Godot/assets/data/items.json`
- Create: `Darkness.Godot/assets/data/random-rewards.json`
- Create: `Darkness.Godot/assets/data/login-calendar.json`

- [ ] **Step 1: Create Master Item Catalog**
Define base items with weights.

```json
[
  { "Name": "Health Potion", "Description": "Heals HP", "Type": "Consumable", "Weight": 1, "Value": 50 },
  { "Name": "Mana Potion", "Description": "Restores Mana", "Type": "Consumable", "Weight": 1, "Value": 50 },
  { "Name": "Iron Ore", "Description": "Raw material", "Type": "Material", "Weight": 5, "Value": 25 },
  { "Name": "Iron Scrap", "Description": "Bits of iron", "Type": "Material", "Weight": 2, "Value": 10 }
]
```

- [ ] **Step 2: Create Random Reward Pool**
Define weighted options.

```json
[
  { "ItemName": "Health Potion", "Weight": 10 },
  { "ItemName": "Mana Potion", "Weight": 10 },
  { "ItemName": "Iron Ore", "Weight": 5 }
]
```

- [ ] **Step 3: Create Login Calendar**
Define monthly reward lists.

```json
{
  "1": ["Health Potion", "Mana Potion", "Iron Ore", "Iron Scrap", ...],
  "2": ["Iron Scrap", "Health Potion", ...],
  ...
  "12": ["Mana Potion", "Iron Ore", ...]
}
```
*(Note: Ensure 31 items per month for simplicity, or vary based on month length)*

---

### Task 3: Seeders & Global Initialization

**Files:**
- Create: `Darkness.Core/Models/RandomReward.cs`
- Create: `Darkness.Core/Models/CalendarReward.cs`
- Create: `Darkness.Core/Services/ItemSeeder.cs`
- Create: `Darkness.Core/Services/RewardSeeder.cs`
- Modify: `Darkness.Godot/src/Core/Global.cs`

- [ ] **Step 1: Create helper models for seeding**
Define simple classes to match JSON structures.

- [ ] **Step 2: Implement ItemSeeder**
Loads `items.json` into LiteDB `items` collection.

- [ ] **Step 3: Implement RewardSeeder**
Loads `random-rewards.json` and `login-calendar.json` into LiteDB.

- [ ] **Step 4: Register in Global.cs**
Instantiate and call `Seed()` for both in `_Ready()`.

---

### Task 4: Reward Service Implementation (Correct Logic)

**Files:**
- Modify: `Darkness.Core/Interfaces/IRewardService.cs`
- Modify: `Darkness.Core/Services/RewardService.cs`

- [ ] **Step 1: Update IRewardService signature**
Change `CheckDailyRewardAsync` to return a `List<Item>` (for the two rewards).

- [ ] **Step 2: Implement Weighted Random Selection**
Rewrite the random part to use the `random_rewards` collection from LiteDB.

- [ ] **Step 3: Implement Calendar Index Selection**
Use `DateTime.Today.Day - 1` to pick from the monthly calendar list.

- [ ] **Step 4: Fix Inventory Addition & Persistence**
Crucial: Fetch `CurrentCharacter`, check `TotalWeight`, add items, and call `charCol.Update(character)`.

```csharp
// Example fix
if (character.TotalWeight + item.Weight <= character.CarryCapacity) {
    character.Inventory.Add(item);
    _db.GetCollection<Character>("characters").Update(character);
}
```

---

### Task 5: UI Integration & Verification

**Files:**
- Modify: `Darkness.Godot/src/UI/MainMenuScene.cs`
- Create: `Darkness.Tests/Services/RewardServiceEncumbranceTests.cs`

- [ ] **Step 1: Update MainMenuScene UI feedback**
Update `CheckDailyReward` to handle the multiple items returned and show distinct alerts.

- [ ] **Step 2: Write regression test for encumbrance**
Ensure rewards aren't added if weight limit is reached.

- [ ] **Step 3: Final Verification**
Build and run all tests sequentially.
Run: `dotnet test Darkness.Tests -p:ParallelizeTestCollections=false`
