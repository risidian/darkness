# SheetDefinition Catalog & Seeding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor sprite data management by moving equipment to individual SheetDefinition JSON files and implementing a LiteDB-backed catalog.

**Architecture:** Use LiteDB to store and query SheetDefinitions and AppearanceOptions. Separate seeders for different data types.

**Tech Stack:** C#, .NET 10, LiteDB, System.Text.Json.

---

### Task 1: ISheetDefinitionCatalog Interface

**Files:**
- Create: `Darkness.Core/Interfaces/ISheetDefinitionCatalog.cs`

- [ ] **Step 1: Create ISheetDefinitionCatalog interface**

```csharp
using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISheetDefinitionCatalog
    {
        List<SheetDefinition> GetSheetDefinitions(CharacterAppearance appearance);
        CharacterAppearance GetDefaultAppearanceForClass(string className);
        List<string> GetOptionNames(string category, string gender);
        SheetDefinition? GetSheetDefinitionByName(string slot, string displayName);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Interfaces/ISheetDefinitionCatalog.cs
git commit -m "feat: add ISheetDefinitionCatalog interface"
```

### Task 2: SheetDefinitionCatalog Implementation

**Files:**
- Create: `Darkness.Core/Services/SheetDefinitionCatalog.cs`
- Modify: `Darkness.Godot/src/Core/Global.cs`

- [ ] **Step 1: Implement SheetDefinitionCatalog**

```csharp
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services
{
    public class SheetDefinitionCatalog : ISheetDefinitionCatalog
    {
        private readonly LiteDatabase _db;

        public SheetDefinitionCatalog(LiteDatabase db)
        {
            _db = db;
        }

        public List<SheetDefinition> GetSheetDefinitions(CharacterAppearance appearance)
        {
            var sheetCol = _db.GetCollection<SheetDefinition>("sheet_definitions");
            var results = new List<SheetDefinition>();

            // 1. Get equipment sheets
            var equipmentSlots = new[] 
            { 
                appearance.ArmorType, 
                appearance.WeaponType, 
                appearance.ShieldType, 
                appearance.OffHandType,
                appearance.Feet,
                appearance.Arms,
                appearance.Legs
            };

            foreach (var equipmentName in equipmentSlots)
            {
                if (string.IsNullOrEmpty(equipmentName) || equipmentName == "None") continue;
                
                var sheet = sheetCol.FindOne(x => x.Name == equipmentName);
                if (sheet != null) results.Add(sheet);
            }

            return results;
        }

        public CharacterAppearance GetDefaultAppearanceForClass(string className)
        {
            // This will be moved to a collection later, for now we can read it from a specialized collection or hardcode/seed
            var col = _db.GetCollection<ClassDefault>("class_defaults");
            var result = col.FindOne(x => x.ClassName == className);
            return result?.Appearance ?? new CharacterAppearance();
        }

        public List<string> GetOptionNames(string category, string gender)
        {
            var col = _db.GetCollection<AppearanceOption>("appearance_options");
            return col.Find(x => x.Category == category && (x.Gender == "universal" || x.Gender == gender))
                      .Select(x => x.DisplayName)
                      .ToList();
        }

        public SheetDefinition? GetSheetDefinitionByName(string slot, string displayName)
        {
            var col = _db.GetCollection<SheetDefinition>("sheet_definitions");
            return col.FindOne(x => x.Slot == slot && x.Name == displayName);
        }
        
        private class ClassDefault
        {
            public int Id { get; set; }
            public string ClassName { get; set; } = string.Empty;
            public CharacterAppearance Appearance { get; set; } = new();
        }
    }
}
```

- [ ] **Step 2: Register SheetDefinitionCatalog in Global.cs**

Modify `Darkness.Godot/src/Core/Global.cs` to register `ISheetDefinitionCatalog`.

```csharp
services.AddSingleton<ISheetDefinitionCatalog, SheetDefinitionCatalog>();
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Services/SheetDefinitionCatalog.cs Darkness.Godot/src/Core/Global.cs
git commit -m "feat: implement SheetDefinitionCatalog"
```

### Task 3: SheetDefinitionSeeder

**Files:**
- Create: `Darkness.Core/Services/SheetDefinitionSeeder.cs`
- Create: `Darkness.Godot/assets/data/sheet_definitions/`
- Create initial JSON files in `Darkness.Godot/assets/data/sheet_definitions/`

- [ ] **Step 1: Implement SheetDefinitionSeeder**

```csharp
using System;
using System.IO;
using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using SystemJson = System.Text.Json;

namespace Darkness.Core.Services;

public class SheetDefinitionSeeder
{
    private readonly IFileSystemService _fileSystem;

    public SheetDefinitionSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(LiteDatabase db)
    {
        const string dataDir = "assets/data/sheet_definitions";

        if (!_fileSystem.DirectoryExists(dataDir))
        {
            Console.WriteLine($"[SheetDefinitionSeeder] WARN: Directory not found: {dataDir}");
            return;
        }

        var col = db.GetCollection<SheetDefinition>("sheet_definitions");
        col.DeleteAll();

        var files = _fileSystem.GetFiles(dataDir, "*.json");
        int count = 0;

        foreach (var file in files)
        {
            try
            {
                var json = _fileSystem.ReadAllText(file);
                var sheet = SystemJson.JsonSerializer.Deserialize<SheetDefinition>(json, new SystemJson.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (sheet != null)
                {
                    col.Insert(sheet);
                    count++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SheetDefinitionSeeder] ERROR: Failed to seed {file}: {ex.Message}");
            }
        }

        col.EnsureIndex(x => x.Name);
        col.EnsureIndex(x => x.Slot);
        Console.WriteLine($"[SheetDefinitionSeeder] INFO: Loaded {count} sheet definitions.");
    }
}
```

- [ ] **Step 2: Create initial SheetDefinition JSONs**

Create `Darkness.Godot/assets/data/sheet_definitions/arming_sword_steel.json`:
```json
{
  "Name": "Arming Sword (Steel)",
  "Slot": "Weapon",
  "Layers": {
    "Main": {
      "ZPos": 140,
      "Paths": {
        "universal": "assets/sprites/full/weapons/sword/arming/universal/bg/steel.png"
      }
    }
  },
  "Animations": ["walk", "slash", "thrust"]
}
```

(Add more as needed: Dagger (Steel), Waraxe (Iron), Mace (Steel), Mage Wand, Recurve Bow (Steel), Spartan Shield (Steel), Crusader Shield (Steel))

- [ ] **Step 3: Update Global.cs to use SheetDefinitionSeeder**

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/Services/SheetDefinitionSeeder.cs Darkness.Godot/assets/data/sheet_definitions/ Darkness.Godot/src/Core/Global.cs
git commit -m "feat: add SheetDefinitionSeeder and initial data"
```

### Task 4: AppearanceSeeder and sprite-catalog.json Update

**Files:**
- Create: `Darkness.Core/Services/AppearanceSeeder.cs`
- Create: `Darkness.Godot/assets/data/appearance_options.json`
- Modify: `Darkness.Godot/assets/data/sprite-catalog.json`

- [ ] **Step 1: Implement AppearanceSeeder**

It should seed `appearance_options` and `class_defaults`.

- [ ] **Step 2: Move data to appearance_options.json**

- [ ] **Step 3: Update sprite-catalog.json**

- [ ] **Step 4: Update Global.cs**

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat: implement AppearanceSeeder and refactor sprite-catalog.json"
```
