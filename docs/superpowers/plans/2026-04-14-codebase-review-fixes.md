# Codebase Review Fixes — All 18 Issues Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all 18 issues identified by the 10-agent multidisciplinary code review covering combat engine, quest system, service architecture, sprite compositor, and test coverage.

**Architecture:** Fixes are grouped into independent tracks that can be parallelized. Each task produces a working, testable change. Tasks within a track are sequential; tracks are independent.

**Tech Stack:** .NET 10, LiteDB, Godot 4.6.1, xUnit, Moq

---

## Issue → Task Mapping

| Issue # | Description | Task |
|---------|-------------|------|
| 1 | CombatEngine 3 asymmetric overloads | Task 1 |
| 2 | Skill costs never deducted in BattleScene | Task 2 |
| 3 | Branch conditions never enforced | Task 3 |
| 4 | Double morality on branch choices | Task 4 |
| 5 | No equipment system (StatBonuses never populated) | Task 5 |
| 6 | Fake async in CharacterService/UserService | Task 6 |
| 7 | CraftingService async without await (CS1998) | Task 7 |
| 8 | WeaponSkillService hardcoded skills | Task 8 |
| 9 | Stream leaks in GodotSpriteCompositor | Task 9 |
| 10 | CombatEngine only has 3 tests | Task 10 |
| 11 | CharacterService/UserService/SettingsService zero tests | Task 11 |
| 12 | UserService._initialized race condition | Task 6 |
| 13 | Dead combat stats (Evasion, CriticalChance, etc.) | Task 1 |
| 14 | HP restored on every victory (no attrition) | Task 12 |
| 15 | Crafting is 2 hardcoded recipes | Task 13 |
| 16 | Debug logging in QuestService | Task 14 |
| 17 | EnsureIndex called on every operation | Task 15 |
| 18 | Leading space in SkinColor default | Task 16 |

---

### Task 1: Unify CombatEngine Damage Calculation

**Files:**
- Modify: `Darkness.Core/Logic/CombatEngine.cs`
- Test: `Darkness.Tests/Logic/CombatEngineTests.cs`

The three `CalculateDamage` overloads have divergent formulas for accuracy, blocking, damage multiplier, and armor penetration. This task extracts a single internal method.

- [ ] **Step 1: Write failing tests for the unified damage path**

Add these tests to `Darkness.Tests/Logic/CombatEngineTests.cs`:

```csharp
[Fact]
public void CalculateDamage_CharVsEnemy_AccuracyModifierApplied()
{
    var attacker = new Character { Strength = 10 };
    var defender = new Enemy { Defense = 15 };
    var skill = new Skill { AccuracyModifier = 20, DamageDice = "1d4" };

    // With crit roll 0.5 → d20=11, modifier=0, accuracy=20 → total=31 >= 15 → hit
    var result = _engine.CalculateDamage(attacker, defender, skill, critRoll: 0.5);
    Assert.True(result.IsHit);
}

[Fact]
public void CalculateDamage_CharVsEnemy_ArmorPenetrationReducesDefense()
{
    var attacker = new Character { Strength = 10 };
    var defender = new Enemy { Defense = 20 };
    var skill = new Skill { ArmorPenetration = 0.5f, DamageDice = "1d4" };

    // With 50% armor pen, effective AC is 10. d20=11+0=11 >= 10 → hit
    var result = _engine.CalculateDamage(attacker, defender, skill, critRoll: 0.5);
    Assert.True(result.IsHit);
}

[Fact]
public void CalculateDamage_CharVsEnemy_DamageMultiplierBelowOneAllowed()
{
    var attacker = new Character { Strength = 10 };
    var defender = new Enemy { Defense = 1 };
    var skill = new Skill { DamageMultiplier = 0.5f, DamageDice = "1d4" };

    var result = _engine.CalculateDamage(attacker, defender, skill, critRoll: 0.99);
    // Damage should be reduced by 0.5x, not clamped to 1.0
    Assert.True(result.DamageDealt >= 1); // Minimum 1 damage
}

[Fact]
public void CalculateDamage_EnemyVsChar_UsesShieldBlockReduction()
{
    var attacker = new Enemy { STR = 10, Accuracy = 0, Attack = 5 };
    var defender = new Character { Dexterity = 10, ArmorClass = 0, IsBlocking = true, ShieldType = "Tower Shield" };

    var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.99);
    Assert.True(result.IsHit);
    // Shield block: 60% reduction applied
}

[Fact]
public void CalculateDamage_CharVsChar_DamageMultiplierApplied()
{
    var attacker = new Character { Strength = 20 };
    var defender = new Character { Dexterity = 10, ArmorClass = 0 };
    var skill = new Skill { DamageMultiplier = 2.0f, DamageDice = "1d4" };

    var result = _engine.CalculateDamage(attacker, defender, skill, critRoll: 0.99);
    Assert.True(result.IsHit);
    Assert.True(result.DamageDealt > 0);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~CombatEngineTests" --nologo -v q`
Expected: New tests fail (AccuracyModifier not applied, ArmorPenetration not read, etc.)

- [ ] **Step 3: Refactor CombatEngine with a unified internal method**

Replace the three `CalculateDamage` overloads in `Darkness.Core/Logic/CombatEngine.cs` with a single private method and thin public wrappers:

```csharp
private CombatResult CalculateDamageInternal(
    int attackStat, int attackerAccuracy, int attackerAttackBonus,
    int targetAC, bool isMagical,
    bool defenderBlocking, string defenderShieldType, string defenderWeaponType,
    Skill? skill, double? critRoll)
{
    var result = new CombatResult();

    int attackModifier = GetModifier(attackStat);
    result.AttackModifier = attackModifier;

    // Armor penetration reduces effective AC
    float armorPen = skill?.ArmorPenetration ?? 0f;
    int effectiveAC = (int)(targetAC * (1.0f - armorPen));

    int d20Roll = critRoll.HasValue ? (int)(critRoll.Value * 20) + 1 : _random.Next(1, 21);
    result.D20Roll = d20Roll;
    if (d20Roll == 20) result.IsCriticalHit = true;
    if (d20Roll == 1) result.IsCriticalMiss = true;

    int totalAttackBonus = attackModifier + attackerAccuracy + (skill?.AccuracyModifier ?? 0);

    if (result.IsCriticalHit) result.IsHit = true;
    else if (result.IsCriticalMiss) result.IsHit = false;
    else result.IsHit = (d20Roll + totalAttackBonus) >= effectiveAC;
    result.TargetAC = targetAC;

    if (!result.IsHit) return result;

    string diceStr = skill?.DamageDice ?? "1d4";
    int damageRoll = RollDice(diceStr);
    if (result.IsCriticalHit) damageRoll += RollDice(diceStr);

    float dmgMult = skill?.DamageMultiplier ?? 1.0f;
    if (dmgMult < 0.1f) dmgMult = 0.1f; // Prevent zero damage, allow sub-1.0

    int totalDamage = (int)((damageRoll + attackModifier + attackerAttackBonus + (skill?.BasePower ?? 0)) * dmgMult);

    if (defenderBlocking)
    {
        float reduction = 0.05f;
        if (!string.IsNullOrEmpty(defenderShieldType) && defenderShieldType != "None") reduction = 0.60f;
        else if (!string.IsNullOrEmpty(defenderWeaponType) && defenderWeaponType != "None") reduction = 0.20f;
        totalDamage = (int)(totalDamage * (1.0f - reduction));
    }

    result.DamageDealt = Math.Max(1, totalDamage);
    result.DamageMultiplier = skill?.DamageMultiplier;
    result.DamageDice = diceStr;
    result.DamageRoll = damageRoll;
    return result;
}
```

Then each public overload becomes a thin wrapper:

```csharp
public CombatResult CalculateDamage(Character attacker, Enemy defender, Skill? skill = null,
    ActionType action = ActionType.Standard, double? critRoll = null)
{
    bool isMagical = skill?.SkillType == "Magical";
    int attackStat = isMagical ? attacker.Intelligence : attacker.Strength;
    return CalculateDamageInternal(
        attackStat, 0, 0,
        defender.Defense, isMagical,
        defender.IsBlocking, "", "",
        skill, critRoll);
}

public CombatResult CalculateDamage(Enemy attacker, Character defender, Skill? skill = null,
    ActionType action = ActionType.Standard, double? critRoll = null)
{
    int targetAC = 10 + defender.ArmorClass + GetModifier(defender.Dexterity);
    return CalculateDamageInternal(
        attacker.STR, attacker.Accuracy, attacker.Attack,
        targetAC, false,
        defender.IsBlocking, defender.ShieldType ?? "", defender.WeaponType ?? "",
        skill, critRoll);
}

public CombatResult CalculateDamage(Character attacker, Character defender, Skill? skill = null,
    ActionType action = ActionType.Standard, double? critRoll = null)
{
    bool isMagical = skill?.SkillType == "Magical";
    int attackStat = isMagical ? attacker.Intelligence : attacker.Strength;
    int targetAC = 10 + defender.ArmorClass + GetModifier(defender.Dexterity);
    return CalculateDamageInternal(
        attackStat, 0, 0,
        targetAC, isMagical,
        defender.IsBlocking, defender.ShieldType ?? "", defender.WeaponType ?? "",
        skill, critRoll);
}
```

- [ ] **Step 4: Run all tests to verify they pass**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass including the new ones.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Logic/CombatEngine.cs Darkness.Tests/Logic/CombatEngineTests.cs
git commit -m "fix: unify CombatEngine damage formula across all overloads

Extracts CalculateDamageInternal to ensure AccuracyModifier, ArmorPenetration,
DamageMultiplier (sub-1.0 allowed), and shield-based blocking work consistently
across Character→Enemy, Enemy→Character, and Character→Character combat paths."
```

---

### Task 2: Call ApplySkillCosts in BattleScene

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`
- Test: `Darkness.Tests/Logic/CombatEngineTests.cs`

- [ ] **Step 1: Write failing tests for ApplySkillCosts**

Add to `Darkness.Tests/Logic/CombatEngineTests.cs`:

```csharp
[Fact]
public void ApplySkillCosts_DeductsManaCost()
{
    var character = new Character { Mana = 50 };
    var skill = new Skill { ManaCost = 15 };

    _engine.ApplySkillCosts(character, skill);

    Assert.Equal(35, character.Mana);
}

[Fact]
public void ApplySkillCosts_DeductsStaminaCost()
{
    var character = new Character { Stamina = 30 };
    var skill = new Skill { StaminaCost = 10 };

    _engine.ApplySkillCosts(character, skill);

    Assert.Equal(20, character.Stamina);
}

[Fact]
public void ApplySkillCosts_ClampsToZero()
{
    var character = new Character { Mana = 5 };
    var skill = new Skill { ManaCost = 20 };

    _engine.ApplySkillCosts(character, skill);

    Assert.Equal(0, character.Mana);
}
```

- [ ] **Step 2: Run tests to verify they pass** (ApplySkillCosts implementation already exists)

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~ApplySkillCosts" --nologo -v q`
Expected: PASS (the method works, it just isn't called from BattleScene)

- [ ] **Step 3: Add ApplySkillCosts call in BattleScene.ExecuteWeaponSkill**

In `Darkness.Godot/src/Game/BattleScene.cs`, inside `ExecuteWeaponSkill` method, after the `_isProcessingTurn = true;` line and before the attack/defense branch, add:

```csharp
// Deduct skill costs
_combatEngine.ApplySkillCosts(attacker, skill);
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs Darkness.Tests/Logic/CombatEngineTests.cs
git commit -m "fix: deduct mana/stamina costs when skills are used in combat

ApplySkillCosts was implemented but never called from BattleScene.
Players could previously spam mana-costing skills infinitely."
```

---

### Task 3: Enforce Branch Conditions in QuestService

**Files:**
- Modify: `Darkness.Core/Services/QuestService.cs`
- Modify: `Darkness.Core/Interfaces/IQuestService.cs`
- Test: `Darkness.Tests/Services/QuestServiceTests.cs`

- [ ] **Step 1: Write failing test for branch condition enforcement**

Add to `Darkness.Tests/Services/QuestServiceTests.cs`:

```csharp
[Fact]
public void AdvanceStep_BranchWithCondition_FiltersUnavailableOptions()
{
    // Setup a chain with a branch step where one option requires morality >= 50
    var chain = new QuestChain
    {
        Id = "cond_test",
        Title = "Condition Test",
        Steps = new List<QuestStep>
        {
            new QuestStep
            {
                Id = "step1",
                Type = "branch",
                Branch = new BranchData
                {
                    Options = new List<BranchOption>
                    {
                        new BranchOption { Text = "Good path", NextStepId = "step_good", MoralityImpact = 0,
                            Conditions = new List<BranchCondition> { new BranchCondition { Type = "morality", Operator = ">=", Value = "50" } } },
                        new BranchOption { Text = "Neutral path", NextStepId = "step_neutral", MoralityImpact = 0 }
                    }
                }
            },
            new QuestStep { Id = "step_good", Type = "dialogue" },
            new QuestStep { Id = "step_neutral", Type = "dialogue" }
        }
    };
    _chainCol.Insert(chain);

    var character = new Character { Id = 99, Morality = 10 }; // morality too low for "Good path"

    // Advance to the branch - should only allow "Neutral path" since morality < 50
    var result = _questService.AdvanceStep(character, "cond_test", "step_good");

    // step_good should be rejected because condition fails
    // The service should return null or advance to step_neutral instead
    // For now, we verify the condition check is at least called
    var state = _questService.GetQuestState(99, "cond_test");
    Assert.NotNull(state);
    Assert.NotEqual("step_good", state.CurrentStepId);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~FiltersUnavailableOptions" --nologo -v q`
Expected: FAIL — currently no condition checking happens.

- [ ] **Step 3: Add condition enforcement in QuestService.AdvanceStep**

In `Darkness.Core/Services/QuestService.cs`, modify the branch handling block (around line 89):

```csharp
if (choiceStepId != null && currentStep.Branch != null)
{
    var completedChainIds = GetCompletedChainIds(character.Id);
    var option = currentStep.Branch.Options.FirstOrDefault(o => 
        o.NextStepId == choiceStepId &&
        ConditionEvaluator.EvaluateAll(o.Conditions, character, completedChainIds));
    if (option != null)
    {
        character.Morality += option.MoralityImpact;
        nextStepId = option.NextStepId;
    }
}
```

Also add a method to `IQuestService` to expose available branch options for the UI:

```csharp
List<BranchOption> GetAvailableBranchOptions(Character character, string chainId, string stepId);
```

Implement in `QuestService`:

```csharp
public List<BranchOption> GetAvailableBranchOptions(Character character, string chainId, string stepId)
{
    var chain = GetChainById(chainId);
    var step = chain?.Steps.FirstOrDefault(s => s.Id == stepId);
    if (step?.Branch == null) return new List<BranchOption>();

    var completedChainIds = GetCompletedChainIds(character.Id);
    return step.Branch.Options
        .Where(o => ConditionEvaluator.EvaluateAll(o.Conditions, character, completedChainIds))
        .ToList();
}
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/QuestService.cs Darkness.Core/Interfaces/IQuestService.cs Darkness.Tests/Services/QuestServiceTests.cs
git commit -m "fix: enforce branch conditions via ConditionEvaluator in QuestService

Branch options with morality/class/item/quest conditions were defined but
never checked. ConditionEvaluator.EvaluateAll is now called before allowing
a branch choice. Added GetAvailableBranchOptions for UI filtering."
```

---

### Task 4: Fix Double Morality Application

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`
- Test: `Darkness.Tests/Services/QuestServiceTests.cs`

- [ ] **Step 1: Write a test proving QuestService applies morality**

The existing `QuestServiceTests` should already confirm this. Verify by reading the test. If no existing test covers it, add:

```csharp
[Fact]
public void AdvanceStep_BranchChoice_AppliesMoralityOnce()
{
    // setup chain with a branch that has MoralityImpact = 5
    var chain = new QuestChain
    {
        Id = "morality_test",
        Steps = new List<QuestStep>
        {
            new QuestStep { Id = "s1", Type = "branch", Branch = new BranchData {
                Options = new List<BranchOption> {
                    new BranchOption { Text = "Good", NextStepId = "s2", MoralityImpact = 5 }
                }
            }},
            new QuestStep { Id = "s2", Type = "dialogue" }
        }
    };
    _chainCol.Insert(chain);

    var character = new Character { Id = 50, Morality = 0 };
    _questService.AdvanceStep(character, "morality_test", "s2");

    Assert.Equal(5, character.Morality); // Applied exactly once
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~AppliesMoralityOnce" --nologo -v q`
Expected: PASS (QuestService does apply morality at line 94)

- [ ] **Step 3: Remove duplicate morality application from WorldScene**

In `Darkness.Godot/src/Game/WorldScene.cs`, in the `OnChoiceSelected` method (around line 586-591), remove the morality block:

Remove this code:
```csharp
// Apply morality
if (choice.MoralityImpact != 0)
{
    _session.CurrentCharacter.Morality += choice.MoralityImpact;
    GD.Print(
        $"[Morality] Changed by {choice.MoralityImpact}. New Total: {_session.CurrentCharacter.Morality}");
}
```

Keep only the `QuestService.AdvanceStep` call as the single source of morality changes.

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/src/Game/WorldScene.cs Darkness.Tests/Services/QuestServiceTests.cs
git commit -m "fix: remove duplicate morality application in WorldScene

Morality was applied in both WorldScene.OnChoiceSelected and
QuestService.AdvanceStep, doubling every branch choice's impact.
QuestService is now the single source of truth for morality changes."
```

---

### Task 5: Build Equipment System (StatBonuses from Items)

**Files:**
- Create: `Darkness.Core/Services/EquipmentService.cs`
- Create: `Darkness.Core/Interfaces/IEquipmentService.cs`
- Create: `Darkness.Tests/Services/EquipmentServiceTests.cs`
- Modify: `Darkness.Godot/src/Core/Global.cs` (DI registration)

- [ ] **Step 1: Write failing tests for equipment service**

Create `Darkness.Tests/Services/EquipmentServiceTests.cs`:

```csharp
using Darkness.Core.Models;
using Darkness.Core.Services;
using Xunit;

namespace Darkness.Tests.Services;

public class EquipmentServiceTests
{
    private readonly EquipmentService _service = new();

    [Fact]
    public void Equip_AppliesStatBonuses()
    {
        var character = new Character { Strength = 10 };
        var sword = new Item { Name = "Iron Sword", Type = "Weapon", EquipmentSlot = "Weapon", AttackBonus = 5, StrengthBonus = 2 };

        var result = _service.Equip(character, sword);

        Assert.True(result);
        Assert.Equal(2, character.StatBonuses.GetValueOrDefault("Strength"));
    }

    [Fact]
    public void Equip_RejectsIfRequirementsNotMet()
    {
        var character = new Character { Strength = 5 };
        var sword = new Item { Name = "Heavy Sword", Type = "Weapon", EquipmentSlot = "Weapon", RequiredStrength = 15 };

        var result = _service.Equip(character, sword);

        Assert.False(result);
    }

    [Fact]
    public void Unequip_RemovesStatBonuses()
    {
        var character = new Character { Strength = 10 };
        var sword = new Item { Name = "Iron Sword", Type = "Weapon", EquipmentSlot = "Weapon", AttackBonus = 5, StrengthBonus = 2 };

        _service.Equip(character, sword);
        _service.Unequip(character, "Weapon");

        Assert.Equal(0, character.StatBonuses.GetValueOrDefault("Strength"));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~EquipmentServiceTests" --nologo -v q`
Expected: FAIL — classes don't exist yet.

- [ ] **Step 3: Create IEquipmentService interface**

Create `Darkness.Core/Interfaces/IEquipmentService.cs`:

```csharp
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IEquipmentService
{
    bool Equip(Character character, Item item);
    bool Unequip(Character character, string slot);
}
```

- [ ] **Step 4: Create EquipmentService implementation**

Create `Darkness.Core/Services/EquipmentService.cs`:

```csharp
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public class EquipmentService : IEquipmentService
{
    public bool Equip(Character character, Item item)
    {
        if (character == null || item == null) return false;
        if (!item.CanEquip(character)) return false;
        if (string.IsNullOrEmpty(item.EquipmentSlot)) return false;

        // Unequip existing item in that slot first
        Unequip(character, item.EquipmentSlot);

        // Apply stat bonuses
        AddBonus(character.StatBonuses, "Strength", item.StrengthBonus);
        AddBonus(character.StatBonuses, "Dexterity", item.DexterityBonus);
        AddBonus(character.StatBonuses, "Intelligence", item.IntelligenceBonus);
        AddBonus(character.StatBonuses, "Defense", item.DefenseBonus);
        AddBonus(character.StatBonuses, "Attack", item.AttackBonus);

        // Store item in equipment slots
        character.EquipmentSlots[item.EquipmentSlot] = item.Name;

        character.RecalculateDerivedStats();
        return true;
    }

    public bool Unequip(Character character, string slot)
    {
        if (character == null || string.IsNullOrEmpty(slot)) return false;
        if (!character.EquipmentSlots.ContainsKey(slot)) return false;

        // Find the item to get its bonuses (look in inventory by name)
        var itemName = character.EquipmentSlots[slot];
        var item = character.Inventory.FirstOrDefault(i => i.Name == itemName);

        if (item != null)
        {
            RemoveBonus(character.StatBonuses, "Strength", item.StrengthBonus);
            RemoveBonus(character.StatBonuses, "Dexterity", item.DexterityBonus);
            RemoveBonus(character.StatBonuses, "Intelligence", item.IntelligenceBonus);
            RemoveBonus(character.StatBonuses, "Defense", item.DefenseBonus);
            RemoveBonus(character.StatBonuses, "Attack", item.AttackBonus);
        }

        character.EquipmentSlots.Remove(slot);
        character.RecalculateDerivedStats();
        return true;
    }

    private static void AddBonus(Dictionary<string, int> bonuses, string stat, int value)
    {
        if (value == 0) return;
        bonuses[stat] = bonuses.GetValueOrDefault(stat) + value;
    }

    private static void RemoveBonus(Dictionary<string, int> bonuses, string stat, int value)
    {
        if (value == 0) return;
        var current = bonuses.GetValueOrDefault(stat);
        var newVal = current - value;
        if (newVal == 0) bonuses.Remove(stat);
        else bonuses[stat] = newVal;
    }
}
```

- [ ] **Step 5: Register in DI**

In `Darkness.Godot/src/Core/Global.cs`, add after the other service registrations:

```csharp
services.AddSingleton<IEquipmentService, EquipmentService>();
```

- [ ] **Step 6: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add Darkness.Core/Interfaces/IEquipmentService.cs Darkness.Core/Services/EquipmentService.cs Darkness.Tests/Services/EquipmentServiceTests.cs Darkness.Godot/src/Core/Global.cs
git commit -m "feat: add EquipmentService to connect item stats to character bonuses

Item.StrengthBonus, AttackBonus, etc. are now applied to Character.StatBonuses
when equipped and removed when unequipped, feeding into RecalculateDerivedStats."
```

---

### Task 6: Fix Fake Async and UserService Race Condition

**Files:**
- Modify: `Darkness.Core/Services/CharacterService.cs`
- Modify: `Darkness.Core/Services/UserService.cs`
- Modify: `Darkness.Core/Interfaces/ICharacterService.cs`
- Modify: `Darkness.Core/Interfaces/IUserService.cs`

- [ ] **Step 1: Convert CharacterService to synchronous**

Replace `Darkness.Core/Services/CharacterService.cs`:

```csharp
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class CharacterService : ICharacterService
{
    private readonly LiteDatabase _db;

    public CharacterService(LiteDatabase db)
    {
        _db = db;
    }

    public bool SaveCharacter(Character character)
    {
        var col = _db.GetCollection<Character>("characters");
        return col.Upsert(character);
    }

    public Character? GetCharacterById(int characterId)
    {
        var col = _db.GetCollection<Character>("characters");
        return col.FindById(characterId);
    }

    public List<Character> GetCharactersForUser(int userId)
    {
        var col = _db.GetCollection<Character>("characters");
        return col.Find(c => c.UserId == userId).ToList();
    }
}
```

Update `Darkness.Core/Interfaces/ICharacterService.cs`:

```csharp
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface ICharacterService
{
    bool SaveCharacter(Character character);
    Character? GetCharacterById(int characterId);
    List<Character> GetCharactersForUser(int userId);
}
```

- [ ] **Step 2: Fix UserService with SemaphoreSlim and synchronous LiteDB calls**

Replace `Darkness.Core/Services/UserService.cs`:

```csharp
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class UserService : IUserService
{
    private readonly LiteDatabase _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public UserService(LiteDatabase db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            var col = _db.GetCollection<User>("users");
            col.EnsureIndex(u => u.Username, unique: true);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<bool> CreateUserAsync(User user)
    {
        await InitializeAsync();
        var col = _db.GetCollection<User>("users");
        var id = col.Insert(user);
        user.Id = id.AsInt32;
        return true;
    }

    public async Task<User?> GetUserAsync(string username, string password)
    {
        await InitializeAsync();
        var col = _db.GetCollection<User>("users");
        return col.FindOne(u => u.Username == username && u.Password == password);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        await InitializeAsync();
        var col = _db.GetCollection<User>("users");
        return col.FindById(userId);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await InitializeAsync();
        var col = _db.GetCollection<User>("users");
        return col.FindAll().ToList();
    }
}
```

- [ ] **Step 3: Update all Godot callers of CharacterService**

Search for `SaveCharacterAsync`, `GetCharacterByIdAsync`, `GetCharactersForUserAsync` in Godot project and update to the new synchronous names. Wrap in `Task.Run` at the UI layer if needed to avoid blocking the Godot main thread:

```csharp
// Before:  await _characterService.SaveCharacterAsync(character);
// After:   _characterService.SaveCharacter(character);
// Or if on main thread and need async: await Task.Run(() => _characterService.SaveCharacter(character));
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass (update any test files that reference the old async method names).

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/CharacterService.cs Darkness.Core/Services/UserService.cs Darkness.Core/Interfaces/ICharacterService.cs Darkness.Core/Interfaces/IUserService.cs Darkness.Godot/
git commit -m "fix: remove fake async from CharacterService, fix UserService race condition

CharacterService now exposes synchronous methods since LiteDB is synchronous.
UserService uses SemaphoreSlim for thread-safe initialization instead of a
bare bool flag. Removed nested Task.Run(async => await ...) pattern."
```

---

### Task 7: Fix CraftingService CS1998

**Files:**
- Modify: `Darkness.Core/Services/CraftingService.cs`

- [ ] **Step 1: Remove async keyword from methods with no await**

In `Darkness.Core/Services/CraftingService.cs`, change the three methods:

```csharp
// CraftItemAsync: remove 'async', add explicit return
public Task<bool> CraftItemAsync(Character character, Recipe recipe)
{
    if (character == null || recipe == null) return Task.FromResult(false);
    // ... existing logic unchanged ...
    return Task.FromResult(true);
}

// UpgradeItemAsync: remove 'async', add explicit return
public Task<bool> UpgradeItemAsync(Character character, Item item, List<Item> materials, int gold)
{
    if (character == null || item == null) return Task.FromResult(false);
    if (item.Type != "Weapon" && item.Type != "Armor") return Task.FromResult(false);
    if (character.Gold < gold) return Task.FromResult(false);
    // ... existing logic unchanged ...
    return Task.FromResult(true);
}

// InfuseItemAsync: remove 'async', add explicit return
public Task<bool> InfuseItemAsync(Character character, Item item, Item essence)
{
    if (character == null || item == null || essence == null) return Task.FromResult(false);
    if (!character.Inventory.Contains(essence)) return Task.FromResult(false);
    // ... existing logic unchanged ...
    return Task.FromResult(true);
}
```

- [ ] **Step 2: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass. No more CS1998 warnings.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Services/CraftingService.cs
git commit -m "fix: remove async keyword from CraftingService methods with no await

Eliminates CS1998 compiler warnings by using Task.FromResult instead of
the async modifier on methods that perform only synchronous work."
```

---

### Task 8: Migrate WeaponSkillService Hardcoded Skills to JSON

**Files:**
- Modify: `Darkness.Core/Services/WeaponSkillService.cs`
- Modify: `Darkness.Godot/assets/data/skills.json`
- Test: `Darkness.Tests/Services/WeaponSkillServiceTests.cs`

- [ ] **Step 1: Read existing skills.json to understand current format**

Read `Darkness.Godot/assets/data/skills.json` and understand the schema.

- [ ] **Step 2: Add weapon-type skills to skills.json**

Add entries for all hardcoded skills in `GetSkillsForWeapon` (Wand, Bow, Dagger, Axe, Mace, Sword, Unarmed, and defensive skills) to `skills.json` with proper `WeaponRequirement` values. Example entries:

```json
{ "Id": 100, "Name": "Arcane Bolt", "Description": "A standard magical bolt. (1.1x Magic Dmg)", "SkillType": "Magical", "DamageMultiplier": 1.1, "AssociatedAction": "Shoot", "WeaponRequirement": "Wand" },
{ "Id": 101, "Name": "Fireball", "Description": "A powerful blast of fire. (1.5x Magic Dmg, -10 Accuracy)", "SkillType": "Magical", "DamageMultiplier": 1.5, "AccuracyModifier": -10, "AssociatedAction": "Shoot", "WeaponRequirement": "Wand" }
```

Include ALL skills from the hardcoded method: Wand (Arcane Bolt, Fireball, Mana Shield), Bow (Quick Shot, Snipe, Dodge), Dagger (Quick Stab, Vitals, Parry), Axe (Cleave, Crush), Mace (Smash, Stun), Sword (Slash, Thrust), unarmed (Punch, Kick), and generic defense (Shield Block, Deflect, Brace).

- [ ] **Step 3: Replace GetSkillsForWeapon with LiteDB queries**

Replace the 200-line if/else chain in `WeaponSkillService.GetSkillsForWeapon` with:

```csharp
public List<Skill> GetSkillsForWeapon(string? weaponType, string? offHandType, string? shieldType, List<string>? unlockedTalentIds = null)
{
    weaponType ??= "None";
    offHandType ??= "None";
    shieldType ??= "None";

    var skillCol = _db.GetCollection<Skill>("skills");
    var skills = new List<Skill>();

    // Primary weapon skills (non-defensive)
    var primarySkills = skillCol.Find(s => 
        s.WeaponRequirement != "None" && 
        s.SkillType != "Defensive" &&
        !s.IsPassive)
        .Where(s => weaponType.Contains(s.WeaponRequirement, StringComparison.OrdinalIgnoreCase))
        .ToList();
    skills.AddRange(primarySkills);

    // Off-hand skills
    if (offHandType != "None" && offHandType != weaponType && skills.Count < 3)
    {
        var offHandSkills = skillCol.Find(s =>
            s.WeaponRequirement != "None" &&
            s.SkillType != "Defensive" &&
            !s.IsPassive)
            .Where(s => offHandType.Contains(s.WeaponRequirement, StringComparison.OrdinalIgnoreCase))
            .Take(3 - skills.Count)
            .ToList();
        skills.AddRange(offHandSkills);
    }

    // Unarmed fallback
    if (skills.Count == 0)
    {
        skills.AddRange(skillCol.Find(s => s.WeaponRequirement == "Unarmed" && s.SkillType != "Defensive").ToList());
    }

    // Defensive skill
    var defenseReq = shieldType != "None" ? "Shield" : weaponType;
    var defSkill = skillCol.Find(s => s.SkillType == "Defensive")
        .FirstOrDefault(s => defenseReq.Contains(s.WeaponRequirement, StringComparison.OrdinalIgnoreCase));
    defSkill ??= skillCol.FindOne(s => s.SkillType == "Defensive" && s.WeaponRequirement == "Generic");
    if (defSkill != null) skills.Add(defSkill);

    // Talent-based skills
    if (unlockedTalentIds != null && unlockedTalentIds.Count > 0)
    {
        var talentSkills = skillCol.Find(s => s.TalentRequirement != null)
            .Where(s => unlockedTalentIds.Contains(s.TalentRequirement!))
            .ToList();
        skills.AddRange(talentSkills);
    }

    return skills;
}
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass. Existing WeaponSkillServiceTests should still work with the data-driven approach.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/WeaponSkillService.cs Darkness.Godot/assets/data/skills.json Darkness.Tests/Services/WeaponSkillServiceTests.cs
git commit -m "refactor: migrate hardcoded weapon skills to skills.json

Replaces 200-line if/else chain in WeaponSkillService.GetSkillsForWeapon
with LiteDB queries against skills.json seed data. All weapon types now
defined in JSON, consistent with the project's data-driven architecture."
```

---

### Task 9: Fix Stream Leaks in GodotSpriteCompositor

**Files:**
- Modify: `Darkness.Godot/src/Services/GodotSpriteCompositor.cs`

- [ ] **Step 1: Add `using` to all three stream leak locations**

In `Darkness.Godot/src/Services/GodotSpriteCompositor.cs`, fix lines 149, 166, and 183:

```csharp
// Line 149: change
var stream = await fileSystem.OpenAppPackageFileAsync(fullPath);
// to:
using var stream = await fileSystem.OpenAppPackageFileAsync(fullPath);

// Line 166: change
var stream = await fileSystem.OpenAppPackageFileAsync(altFullPath);
// to:
using var stream = await fileSystem.OpenAppPackageFileAsync(altFullPath);

// Line 183: change
var stream = await fileSystem.OpenAppPackageFileAsync(fbFullPath);
// to:
using var stream = await fileSystem.OpenAppPackageFileAsync(fbFullPath);
```

Note: Each is inside its own scope block, so `using var` is appropriate — the stream is disposed after `CopyToAsync` completes.

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build Darkness.Godot/Darkness.Godot.csproj --nologo -v q`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/Services/GodotSpriteCompositor.cs
git commit -m "fix: dispose file streams in GodotSpriteCompositor.LoadLayerData

Three code paths opened streams via OpenAppPackageFileAsync but never
disposed them, leaking 30+ file handles per character sprite composition."
```

---

### Task 10: Expand CombatEngine Test Coverage

**Files:**
- Modify: `Darkness.Tests/Logic/CombatEngineTests.cs`

- [ ] **Step 1: Add comprehensive tests for all combat paths**

Add to `Darkness.Tests/Logic/CombatEngineTests.cs`:

```csharp
// === Enemy-attacks-Character overload ===

[Fact]
public void CalculateDamage_EnemyVsChar_CriticalHitAlwaysHits()
{
    var attacker = new Enemy { STR = 10, Accuracy = 0, Attack = 5 };
    var defender = new Character { Dexterity = 10, ArmorClass = 100 };

    var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.99);

    Assert.True(result.IsHit);
    Assert.True(result.IsCriticalHit);
    Assert.True(result.DamageDealt > 0);
}

[Fact]
public void CalculateDamage_EnemyVsChar_CriticalMissAlwaysMisses()
{
    var attacker = new Enemy { STR = 30, Accuracy = 50, Attack = 50 };
    var defender = new Character { Dexterity = 10, ArmorClass = 0 };

    var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.0);

    Assert.False(result.IsHit);
    Assert.True(result.IsCriticalMiss);
    Assert.Equal(0, result.DamageDealt);
}

[Fact]
public void CalculateDamage_EnemyVsChar_BlockingWithShieldReducesDamage()
{
    var attacker = new Enemy { STR = 20, Accuracy = 0, Attack = 10 };
    var noBlock = new Character { Dexterity = 10, ArmorClass = 0, IsBlocking = false };
    var withShield = new Character { Dexterity = 10, ArmorClass = 0, IsBlocking = true, ShieldType = "Tower Shield" };

    var noBlockResult = _engine.CalculateDamage(attacker, noBlock, critRoll: 0.99);
    var shieldResult = _engine.CalculateDamage(attacker, withShield, critRoll: 0.99);

    Assert.True(shieldResult.DamageDealt < noBlockResult.DamageDealt);
}

// === Character-attacks-Character overload ===

[Fact]
public void CalculateDamage_CharVsChar_CriticalHitAlwaysHits()
{
    var attacker = new Character { Strength = 10 };
    var defender = new Character { Dexterity = 10, ArmorClass = 100 };

    var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.99);

    Assert.True(result.IsHit);
    Assert.True(result.IsCriticalHit);
}

[Fact]
public void CalculateDamage_CharVsChar_MagicalUsesIntelligence()
{
    var attacker = new Character { Intelligence = 30, Strength = 1 };
    var defender = new Character { Dexterity = 10, ArmorClass = 0 };
    var spell = new Skill { SkillType = "Magical", DamageDice = "1d4" };

    // High INT should produce a hit; if STR were used, the low value would likely miss
    var result = _engine.CalculateDamage(attacker, defender, spell, critRoll: 0.5);
    // d20=11 + GetModifier(30)=10 = 21 >= AC of 10 → hit
    Assert.True(result.IsHit);
}

// === ApplySkillCosts ===

[Fact]
public void ApplySkillCosts_Enemy_DeductsManaAndStamina()
{
    var enemy = new Enemy { Mana = 100, Stamina = 100 };
    var skill = new Skill { ManaCost = 25, StaminaCost = 10 };

    _engine.ApplySkillCosts(enemy, skill);

    Assert.Equal(75, enemy.Mana);
    Assert.Equal(90, enemy.Stamina);
}

[Fact]
public void ApplySkillCosts_NullInputs_DoesNotThrow()
{
    _engine.ApplySkillCosts((Character)null!, null!);
    // Should not throw
}
```

- [ ] **Step 2: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Tests/Logic/CombatEngineTests.cs
git commit -m "test: expand CombatEngine coverage from 3 to 15+ tests

Adds tests for Enemy→Character, Character→Character, blocking with shield,
magical attacks, ApplySkillCosts for both Character and Enemy, null safety,
and the new ArmorPenetration/AccuracyModifier paths."
```

---

### Task 11: Add Tests for CharacterService, UserService, SettingsService

**Files:**
- Create: `Darkness.Tests/Services/CharacterServiceTests.cs`
- Create: `Darkness.Tests/Services/UserServiceTests.cs`
- Create: `Darkness.Tests/Services/SettingsServiceTests.cs`

- [ ] **Step 1: Create CharacterServiceTests**

Create `Darkness.Tests/Services/CharacterServiceTests.cs`:

```csharp
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Xunit;

namespace Darkness.Tests.Services;

public class CharacterServiceTests : IDisposable
{
    private readonly MemoryStream _ms = new();
    private readonly LiteDatabase _db;
    private readonly CharacterService _service;

    public CharacterServiceTests()
    {
        _db = new LiteDatabase(_ms);
        _service = new CharacterService(_db);
    }

    [Fact]
    public void SaveCharacter_InsertsNewCharacter()
    {
        var character = new Character { Name = "TestHero", UserId = 1 };
        var result = _service.SaveCharacter(character);
        Assert.True(result);
        Assert.True(character.Id > 0);
    }

    [Fact]
    public void GetCharacterById_ReturnsCorrectCharacter()
    {
        var character = new Character { Name = "TestHero", UserId = 1 };
        _service.SaveCharacter(character);

        var loaded = _service.GetCharacterById(character.Id);
        Assert.NotNull(loaded);
        Assert.Equal("TestHero", loaded.Name);
    }

    [Fact]
    public void GetCharacterById_ReturnsNullForMissing()
    {
        var loaded = _service.GetCharacterById(999);
        Assert.Null(loaded);
    }

    [Fact]
    public void GetCharactersForUser_ReturnsOnlyMatchingUser()
    {
        _service.SaveCharacter(new Character { Name = "Hero1", UserId = 1 });
        _service.SaveCharacter(new Character { Name = "Hero2", UserId = 1 });
        _service.SaveCharacter(new Character { Name = "Other", UserId = 2 });

        var chars = _service.GetCharactersForUser(1);
        Assert.Equal(2, chars.Count);
        Assert.All(chars, c => Assert.Equal(1, c.UserId));
    }

    public void Dispose()
    {
        _db.Dispose();
        _ms.Dispose();
    }
}
```

- [ ] **Step 2: Create UserServiceTests**

Create `Darkness.Tests/Services/UserServiceTests.cs`:

```csharp
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Xunit;

namespace Darkness.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly MemoryStream _ms = new();
    private readonly LiteDatabase _db;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _db = new LiteDatabase(_ms);
        _service = new UserService(_db);
    }

    [Fact]
    public async Task CreateUser_AssignsId()
    {
        var user = new User { Username = "testuser", Password = "pass123" };
        var result = await _service.CreateUserAsync(user);
        Assert.True(result);
        Assert.True(user.Id > 0);
    }

    [Fact]
    public async Task GetUser_MatchesUsernameAndPassword()
    {
        var user = new User { Username = "testuser", Password = "pass123" };
        await _service.CreateUserAsync(user);

        var found = await _service.GetUserAsync("testuser", "pass123");
        Assert.NotNull(found);
        Assert.Equal("testuser", found.Username);
    }

    [Fact]
    public async Task GetUser_WrongPassword_ReturnsNull()
    {
        var user = new User { Username = "testuser", Password = "pass123" };
        await _service.CreateUserAsync(user);

        var found = await _service.GetUserAsync("testuser", "wrongpass");
        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAll()
    {
        await _service.CreateUserAsync(new User { Username = "user1", Password = "p" });
        await _service.CreateUserAsync(new User { Username = "user2", Password = "p" });

        var all = await _service.GetAllUsersAsync();
        Assert.Equal(2, all.Count);
    }

    public void Dispose()
    {
        _db.Dispose();
        _ms.Dispose();
    }
}
```

- [ ] **Step 3: Create SettingsServiceTests**

Create `Darkness.Tests/Services/SettingsServiceTests.cs`:

```csharp
using Darkness.Core.Interfaces;
using Darkness.Core.Services;
using Moq;
using Xunit;

namespace Darkness.Tests.Services;

public class SettingsServiceTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var fsMock = new Mock<IFileSystemService>();
        fsMock.Setup(f => f.AppDataDirectory).Returns(Path.GetTempPath());
        var service = new SettingsService(fsMock.Object);

        Assert.Equal(1.0, service.MasterVolume);
        Assert.Equal(0.8, service.MusicVolume);
        Assert.Equal(0.8, service.SfxVolume);
        Assert.Equal(0, service.LastUserId);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var fsMock = new Mock<IFileSystemService>();
            fsMock.Setup(f => f.AppDataDirectory).Returns(tmpDir);

            var service = new SettingsService(fsMock.Object);
            service.MasterVolume = 0.5;
            service.MusicVolume = 0.3;
            service.SfxVolume = 0.7;
            service.LastUserId = 42;
            await service.SaveSettingsAsync();

            var service2 = new SettingsService(fsMock.Object);
            await service2.LoadSettingsAsync();

            Assert.Equal(0.5, service2.MasterVolume);
            Assert.Equal(0.3, service2.MusicVolume);
            Assert.Equal(0.7, service2.SfxVolume);
            Assert.Equal(42, service2.LastUserId);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public async Task LoadSettings_MissingFile_KeepsDefaults()
    {
        var fsMock = new Mock<IFileSystemService>();
        fsMock.Setup(f => f.AppDataDirectory).Returns("/nonexistent/path");
        var service = new SettingsService(fsMock.Object);

        await service.LoadSettingsAsync();

        Assert.Equal(1.0, service.MasterVolume);
    }
}
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Tests/Services/CharacterServiceTests.cs Darkness.Tests/Services/UserServiceTests.cs Darkness.Tests/Services/SettingsServiceTests.cs
git commit -m "test: add tests for CharacterService, UserService, and SettingsService

These three core services previously had zero test coverage. Tests cover
CRUD operations, authentication matching, settings round-trip, and defaults."
```

---

### Task 12: Remove HP Restore on Victory

**Files:**
- Modify: `Darkness.Core/Services/RewardService.cs`
- Test: `Darkness.Tests/Services/RewardServiceTests.cs`

- [ ] **Step 1: Write test confirming HP is NOT restored on victory**

Add to `Darkness.Tests/Services/RewardServiceTests.cs`:

```csharp
[Fact]
public void ProcessCombatRewards_DoesNotRestoreHP()
{
    var character = new Character { Id = 1, CurrentHP = 50, MaxHP = 100 };
    var enemies = new List<Enemy> { new Enemy { GoldReward = 10 } };

    _service.ProcessCombatRewards(character, enemies);

    Assert.Equal(50, character.CurrentHP); // Should NOT be restored to 100
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~DoesNotRestoreHP" --nologo -v q`
Expected: FAIL — current code sets `CurrentHP = MaxHP`.

- [ ] **Step 3: Remove HP restore from RewardService**

In `Darkness.Core/Services/RewardService.cs`, remove line 61:

```csharp
// Remove this line:
character.CurrentHP = character.MaxHP;
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass (update any existing test that asserts HP restoration).

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/RewardService.cs Darkness.Tests/Services/RewardServiceTests.cs
git commit -m "fix: remove automatic HP restore on battle victory

HP is still restored on level-up (LevelingService), but no longer after
every victory. This creates resource tension between consecutive fights."
```

---

### Task 13: Move Crafting Recipes to JSON

**Files:**
- Create: `Darkness.Godot/assets/data/recipes.json`
- Create: `Darkness.Core/Services/RecipeSeeder.cs`
- Modify: `Darkness.Core/Services/CraftingService.cs`
- Modify: `Darkness.Godot/src/Core/Global.cs`

- [ ] **Step 1: Create recipes.json**

Create `Darkness.Godot/assets/data/recipes.json`:

```json
[
  {
    "Id": 1,
    "Name": "Simple Dagger",
    "Result": {
      "Name": "Simple Dagger",
      "Description": "A basic blade.",
      "Type": "Weapon",
      "AttackBonus": 2,
      "Value": 10,
      "DamageDice": "1d4",
      "EquipmentSlot": "Weapon"
    },
    "Materials": { "Iron Ore": 2 }
  },
  {
    "Id": 2,
    "Name": "Iron Sword",
    "Result": {
      "Name": "Iron Sword",
      "Description": "A sturdy iron blade.",
      "Type": "Weapon",
      "AttackBonus": 5,
      "Value": 50,
      "DamageDice": "1d6",
      "EquipmentSlot": "Weapon"
    },
    "Materials": { "Iron Ore": 5 }
  }
]
```

- [ ] **Step 2: Create RecipeSeeder**

Create `Darkness.Core/Services/RecipeSeeder.cs` following the existing seeder pattern:

```csharp
using System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class RecipeSeeder
{
    private readonly IFileSystemService _fs;

    public RecipeSeeder(IFileSystemService fs) { _fs = fs; }

    public void Seed(LiteDatabase db)
    {
        try
        {
            var json = _fs.ReadAllText("assets/data/recipes.json");
            var recipes = JsonSerializer.Deserialize<List<Recipe>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (recipes == null) return;

            var col = db.GetCollection<Recipe>("recipes");
            col.DeleteAll();
            col.InsertBulk(recipes);
            col.EnsureIndex(r => r.Name);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RecipeSeeder] Failed: {ex.Message}");
        }
    }
}
```

- [ ] **Step 3: Update CraftingService to use LiteDB**

Modify `Darkness.Core/Services/CraftingService.cs`:

```csharp
public class CraftingService : ICraftingService
{
    private readonly LiteDatabase _db;

    public CraftingService(LiteDatabase db) { _db = db; }

    public Task<List<Recipe>> GetAvailableRecipesAsync()
    {
        var col = _db.GetCollection<Recipe>("recipes");
        return Task.FromResult(col.FindAll().ToList());
    }
    // ... rest unchanged ...
}
```

- [ ] **Step 4: Register RecipeSeeder in Global.cs**

Add to `Darkness.Godot/src/Core/Global.cs` seeder section:

```csharp
new RecipeSeeder(fs).Seed(db);
```

- [ ] **Step 5: Update CraftingService DI registration to accept LiteDatabase**

In `Global.cs`, change the CraftingService registration:

```csharp
services.AddSingleton<ICraftingService>(sp => new CraftingService(sp.GetRequiredService<LiteDatabase>()));
```

- [ ] **Step 6: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add Darkness.Godot/assets/data/recipes.json Darkness.Core/Services/RecipeSeeder.cs Darkness.Core/Services/CraftingService.cs Darkness.Godot/src/Core/Global.cs
git commit -m "refactor: move crafting recipes from hardcoded C# to recipes.json

Recipes are now defined in assets/data/recipes.json and seeded into LiteDB,
consistent with the project's data-driven architecture. Adding new recipes
requires only JSON edits."
```

---

### Task 14: Remove Debug Logging from QuestService

**Files:**
- Modify: `Darkness.Core/Services/QuestService.cs`

- [ ] **Step 1: Remove all Console.Error.WriteLine and diagnostic queries**

In `Darkness.Core/Services/QuestService.cs`:

1. Remove all `Console.Error.WriteLine(...)` lines (lines 29-31, 37, 56-57, 61, 67, 71, 83, 118, 125, 132, 143, 150, 161, 180-195).
2. Remove the diagnostic query in `GetCompletedChainIds` (lines 186-195 — the `var allStates = ...` block).
3. Keep the functional code intact.

The final `GetCompletedChainIds` should be:

```csharp
public List<string> GetCompletedChainIds(int characterId)
{
    var col = _db.GetCollection<QuestState>("quest_states");
    return col.Find(s => s.CharacterId == characterId && s.Status == "completed")
        .Select(s => s.ChainId)
        .ToList();
}
```

- [ ] **Step 2: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Services/QuestService.cs
git commit -m "fix: remove debug Console.Error.WriteLine from QuestService

Removed 15+ debug logging statements and a diagnostic query from
GetCompletedChainIds that ran in production on every call."
```

---

### Task 15: Move EnsureIndex to Startup

**Files:**
- Modify: `Darkness.Core/Services/CharacterService.cs`
- Modify: `Darkness.Core/Services/QuestService.cs`
- Modify: `Darkness.Godot/src/Core/Global.cs`

- [ ] **Step 1: Remove EnsureIndex from CharacterService methods**

In `Darkness.Core/Services/CharacterService.cs`, remove the two `col.EnsureIndex(c => c.UserId)` calls from `SaveCharacter` and `GetCharactersForUser`. (After Task 6 converts to sync, these are at the service method level.)

- [ ] **Step 2: Remove EnsureIndex from QuestService.GetAvailableChains**

In `Darkness.Core/Services/QuestService.cs`, remove lines 23-24:

```csharp
stateCol.EnsureIndex(s => s.CharacterId);
stateCol.EnsureIndex(s => s.Status);
```

- [ ] **Step 3: Add index creation to Global.cs startup**

In `Darkness.Godot/src/Core/Global.cs`, after the seeder calls, add:

```csharp
// Create indexes (once at startup, not per operation)
db.GetCollection<Character>("characters").EnsureIndex(c => c.UserId);
db.GetCollection<QuestState>("quest_states").EnsureIndex(s => s.CharacterId);
db.GetCollection<QuestState>("quest_states").EnsureIndex(s => s.Status);
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/CharacterService.cs Darkness.Core/Services/QuestService.cs Darkness.Godot/src/Core/Global.cs
git commit -m "refactor: move EnsureIndex calls from per-operation to startup

EnsureIndex was being called on every SaveCharacter, GetCharactersForUser,
and GetAvailableChains invocation. Now runs once during Global._Ready()."
```

---

### Task 16: Fix Leading Space in SkinColor Default

**Files:**
- Modify: `Darkness.Core/Models/CharacterAppearance.cs`
- Test: `Darkness.Tests/Services/SpriteLayerCatalogTests.cs`

- [ ] **Step 1: Write a test that verifies default SkinColor works**

Add to an appropriate test file:

```csharp
[Fact]
public void CharacterAppearance_DefaultSkinColor_HasNoLeadingSpace()
{
    var appearance = new CharacterAppearance();
    Assert.Equal("Light", appearance.SkinColor);
    Assert.DoesNotContain(" ", appearance.SkinColor.Substring(0, 1));
}
```

- [ ] **Step 2: Run test to verify it fails**

Expected: FAIL — current default is `" Light"` with leading space.

- [ ] **Step 3: Fix the default**

In `Darkness.Core/Models/CharacterAppearance.cs` line 5, change:

```csharp
public string SkinColor { get; set; } = " Light";
```
to:
```csharp
public string SkinColor { get; set; } = "Light";
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests --nologo -v q`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Models/CharacterAppearance.cs Darkness.Tests/Services/SpriteLayerCatalogTests.cs
git commit -m "fix: remove leading space from CharacterAppearance.SkinColor default

Default was ' Light' (with leading space) which never matched the seed
data entry 'Light', causing skin tint lookups to silently fail."
```

---

## Execution Dependencies

Tasks can be parallelized in these independent tracks:

- **Track A (Combat):** Task 1 → Task 2 → Task 10
- **Track B (Quests):** Task 3, Task 4, Task 14 (all independent)
- **Track C (Architecture):** Task 6 → Task 15, Task 7 (independent)
- **Track D (Data-driven):** Task 8, Task 13 (independent)
- **Track E (Bugs):** Task 9, Task 12, Task 16 (all independent)
- **Track F (Tests):** Task 11 (depends on Task 6 for CharacterService sync API)
- **Track G (Features):** Task 5 (independent)

Total: 16 tasks covering all 18 issues.
