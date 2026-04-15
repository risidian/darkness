# Sprite Compositor Bugfix Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 10 bugs introduced during the LPC sprite compositor redesign so that armor, weapons, hair, eyes, and face render correctly on characters, and eliminate performance lag.

**Architecture:** The sprite pipeline flows: JSON seed files → LiteDB → SheetDefinitionCatalog → SkiaSharpSpriteCompositor → PNG bytes → Godot texture. Bugs exist at every stage of this pipeline. Fixes are ordered bottom-up: models first, then seeders, then compositor, then data files.

**Tech Stack:** .NET 10, LiteDB, SkiaSharp, System.Text.Json, Godot 4.6.1

---

### Task 1: Fix JSON snake_case deserialization in SheetLayer and SheetDefinition

The `SheetDefinitionSeeder` uses `PropertyNameCaseInsensitive = true` which only handles case differences, NOT snake_case → PascalCase. Fields `custom_animation`, `preview_row`, `preview_column` silently fail to deserialize.

**Files:**
- Modify: `Darkness.Core/Models/SheetLayer.cs:7` — add `JsonPropertyName` attribute
- Modify: `Darkness.Core/Models/SheetDefinition.cs` — add `JsonPropertyName` attributes
- Test: `Darkness.Tests/Services/SheetDefinitionSeederTests.cs` (new)

- [ ] **Step 1: Write failing test for snake_case deserialization**

Create `Darkness.Tests/Services/SheetDefinitionSeederTests.cs`:

```csharp
using System;
using System.IO;
using System.Text.Json;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using Xunit;

namespace Darkness.Tests.Services;

public class SheetDefinitionSeederTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;

    public SheetDefinitionSeederTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"SheetDefSeederTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath, new BsonMapper());
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void Seed_DeserializesSnakeCaseFields()
    {
        var weaponJson = @"{
            ""name"": ""Test Sword"",
            ""slot"": ""Weapon"",
            ""layers"": {
                ""slash_front"": {
                    ""custom_animation"": ""slash_oversize"",
                    ""zPos"": 150,
                    ""paths"": { ""male"": ""weapons/test/"" }
                }
            },
            ""variants"": [""steel""],
            ""animations"": [""walk"", ""slash_oversize""],
            ""preview_row"": 10,
            ""preview_column"": 1
        }";

        var fsMock = new Mock<IFileSystemService>();
        fsMock.Setup(f => f.DirectoryExists("assets/data/sheet_definitions")).Returns(true);
        fsMock.Setup(f => f.GetFiles("assets/data/sheet_definitions", "*.json", true))
              .Returns(new[] { "assets/data/sheet_definitions/test.json" });
        fsMock.Setup(f => f.ReadAllText("assets/data/sheet_definitions/test.json")).Returns(weaponJson);

        var seeder = new SheetDefinitionSeeder(fsMock.Object);
        seeder.Seed(_db);

        var col = _db.GetCollection<SheetDefinition>("sheet_definitions");
        var def = col.FindOne(x => x.Name == "Test Sword");

        Assert.NotNull(def);
        Assert.Equal(10, def.PreviewRow);
        Assert.Equal(1, def.PreviewColumn);
        Assert.Equal("slash_oversize", def.Layers["slash_front"].CustomAnimation);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~SheetDefinitionSeederTests.Seed_DeserializesSnakeCaseFields" -v n`

Expected: FAIL — `PreviewRow` is 0 (not 10), `CustomAnimation` is null.

- [ ] **Step 3: Add JsonPropertyName attributes to SheetLayer**

In `Darkness.Core/Models/SheetLayer.cs`, add `using System.Text.Json.Serialization;` and decorate:

```csharp
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Darkness.Core.Models;

public class SheetLayer
{
    [JsonPropertyName("custom_animation")]
    public string? CustomAnimation { get; set; }

    [JsonPropertyName("zPos")]
    public int ZPos { get; set; }

    public string TintHex { get; set; } = "#FFFFFF";
    public Dictionary<string, string> Paths { get; set; } = new();

    public string GetPath(string gender)
    {
        if (Paths.TryGetValue(gender.ToLower(), out var path)) return path;
        if (Paths.TryGetValue("male", out path)) return path;
        return string.Empty;
    }
}
```

- [ ] **Step 4: Add JsonPropertyName attributes to SheetDefinition**

In `Darkness.Core/Models/SheetDefinition.cs`, add:

```csharp
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Darkness.Core.Models;

public class SheetDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public Dictionary<string, SheetLayer> Layers { get; set; } = new();
    public List<string> Variants { get; set; } = new();
    public List<string> Animations { get; set; } = new();

    [JsonPropertyName("preview_row")]
    public int PreviewRow { get; set; }

    [JsonPropertyName("preview_column")]
    public int PreviewColumn { get; set; }

    public bool IsFlipped { get; set; }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~SheetDefinitionSeederTests.Seed_DeserializesSnakeCaseFields" -v n`

Expected: PASS

- [ ] **Step 6: Run all tests**

Run: `dotnet test Darkness.Tests`

Expected: All 184+ tests pass.

---

### Task 2: Add DefaultVariant to SheetLayer for proper path resolution

The compositor's `ResolveAssetPath` uses `ExtractVariant(displayName)` which returns "default" for items without parenthetical variants (e.g., "Mace", "Long" hair). Hair/eyes need the color from `FileNameTemplate`, weapons need the variant from the `Variants` list.

**Files:**
- Modify: `Darkness.Core/Models/SheetLayer.cs` — add `DefaultVariant` property
- Modify: `Darkness.Core/Services/SheetDefinitionCatalog.cs:101-118` — use FileNameTemplate in WrapAppearanceOption
- Modify: `Darkness.Core/Services/SkiaSharpSpriteCompositor.cs:302-326,355-367,369-384` — use DefaultVariant in ResolveLayers and variant resolution
- Test: `Darkness.Tests/Services/SheetDefinitionCatalogTests.cs` — add test

- [ ] **Step 1: Write failing test for appearance option variant resolution**

Add to `Darkness.Tests/Services/SheetDefinitionCatalogTests.cs`:

```csharp
[Fact]
public void GetSheetDefinitions_HairUsesFileNameTemplateVariant()
{
    // Hair "Long" has FileNameTemplate "{action}/blonde.png" → variant should be "blonde"
    var appearance = new CharacterAppearance { HairStyle = "Long", Head = "Human Male" };
    var definitions = _catalog.GetSheetDefinitions(appearance);
    var hair = definitions.FirstOrDefault(d => d.Slot == "Hair");

    Assert.NotNull(hair);
    var layer = hair.Layers.Values.First();
    Assert.Equal("blonde", layer.DefaultVariant);
}

[Fact]
public void GetSheetDefinitions_BodyHasNoVariant()
{
    // Body "Human Male" has FileNameTemplate "{action}.png" → no variant (uses animation.png pattern)
    var appearance = new CharacterAppearance { Head = "Human Male" };
    var definitions = _catalog.GetSheetDefinitions(appearance);
    var body = definitions.FirstOrDefault(d => d.Slot == "Body");

    Assert.NotNull(body);
    var layer = body.Layers.Values.First();
    Assert.Null(layer.DefaultVariant);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~SheetDefinitionCatalogTests.GetSheetDefinitions_Hair" -v n`

Expected: FAIL — `DefaultVariant` property doesn't exist.

- [ ] **Step 3: Add DefaultVariant property to SheetLayer**

In `Darkness.Core/Models/SheetLayer.cs`, add:

```csharp
[JsonPropertyName("default_variant")]
public string? DefaultVariant { get; set; }
```

- [ ] **Step 4: Update WrapAppearanceOption to extract variant from FileNameTemplate**

In `Darkness.Core/Services/SheetDefinitionCatalog.cs`, replace the `WrapAppearanceOption` method:

```csharp
private SheetDefinition? WrapAppearanceOption(AppearanceOption? option, string slot, string tint)
{
    if (option == null) return null;

    // Parse FileNameTemplate to extract variant
    // "{action}/blonde.png" → variant = "blonde"
    // "{action}.png" → variant = null (uses {animation}.png pattern)
    string? variant = null;
    if (!string.IsNullOrEmpty(option.FileNameTemplate) && option.FileNameTemplate.Contains("/"))
    {
        var fileName = option.FileNameTemplate.Replace("{action}/", "");
        variant = System.IO.Path.GetFileNameWithoutExtension(fileName);
    }

    var layer = new SheetLayer
    {
        ZPos = option.ZOrder,
        TintHex = tint,
        DefaultVariant = variant,
        Paths = new Dictionary<string, string> { { "male", option.AssetPath }, { "female", option.AssetPath } }
    };

    return new SheetDefinition
    {
        Name = option.DisplayName,
        Slot = slot,
        Layers = new Dictionary<string, SheetLayer> { { "base", layer } },
        Animations = new List<string> { "walk", "idle", "slash", "thrust", "shoot", "hurt" }
    };
}
```

- [ ] **Step 5: Update ResolveLayers to carry DefaultVariant from both SheetLayer and Variants list**

In `Darkness.Core/Services/SkiaSharpSpriteCompositor.cs`, update `ResolvedLayer` class:

```csharp
private class ResolvedLayer
{
    public string DefinitionName { get; }
    public SheetLayer Layer { get; }
    public bool IsFlipped { get; }
    public string TintHex { get; }
    public string? DefaultVariant { get; }
    public int ZPos => Layer.ZPos;

    public ResolvedLayer(string defName, SheetLayer layer, bool isFlipped, string tintHex, string? defaultVariant)
    {
        DefinitionName = defName;
        Layer = layer;
        IsFlipped = isFlipped;
        TintHex = tintHex;
        DefaultVariant = defaultVariant;
    }
}
```

Update `ResolveLayers` method to pass variant through:

```csharp
private List<ResolvedLayer> ResolveLayers(IReadOnlyList<SheetDefinition> definitions, string animation, string gender, CharacterAppearance appearance)
{
    var result = new List<ResolvedLayer>();
    foreach (var def in definitions)
    {
        // Prefer Variants[0] from definition, then SheetLayer.DefaultVariant
        string? defVariant = def.Variants?.Count > 0 ? def.Variants[0] : null;

        foreach (var kvp in def.Layers)
        {
            var layer = kvp.Value;
            string? variant = layer.DefaultVariant ?? defVariant;

            if (string.IsNullOrEmpty(layer.CustomAnimation))
            {
                if (!animation.EndsWith("_oversize"))
                {
                    result.Add(new ResolvedLayer(def.Name, layer, def.IsFlipped, layer.TintHex, variant));
                }
            }
            else if (layer.CustomAnimation == animation)
            {
                result.Add(new ResolvedLayer(def.Name, layer, def.IsFlipped, layer.TintHex, variant));
            }
        }
    }
    return result;
}
```

- [ ] **Step 6: Update variant resolution in compositing to use DefaultVariant**

In `SkiaSharpSpriteCompositor.cs`, everywhere `ExtractVariant` is called, replace with:

```csharp
string variant = ResolveVariant(layer);
```

Add this helper method:

```csharp
private string ResolveVariant(ResolvedLayer layer)
{
    string extracted = ExtractVariant(layer.DefinitionName);
    if (extracted != "default") return extracted;
    return layer.DefaultVariant ?? "default";
}
```

Apply this in `CompositeFullSheet` (standard section ~line 57), `CompositeFullSheet` (oversize section ~line 131 and ~line 166), and `CompositePreviewFrame` (~line 241).

- [ ] **Step 7: Run tests**

Run: `dotnet test Darkness.Tests`

Expected: All tests pass including the two new ones.

---

### Task 3: Load ClassDefaults and implement GetDefaultAppearanceForClass

`GetDefaultAppearanceForClass()` returns an empty `CharacterAppearance()` ignoring the ClassDefaults in sprite-catalog.json. The `AppearanceSeeder` also doesn't deserialize ClassDefaults.

**Files:**
- Modify: `Darkness.Core/Services/AppearanceSeeder.cs:71-74` — add ClassDefaults to SeedData
- Modify: `Darkness.Core/Services/SheetDefinitionCatalog.cs:121-127` — implement GetDefaultAppearanceForClass
- Test: `Darkness.Tests/Services/SheetDefinitionCatalogTests.cs` — add test

- [ ] **Step 1: Write failing test for class defaults**

Add to `Darkness.Tests/Services/SheetDefinitionCatalogTests.cs`:

```csharp
[Theory]
[InlineData("Knight", "Arming Sword (Steel)", "Spartan")]
[InlineData("Warrior", "Waraxe", "None")]
[InlineData("Mage", "Mage Wand", "None")]
[InlineData("Rogue", "Dagger (Steel)", "None")]
[InlineData("Cleric", "Mace", "Crusader")]
public void GetDefaultAppearanceForClass_ReturnsCorrectEquipment(string className, string expectedWeapon, string expectedShield)
{
    var appearance = _catalog.GetDefaultAppearanceForClass(className);
    Assert.Equal(expectedWeapon, appearance.WeaponType);
    Assert.Equal(expectedShield, appearance.ShieldType);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~GetDefaultAppearanceForClass_ReturnsCorrectEquipment" -v n`

Expected: FAIL — all classes return "Arming Sword (Steel)" and "None" (hardcoded defaults).

- [ ] **Step 3: Update AppearanceSeeder to deserialize and store ClassDefaults**

In `Darkness.Core/Services/AppearanceSeeder.cs`, update `SeedData` and `Seed`:

```csharp
public void Seed(ILiteDatabase db)
{
    string json;
    try
    {
        json = _fileSystem.ReadAllText("assets/data/sprite-catalog.json");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AppearanceSeeder] ERROR: Failed to read sprite-catalog.json — {ex.Message}");
        return;
    }

    SeedData? data;
    try
    {
        data = SystemJson.JsonSerializer.Deserialize<SeedData>(json, new SystemJson.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    catch (SystemJson.JsonException ex)
    {
        Console.WriteLine($"[AppearanceSeeder] ERROR: Failed to parse sprite-catalog.json — {ex.Message}");
        return;
    }

    if (data == null)
    {
        Console.WriteLine("[AppearanceSeeder] ERROR: sprite-catalog.json deserialized to null");
        return;
    }

    var optionCol = db.GetCollection<AppearanceOption>("appearance_options");
    optionCol.DeleteAll();
    if (data.AppearanceOptions != null)
    {
        foreach (var option in data.AppearanceOptions)
        {
            if (option.AssetPath.StartsWith("assets/sprites/full/"))
            {
                option.AssetPath = option.AssetPath.Replace("assets/sprites/full/", "");
            }
            optionCol.Insert(option);
        }
    }
    optionCol.EnsureIndex(o => o.Category);
    optionCol.EnsureIndex(o => o.DisplayName);

    // Seed ClassDefaults
    var defaultsCol = db.GetCollection<ClassDefault>("class_defaults");
    defaultsCol.DeleteAll();
    if (data.ClassDefaults != null)
    {
        foreach (var kvp in data.ClassDefaults)
        {
            var cd = kvp.Value;
            cd.ClassName = kvp.Key;
            defaultsCol.Insert(cd);
        }
    }
    defaultsCol.EnsureIndex(c => c.ClassName);

    Console.WriteLine($"[AppearanceSeeder] INFO: Loaded {optionCol.Count()} appearance options, {defaultsCol.Count()} class defaults");
}

private class SeedData
{
    public List<AppearanceOption>? AppearanceOptions { get; set; }
    public Dictionary<string, ClassDefault>? ClassDefaults { get; set; }
}
```

- [ ] **Step 4: Create ClassDefault model**

Create `Darkness.Core/Models/ClassDefault.cs`:

```csharp
namespace Darkness.Core.Models;

public class ClassDefault
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ArmorType { get; set; } = "None";
    public string WeaponType { get; set; } = "None";
    public string ShieldType { get; set; } = "None";
    public string? OffHandType { get; set; } = "None";
    public string Head { get; set; } = "Human Male";
    public string Face { get; set; } = "Default";
    public string Feet { get; set; } = "None";
    public string Arms { get; set; } = "None";
    public string Legs { get; set; } = "None";
}
```

- [ ] **Step 5: Implement GetDefaultAppearanceForClass**

In `Darkness.Core/Services/SheetDefinitionCatalog.cs`, replace the stub:

```csharp
public CharacterAppearance GetDefaultAppearanceForClass(string className)
{
    var col = _db.GetCollection<ClassDefault>("class_defaults");
    var defaults = col.FindOne(x => x.ClassName == className);
    if (defaults == null) return new CharacterAppearance();

    return new CharacterAppearance
    {
        Head = defaults.Head,
        Face = defaults.Face,
        ArmorType = defaults.ArmorType,
        WeaponType = defaults.WeaponType,
        ShieldType = defaults.ShieldType,
        OffHandType = defaults.OffHandType,
        Feet = defaults.Feet,
        Arms = defaults.Arms,
        Legs = defaults.Legs
    };
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test Darkness.Tests`

Expected: All tests pass including the 5 new class defaults tests.

---

### Task 4: Fix equipment JSON paths (gender directories) and create missing sheet definitions

Armor, legs, feet, and arms assets have `{gender}/` subdirectories but the JSON paths don't include them. Also, 7 of 8 required weapons/shields are missing.

**Files:**
- Modify: `Darkness.Godot/assets/data/sheet_definitions/armor/armor_leather.json`
- Modify: `Darkness.Godot/assets/data/sheet_definitions/legs/legs_slacks.json`
- Modify: `Darkness.Godot/assets/data/sheet_definitions/feet/feet_shoes.json`
- Create: `Darkness.Godot/assets/data/sheet_definitions/armor/armor_plate.json`
- Create: `Darkness.Godot/assets/data/sheet_definitions/arms/arms_gloves.json`
- Create: `Darkness.Godot/assets/data/sheet_definitions/weapons/sword/weapon_sword_dagger.json`
- Create: `Darkness.Godot/assets/data/sheet_definitions/weapons/blunt/weapon_blunt_mace.json`
- Create: `Darkness.Godot/assets/data/sheet_definitions/weapons/blunt/weapon_blunt_waraxe.json`
- Create: `Darkness.Godot/assets/data/sheet_definitions/weapons/magic/weapon_magic_wand.json`
- Create: `Darkness.Godot/assets/data/sheet_definitions/shields/shield_spartan.json`
- Create: `Darkness.Godot/assets/data/sheet_definitions/shields/shield_crusader.json`

- [ ] **Step 1: Fix armor_leather.json paths to include gender directories**

```json
{
  "name": "Leather",
  "slot": "Armor",
  "layers": {
    "main": {
      "zPos": 60,
      "paths": {
        "male": "armor/leather/male",
        "female": "armor/leather/female"
      }
    }
  },
  "variants": ["leather", "black", "brown"],
  "animations": ["walk", "idle", "slash", "thrust", "shoot", "hurt"]
}
```

- [ ] **Step 2: Fix legs_slacks.json paths**

```json
{
  "name": "Slacks",
  "slot": "Legs",
  "layers": {
    "main": {
      "zPos": 40,
      "paths": {
        "male": "legs/pants/male",
        "female": "legs/pants/female"
      }
    }
  },
  "variants": ["black", "brown"],
  "animations": ["walk", "idle", "slash", "thrust", "shoot", "hurt"]
}
```

- [ ] **Step 3: Fix feet_shoes.json paths**

```json
{
  "name": "Shoes",
  "slot": "Feet",
  "layers": {
    "main": {
      "zPos": 15,
      "paths": {
        "male": "feet/shoes/basic/male",
        "female": "feet/shoes/basic/female"
      }
    }
  },
  "variants": ["black", "brown"],
  "animations": ["walk", "idle", "slash", "thrust", "shoot", "hurt"]
}
```

- [ ] **Step 4: Create armor_plate.json**

Verified asset path: `armor/plate/male/walk/steel.png` exists.

```json
{
  "name": "Plate (Steel)",
  "slot": "Armor",
  "layers": {
    "main": {
      "zPos": 60,
      "paths": {
        "male": "armor/plate/male",
        "female": "armor/plate/female"
      }
    }
  },
  "variants": ["steel", "gold", "iron"],
  "animations": ["walk", "idle", "slash", "thrust", "shoot", "hurt"]
}
```

- [ ] **Step 5: Create arms_gloves.json**

Verified asset path: `arms/gloves/male/walk/black.png` exists.

```json
{
  "name": "Gloves",
  "slot": "Arms",
  "layers": {
    "main": {
      "zPos": 55,
      "paths": {
        "male": "arms/gloves/male",
        "female": "arms/gloves/female"
      }
    }
  },
  "variants": ["black"],
  "animations": ["walk", "idle", "slash", "thrust", "shoot", "hurt"]
}
```

- [ ] **Step 6: Create weapon_sword_dagger.json**

Verified paths: `weapons/sword/dagger/walk/dagger.png`, `weapons/sword/dagger/behind/walk/dagger.png`, etc.

```json
{
  "name": "Dagger (Steel)",
  "slot": "Weapon",
  "layers": {
    "main": {
      "zPos": 140,
      "paths": {
        "male": "weapons/sword/dagger",
        "female": "weapons/sword/dagger"
      }
    },
    "behind": {
      "zPos": 9,
      "paths": {
        "male": "weapons/sword/dagger/behind",
        "female": "weapons/sword/dagger/behind"
      }
    }
  },
  "variants": ["dagger"],
  "animations": ["walk", "slash", "thrust", "hurt"]
}
```

- [ ] **Step 7: Create weapon_blunt_mace.json**

Verified paths: `weapons/blunt/mace/walk/mace.png`, `weapons/blunt/mace/universal_behind/walk/mace.png`, `weapons/blunt/mace/attack_slash/mace.png`.

```json
{
  "name": "Mace",
  "slot": "Weapon",
  "layers": {
    "main": {
      "zPos": 140,
      "paths": {
        "male": "weapons/blunt/mace",
        "female": "weapons/blunt/mace"
      }
    },
    "behind": {
      "zPos": 9,
      "paths": {
        "male": "weapons/blunt/mace/universal_behind",
        "female": "weapons/blunt/mace/universal_behind"
      }
    }
  },
  "variants": ["mace"],
  "animations": ["walk", "thrust", "hurt"]
}
```

- [ ] **Step 8: Create weapon_blunt_waraxe.json**

Verified paths: `weapons/blunt/waraxe/walk/waraxe.png`, `weapons/blunt/waraxe/behind/walk/waraxe.png`.

```json
{
  "name": "Waraxe",
  "slot": "Weapon",
  "layers": {
    "main": {
      "zPos": 140,
      "paths": {
        "male": "weapons/blunt/waraxe",
        "female": "weapons/blunt/waraxe"
      }
    },
    "behind": {
      "zPos": 9,
      "paths": {
        "male": "weapons/blunt/waraxe/behind",
        "female": "weapons/blunt/waraxe/behind"
      }
    }
  },
  "variants": ["waraxe"],
  "animations": ["walk", "hurt"]
}
```

- [ ] **Step 9: Create weapon_magic_wand.json**

Verified paths: `weapons/magic/wand/male/shoot/wand.png` (gendered, limited animations).

```json
{
  "name": "Mage Wand",
  "slot": "Weapon",
  "layers": {
    "main": {
      "zPos": 140,
      "paths": {
        "male": "weapons/magic/wand/male",
        "female": "weapons/magic/wand/female"
      }
    }
  },
  "variants": ["wand"],
  "animations": ["shoot"]
}
```

- [ ] **Step 10: Create shield_spartan.json**

Verified paths: `shields/spartan/fg/walk/spartan.png`, `shields/spartan/bg/walk/spartan.png`.

```json
{
  "name": "Spartan",
  "slot": "Shield",
  "layers": {
    "front": {
      "zPos": 130,
      "paths": {
        "male": "shields/spartan/fg",
        "female": "shields/spartan/fg"
      }
    },
    "behind": {
      "zPos": 8,
      "paths": {
        "male": "shields/spartan/bg",
        "female": "shields/spartan/bg"
      }
    }
  },
  "variants": ["spartan"],
  "animations": ["walk", "slash", "thrust", "shoot", "spellcast", "hurt"]
}
```

- [ ] **Step 11: Create shield_crusader.json**

Verified paths: `shields/crusader/fg/male/walk/crusader.png` (gendered fg), `shields/crusader/bg/walk/crusader.png` (ungendered bg).

```json
{
  "name": "Crusader",
  "slot": "Shield",
  "layers": {
    "front": {
      "zPos": 130,
      "paths": {
        "male": "shields/crusader/fg/male",
        "female": "shields/crusader/fg/female"
      }
    },
    "behind": {
      "zPos": 8,
      "paths": {
        "male": "shields/crusader/bg",
        "female": "shields/crusader/bg"
      }
    }
  },
  "variants": ["crusader"],
  "animations": ["walk", "slash", "thrust", "shoot", "spellcast", "hurt"]
}
```

- [ ] **Step 12: Run all tests**

Run: `dotnet test Darkness.Tests`

Expected: All tests pass.

---

### Task 5: Add bitmap caching and reduce logging in compositor

The compositor reloads the same PNG file separately for every animation row. `GodotFileSystemService` logs every image load. `ImageUtils.CreateSpriteFrames` recreates idle animations inside a loop.

**Files:**
- Modify: `Darkness.Core/Services/SkiaSharpSpriteCompositor.cs` — add bitmap cache
- Modify: `Darkness.Godot/src/Services/GodotFileSystemService.cs:63` — remove per-image logging
- Modify: `Darkness.Godot/src/Core/ImageUtils.cs:62-66` — move idle creation outside loop

- [ ] **Step 1: Add bitmap caching to SkiaSharpSpriteCompositor**

In `SkiaSharpSpriteCompositor.cs`, modify `CompositeFullSheet` to cache bitmaps. Add a dictionary at the start of the method, replace the inner bitmap loading, and dispose all at end:

At the top of `CompositeFullSheet`, after `canvas.Clear`:

```csharp
var bitmapCache = new Dictionary<string, SKBitmap?>();
```

Add a helper method to the class:

```csharp
private async Task<SKBitmap?> LoadBitmap(string fullPath, IFileSystemService fileSystem, Dictionary<string, SKBitmap?> cache)
{
    if (cache.TryGetValue(fullPath, out var cached)) return cached;
    try
    {
        using var stream = await fileSystem.OpenAppPackageFileAsync(fullPath);
        var bitmap = SKBitmap.Decode(stream);
        cache[fullPath] = bitmap;
        return bitmap;
    }
    catch
    {
        cache[fullPath] = null;
        return null;
    }
}
```

Replace all occurrences of:
```csharp
using var stream = await fileSystem.OpenAppPackageFileAsync(BASE_PATH + path);
using var bitmap = SKBitmap.Decode(stream);
if (bitmap != null)
```

With:
```csharp
var bitmap = await LoadBitmap(BASE_PATH + path, fileSystem, bitmapCache);
if (bitmap != null)
```

Remove the `using` on bitmap since the cache owns the lifecycle. At the end of the method, before the return, dispose all cached bitmaps:

```csharp
foreach (var bmp in bitmapCache.Values)
    bmp?.Dispose();
```

Also apply the same pattern for `CompositePreviewFrame`.

- [ ] **Step 2: Integrate FileExists into the cache to avoid double probing**

In `ResolveAssetPath`, the `FileExists` calls can be eliminated since `LoadBitmap` already handles missing files gracefully (returns null and caches the miss). Change the approach: instead of probing with FileExists, try loading directly and use the cache.

Replace `ResolveAssetPath`:

```csharp
private string ResolveAssetPath(SheetLayer layer, string animation, string variant, string gender)
{
    string layerPath = layer.GetPath(gender);
    if (string.IsNullOrEmpty(layerPath)) return string.Empty;

    if (layerPath.StartsWith("assets/sprites/full/"))
        layerPath = layerPath.Replace("assets/sprites/full/", "");

    string basePath = layerPath.TrimEnd('/');

    // Strategy 1: {animation}/{variant}.png (weapons/gear with variants)
    string path1 = $"{basePath}/{animation}/{variant}.png";

    // Strategy 2: {animation}.png (body/head/face single files)
    string path2 = $"{basePath}/{animation}.png";

    // Strategy 3: Direct file
    if (layerPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        return layerPath;

    // Return paths in priority order — caller tries loading and falls through
    return path1;
}
```

Actually, a cleaner approach: return multiple candidate paths and let the caller try them against the cache:

```csharp
private List<string> ResolveAssetPaths(SheetLayer layer, string animation, string variant, string gender)
{
    var candidates = new List<string>();
    string layerPath = layer.GetPath(gender);
    if (string.IsNullOrEmpty(layerPath)) return candidates;

    if (layerPath.StartsWith("assets/sprites/full/"))
        layerPath = layerPath.Replace("assets/sprites/full/", "");

    string basePath = layerPath.TrimEnd('/');

    // Strategy 1: {animation}/{variant}.png
    candidates.Add($"{basePath}/{animation}/{variant}.png");

    // Strategy 2: {animation}.png
    candidates.Add($"{basePath}/{animation}.png");

    // Strategy 3: attack_{animation}/{variant}.png (for slash/thrust/shoot)
    if (animation == "slash" || animation == "thrust" || animation == "shoot")
    {
        candidates.Add($"{basePath}/attack_{animation}/{variant}.png");
        candidates.Add($"{basePath}/attack_{animation}.png");
    }

    // Strategy 4: Direct file path
    if (layerPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        candidates.Add(layerPath);

    return candidates;
}
```

Then in the compositing loop, replace the path resolution + load with:

```csharp
var candidates = ResolveAssetPaths(layer.Layer, animation, variant, gender);
SKBitmap? bitmap = null;
foreach (var candidate in candidates)
{
    bitmap = await LoadBitmap(BASE_PATH + candidate, fileSystem, bitmapCache);
    if (bitmap != null) break;
}
if (bitmap != null)
{
    // ... draw frames ...
}
```

- [ ] **Step 3: Remove verbose per-image logging from GodotFileSystemService**

In `Darkness.Godot/src/Services/GodotFileSystemService.cs`, remove or comment out the log at line 63:

```csharp
// Remove this line:
// GD.Print($"[FileSystem] Loaded image via ResourceLoader: {path} ({pngBytes.Length} bytes, Format: {img.GetFormat()})");
```

Also remove the fallback log at line 83:

```csharp
// Remove this line:
// GD.Print($"[FileSystem] Successfully read {bytes.Length} bytes from {path} (Fallback/Non-Image)");
```

Keep the error logs (GD.PrintErr) — those are important.

- [ ] **Step 4: Fix idle animation duplication in ImageUtils**

In `Darkness.Godot/src/Core/ImageUtils.cs`, move the idle creation OUTSIDE the foreach loop. Replace the contents of `CreateSpriteFrames` after the frames setup:

```csharp
foreach (var kvp in SheetConstants.AnimationRows)
{
    string anim = kvp.Key;
    int startRow = kvp.Value;
    int frameCount = SheetConstants.FrameCounts[anim];

    AddLpcRow(frames, tex, $"{anim}_up", startRow + 0, frameCount, frameWidth, frameHeight);
    AddLpcRow(frames, tex, $"{anim}_left", startRow + 1, frameCount, frameWidth, frameHeight);
    AddLpcRow(frames, tex, $"{anim}_down", startRow + 2, frameCount, frameWidth, frameHeight);
    AddLpcRow(frames, tex, $"{anim}_right", startRow + 3, frameCount, frameWidth, frameHeight);

    if (anim == "hurt" || anim == "climb")
    {
        AddLpcRow(frames, tex, anim, startRow, frameCount, frameWidth, frameHeight);
    }
}

// Generate idles from walk frame 0 (OUTSIDE the loop — only once)
AddSingleFrame(frames, tex, "idle_up", SheetConstants.AnimationRows["walk"] + 0, 0, frameWidth, frameHeight);
AddSingleFrame(frames, tex, "idle_left", SheetConstants.AnimationRows["walk"] + 1, 0, frameWidth, frameHeight);
AddSingleFrame(frames, tex, "idle_down", SheetConstants.AnimationRows["walk"] + 2, 0, frameWidth, frameHeight);
AddSingleFrame(frames, tex, "idle_right", SheetConstants.AnimationRows["walk"] + 3, 0, frameWidth, frameHeight);
```

- [ ] **Step 5: Run all tests**

Run: `dotnet test Darkness.Tests`

Expected: All tests pass.

---

### Task 6: Regenerate sprite sheets and verify visually

Now that all code and data fixes are in place, regenerate the test sprite sheets and verify they contain visible equipment layers.

**Files:**
- Verify: `GeneratedSpriteSheets/*.png` — should be much larger than 18750 bytes

- [ ] **Step 1: Run the sprite sheet generator test**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteSheetGenerator" -v n`

Expected: PASS — generates 10 PNG files.

- [ ] **Step 2: Verify generated file sizes are reasonable**

Run: `ls -la GeneratedSpriteSheets/*.png`

Expected: Files should be significantly larger than 18750 bytes (old working sheets were 111-138KB). If files are still small, there's a remaining path resolution issue.

- [ ] **Step 3: If file sizes are still small, add diagnostic logging**

Add a temporary test to trace exactly which paths succeed/fail:

```csharp
[Fact]
public async Task DiagnosePathResolution()
{
    var appearance = new CharacterAppearance { Head = "Human Male" };
    // Manually seed a sheet definition to DB for this test
    var col = _db.GetCollection<SheetDefinition>("sheet_definitions");

    // Read the actual arming sword JSON
    var root = GetProjectRoot();
    var swordJson = File.ReadAllText(Path.Combine(root, "Darkness.Godot", "assets", "data", "sheet_definitions", "weapons", "sword", "weapon_sword_arming_steel.json"));
    var swordDef = System.Text.Json.JsonSerializer.Deserialize<SheetDefinition>(swordJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    col.Insert(swordDef);

    var definitions = _catalog.GetSheetDefinitions(appearance);

    // Check that weapon definition was included
    var weaponDef = definitions.FirstOrDefault(d => d.Slot == "Weapon");
    Assert.NotNull(weaponDef);
    Assert.True(weaponDef.Layers.Count > 0, "Weapon should have layers");

    // Check variant resolution
    var mainLayer = weaponDef.Layers.Values.First();
    var path = mainLayer.GetPath("male");
    Assert.False(string.IsNullOrEmpty(path), $"Weapon main layer should have a male path, got: '{path}'");
}
```

- [ ] **Step 4: Run full test suite**

Run: `dotnet test Darkness.Tests`

Expected: All tests pass.

---

### Task 7: Update SpriteSheetGenerator to seed sheet definitions

The test generator only seeds AppearanceOptions, not SheetDefinitions. Without sheet definitions in the test DB, equipment layers are never resolved.

**Files:**
- Modify: `Darkness.Tests/Generation/SpriteSheetGenerator.cs` — seed sheet definitions from JSON files

- [ ] **Step 1: Update SpriteSheetGenerator constructor to seed SheetDefinitions**

In the constructor, after seeding appearance options, add sheet definition seeding:

```csharp
public SpriteSheetGenerator()
{
    _dbPath = Path.Combine(Path.GetTempPath(), $"SpriteSheetGen_{Guid.NewGuid()}.db");
    _db = new LiteDatabase(_dbPath, new BsonMapper());
    _fsMock = new Mock<IFileSystemService>();

    var root = GetProjectRoot();
    var json = File.ReadAllText(FindSeedFile());
    _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

    var appSeeder = new AppearanceSeeder(_fsMock.Object);
    appSeeder.Seed(_db);

    // Seed SheetDefinitions from actual JSON files
    var sheetDefDir = Path.Combine(root, "Darkness.Godot", "assets", "data", "sheet_definitions");
    var col = _db.GetCollection<SheetDefinition>("sheet_definitions");
    foreach (var file in Directory.GetFiles(sheetDefDir, "*.json", SearchOption.AllDirectories))
    {
        var defJson = File.ReadAllText(file);
        var def = System.Text.Json.JsonSerializer.Deserialize<SheetDefinition>(defJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (def != null) col.Insert(def);
    }

    _catalog = new SheetDefinitionCatalog(_db);
    _compositor = new SkiaSharpSpriteCompositor();
}
```

Add `using System.Text.Json;` at the top if not present.

- [ ] **Step 2: Also update SheetDefinitionCatalogTests constructor to seed from JSON files**

Apply the same pattern to `SheetDefinitionCatalogTests` constructor — replace the manual inserts with loading from actual JSON files:

```csharp
public SheetDefinitionCatalogTests()
{
    _dbPath = Path.Combine(Path.GetTempPath(), $"SheetDefinitionCatalogTests_{Guid.NewGuid()}.db");
    _db = new LiteDatabase(_dbPath, new BsonMapper());
    _fsMock = new Mock<IFileSystemService>();

    var json = File.ReadAllText(FindSeedFile());
    _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

    var appSeeder = new AppearanceSeeder(_fsMock.Object);
    appSeeder.Seed(_db);

    // Seed SheetDefinitions from actual JSON files
    var root = FindProjectRoot();
    var sheetDefDir = Path.Combine(root, "Darkness.Godot", "assets", "data", "sheet_definitions");
    var col = _db.GetCollection<SheetDefinition>("sheet_definitions");
    foreach (var file in Directory.GetFiles(sheetDefDir, "*.json", SearchOption.AllDirectories))
    {
        var defJson = File.ReadAllText(file);
        var def = System.Text.Json.JsonSerializer.Deserialize<SheetDefinition>(defJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (def != null) col.Insert(def);
    }

    _catalog = new SheetDefinitionCatalog(_db);
}

private static string FindProjectRoot()
{
    var dir = Directory.GetCurrentDirectory();
    while (dir != null && !File.Exists(Path.Combine(dir, "Darkness.sln")))
        dir = Directory.GetParent(dir)?.FullName;
    return dir!;
}
```

- [ ] **Step 3: Also update CharacterGenWizardTests constructor identically**

Apply the same sheet definition seeding pattern to `CharacterGenWizardTests`.

- [ ] **Step 4: Run all tests**

Run: `dotnet test Darkness.Tests`

Expected: All tests pass.

- [ ] **Step 5: Regenerate sprite sheets**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteSheetGenerator" -v n`

- [ ] **Step 6: Verify file sizes**

Run: `ls -la GeneratedSpriteSheets/*.png`

Expected: Files significantly larger than 18750 bytes, varying by class (different equipment counts).

---

### Task 8: Final verification — build and run full test suite

- [ ] **Step 1: Build the full solution**

Run: `dotnet build Darkness.sln`

Expected: Build succeeds with 0 errors.

- [ ] **Step 2: Run all tests**

Run: `dotnet test Darkness.Tests`

Expected: All tests pass (should be 190+ with new tests).

- [ ] **Step 3: Verify no regressions in generated sprites**

Open the generated PNG files in `GeneratedSpriteSheets/` and visually confirm:
- Body is visible
- Hair is visible
- Armor/clothing layers are visible
- Weapons are visible for appropriate classes (sword for Knight, dagger for Rogue, etc.)
