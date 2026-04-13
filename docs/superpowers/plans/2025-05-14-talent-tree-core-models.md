# Talent Tree Core Models Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Core Talent Models and update the Character model to support the new Talent Tree system.

**Architecture:** Data-driven models using LiteDB for persistence. Models will be pure POCOs in `Darkness.Core.Models`.

**Tech Stack:** .NET 10, C#.

---

### Task 1: Update Character Model

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`

- [ ] **Step 1: Add TalentPoints and UnlockedTalentIds to Character class**
```csharp
        public int Level { get; set; }
        public int Experience { get; set; }
        public int AttributePoints { get; set; } = 5;
        public int TalentPoints { get; set; } = 0; // New field
        public List<string> UnlockedTalentIds { get; set; } = new(); // New field
```

- [ ] **Step 2: Verify Character.cs compiles**
Run: `dotnet build Darkness.Core/Darkness.Core.csproj`

- [ ] **Step 3: Commit**
```bash
git add Darkness.Core/Models/Character.cs
git commit -m "feat: add talent fields to Character model"
```

### Task 2: Create TalentEffect Model

**Files:**
- Create: `Darkness.Core/Models/TalentEffect.cs`

- [ ] **Step 1: Implement TalentEffect class**
```csharp
namespace Darkness.Core.Models
{
    public class TalentEffect
    {
        public string? Stat { get; set; }
        public int Value { get; set; }
        public string? Skill { get; set; }
    }
}
```

- [ ] **Step 2: Verify compilation**
Run: `dotnet build Darkness.Core/Darkness.Core.csproj`

- [ ] **Step 3: Commit**
```bash
git add Darkness.Core/Models/TalentEffect.cs
git commit -m "feat: add TalentEffect model"
```

### Task 3: Create TalentNode Model

**Files:**
- Create: `Darkness.Core/Models/TalentNode.cs`

- [ ] **Step 1: Implement TalentNode class**
```csharp
namespace Darkness.Core.Models
{
    public class TalentNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PointsRequired { get; set; } = 1;
        public string? PrerequisiteNodeId { get; set; }
        public TalentEffect Effect { get; set; } = new();
    }
}
```

- [ ] **Step 2: Verify compilation**
Run: `dotnet build Darkness.Core/Darkness.Core.csproj`

- [ ] **Step 3: Commit**
```bash
git add Darkness.Core/Models/TalentNode.cs
git commit -m "feat: add TalentNode model"
```

### Task 4: Create TalentTree Model

**Files:**
- Create: `Darkness.Core/Models/TalentTree.cs`

- [ ] **Step 1: Implement TalentTree class**
```csharp
using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class TalentTree
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Tier { get; set; }
        public bool IsHidden { get; set; } = false;
        public Dictionary<string, int> Prerequisites { get; set; } = new();
        public List<TalentNode> Nodes { get; set; } = new();
    }
}
```

- [ ] **Step 2: Verify full project compilation**
Run: `dotnet build Darkness.sln`

- [ ] **Step 3: Commit**
```bash
git add Darkness.Core/Models/TalentTree.cs
git commit -m "feat: add TalentTree model"
```
