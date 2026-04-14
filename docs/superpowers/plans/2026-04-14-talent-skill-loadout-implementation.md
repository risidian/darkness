# Unified Talent & Skill Loadout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate class-based starting skills into the talent tree, enable 5-slot active skill loadouts, and remove hardcoded class skill logic.

**Architecture:**
1.  **Data:** Update `talent-trees.json` with class-specific starter nodes.
2.  **Models:** Add `ActiveSkillSlots` to `Character.cs`.
3.  **Services:** Update `WeaponSkillService` to use `ActiveSkillSlots`.
4.  **UI:** (Implicitly covered by service updates, specific UI task added).

**Tech Stack:** C# (Godot/.NET 10), LiteDB.

---

### Task 1: Update Character Model

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`

- [ ] **Step 1: Update `Character` model to include `ActiveSkillSlots`**

```csharp
public class Character
{
    // Existing properties...
    public List<string> UnlockedTalentIds { get; set; } = new();
    
    // New property
    public List<int> ActiveSkillSlots { get; set; } = new List<int>(new int[5]); 
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Models/Character.cs
git commit -m "feat: add ActiveSkillSlots to Character model"
```

### Task 2: Seed Starter Talents

**Files:**
- Modify: `assets/data/talent-trees.json` (Add starter nodes)
- Modify: `Darkness.Core/Services/TalentService.cs` (or relevant seeder if exists)

- [ ] **Step 1: Add starter nodes to `talent-trees.json`**

(Assuming structure allows `AutomaticallyUnlocked` property)
```json
{
  "Id": "cleric_starter",
  "Name": "Cleric Basics",
  "AutomaticallyUnlocked": true,
  "GrantedSkills": [101, 102, 103] // IDs of starter skills
}
```

- [ ] **Step 2: Update `CharacterGen` to unlock starter talents**

(Modify `CharacterGenScene.cs` or the service creating the character)
```csharp
// In Character creation logic:
var starterTalents = _talentService.GetAutomaticallyUnlockedTalents(characterClass);
foreach (var talent in starterTalents) {
    _characterService.UnlockTalent(character, talent);
}
```

- [ ] **Step 3: Commit**

```bash
git add assets/data/talent-trees.json
# Add files modified for character gen
git commit -m "feat: add starter talents to data and initialization"
```

### Task 3: Refactor WeaponSkillService

**Files:**
- Modify: `Darkness.Core/Services/WeaponSkillService.cs`

- [ ] **Step 1: Remove hardcoded skill checks and use `ActiveSkillSlots`**

```csharp
public List<Skill> GetAvailableSkills(Character character)
{
    // Old: switch(character.Class) ...
    // New:
    return character.ActiveSkillSlots
        .Where(id => id > 0)
        .Select(id => _skillRepository.GetById(id))
        .ToList();
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Services/WeaponSkillService.cs
git commit -m "refactor: use ActiveSkillSlots in WeaponSkillService"
```

### Task 4: Verification Tests

**Files:**
- Create: `Darkness.Tests/Services/WeaponSkillServiceTests.cs`

- [ ] **Step 1: Write test for skill retrieval**

```csharp
[Fact]
public void GetAvailableSkills_ReturnsSkillsInSlots()
{
    var character = new Character { ActiveSkillSlots = new List<int> { 1, 2, 0, 0, 0 } };
    var service = new WeaponSkillService(mockRepo);
    var skills = service.GetAvailableSkills(character);
    Assert.Equal(2, skills.Count);
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test Darkness.Tests`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add Darkness.Tests/Services/WeaponSkillServiceTests.cs
git commit -m "test: add verification for WeaponSkillService"
```
