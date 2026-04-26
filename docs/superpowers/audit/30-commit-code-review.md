# 30-Commit Code Review: Darkness RPG

**Review period:** Commits `1ac7d745a` through `e6adab068`  
**Date:** 2026-04-26  
**Reviewer:** Claude (Opus 4.6)

---

## Executive Summary

The last 30 commits span five major feature areas: talent mapping fix, game balance audit infrastructure, daily rewards & encumbrance, encounter system, and a robustness/integrity pass. The final commit (`e6adab06`) addresses issues from a prior 20-commit review. Overall the work is solid and well-structured, but I've identified **8 critical**, **12 high**, and **15 medium** issues.

---

## CRITICAL ISSUES

### 1. Enemy Stamina/Mana Regen Caps to MaxHP Instead of MaxStamina/MaxMana
**File:** `Darkness.Core/Logic/CombatEngine.cs:170-174`  
**Commit:** Pre-existing, not caught in these 30 commits

```csharp
enemy.Stamina = Math.Min(enemy.MaxHP, enemy.Stamina + staminaRegen);  // BUG: should be MaxStamina
enemy.Mana = Math.Min(enemy.MaxHP, enemy.Mana + manaRegen);           // BUG: should be MaxMana
```

The character version (line 157-161) correctly uses `MaxStamina` and `MaxMana`, but the Enemy version still caps to `MaxHP`. This was introduced in commit `a099ef5d4` (combat balance) where character stats were fixed but the enemy equivalent was missed.

### 2. WorldScene Distance Reset Never Executes
**File:** `Darkness.Godot/src/Game/WorldScene.cs:386-392`  
**Commit:** `e6adab068`

```csharp
else if (_distanceMovedSinceLastEncounter > 2000)
{
     // Empty block — distance is NEVER reset
}
```

The fallback safety to prevent overflow has an empty body. The `_distanceMovedSinceLastEncounter` is never reset when a roll fails, meaning after many failed rolls the distance grows unboundedly. The comment says "hack until EncounterService is fixed" but the fix was never applied.

### 3. EncounterService Doesn't Reset Distance on Failed Roll
**File:** `Darkness.Core/Services/EncounterService.cs:49-85`  
**Commit:** `be111ff05` through `e6adab068`

`RollForEncounter()` returns `null` when the roll fails, but the caller has no way to know if the distance threshold was crossed. This means if a roll fails (92% of the time with 8% chance), the distance is never reset, so the next frame will re-roll immediately since distance is already > threshold. This creates a burst of rolls after threshold is crossed rather than one roll per distance interval.

**Fix:** Return a result object with `{ RolledButMissed: true }` or reset distance internally.

### 4. `.bak` File Committed to Repository
**File:** `Darkness.Godot/assets/data/quests/beat_6_the_knight.json.bak`  
**Commit:** `a099ef5d4`

A backup file was committed and modified in the combat balance commit. This should be gitignored and removed.

### 5. RewardService Creates `new Random()` Per Call
**File:** `Darkness.Core/Services/RewardService.cs:94`  
**Commit:** `df770aa75`

```csharp
var random = new Random();  // New instance every call
```

Two rapid calls to `CheckDailyRewardAsync()` would get identical `new Random()` seeds (system clock based), producing identical "random" reward selections. Should use `Random.Shared` like EncounterService does.

### 6. Seeder Pattern Change Creates Inconsistency
**Commits:** `e1054dcf7` (robustness improvements)

The seeders were partially migrated from `DeleteAll() + Insert` to `Upsert + DeleteMany orphans`. But this was only done for `AppearanceSeeder` and `ItemSeeder`. The other 8 seeders still use the old pattern or a mix. This inconsistency means some seeders are idempotent and others aren't in the same way.

### 7. CombatEngine Status Effect Resistance Logic May Be Inverted
**File:** `Darkness.Core/Logic/CombatEngine.cs:193-202`

```csharp
return _random.Next(1, 101) > resistance;  // High wisdom = less resistance
```

A character with Wisdom 80 has only 20% resistance (roll must be >80). This seems inverted — higher wisdom should mean higher resistance, not lower.

### 8. Stat Inflation Bug Was Critical — Fix Should Have Regression Test
**Commit:** `e6adab068`  
The StudyScene was writing to computed `Strength` property (which had a setter routing to `BaseStrength`) instead of `BaseStrength` directly. The fix correctly changed all references, but **no regression test was added** to prevent this from recurring. Given this was caught by review, a test like `StudyScene_AllocatePoints_OnlyModifiesBaseStats()` should exist.

---

## HIGH PRIORITY ISSUES

### 9. Commit Message Typo: "Feat: Add Radnom Ecnounter"
**Commit:** `be111ff05`  
Minor but unprofessional — "Random Encounter" is misspelled in the permanent git history.

### 10. CharacterGenScene Duplicated Derived Stat Calculation Removed (Good) but ArmorClass Now Different
**Commit:** `e6adab068`  
Before the fix, `CharacterGenScene` manually calculated derived stats:
```csharp
c.MaxHP = c.Constitution * 10;  // Was * 15 before combat balance
```
After the fix, it calls `c.RecalculateDerivedStats()` which computes `MaxHP = Constitution * 10`. However, the old code also set `ArmorClass` per-class (Knight=5, Warrior=3, etc.) **before** RecalculateDerivedStats. The new `RecalculateDerivedStats()` then overwrites `ArmorClass = Constitution / 2 + GetTotalBonus("ArmorClass")`.

For a Knight with Constitution 14: `ArmorClass = 14/2 = 7`, not the intended 5. The per-class ArmorClass values are now overwritten. Verify if this is intentional.

### 11. QuestService.AdvanceStep Morality Applied Before DB Update
**File:** `Darkness.Core/Services/QuestService.cs:103`  
**Commit:** `e1054dcf7`

```csharp
character.Morality += option.MoralityImpact;  // Applied immediately
nextStepId = option.NextStepId;
```

If the subsequent database update fails, the character keeps the morality change but the quest state is inconsistent.

### 12. TriggerService Debounce Uses DateTime.UtcNow (Non-Deterministic)
**File:** `Darkness.Core/Services/TriggerService.cs:22-27`  
**Commit:** `e1054dcf7`

The 500ms cooldown uses `DateTime.UtcNow` comparison. In tests, this is hard to mock and could cause flaky tests. The debounce tests (`TriggerServiceDebounceTests.cs`) may pass/fail depending on machine speed.

### 13. CombatSnapshot Persistence on Every Turn Start
**File:** `Darkness.Godot/src/Game/BattleScene.cs:1278-1287`  
**Commit:** `e1054dcf7`

`SyncCombatState()` is called at every turn start (line 811), which calls `_questService.UpdateQuestState()` — a database write on every single turn. For a 20-turn battle, that's 20 DB writes. Consider batching or only persisting on significant events.

### 14. WeaponSkillService Loads TalentTrees on Every GetAvailableSkills Call
**File:** `Darkness.Core/Services/WeaponSkillService.cs:72-80`  
**Commit:** `1ac7d745a`

```csharp
var talentTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
```

Called every time skills are resolved (multiple times per combat turn). Should be cached.

### 15. EncounterService Does Duplicate DB Lookup
**File:** `Darkness.Core/Services/EncounterService.cs`  
**Commit:** `e6adab068` partially fixed this

The `GetRandomEncounter()` method (lines 18-47) still does its own DB lookup, while `RollForEncounter()` (lines 49-85) was refactored to inline the logic to avoid the double lookup. But `GetRandomEncounter()` is now dead code — never called. Should be removed.

### 16. Quest Log Shows Step ID Instead of Description
**File:** `Darkness.Godot/src/Game/WorldScene.cs:987`  
**Commit:** `e6adab068`

```csharp
_questLogLabel.Text = $"Quest: {chain.Title}\nObjective: {step.Id}";
```

Shows the internal step ID (e.g., "beat_1_step_3") to the player instead of a human-readable objective description.

### 17. WebAPI Models Use `required` Without Constructor Initialization
**File:** `Darkness.WebAPI/Models/Characters.cs`, `Users.cs`  
**Commit:** `e6adab068`

Adding `required` modifier fixes the build warnings but means any code creating `Characters` or `Users` instances must now provide all required properties. Verify EF Core and any test code still compiles.

### 18. DataValidator.cs Added but Never Integrated
**Commit:** `e6adab068`  
The `DataValidator` class was created but I don't see it called from `Global.cs` or any startup path. It validates quest step links and recipe materials but is dead code.

### 19. Level Scaling Changed: MaxHP From Constitution * 15 to * 10
**Commits:** `a099ef5d4`, `e6adab068`

The MaxHP formula changed from `Constitution * 15` to `Constitution * 10`. Similarly Mana went from `Wisdom * 10` to `Wisdom * 5` and Stamina from `Constitution * 10` to `Constitution * 5`. These are significant balance changes that affect all content. Existing characters loaded from DB will have stale derived stats until `RecalculateDerivedStats()` is called.

### 20. Combat Auto-Resume Snapshot Doesn't Restore Party Mana/Stamina
**File:** `Darkness.Godot/src/Game/BattleScene.cs:135-152`  
**Commit:** `e1054dcf7`

On resume, only enemy HP is restored from snapshot. Party HP, mana, stamina, and skill cooldowns are not restored from the snapshot, meaning the party starts at full resources after a resume.

---

## MEDIUM PRIORITY ISSUES

### 21. Inconsistent Error Logging
Across the 30 commits, some errors use `Console.Error.WriteLine()` (QuestService), some use `Console.WriteLine()` (Seeders), and some use `GD.Print()` (BattleScene). No structured logging framework.

### 22. Test Coverage Gaps for New Features
- `EncounterService` has 166-line test file (good)
- `RewardServiceEncumbranceTests` has 139-line test file (good)
- `QuestServiceRobustnessTests` has 89-line test file (good)
- `TriggerServiceDebounceTests` has 83-line test file (good)
- **Missing:** No tests for CombatSnapshot persistence/restore, no tests for the stat inflation fix, no tests for DataValidator

### 23. Encounter Table Has Only One Area
`encounters.json` only defines `camelot_shore`. All other areas will return `null` from `RollForEncounter()`, meaning no random encounters outside the first area.

### 24. Login Calendar Uses Mixed DateTime Sources
**File:** `Darkness.Core/Services/RewardService.cs`
- Line 77: `DateTime.Today` (local time)
- Line 124: `DateTime.Now` (local time)
- TriggerService uses `DateTime.UtcNow`

Mixing local and UTC time could cause issues across time zones or DST transitions.

### 25. EncounterTable Model Missing Validation
**File:** `Darkness.Core/Models/EncounterTable.cs`  
**Commit:** `e6adab068` added `[BsonId] public string BackgroundKey` but no validation that EncounterChance is [0,100] or EncounterDistance > 0.

### 26. Export_presets.cfg Version Bumped Multiple Times
Commits `be111ff05`, `8f1e18e89`, and `e1054dcf7` each bump the version code. This is fine but suggests no automated versioning.

### 27. Audit Test Files in Darkness.Tests/Audit/
**Commit:** `1885f5c67` through `991ebf57e`  
`CombatSim.cs` and `QuestGraphAudit.cs` are more like scripts/tools than unit tests. They output to console rather than asserting. Consider moving to a separate project or marking as `[Trait("Category", "Audit")]`.

### 28. SettingsService Changed from Newtonsoft.Json to System.Text.Json
**Commit:** `e1054dcf7`  
This is a good change but could break existing settings files if they use Newtonsoft-specific formatting (comments, trailing commas, etc.).

### 29. Quest Data Enemy Stat Adjustments Lack Rationale
**Commit:** `a099ef5d4`  
Multiple quest files had enemy stats changed (e.g., Tywin Attack 20→12, XP 200→1000) but the commit message is just "Fix combat balance" with no documentation of the balance rationale.

### 30. ConsolidateInventory Called After Every Reward
**File:** `Darkness.Core/Services/RewardService.cs:60`  
Calling `ConsolidateInventory()` after every combat creates O(n²) scanning of inventory. For large inventories this could be slow.

### 31. BattleScene SyncCombatState EnemyMap FirstOrDefault
**File:** `Darkness.Godot/src/Game/BattleScene.cs:1269`
```csharp
var liveEnemy = _enemyMap.FirstOrDefault(p => p.Value == spawn).Key;
```
`FirstOrDefault` on a dictionary with `.Key` access — if no match found, returns `default(KeyValuePair).Key` which is null for reference types but could be confusing.

### 32. No .gitignore Entry for .bak Files
The `.bak` file committed in `a099ef5d4` suggests `.gitignore` doesn't exclude backup files.

### 33. Commit `8f1e18e89` is Overly Broad
"fix: implement playability and data integrity improvements" touches 11 files across skills, recipes, login calendar, quest data, crafting service, GEMINI.md, and docs. Should have been split into focused commits.

### 34. docs/superpowers/plans/ Contains Stale Plans
Three plan files from this sprint are tracked in git. These are working documents that may become stale and misleading.

### 35. obj/project.assets.json Committed
**Commits:** `e1054dcf7`, `be111ff05`  
`Darkness.WebAPI/obj/project.assets.json` changes appear in the diff — this should be gitignored.

---

## POSITIVE OBSERVATIONS

1. **Stat model refactor was well-executed** — Removing setters from computed properties (`Strength`, `Dexterity`, etc.) and routing all mutations through `Base*` properties eliminates an entire class of bugs.

2. **MaxMana/MaxStamina separation** — Adding explicit `MaxMana` and `MaxStamina` properties instead of calculating from `MaxHP * 10` is much cleaner.

3. **StudyScene unsaved changes protection** — The snapshot/compare/confirm pattern in StudyScene is excellent UX.

4. **Combat auto-resume** — Persisting combat state to database for quest-aware battles is a good feature, even if the implementation needs refinement.

5. **Seeder robustness** — Moving to upsert + orphan cleanup is better than DeleteAll + re-insert for data stability.

6. **Quest service error logging** — Adding `Console.Error.WriteLine` for quest advancement failures will help debug quest issues.

7. **Test coverage for new features** — 4 new test files added (EncounterService, RewardServiceEncumbrance, QuestServiceRobustness, TriggerServiceDebounce) shows good testing discipline.

8. **TriggerService debounce** — Simple 500ms cooldown prevents the bouncing-trigger problem near location boundaries.

---

## SUMMARY TABLE

| Severity | Count | Key Areas |
|----------|-------|-----------|
| Critical | 8 | Enemy regen cap, distance reset, seeder inconsistency, .bak file |
| High | 12 | ArmorClass overwrite, morality timing, snapshot gaps, dead code |
| Medium | 15 | Logging, test gaps, data completeness, performance |
| Positive | 8 | Stat refactor, MaxMana/MaxStamina, unsaved changes, tests |

---

## RECOMMENDED IMMEDIATE ACTIONS

1. Fix enemy `HandleTurnStart` to use `MaxStamina`/`MaxMana` instead of `MaxHP`
2. Fix WorldScene distance reset (either reset after threshold or have EncounterService return roll status)
3. Remove `.bak` file and add `*.bak` to `.gitignore`
4. Remove `obj/` from tracking and add to `.gitignore`
5. Add regression test for stat inflation bug (StudyScene writes to Base properties)
6. Integrate `DataValidator` into startup or remove it
7. Fix quest log to show objective description instead of step ID
