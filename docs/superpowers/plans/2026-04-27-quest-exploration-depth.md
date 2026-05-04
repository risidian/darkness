# Quest Exploration & Zone Depth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand the quest system to support "Travel" zones, multi-stage prerequisites (World Flags, Items), and quest rewards.

**Architecture:** 
- Introduce `WorldFlag` collection in LiteDB for persistent environmental state.
- Update `QuestStep` to include `Requirements` (using `BranchCondition`) and `Rewards` (list of `QuestReward`).
- Enhance `WorldScene` to support non-blocking `"travel"` steps and interactive zones.
- Extend `ConditionEvaluator` to support `world_flag` checks.

**Tech Stack:** .NET 10, LiteDB, Godot 4.6.1 (C#).

---

### Task 1: Core Models & Database Setup

**Files:**
- Create: `Darkness.Core/Models/QuestReward.cs`
- Create: `Darkness.Core/Models/WorldFlag.cs`
- Modify: `Darkness.Core/Models/QuestStep.cs`
- Modify: `Darkness.Core/Models/ZoneConfig.cs`
- Modify: `Darkness.Core/Services/LocalDatabaseService.cs`

- [ ] **Step 1: Create QuestReward model**
```csharp
namespace Darkness.Core.Models;

public class QuestReward
{
    public string Type { get; set; } = string.Empty; // "Item", "Experience", "Gold", "WorldFlag", "AttributePoint"
    public string Value { get; set; } = string.Empty;
    public int Amount { get; set; } = 1;
}
```

- [ ] **Step 2: Create WorldFlag model**
```csharp
namespace Darkness.Core.Models;

public class WorldFlag
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty; // Usually "true" or "false"
}
```

- [ ] **Step 3: Update QuestStep with Requirements and Rewards**
```csharp
// Modify Darkness.Core/Models/QuestStep.cs
// Add these properties:
public List<BranchCondition> Requirements { get; set; } = new();
public List<QuestReward> Rewards { get; set; } = new();
```

- [ ] **Step 4: Update ZoneConfig with Flags and ActionId**
```csharp
// Modify Darkness.Core/Models/ZoneConfig.cs
// Add these properties:
public string? RequiredFlag { get; set; } // Only show/enable if this flag is "true"
public string? ForbiddenFlag { get; set; } // Only show/enable if this flag is "false" or missing
```

- [ ] **Step 5: Register WorldFlag collection in LocalDatabaseService**
```csharp
// Modify Darkness.Core/Services/LocalDatabaseService.cs
// Add to constructor or initialization:
// BsonMapper.Global.Entity<WorldFlag>().Id(x => x.Key);
```

- [ ] **Step 6: Commit models**
```bash
git add Darkness.Core/Models/QuestReward.cs Darkness.Core/Models/WorldFlag.cs Darkness.Core/Models/QuestStep.cs Darkness.Core/Models/ZoneConfig.cs Darkness.Core/Services/LocalDatabaseService.cs
git commit -m "feat: add QuestReward and WorldFlag models and update existing quest/zone models"
```

---

### Task 2: Service Logic Enhancements

**Files:**
- Modify: `Darkness.Core/Services/ConditionEvaluator.cs`
- Modify: `Darkness.Core/Interfaces/IQuestService.cs`
- Modify: `Darkness.Core/Services/QuestService.cs`

- [ ] **Step 1: Update ConditionEvaluator to handle world_flag**
Modify `ConditionEvaluator.cs` to include a check for world flags. We will update the signatures to accept a flag checker function. Update existing callers in `QuestService.cs` and tests to pass `_ => false` or a mock provider as a default for now.

```csharp
public static bool EvaluateAll(List<BranchCondition>? conditions, Character character, List<string> completedChainIds, Func<string, bool> flagProvider)
{
    if (conditions == null || conditions.Count == 0)
        return true;
    return conditions.All(c => Evaluate(c, character, completedChainIds, flagProvider));
}

public static bool Evaluate(BranchCondition condition, Character character, List<string> completedChainIds, Func<string, bool> flagProvider)
{
    return condition.Type switch
    {
        "morality" => EvaluateNumeric(character.Morality, condition.Operator, condition.Value),
        "class" => condition.Operator == "==" && string.Equals(character.Class, condition.Value, StringComparison.OrdinalIgnoreCase),
        "has_item" => character.Inventory.Any(i => string.Equals(i.Name, condition.Value, StringComparison.OrdinalIgnoreCase)),
        "quest_completed" => completedChainIds.Contains(condition.Value),
        "world_flag" => flagProvider(condition.Value) == (condition.Operator != "!="), // Simplified: == true, != false
        _ => false
    };
}
```

- [ ] **Step 2: Add flag-aware methods to IQuestService**
Update `IQuestService.cs` and `QuestService.cs`:
```csharp
bool GetWorldFlag(string key);
void SetWorldFlag(string key, bool value);
```

- [ ] **Step 3: Implement requirement checking in AdvanceStep**
Update `QuestService.AdvanceStep`. Before returning the `nextStep`, ensure it meets its own `Requirements`. If not, return null (or the current step).

```csharp
// Inside AdvanceStep:
if (nextStep != null)
{
    var completedChainIds = GetCompletedChainIds(character.Id);
    if (!ConditionEvaluator.EvaluateAll(nextStep.Requirements, character, completedChainIds, GetWorldFlag))
    {
        Console.Error.WriteLine($"[QuestService] WARN: Requirements not met for next step '{nextStep.Id}'");
        return null; 
    }
}
```

- [ ] **Step 4: Implement reward granting in AdvanceStep**
When `nextStep` is confirmed (or when the chain completes), grant rewards from the *previous* step that just finished.

```csharp
private void GrantRewards(Character character, List<QuestReward> rewards)
{
    foreach (var reward in rewards)
    {
        switch (reward.Type.ToLower())
        {
            case "item":
                // character.Inventory.Add(new Item { Name = reward.Value });
                break;
            case "experience":
                // _levelingService.AwardExperience(character, int.Parse(reward.Value));
                break;
            case "gold":
                character.Gold += int.Parse(reward.Value);
                break;
            case "worldflag":
                SetWorldFlag(reward.Value, true);
                break;
        }
    }
}
```

- [ ] **Step 5: Write a test for Quest Rewards and Requirements**
Create `Darkness.Tests/Services/QuestExplorationTests.cs`.

- [ ] **Step 6: Commit service changes**
```bash
git add Darkness.Core/Services/ConditionEvaluator.cs Darkness.Core/Services/QuestService.cs Darkness.Core/Interfaces/IQuestService.cs Darkness.Tests/Services/QuestExplorationTests.cs
git commit -m "feat: implement quest rewards, requirements, and world flags logic"
```

---

### Task 3: WorldScene Integration

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Support "travel" step type**
Update `TriggerEncounter` to allow movement if the current step type is `"travel"`.
Update `UpdateVisuals` to correctly handle travel steps (non-blocking).

- [ ] **Step 2: Implement dynamic zone visibility**
In `WorldScene._Process`, when checking zones, respect `RequiredFlag` and `ForbiddenFlag` by querying `QuestService.GetWorldFlag()`.

- [ ] **Step 3: Handle "interact" trigger zones**
Allow a `Trigger` zone with `ActionId` to call `QuestService.AdvanceStep(character, chainId, actionId)` to specifically trigger a branch or interaction.

- [ ] **Step 4: Commit WorldScene changes**
```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: integrate travel steps and dynamic zones in WorldScene"
```

---

### Task 5: Verification & Content

- [ ] **Step 1: Create a demonstration quest**
Create `Darkness.Godot/assets/data/quests/exploration_demo.json` with a hub-and-spoke layout.

- [ ] **Step 2: Run all tests**
```bash
dotnet test Darkness.Tests -p:ParallelizeTestCollections=false
```

- [ ] **Step 3: Commit demo quest**
```bash
git add Darkness.Godot/assets/data/quests/exploration_demo.json
git commit -m "feat: add exploration demo quest chain"
```
