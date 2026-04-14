# WeaponSkillService Update Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update `WeaponSkillService` to support available and equipped skills from LiteDB.

**Architecture:** Add `GetAvailableSkills` and `GetEquippedSkills` to `IWeaponSkillService` and `WeaponSkillService`. `GetAvailableSkills` filters by weapon/talent requirements. `GetEquippedSkills` uses `character.SelectedSkillIds` with a fallback to `GetSkillsForWeapon`.

**Tech Stack:** .NET 10, LiteDB, XUnit.

---

### Task 1: Update IWeaponSkillService interface

**Files:**
- Modify: `Darkness.Core/Interfaces/IWeaponSkillService.cs`

- [ ] **Step 1: Update IWeaponSkillService.cs**
Add the new methods to the interface.

```csharp
using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IWeaponSkillService
{
    List<Skill> GetSkillsForWeapon(string? weaponType, string? offHandType, string? shieldType, List<string>? unlockedTalentIds = null);
    List<Skill> GetAvailableSkills(Character character);
    List<Skill> GetEquippedSkills(Character character);
}
```

- [ ] **Step 2: Commit changes**
```bash
git add Darkness.Core/Interfaces/IWeaponSkillService.cs
git commit -m "feat: update IWeaponSkillService interface with GetAvailableSkills and GetEquippedSkills"
```

### Task 2: Implement WeaponSkillService updates

**Files:**
- Modify: `Darkness.Core/Services/WeaponSkillService.cs`

- [ ] **Step 1: Implement GetAvailableSkills**
Query LiteDB for skills matching weapon or talent requirements.

```csharp
    public List<Skill> GetAvailableSkills(Character character)
    {
        var allSkills = _db.GetCollection<Skill>("skills").FindAll().ToList();
        return allSkills.Where(s => 
            (s.WeaponRequirement == "None" || s.WeaponRequirement == character.WeaponType) || 
            (s.TalentRequirement != null && character.UnlockedTalentIds.Contains(s.TalentRequirement))
        ).ToList();
    }
```

- [ ] **Step 2: Implement GetEquippedSkills**
Return selected skills or fallback to default weapon skills.

```csharp
    public List<Skill> GetEquippedSkills(Character character)
    {
        if (character.SelectedSkillIds != null && character.SelectedSkillIds.Count > 0)
        {
            return _db.GetCollection<Skill>("skills").Find(s => character.SelectedSkillIds.Contains(s.Id)).ToList();
        }
        
        return GetSkillsForWeapon(character.WeaponType, character.OffHandType, character.ShieldType, character.UnlockedTalentIds);
    }
```

- [ ] **Step 3: Verify build**
Run `dotnet build Darkness.Core`.

- [ ] **Step 4: Commit changes**
```bash
git add Darkness.Core/Services/WeaponSkillService.cs
git commit -m "feat: implement GetAvailableSkills and GetEquippedSkills in WeaponSkillService"
```

### Task 3: Regression Testing

**Files:**
- Create: `Darkness.Tests/Services/WeaponSkillServiceTests.cs`

- [ ] **Step 1: Write regression tests**
Verify both new methods with mocked LiteDB data.

- [ ] **Step 2: Run tests**
Run `dotnet test Darkness.Tests --filter "FullyQualifiedName~WeaponSkillServiceTests"`.

- [ ] **Step 3: Commit tests**
```bash
git add Darkness.Tests/Services/WeaponSkillServiceTests.cs
git commit -m "test: add regression tests for WeaponSkillService updates"
```
