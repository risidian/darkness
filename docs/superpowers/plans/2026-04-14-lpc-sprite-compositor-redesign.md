# LPC Sprite Compositor Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a new sprite composition system in Darkness.Core using SkiaSharp, following the Universal LPC standard (832x3456 sheets with oversize frames).

**Architecture:** A data-driven multi-layer system where `SheetDefinition` objects define per-animation layer stacks and z-orders. Compositing is handled in Core by `SkiaSharpSpriteCompositor`, and `LpcAnimationHelper` provides frame rects for Godot.

**Tech Stack:** .NET 10, SkiaSharp, LiteDB, Godot (for rendering).

---

### Task 1: Environment Setup & Core Models

**Files:**
- Modify: `Darkness.Core/Darkness.Core.csproj`
- Create: `Darkness.Core/Models/SheetConstants.cs`
- Create: `Darkness.Core/Models/SheetLayer.cs`
- Create: `Darkness.Core/Models/SheetDefinition.cs`

- [ ] **Step 1: Add SkiaSharp to Darkness.Core.csproj**

```xml
<PackageReference Include="SkiaSharp" Version="3.119.2" />
```

Run: `dotnet build Darkness.Core`
Expected: Success

- [ ] **Step 2: Implement SheetConstants.cs**

```csharp
namespace Darkness.Core.Models;

public static class SheetConstants
{
    public const int FRAME_SIZE = 64;
    public const int OVERSIZE_FRAME_SIZE = 192;
    public const int SHEET_WIDTH = 832;
    public const int SHEET_HEIGHT = 3456;
    public const int COLUMNS = 13;
    public const int ROWS = 54;
    public const int OVERSIZE_Y_OFFSET = 3456;

    public static readonly Dictionary<string, int> AnimationRows = new()
    {
        { "spellcast", 0 },
        { "thrust", 4 },
        { "walk", 8 },
        { "slash", 12 },
        { "shoot", 16 },
        { "hurt", 20 },
        { "climb", 21 },
        { "idle", 22 },
        { "jump", 26 },
        { "sit", 30 },
        { "emote", 34 },
        { "run", 38 },
        { "combat_idle", 42 },
        { "backslash", 46 },
        { "halfslash", 49 }
    };

    public static readonly Dictionary<string, int> FrameCounts = new()
    {
        { "spellcast", 7 },
        { "thrust", 8 },
        { "walk", 9 },
        { "slash", 6 },
        { "shoot", 13 },
        { "hurt", 6 },
        { "climb", 6 },
        { "idle", 3 },
        { "jump", 6 },
        { "sit", 15 },
        { "emote", 15 },
        { "run", 8 },
        { "combat_idle", 3 },
        { "backslash", 12 },
        { "halfslash", 6 },
        { "slash_oversize", 6 },
        { "slash_reverse_oversize", 6 },
        { "thrust_oversize", 8 }
    };
}
```

- [ ] **Step 3: Implement SheetLayer.cs**

```csharp
using System.Collections.Generic;

namespace Darkness.Core.Models;

public class SheetLayer
{
    public string? CustomAnimation { get; set; }
    public int ZPos { get; set; }
    public Dictionary<string, string> Paths { get; set; } = new(); // "male", "female" keys
    
    // Helper to get path for gender
    public string GetPath(string gender)
    {
        if (Paths.TryGetValue(gender.ToLower(), out var path)) return path;
        if (Paths.TryGetValue("male", out path)) return path;
        return string.Empty;
    }
}
```

- [ ] **Step 4: Implement SheetDefinition.cs**

```csharp
using System.Collections.Generic;

namespace Darkness.Core.Models;

public class SheetDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public Dictionary<string, SheetLayer> Layers { get; set; } = new();
    public List<string> Variants { get; set; } = new();
    public List<string> Animations { get; set; } = new();
    public int PreviewRow { get; set; }
    public int PreviewColumn { get; set; }
    public bool IsFlipped { get; set; } // For OffHand
}
```

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Darkness.Core.csproj Darkness.Core/Models/SheetConstants.cs Darkness.Core/Models/SheetLayer.cs Darkness.Core/Models/SheetDefinition.cs
git commit -m "feat: add SkiaSharp and basic Sheet models"
```

---

### Task 2: LpcAnimationHelper

**Files:**
- Create: `Darkness.Core/Logic/LpcAnimationHelper.cs`
- Create: `Darkness.Tests/Logic/LpcAnimationHelperTests.cs`

- [ ] **Step 1: Write failing tests for LpcAnimationHelper**

```csharp
using Darkness.Core.Logic;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Logic;

public class LpcAnimationHelperTests
{
    [Fact]
    public void GetFrameRect_Walk_ReturnsCorrectRect()
    {
        var helper = new LpcAnimationHelper();
        var rect = helper.GetFrameRect("walk", 2, 0); // South (Row 10), Frame 0
        Assert.Equal(0, rect.X);
        Assert.Equal(10 * 64, rect.Y);
        Assert.Equal(64, rect.Width);
        Assert.Equal(64, rect.Height);
    }

    [Fact]
    public void GetOversizeFrameRect_Slash_ReturnsCorrectRect()
    {
        var helper = new LpcAnimationHelper();
        // Row offset for slash_oversize is fixed at 3456 (Row 54 start)
        // 4 directions (N, W, S, E)
        var rect = helper.GetOversizeFrameRect("slash_oversize", 2, 0); // South (Row 56), Frame 0
        Assert.Equal(0, rect.X);
        Assert.Equal(3456 + (2 * 192), rect.Y);
        Assert.Equal(192, rect.Width);
        Assert.Equal(192, rect.Height);
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

- [ ] **Step 3: Implement LpcAnimationHelper.cs**

```csharp
using Darkness.Core.Models;

namespace Darkness.Core.Logic;

public class LpcAnimationHelper
{
    public (int X, int Y, int Width, int Height) GetFrameRect(string animation, int direction, int frameIndex)
    {
        if (!SheetConstants.AnimationRows.TryGetValue(animation, out var startRow)) return (0, 0, 0, 0);
        int row = startRow + direction;
        return (frameIndex * SheetConstants.FRAME_SIZE, row * SheetConstants.FRAME_SIZE, SheetConstants.FRAME_SIZE, SheetConstants.FRAME_SIZE);
    }

    public (int X, int Y, int Width, int Height) GetOversizeFrameRect(string customAnimation, int direction, int frameIndex)
    {
        int rowOffset = 0;
        if (customAnimation == "slash_oversize") rowOffset = 0;
        else if (customAnimation == "slash_reverse_oversize") rowOffset = 4;
        else if (customAnimation == "thrust_oversize") rowOffset = 8;
        else return (0, 0, 0, 0);

        int row = rowOffset + direction;
        return (frameIndex * SheetConstants.OVERSIZE_FRAME_SIZE, 
                SheetConstants.OVERSIZE_Y_OFFSET + (row * SheetConstants.OVERSIZE_FRAME_SIZE), 
                SheetConstants.OVERSIZE_FRAME_SIZE, SheetConstants.OVERSIZE_FRAME_SIZE);
    }

    public int GetFrameCount(string animation) => SheetConstants.FrameCounts.GetValueOrDefault(animation, 0);
    
    public bool IsOversize(string animation) => animation.EndsWith("_oversize");
}
```

- [ ] **Step 4: Run tests and verify success**

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Logic/LpcAnimationHelper.cs Darkness.Tests/Logic/LpcAnimationHelperTests.cs
git commit -m "feat: implement LpcAnimationHelper with oversize support"
```

---

### Task 3: SkiaSharpSpriteCompositor

**Files:**
- Modify: `Darkness.Core/Interfaces/ISpriteCompositor.cs`
- Create: `Darkness.Core/Services/SkiaSharpSpriteCompositor.cs`
- Create: `Darkness.Tests/Services/SkiaSharpSpriteCompositorTests.cs`

- [ ] **Step 1: Update ISpriteCompositor.cs interface**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISpriteCompositor
    {
        Task<byte[]> CompositeFullSheet(
            IReadOnlyList<SheetDefinition> definitions,
            CharacterAppearance appearance,
            IFileSystemService fileSystem);

        Task<byte[]> CompositePreviewFrame(
            IReadOnlyList<SheetDefinition> definitions,
            CharacterAppearance appearance,
            IFileSystemService fileSystem);

        byte[] ExtractFrame(byte[] spriteSheetPng, string animation, int frameIndex, int direction);
    }
}
```

- [ ] **Step 2: Implement SkiaSharpSpriteCompositor.cs (Core logic)**

Implement the `CompositeFullSheet` method using SkiaSharp. Handle z-ordering per animation pass.

- [ ] **Step 3: Write tests for SkiaSharpSpriteCompositor**

- [ ] **Step 4: Run tests and verify success**

- [ ] **Step 5: Commit**

---

### Task 4: SheetDefinition Catalog & Seeding

**Files:**
- Create: `Darkness.Core/Interfaces/ISheetDefinitionCatalog.cs`
- Create: `Darkness.Core/Services/SheetDefinitionCatalog.cs`
- Create: `Darkness.Core/Services/SheetDefinitionSeeder.cs`
- Create: `Darkness.Core/Services/AppearanceSeeder.cs`
- Modify: `Darkness.Godot/assets/data/sprite-catalog.json`
- Create: `Darkness.Godot/assets/data/sheet_definitions/` (and initial JSONs)

- [ ] **Step 1: Implement ISheetDefinitionCatalog and SheetDefinitionCatalog**
- [ ] **Step 2: Implement SheetDefinitionSeeder**
- [ ] **Step 3: Update sprite-catalog.json and create sheet_definitions JSONs**
- [ ] **Step 4: Implement AppearanceSeeder**
- [ ] **Step 5: Commit**

---

### Task 5: Refactoring & Godot Integration

**Files:**
- Modify: `Darkness.Godot/src/Services/Global.cs`
- Modify: `Darkness.Godot/scenes/BattleScene.cs`
- Modify: `Darkness.Godot/scenes/WorldScene.cs`
- Remove: Old sprite services and models

- [ ] **Step 1: Update Global.cs to register new services**
- [ ] **Step 2: Update Godot scenes to use LpcAnimationHelper and names instead of rows**
- [ ] **Step 3: Cleanup old code**
- [ ] **Step 4: Final validation**
- [ ] **Step 5: Commit**
