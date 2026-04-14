# Skills Menu UI Update Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Modify the Skills Menu UI to filter out passive skills.

**Architecture:** Update the data fetching logic in `SkillsScene.cs` to apply a filter on the available skills list.

**Tech Stack:** C#, Godot, .NET 10

---

### Task 1: UI Updates (Skills Menu)

**Files:**
- Modify: `C:\Users\Mayce\Documents\GitHub\darkness\Darkness.Godot\src\UI\SkillsScene.cs`
- Test: `C:\Users\Mayce\Documents\GitHub\darkness\Darkness.Tests\Scenes\SkillsSceneTests.cs` (or create if missing)

- [ ] **Step 1: Locate `LoadSkills` in `SkillsScene.cs` and modify the filter**

```csharp
// Current
var availableSkills = _weaponSkillService.GetAvailableSkills(character);

// New
var availableSkills = _weaponSkillService.GetAvailableSkills(character)
    .Where(s => !s.IsPassive).ToList();
```

- [ ] **Step 2: Add/Update regression test in `Darkness.Tests` to verify passive skills are filtered**

```csharp
[Fact]
public void LoadSkills_ShouldFilterOutPassiveSkills()
{
    // Arrange
    var character = new Character { /* ... */ };
    var skills = new List<Skill> {
        new Skill { Id = "active1", IsPassive = false },
        new Skill { Id = "passive1", IsPassive = true }
    };
    _mockWeaponSkillService.Setup(s => s.GetAvailableSkills(character)).Returns(skills);
    
    // Act
    var result = _skillsScene.LoadSkillsTestable(character); // Assuming accessible method
    
    // Assert
    Assert.DoesNotContain(result, s => s.IsPassive);
    Assert.Contains(result, s => s.Id == "active1");
}
```

- [ ] **Step 3: Build and Test**

Run: `dotnet build Darkness.Godot\Darkness.Godot.csproj`
Run: `dotnet test Darkness.Tests`

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/src/UI/SkillsScene.cs Darkness.Tests/Scenes/SkillsSceneTests.cs
git commit -m "feat: filter passive skills in Skills Menu"
```
