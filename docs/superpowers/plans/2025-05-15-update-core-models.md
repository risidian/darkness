# Update Core Models Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update the core library with `VisualConfig` and `NpcConfig` models to support richer quest step visuals.

**Architecture:** Add new DTO-style models to `Darkness.Core/Models` and integrate them into the existing `QuestStep` model.

**Tech Stack:** .NET 10, C#.

---

### Task 1: Create NpcConfig Model

**Files:**
- Create: `Darkness.Core/Models/NpcConfig.cs`

- [ ] **Step 1: Create NpcConfig model**

```csharp
namespace Darkness.Core.Models;

public class NpcConfig
{
    public string Name { get; set; } = string.Empty;
    public string? SpriteKey { get; set; }     // Path to full sheet (e.g. "bosses/Balgathor")
    public float PositionX { get; set; } = 400; 
    public float PositionY { get; set; } = 200;
    // LPC override if SpriteKey is missing
    public CharacterAppearance? Appearance { get; set; } 
}
```

---

### Task 2: Create VisualConfig Model

**Files:**
- Create: `Darkness.Core/Models/VisualConfig.cs`

- [ ] **Step 1: Create VisualConfig model**

```csharp
namespace Darkness.Core.Models;

public class VisualConfig
{
    public string? BackgroundKey { get; set; } // Path to PNG artwork
    public string? GroundColor { get; set; }   // Hex fallback (e.g. "#222222")
    public string? WaterColor { get; set; }    // Hex fallback (null to hide water)
    public NpcConfig? Npc { get; set; }        // The NPC present in this beat
}
```

---

### Task 3: Add Visuals to QuestStep

**Files:**
- Modify: `Darkness.Core/Models/QuestStep.cs`

- [ ] **Step 1: Update QuestStep.cs**

```csharp
namespace Darkness.Core.Models;

public class QuestStep
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? NextStepId { get; set; }
    public DialogueData? Dialogue { get; set; }
    public CombatData? Combat { get; set; }
    public LocationTrigger? Location { get; set; }
    public BranchData? Branch { get; set; }
    public VisualConfig? Visuals { get; set; } // Add this line
}
```

---

### Task 4: Verification and Commit

- [ ] **Step 1: Verify Build**

Run: `dotnet build Darkness.sln`
Expected: SUCCESS

- [ ] **Step 2: Run Tests**

Run: `dotnet test Darkness.Tests`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Models/VisualConfig.cs Darkness.Core/Models/NpcConfig.cs Darkness.Core/Models/QuestStep.cs
git commit -m "feat: add VisualConfig and NpcConfig models to QuestStep"
```
