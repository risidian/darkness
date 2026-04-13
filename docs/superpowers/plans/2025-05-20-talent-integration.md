# Talent Tree Implementation Plan - Task 4

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate talents with leveling (award points), stats (apply passives), and skills (unlocked by talents).

**Architecture:** 
- Update `LevelingService` to award talent points on even levels.
- Refactor `Character` to handle stat bonuses from talents without additive pollution.
- Update `WeaponSkillService` to include skills granted by talent nodes.

**Tech Stack:** .NET 10, LiteDB, XUnit.

---

### Task 1: Update `LevelUpResult` and `LevelingService`

**Files:**
- Modify: `Darkness.Core/Models/LevelUpResult.cs`
- Modify: `Darkness.Core/Services/LevelingService.cs`
- Test: `Darkness.Tests/Services/LevelingServiceTests.cs`

- [ ] **Step 1: Update `LevelUpResult`**
```csharp
namespace Darkness.Core.Models;

public class LevelUpResult
{
    public int XpAwarded { get; set; }
    public int TotalXp { get; set; }
    public int PreviousLevel { get; set; }
    public int NewLevel { get; set; }
    public bool DidLevelUp => NewLevel > PreviousLevel;
    public int LevelsGained => NewLevel - PreviousLevel;
    public int AttributePointsAwarded { get; set; }
    public int TalentPointsAwarded { get; set; } // Added this
}
```

- [ ] **Step 2: Add failing test for Talent Points**
Add the following tests to `Darkness.Tests/Services/LevelingServiceTests.cs`:
```csharp
    [Fact]
    public void AwardExperience_EvenLevels_AwardsTalentPoints()
    {
        var character = new Character { Level = 1, Experience = 0, TalentPoints = 0 };
        
        // Level 1 -> 2 (Even)
        var result = _levelingService.AwardExperience(character, 100);
        Assert.Equal(2, character.Level);
        Assert.Equal(1, character.TalentPoints);
        Assert.Equal(1, result.TalentPointsAwarded);

        // Level 2 -> 3 (Odd)
        result = _levelingService.AwardExperience(character, 150);
        Assert.Equal(3, character.Level);
        Assert.Equal(1, character.TalentPoints);
        Assert.Equal(0, result.TalentPointsAwarded);

        // Level 3 -> 4 (Even)
        result = _levelingService.AwardExperience(character, 250);
        Assert.Equal(4, character.Level);
        Assert.Equal(2, character.TalentPoints);
        Assert.Equal(1, result.TalentPointsAwarded);
    }
```

- [ ] **Step 3: Run test to verify it fails**
Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~LevelingServiceTests"`
Expected: FAIL (compilation error if property missing, otherwise value mismatch)

- [ ] **Step 4: Implement Talent Points awarding in `LevelingService`**
```csharp
        int previousLevel = character.Level;
        character.Experience += xp;

        int newLevel = GetLevelForXp(character.Experience);
        int levelsGained = newLevel - previousLevel;
        int pointsAwarded = 0;
        int talentPointsAwarded = 0; // Added

        if (levelsGained > 0)
        {
            character.Level = newLevel;
            pointsAwarded = levelsGained * AttributePointsPerLevel;
            character.AttributePoints += pointsAwarded;
            
            // Award 1 talent point for every even level gained
            for (int i = previousLevel + 1; i <= newLevel; i++)
            {
                if (i % 2 == 0)
                {
                    character.TalentPoints++;
                    talentPointsAwarded++;
                }
            }
            
            character.CurrentHP = character.MaxHP;
        }

        return new LevelUpResult
        {
            XpAwarded = xp,
            TotalXp = character.Experience,
            PreviousLevel = previousLevel,
            NewLevel = newLevel,
            AttributePointsAwarded = pointsAwarded,
            TalentPointsAwarded = talentPointsAwarded // Added
        };
```

- [ ] **Step 5: Run tests to verify they pass**

### Task 2: Refactor `Character` Stats for Talents

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`
- Modify: `Darkness.Core/Services/TalentService.cs`

- [ ] **Step 1: Modify `Character.cs` to include Base Stats and StatBonuses**
Rename `Strength`, `Dexterity`, etc. to `BaseStrength`, `BaseDexterity`, etc. and make `Strength`, `Dexterity`, etc. computed properties.
Add `public Dictionary<string, int> StatBonuses { get; set; } = new();`.

- [ ] **Step 2: Update `RecalculateDerivedStats` to use computed properties**

- [ ] **Step 3: Update `TalentService.ApplyTalentPassives`**
Instead of modifying `character.Strength`, it should populate `character.StatBonuses`.

- [ ] **Step 4: Update all references in the codebase**
Use `grep_search` and `replace` to update calls to `Strength`, `Dexterity`, etc. if needed.
(Wait, if I keep the names `Strength`, etc. but change them from fields to properties, many usages should still work.)

### Task 3: Update `WeaponSkillService`

**Files:**
- Modify: `Darkness.Core/Interfaces/IWeaponSkillService.cs`
- Modify: `Darkness.Core/Services/WeaponSkillService.cs`

- [ ] **Step 1: Update Interface**
```csharp
List<Skill> GetSkillsForWeapon(string? weaponType, string? offHandType, string? shieldType, List<string>? unlockedTalentIds = null);
```

- [ ] **Step 2: Inject `LiteDatabase` into `WeaponSkillService`**

- [ ] **Step 3: Implement Skill lookup from Talents**
In `GetSkillsForWeapon`, query `talent_trees` for nodes whose IDs are in `unlockedTalentIds` and have a `Skill` defined.

- [ ] **Step 4: Verify with tests**

---
