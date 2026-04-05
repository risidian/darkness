# Sprite, Quest & Leveling System Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace hardcoded sprite dictionaries and monolithic quest JSON with data-driven LiteDB systems, wire up the unused XP/leveling pipeline, and remove all hardcoded quest logic from scenes.

**Architecture:** Sprite catalog and appearance options stored in LiteDB, seeded from JSON. Quest definitions in per-chain JSON files, seeded into LiteDB at startup. Quest state tracked per-character in LiteDB. Leveling service awards XP after combat and checks thresholds from a seeded level table. All seeders follow the same pattern: read JSON, validate, upsert to LiteDB, log errors.

**Tech Stack:** .NET 10, LiteDB, System.Text.Json, XUnit + Moq, CommunityToolkit.Mvvm, Godot 4.6.1

**Spec:** `docs/superpowers/specs/2026-04-05-sprite-quest-redesign-design.md`

---

## File Map

### New Files — Models
- `Darkness.Core/Models/EquipmentSprite.cs` — LiteDB document for equipment-to-sprite mapping
- `Darkness.Core/Models/AppearanceOption.cs` — LiteDB document for hair/skin/face/eye options
- `Darkness.Core/Models/QuestChain.cs` — quest chain container
- `Darkness.Core/Models/QuestStep.cs` — typed quest step (dialogue/combat/location/branch)
- `Darkness.Core/Models/BranchData.cs` — branch options + conditions
- `Darkness.Core/Models/BranchCondition.cs` — extensible condition evaluator
- `Darkness.Core/Models/CombatData.cs` — combat encounter definition (replaces EncounterData)
- `Darkness.Core/Models/EnemySpawn.cs` — enemy reference with count/level override
- `Darkness.Core/Models/RewardData.cs` — reward definition
- `Darkness.Core/Models/LocationTrigger.cs` — location-based trigger
- `Darkness.Core/Models/QuestState.cs` — per-character quest progress in LiteDB
- `Darkness.Core/Models/LevelUpResult.cs` — returned after XP award

### New Files — Services & Interfaces
- `Darkness.Core/Interfaces/ILevelingService.cs` — leveling interface
- `Darkness.Core/Interfaces/ITriggerService.cs` — location trigger interface
- `Darkness.Core/Services/LevelingService.cs` — XP award + level-up logic
- `Darkness.Core/Services/TriggerService.cs` — location trigger resolution
- `Darkness.Core/Services/SpriteSeeder.cs` — seeds EquipmentSprite + AppearanceOption from JSON
- `Darkness.Core/Services/QuestSeeder.cs` — seeds QuestChain from per-chain JSON files
- `Darkness.Core/Services/LevelSeeder.cs` — seeds Level table from JSON
- `Darkness.Core/Services/ConditionEvaluator.cs` — evaluates BranchCondition against character

### New Files — Seed Data
- `Darkness.Godot/assets/data/sprite-catalog.json` — all equipment sprites + appearance options
- `Darkness.Godot/assets/data/level-table.json` — XP thresholds per level
- `Darkness.Godot/assets/data/quests/beat_1_the_awakening.json` — quest chain
- `Darkness.Godot/assets/data/quests/beat_2_dark_warrior.json` — quest chain
- `Darkness.Godot/assets/data/quests/beat_3_the_sorcerer.json` — quest chain

### New Files — Tests
- `Darkness.Tests/Services/LevelingServiceTests.cs`
- `Darkness.Tests/Services/TriggerServiceTests.cs`
- `Darkness.Tests/Services/ConditionEvaluatorTests.cs`
- `Darkness.Tests/Services/SpriteSeederTests.cs`
- `Darkness.Tests/Services/QuestSeederTests.cs`
- `Darkness.Tests/Services/LevelSeederTests.cs`

### Modified Files
- `Darkness.Core/Models/Item.cs` — add EquipmentSlot + EquipmentSpriteId
- `Darkness.Core/Models/Character.cs` — remove CompletedQuestIds
- `Darkness.Core/Models/NavigationArgs.cs` — add quest context to BattleArgs
- `Darkness.Core/Interfaces/ISpriteLayerCatalog.cs` — remove legacy method, add DB dependency
- `Darkness.Core/Interfaces/IQuestService.cs` — new method signatures
- `Darkness.Core/Interfaces/IFileSystemService.cs` — add DirectoryExists, GetFiles
- `Darkness.Core/Services/SpriteLayerCatalog.cs` — rewrite to query LiteDB
- `Darkness.Core/Services/QuestService.cs` — rewrite with AdvanceStep flow
- `Darkness.Core/Data/LocalDatabaseService.cs` — add database accessor
- `Darkness.Godot/src/Core/Global.cs` — register new services, run seeders
- `Darkness.Godot/src/Game/BattleScene.cs` — XP award, quest advance, fix victory nav
- `Darkness.Godot/src/Game/WorldScene.cs` — remove hardcoded IDs, use TriggerService
- `Darkness.Tests/Services/SpriteLayerCatalogTests.cs` — rewrite for LiteDB-backed catalog
- `Darkness.Tests/Services/QuestServiceTests.cs` — rewrite for new service API

### Deleted Files
- `Darkness.Core/Models/SpriteLayerDefinition.cs`
- `Darkness.Core/Models/QuestNode.cs`
- `Darkness.Core/Models/EncounterData.cs`
- `Darkness.Core/Models/DialogueChoice.cs` (replaced by BranchData)
- `Darkness.Godot/assets/data/quests.json` (replaced by per-chain files)

---

## Track A: Sprite System

### Task 1: New Sprite Models

**Files:**
- Create: `Darkness.Core/Models/EquipmentSprite.cs`
- Create: `Darkness.Core/Models/AppearanceOption.cs`

- [ ] **Step 1: Create EquipmentSprite model**

```csharp
// Darkness.Core/Models/EquipmentSprite.cs
namespace Darkness.Core.Models;

public class EquipmentSprite
{
    public int Id { get; set; }
    public string Slot { get; set; } = string.Empty;        // "Armor", "Weapon", "Shield", "Feet", "Legs", "Arms"
    public string DisplayName { get; set; } = string.Empty;  // "Plate (Steel)"
    public string AssetPath { get; set; } = string.Empty;    // "armor/plate" (relative to assets/sprites/full/)
    public string FileNameTemplate { get; set; } = "{action}.png";
    public int ZOrder { get; set; }
    public string Gender { get; set; } = "universal";        // "universal", "male", "female"
    public string? FallbackGender { get; set; }
    public string TintHex { get; set; } = "#FFFFFF";
}
```

- [ ] **Step 2: Create AppearanceOption model**

```csharp
// Darkness.Core/Models/AppearanceOption.cs
namespace Darkness.Core.Models;

public class AppearanceOption
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;     // "Hair", "Skin", "Face", "Eyes", "Head"
    public string DisplayName { get; set; } = string.Empty;  // "Long", "Light"
    public string AssetPath { get; set; } = string.Empty;    // "hair/long/adult"
    public string FileNameTemplate { get; set; } = "{action}.png";
    public string TintHex { get; set; } = "#FFFFFF";
    public int ZOrder { get; set; }
    public string Gender { get; set; } = "universal";
    public string? FallbackGender { get; set; }
}
```

- [ ] **Step 3: Build to verify models compile**

Run: `dotnet build Darkness.Core/Darkness.Core.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/Models/EquipmentSprite.cs Darkness.Core/Models/AppearanceOption.cs
git commit -m "feat: add EquipmentSprite and AppearanceOption models for data-driven sprite catalog"
```

---

### Task 2: Add Item Equipment Link

**Files:**
- Modify: `Darkness.Core/Models/Item.cs`

- [ ] **Step 1: Add equipment properties to Item**

Add these two properties to `Item.cs` after the existing `ArmorClass` property:

```csharp
public string? EquipmentSlot { get; set; }    // "Armor", "Weapon", "Shield", "Feet", "Legs", "Arms"
public int? EquipmentSpriteId { get; set; }   // FK to EquipmentSprite
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build Darkness.Core/Darkness.Core.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Models/Item.cs
git commit -m "feat: add EquipmentSlot and EquipmentSpriteId to Item model"
```

---

### Task 3: Sprite Seed Data File

**Files:**
- Create: `Darkness.Godot/assets/data/sprite-catalog.json`

- [ ] **Step 1: Create the sprite catalog seed file**

This file contains all the equipment sprites and appearance options that currently live as hardcoded dictionaries in `SpriteLayerCatalog.cs`. The data below is a direct translation of every dictionary entry.

```json
{
  "EquipmentSprites": [
    { "Slot": "Armor", "DisplayName": "Plate (Steel)", "AssetPath": "armor/plate", "FileNameTemplate": "{action}/steel.png", "ZOrder": 60, "Gender": "gendered" },
    { "Slot": "Armor", "DisplayName": "Plate (Iron)", "AssetPath": "armor/plate", "FileNameTemplate": "{action}/iron.png", "ZOrder": 60, "Gender": "gendered" },
    { "Slot": "Armor", "DisplayName": "Plate (Gold)", "AssetPath": "armor/plate", "FileNameTemplate": "{action}/gold.png", "ZOrder": 60, "Gender": "gendered" },
    { "Slot": "Armor", "DisplayName": "Leather", "AssetPath": "armor/leather", "FileNameTemplate": "{action}/leather.png", "ZOrder": 60, "Gender": "gendered" },
    { "Slot": "Armor", "DisplayName": "Leather (Black)", "AssetPath": "armor/leather", "FileNameTemplate": "{action}/black.png", "ZOrder": 60, "Gender": "gendered" },
    { "Slot": "Armor", "DisplayName": "Leather (Brown)", "AssetPath": "armor/leather", "FileNameTemplate": "{action}/brown.png", "ZOrder": 60, "Gender": "gendered" },
    { "Slot": "Armor", "DisplayName": "Mage Robes (Blue)", "AssetPath": "torso/robes", "FileNameTemplate": "blue/{action}.png", "ZOrder": 60, "Gender": "female" },
    { "Slot": "Armor", "DisplayName": "Mage Robes (Red)", "AssetPath": "torso/robes", "FileNameTemplate": "red/{action}.png", "ZOrder": 60, "Gender": "female" },
    { "Slot": "Armor", "DisplayName": "Mage Robes (White)", "AssetPath": "torso/robes", "FileNameTemplate": "white/{action}.png", "ZOrder": 60, "Gender": "female" },
    { "Slot": "Armor", "DisplayName": "Longsleeve (White)", "AssetPath": "torso/robes", "FileNameTemplate": "white/{action}.png", "ZOrder": 60, "Gender": "female" },
    { "Slot": "Armor", "DisplayName": "Longsleeve (Blue)", "AssetPath": "torso/robes", "FileNameTemplate": "blue/{action}.png", "ZOrder": 60, "Gender": "female" },
    { "Slot": "Armor", "DisplayName": "Longsleeve (Brown)", "AssetPath": "torso/robes", "FileNameTemplate": "{action}/brown.png", "ZOrder": 60, "Gender": "female" },

    { "Slot": "Weapon", "DisplayName": "Arming Sword (Steel)", "AssetPath": "weapons/sword/arming/universal/bg", "FileNameTemplate": "{action}/steel.png", "ZOrder": 140, "Gender": "universal" },
    { "Slot": "Weapon", "DisplayName": "Arming Sword (Iron)", "AssetPath": "weapons/sword/arming/universal/bg", "FileNameTemplate": "{action}/iron.png", "ZOrder": 140, "Gender": "universal" },
    { "Slot": "Weapon", "DisplayName": "Arming Sword (Gold)", "AssetPath": "weapons/sword/arming/universal/bg", "FileNameTemplate": "{action}/gold.png", "ZOrder": 140, "Gender": "universal" },
    { "Slot": "Weapon", "DisplayName": "Dagger (Steel)", "AssetPath": "weapons/sword/dagger", "FileNameTemplate": "{action}/dagger.png", "ZOrder": 140, "Gender": "universal" },
    { "Slot": "Weapon", "DisplayName": "Recurve Bow", "AssetPath": "weapons/ranged/bow/normal/walk/foreground", "FileNameTemplate": "steel.png", "ZOrder": 140, "Gender": "universal" },
    { "Slot": "Weapon", "DisplayName": "Mage Wand", "AssetPath": "weapons/magic/wand", "FileNameTemplate": "slash/wand.png", "ZOrder": 140, "Gender": "gendered" },

    { "Slot": "Shield", "DisplayName": "Crusader", "AssetPath": "shields/crusader/bg", "FileNameTemplate": "{action}/crusader.png", "ZOrder": 130, "Gender": "universal" },
    { "Slot": "Shield", "DisplayName": "Spartan", "AssetPath": "shields/spartan/bg", "FileNameTemplate": "{action}/spartan.png", "ZOrder": 130, "Gender": "universal" },

    { "Slot": "Feet", "DisplayName": "Boots (Basic)", "AssetPath": "feet/shoes/basic", "FileNameTemplate": "{action}/black.png", "ZOrder": 15, "Gender": "gendered" },
    { "Slot": "Feet", "DisplayName": "Boots (Fold)", "AssetPath": "feet/shoes/basic", "FileNameTemplate": "{action}/black.png", "ZOrder": 15, "Gender": "gendered" },
    { "Slot": "Feet", "DisplayName": "Boots (Rimmed)", "AssetPath": "feet/shoes/basic", "FileNameTemplate": "{action}/black.png", "ZOrder": 15, "Gender": "gendered" },
    { "Slot": "Feet", "DisplayName": "Shoes", "AssetPath": "feet/shoes/basic", "FileNameTemplate": "{action}/black.png", "ZOrder": 15, "Gender": "gendered" },
    { "Slot": "Feet", "DisplayName": "Sandals", "AssetPath": "feet/shoes/basic", "FileNameTemplate": "{action}/black.png", "ZOrder": 15, "Gender": "gendered" },

    { "Slot": "Arms", "DisplayName": "Gloves", "AssetPath": "arms/gloves", "FileNameTemplate": "{action}/black.png", "ZOrder": 55, "Gender": "gendered" },

    { "Slot": "Legs", "DisplayName": "Slacks", "AssetPath": "legs/pants", "FileNameTemplate": "{action}/black.png", "ZOrder": 40, "Gender": "gendered" },
    { "Slot": "Legs", "DisplayName": "Leggings", "AssetPath": "legs/leggings", "FileNameTemplate": "{action}/black.png", "ZOrder": 40, "Gender": "male", "FallbackGender": "male" },
    { "Slot": "Legs", "DisplayName": "Formal", "AssetPath": "legs/formal", "FileNameTemplate": "{action}/black.png", "ZOrder": 40, "Gender": "gendered" },
    { "Slot": "Legs", "DisplayName": "Cuffed", "AssetPath": "legs/cuffed", "FileNameTemplate": "{action}/black.png", "ZOrder": 40, "Gender": "male", "FallbackGender": "male" },
    { "Slot": "Legs", "DisplayName": "Pantaloons", "AssetPath": "legs/pantaloons", "FileNameTemplate": "{action}/black.png", "ZOrder": 40, "Gender": "male", "FallbackGender": "male" }
  ],
  "AppearanceOptions": [
    { "Category": "Hair", "DisplayName": "Long", "AssetPath": "hair/long/adult", "FileNameTemplate": "{action}/blonde.png", "ZOrder": 120 },
    { "Category": "Hair", "DisplayName": "Plain", "AssetPath": "hair/plain/adult", "FileNameTemplate": "{action}/blonde.png", "ZOrder": 120 },
    { "Category": "Hair", "DisplayName": "Curly Long", "AssetPath": "hair/curly_long/adult", "FileNameTemplate": "{action}/blonde.png", "ZOrder": 120 },
    { "Category": "Hair", "DisplayName": "Shorthawk", "AssetPath": "hair/shorthawk/adult", "FileNameTemplate": "{action}/blonde.png", "ZOrder": 120 },
    { "Category": "Hair", "DisplayName": "Spiked", "AssetPath": "hair/spiked/adult", "FileNameTemplate": "{action}/blonde.png", "ZOrder": 120 },
    { "Category": "Hair", "DisplayName": "Bob", "AssetPath": "hair/bob/adult", "FileNameTemplate": "{action}/blonde.png", "ZOrder": 120 },
    { "Category": "Hair", "DisplayName": "Afro", "AssetPath": "hair/afro/adult", "FileNameTemplate": "{action}/blonde.png", "ZOrder": 120 },

    { "Category": "Skin", "DisplayName": "Light", "TintHex": "#FFFFFF", "AssetPath": "", "ZOrder": 0 },
    { "Category": "Skin", "DisplayName": "Amber", "TintHex": "#E0AC69", "AssetPath": "", "ZOrder": 0 },
    { "Category": "Skin", "DisplayName": "Olive", "TintHex": "#C68642", "AssetPath": "", "ZOrder": 0 },
    { "Category": "Skin", "DisplayName": "Taupe", "TintHex": "#8D5524", "AssetPath": "", "ZOrder": 0 },
    { "Category": "Skin", "DisplayName": "Bronze", "TintHex": "#754C24", "AssetPath": "", "ZOrder": 0 },
    { "Category": "Skin", "DisplayName": "Brown", "TintHex": "#4B3018", "AssetPath": "", "ZOrder": 0 },
    { "Category": "Skin", "DisplayName": "Black", "TintHex": "#2D1B0F", "AssetPath": "", "ZOrder": 0 },

    { "Category": "HairColor", "DisplayName": "Blonde", "TintHex": "#FFFFFF", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "Black", "TintHex": "#090806", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "Dark Brown", "TintHex": "#3B3024", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "Redhead", "TintHex": "#A52A2A", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "White", "TintHex": "#EAEAEA", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "Gray", "TintHex": "#808080", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "Platinum", "TintHex": "#E5E4E2", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "Chestnut", "TintHex": "#954535", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "Blue", "TintHex": "#0000FF", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "Green", "TintHex": "#00FF00", "AssetPath": "", "ZOrder": 0 },
    { "Category": "HairColor", "DisplayName": "Purple", "TintHex": "#800080", "AssetPath": "", "ZOrder": 0 },

    { "Category": "Face", "DisplayName": "Default", "AssetPath": "face/male", "FileNameTemplate": "{action}.png", "ZOrder": 100, "Gender": "gendered" },
    { "Category": "Face", "DisplayName": "Female", "AssetPath": "face/female", "FileNameTemplate": "{action}.png", "ZOrder": 100, "Gender": "female" },

    { "Category": "Eyes", "DisplayName": "Default", "AssetPath": "eyes/human/adult/default", "FileNameTemplate": "{action}/blue.png", "ZOrder": 105 },
    { "Category": "Eyes", "DisplayName": "Neutral", "AssetPath": "eyes/human/adult/neutral", "FileNameTemplate": "{action}/blue.png", "ZOrder": 105 },
    { "Category": "Eyes", "DisplayName": "Anger", "AssetPath": "eyes/human/adult/anger", "FileNameTemplate": "{action}/blue.png", "ZOrder": 105 },
    { "Category": "Eyes", "DisplayName": "Sad", "AssetPath": "eyes/human/adult/sad", "FileNameTemplate": "{action}/blue.png", "ZOrder": 105 },
    { "Category": "Eyes", "DisplayName": "Shock", "AssetPath": "eyes/human/adult/shock", "FileNameTemplate": "{action}/blue.png", "ZOrder": 105 },

    { "Category": "Head", "DisplayName": "Human Male", "AssetPath": "head/human/male", "FileNameTemplate": "{action}.png", "ZOrder": 90 },
    { "Category": "Head", "DisplayName": "Human Female", "AssetPath": "head/human/female", "FileNameTemplate": "{action}.png", "ZOrder": 90 }
  ],
  "ClassDefaults": {
    "Warrior": { "ArmorType": "Plate (Steel)", "WeaponType": "Arming Sword (Steel)", "Feet": "Boots (Basic)", "Arms": "Gloves", "Legs": "Slacks", "ShieldType": "Crusader", "Head": "Human Male", "Face": "Default" },
    "Mage": { "ArmorType": "Mage Robes (Blue)", "WeaponType": "Mage Wand", "Feet": "Sandals", "Arms": "None", "Legs": "Formal", "ShieldType": "None", "Head": "Human Female", "Face": "Female" },
    "Rogue": { "ArmorType": "Leather (Black)", "WeaponType": "Dagger (Steel)", "Feet": "Boots (Fold)", "Arms": "Gloves", "Legs": "Leggings", "ShieldType": "None", "Head": "Human Male", "Face": "Default" },
    "Knight": { "ArmorType": "Plate (Steel)", "WeaponType": "Arming Sword (Steel)", "Feet": "Boots (Rimmed)", "Arms": "Gloves", "Legs": "Formal", "ShieldType": "Spartan", "Head": "Human Male", "Face": "Default" },
    "Cleric": { "ArmorType": "Longsleeve (White)", "WeaponType": "Arming Sword (Iron)", "Feet": "Shoes", "Arms": "None", "Legs": "Slacks", "ShieldType": "Crusader", "Head": "Human Female", "Face": "Female" }
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/assets/data/sprite-catalog.json
git commit -m "feat: add sprite catalog seed data file"
```

---

### Task 4: IFileSystemService Extensions

**Files:**
- Modify: `Darkness.Core/Interfaces/IFileSystemService.cs`

- [ ] **Step 1: Add DirectoryExists and GetFiles to the interface**

Add these methods to `IFileSystemService`:

```csharp
bool DirectoryExists(string path);
string[] GetFiles(string path, string searchPattern);
```

The full interface becomes:

```csharp
using System.IO;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface IFileSystemService
    {
        string AppDataDirectory { get; }
        Task<Stream> OpenAppPackageFileAsync(string filename);
        string ReadAllText(string filename);
        bool DirectoryExists(string path);
        string[] GetFiles(string path, string searchPattern);
    }
}
```

- [ ] **Step 2: Implement in GodotFileSystemService**

Find `Darkness.Godot/src/Services/GodotFileSystemService.cs` and add the implementations. These should delegate to `System.IO.Directory`:

```csharp
public bool DirectoryExists(string path)
{
    var fullPath = Path.Combine("res://", path);
    return Godot.DirAccess.DirExistsAbsolute(Godot.ProjectSettings.GlobalizePath(fullPath));
}

public string[] GetFiles(string path, string searchPattern)
{
    var fullPath = Godot.ProjectSettings.GlobalizePath(Path.Combine("res://", path));
    if (!Directory.Exists(fullPath))
        return Array.Empty<string>();
    return Directory.GetFiles(fullPath, searchPattern);
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build Darkness.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/Interfaces/IFileSystemService.cs Darkness.Godot/src/Services/GodotFileSystemService.cs
git commit -m "feat: add DirectoryExists and GetFiles to IFileSystemService"
```

---

### Task 5: LocalDatabaseService — Add Database Accessor

**Files:**
- Modify: `Darkness.Core/Data/LocalDatabaseService.cs`

- [ ] **Step 1: Add OpenDatabase method**

Currently each service opens its own LiteDatabase. Add a central accessor so seeders and new services can share the pattern:

```csharp
using Darkness.Core.Interfaces;
using LiteDB;
using System.IO;

namespace Darkness.Core.Data
{
    public class LocalDatabaseService
    {
        private readonly IFileSystemService _fileSystem;
        private readonly string _dbPath;

        public LocalDatabaseService(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
            string directory = _fileSystem.AppDataDirectory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            _dbPath = Path.Combine(directory, "Darkness.db");
        }

        public string GetLocalFilePath(string filename)
        {
            string directory = _fileSystem.AppDataDirectory;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return Path.Combine(directory, filename);
        }

        public LiteDatabase OpenDatabase() => new LiteDatabase(_dbPath);
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build Darkness.Core/Darkness.Core.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Data/LocalDatabaseService.cs
git commit -m "feat: add OpenDatabase accessor to LocalDatabaseService"
```

---

### Task 6: Sprite Seeder with Tests

**Files:**
- Create: `Darkness.Core/Services/SpriteSeeder.cs`
- Create: `Darkness.Tests/Services/SpriteSeederTests.cs`

- [ ] **Step 1: Write failing tests for SpriteSeeder**

```csharp
// Darkness.Tests/Services/SpriteSeederTests.cs
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using System.Text.Json;

namespace Darkness.Tests.Services;

public class SpriteSeederTests : IDisposable
{
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly string _dbPath;
    private readonly LiteDatabase _db;

    public SpriteSeederTests()
    {
        _fsMock = new Mock<IFileSystemService>();
        _dbPath = Path.Combine(Path.GetTempPath(), $"SpriteSeederTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void Seed_LoadsEquipmentSpritesIntoDatabase()
    {
        var json = JsonSerializer.Serialize(new
        {
            EquipmentSprites = new[]
            {
                new { Slot = "Armor", DisplayName = "Plate (Steel)", AssetPath = "armor/plate",
                      FileNameTemplate = "{action}/steel.png", ZOrder = 60, Gender = "gendered",
                      FallbackGender = (string?)null, TintHex = "#FFFFFF" }
            },
            AppearanceOptions = Array.Empty<object>(),
            ClassDefaults = new Dictionary<string, object>()
        });
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        var seeder = new SpriteSeeder(_fsMock.Object);
        seeder.Seed(_db);

        var col = _db.GetCollection<EquipmentSprite>("equipment_sprites");
        Assert.Equal(1, col.Count());
        var sprite = col.FindAll().First();
        Assert.Equal("Plate (Steel)", sprite.DisplayName);
        Assert.Equal("Armor", sprite.Slot);
    }

    [Fact]
    public void Seed_LoadsAppearanceOptionsIntoDatabase()
    {
        var json = JsonSerializer.Serialize(new
        {
            EquipmentSprites = Array.Empty<object>(),
            AppearanceOptions = new[]
            {
                new { Category = "Hair", DisplayName = "Long", AssetPath = "hair/long/adult",
                      FileNameTemplate = "{action}/blonde.png", TintHex = "#FFFFFF", ZOrder = 120,
                      Gender = "universal", FallbackGender = (string?)null }
            },
            ClassDefaults = new Dictionary<string, object>()
        });
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        var seeder = new SpriteSeeder(_fsMock.Object);
        seeder.Seed(_db);

        var col = _db.GetCollection<AppearanceOption>("appearance_options");
        Assert.Equal(1, col.Count());
        Assert.Equal("Long", col.FindAll().First().DisplayName);
    }

    [Fact]
    public void Seed_DuplicateRun_DoesNotCreateDuplicates()
    {
        var json = JsonSerializer.Serialize(new
        {
            EquipmentSprites = new[]
            {
                new { Slot = "Armor", DisplayName = "Plate (Steel)", AssetPath = "armor/plate",
                      FileNameTemplate = "{action}/steel.png", ZOrder = 60, Gender = "gendered",
                      FallbackGender = (string?)null, TintHex = "#FFFFFF" }
            },
            AppearanceOptions = Array.Empty<object>(),
            ClassDefaults = new Dictionary<string, object>()
        });
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);

        var seeder = new SpriteSeeder(_fsMock.Object);
        seeder.Seed(_db);
        seeder.Seed(_db);

        var col = _db.GetCollection<EquipmentSprite>("equipment_sprites");
        Assert.Equal(1, col.Count());
    }

    [Fact]
    public void Seed_MissingFile_LogsErrorAndDoesNotThrow()
    {
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json"))
               .Throws(new FileNotFoundException("File not found"));

        var seeder = new SpriteSeeder(_fsMock.Object);
        var ex = Record.Exception(() => seeder.Seed(_db));

        Assert.Null(ex);
        Assert.Equal(0, _db.GetCollection<EquipmentSprite>("equipment_sprites").Count());
    }

    [Fact]
    public void Seed_MalformedJson_LogsErrorAndDoesNotThrow()
    {
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns("{ invalid json");

        var seeder = new SpriteSeeder(_fsMock.Object);
        var ex = Record.Exception(() => seeder.Seed(_db));

        Assert.Null(ex);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteSeederTests" --no-restore`
Expected: Compilation error — `SpriteSeeder` does not exist

- [ ] **Step 3: Implement SpriteSeeder**

```csharp
// Darkness.Core/Services/SpriteSeeder.cs
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class SpriteSeeder
{
    private readonly IFileSystemService _fileSystem;

    public SpriteSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(LiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/sprite-catalog.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpriteSeeder] ERROR: Failed to read sprite-catalog.json — {ex.Message}");
            return;
        }

        SeedData? data;
        try
        {
            data = JsonSerializer.Deserialize<SeedData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[SpriteSeeder] ERROR: Failed to parse sprite-catalog.json — {ex.Message}");
            return;
        }

        if (data == null)
        {
            Console.WriteLine("[SpriteSeeder] ERROR: sprite-catalog.json deserialized to null");
            return;
        }

        var spriteCol = db.GetCollection<EquipmentSprite>("equipment_sprites");
        spriteCol.DeleteAll();
        if (data.EquipmentSprites != null)
        {
            foreach (var sprite in data.EquipmentSprites)
                spriteCol.Insert(sprite);
        }
        spriteCol.EnsureIndex(s => s.Slot);
        spriteCol.EnsureIndex(s => s.DisplayName);

        var optionCol = db.GetCollection<AppearanceOption>("appearance_options");
        optionCol.DeleteAll();
        if (data.AppearanceOptions != null)
        {
            foreach (var option in data.AppearanceOptions)
                optionCol.Insert(option);
        }
        optionCol.EnsureIndex(o => o.Category);
        optionCol.EnsureIndex(o => o.DisplayName);

        Console.WriteLine($"[SpriteSeeder] INFO: Loaded {spriteCol.Count()} equipment sprites and {optionCol.Count()} appearance options");
    }

    private class SeedData
    {
        public List<EquipmentSprite>? EquipmentSprites { get; set; }
        public List<AppearanceOption>? AppearanceOptions { get; set; }
        public Dictionary<string, CharacterAppearance>? ClassDefaults { get; set; }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteSeederTests"`
Expected: All 5 tests pass

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/SpriteSeeder.cs Darkness.Tests/Services/SpriteSeederTests.cs
git commit -m "feat: add SpriteSeeder with JSON-to-LiteDB seeding and error logging"
```

---

### Task 7: Rewrite SpriteLayerCatalog with Tests

**Files:**
- Modify: `Darkness.Core/Interfaces/ISpriteLayerCatalog.cs`
- Modify: `Darkness.Core/Services/SpriteLayerCatalog.cs`
- Modify: `Darkness.Tests/Services/SpriteLayerCatalogTests.cs`

- [ ] **Step 1: Update ISpriteLayerCatalog — remove legacy method**

```csharp
// Darkness.Core/Interfaces/ISpriteLayerCatalog.cs
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISpriteLayerCatalog
    {
        List<StitchLayer> GetStitchLayers(CharacterAppearance appearance);
        CharacterAppearance GetDefaultAppearanceForClass(string className);
        List<string> GetOptionNames(string category);
    }
}
```

`GetOptionNames("Hair")` replaces all the individual `HairStyles`, `ArmorTypes`, etc. properties. One method handles all categories.

- [ ] **Step 2: Rewrite SpriteLayerCatalog to query LiteDB**

```csharp
// Darkness.Core/Services/SpriteLayerCatalog.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class SpriteLayerCatalog : ISpriteLayerCatalog
{
    private readonly LiteDatabase _db;
    private readonly Dictionary<string, CharacterAppearance>? _classDefaults;

    public SpriteLayerCatalog(LiteDatabase db, IFileSystemService fileSystem)
    {
        _db = db;
        try
        {
            var json = fileSystem.ReadAllText("assets/data/sprite-catalog.json");
            var data = JsonSerializer.Deserialize<SeedWrapper>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            _classDefaults = data?.ClassDefaults;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SpriteLayerCatalog] WARN: Could not load class defaults — {ex.Message}");
            _classDefaults = new();
        }
    }

    public List<string> GetOptionNames(string category)
    {
        // Equipment slots query EquipmentSprites
        if (category is "Armor" or "Weapon" or "Shield" or "Feet" or "Legs" or "Arms")
        {
            var col = _db.GetCollection<EquipmentSprite>("equipment_sprites");
            var names = col.Find(s => s.Slot == category).Select(s => s.DisplayName).ToList();
            // Always include "None" for optional slots
            if (category is "Weapon" or "Shield" or "Feet" or "Arms" or "Legs" && !names.Contains("None"))
                names.Add("None");
            return names;
        }

        // Appearance options query AppearanceOptions
        var optCol = _db.GetCollection<AppearanceOption>("appearance_options");
        return optCol.Find(o => o.Category == category).Select(o => o.DisplayName).ToList();
    }

    public List<StitchLayer> GetStitchLayers(CharacterAppearance appearance)
    {
        var spriteCol = _db.GetCollection<EquipmentSprite>("equipment_sprites");
        var optionCol = _db.GetCollection<AppearanceOption>("appearance_options");

        var head = appearance.Head ?? "Human Male";
        string gender = head.ToLower().Contains("female") ? "female" : "male";

        var skinOpt = optionCol.FindOne(o => o.Category == "Skin" && o.DisplayName == (appearance.SkinColor ?? "Light"));
        var hairColorOpt = optionCol.FindOne(o => o.Category == "HairColor" && o.DisplayName == (appearance.HairColor ?? "Black"));
        string skinHex = skinOpt?.TintHex ?? "#FFFFFF";
        string hairHex = hairColorOpt?.TintHex ?? "#FFFFFF";

        var layers = new List<(StitchLayer Layer, int Z)>();

        // Body
        layers.Add((new StitchLayer($"assets/sprites/full/body/{gender}", "{action}.png", skinHex), 10));

        // Head
        var headOpt = optionCol.FindOne(o => o.Category == "Head" && o.DisplayName == head);
        if (headOpt != null)
            layers.Add((new StitchLayer($"assets/sprites/full/{headOpt.AssetPath}", headOpt.FileNameTemplate, skinHex), headOpt.ZOrder));

        // Face
        string faceKey = appearance.Face ?? "Default";
        var faceOpt = optionCol.FindOne(o => o.Category == "Face" && o.DisplayName == faceKey);
        if (faceOpt != null)
        {
            string facePath = faceOpt.Gender == "gendered"
                ? faceOpt.AssetPath.Replace("male", gender)
                : faceOpt.AssetPath;
            layers.Add((new StitchLayer($"assets/sprites/full/{facePath}", faceOpt.FileNameTemplate, skinHex), faceOpt.ZOrder));
        }

        // Eyes
        string eyeKey = appearance.Eyes ?? "Default";
        var eyeOpt = optionCol.FindOne(o => o.Category == "Eyes" && o.DisplayName == eyeKey);
        if (eyeOpt != null)
            layers.Add((new StitchLayer($"assets/sprites/full/{eyeOpt.AssetPath}", eyeOpt.FileNameTemplate), eyeOpt.ZOrder));

        // Hair
        string hairKey = appearance.HairStyle ?? "Long";
        var hairOpt = optionCol.FindOne(o => o.Category == "Hair" && o.DisplayName == hairKey);
        if (hairOpt != null)
            layers.Add((new StitchLayer($"assets/sprites/full/{hairOpt.AssetPath}", hairOpt.FileNameTemplate, hairHex), hairOpt.ZOrder));

        // Equipment slots
        AddEquipmentLayer(spriteCol, layers, "Armor", appearance.ArmorType ?? "Leather", gender);
        AddEquipmentLayer(spriteCol, layers, "Feet", appearance.Feet ?? "Boots (Basic)", gender);
        AddEquipmentLayer(spriteCol, layers, "Legs", appearance.Legs ?? "Slacks", gender);
        AddEquipmentLayer(spriteCol, layers, "Arms", appearance.Arms ?? "None", gender);
        AddEquipmentLayer(spriteCol, layers, "Weapon", appearance.WeaponType ?? "None", gender);
        AddEquipmentLayer(spriteCol, layers, "Shield", appearance.ShieldType ?? "None", gender);

        return layers.OrderBy(l => l.Z).Select(l => l.Layer).ToList();
    }

    private void AddEquipmentLayer(ILiteCollection<EquipmentSprite> col,
        List<(StitchLayer Layer, int Z)> layers, string slot, string displayName, string gender)
    {
        if (displayName == "None") return;

        var sprite = col.FindOne(s => s.Slot == slot && s.DisplayName == displayName);
        if (sprite == null) return;

        string resolvedGender = sprite.Gender switch
        {
            "gendered" => gender,
            "universal" => "",
            _ => sprite.FallbackGender ?? sprite.Gender
        };

        string path = string.IsNullOrEmpty(resolvedGender)
            ? $"assets/sprites/full/{sprite.AssetPath}"
            : $"assets/sprites/full/{sprite.AssetPath}/{resolvedGender}";

        layers.Add((new StitchLayer(path, sprite.FileNameTemplate, sprite.TintHex), sprite.ZOrder));
    }

    public CharacterAppearance GetDefaultAppearanceForClass(string className)
    {
        if (_classDefaults != null && _classDefaults.TryGetValue(className, out var appearance))
            return appearance;

        return new CharacterAppearance
        {
            ArmorType = "Leather",
            WeaponType = "Arming Sword (Steel)",
            Feet = "Boots (Basic)",
            Arms = "None",
            Legs = "Slacks",
            ShieldType = "None",
            Head = "Human Male",
            Face = "Default"
        };
    }

    private class SeedWrapper
    {
        public Dictionary<string, CharacterAppearance>? ClassDefaults { get; set; }
    }
}
```

- [ ] **Step 3: Rewrite SpriteLayerCatalogTests**

```csharp
// Darkness.Tests/Services/SpriteLayerCatalogTests.cs
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;

namespace Darkness.Tests.Services;

public class SpriteLayerCatalogTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly SpriteLayerCatalog _catalog;
    private readonly Mock<IFileSystemService> _fsMock;

    public SpriteLayerCatalogTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"SpriteLayerCatalogTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
        _fsMock = new Mock<IFileSystemService>();

        // Seed test data
        var seeder = new SpriteSeeder(_fsMock.Object);
        var json = File.ReadAllText(FindSpritesCatalogPath());
        _fsMock.Setup(f => f.ReadAllText("assets/data/sprite-catalog.json")).Returns(json);
        seeder.Seed(_db);

        _catalog = new SpriteLayerCatalog(_db, _fsMock.Object);
    }

    private static string FindSpritesCatalogPath()
    {
        // Walk up from test bin to find the repo root
        var dir = Directory.GetCurrentDirectory();
        while (dir != null && !File.Exists(Path.Combine(dir, "Darkness.sln")))
            dir = Directory.GetParent(dir)?.FullName;
        return Path.Combine(dir!, "Darkness.Godot", "assets", "data", "sprite-catalog.json");
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void GetOptionNames_ReturnsArmorTypes()
    {
        var armors = _catalog.GetOptionNames("Armor");
        Assert.Contains("Plate (Steel)", armors);
        Assert.Contains("Leather", armors);
        Assert.Contains("Mage Robes (Blue)", armors);
    }

    [Fact]
    public void GetOptionNames_ReturnsWeaponTypesWithNone()
    {
        var weapons = _catalog.GetOptionNames("Weapon");
        Assert.Contains("Arming Sword (Steel)", weapons);
        Assert.Contains("None", weapons);
    }

    [Fact]
    public void GetOptionNames_ReturnsHairStyles()
    {
        var styles = _catalog.GetOptionNames("Hair");
        Assert.Contains("Long", styles);
        Assert.Contains("Afro", styles);
    }

    [Fact]
    public void GetStitchLayers_DefaultAppearance_ReturnsLayersInZOrder()
    {
        var appearance = new CharacterAppearance();
        var layers = _catalog.GetStitchLayers(appearance);

        Assert.NotEmpty(layers);
        // Body should be first (Z=10)
        Assert.Contains("body/male", layers[0].RootPath);
    }

    [Fact]
    public void GetStitchLayers_FemaleHead_UsesFemaleGenderPaths()
    {
        var appearance = new CharacterAppearance { Head = "Human Female" };
        var layers = _catalog.GetStitchLayers(appearance);

        Assert.Contains(layers, l => l.RootPath.Contains("female"));
    }

    [Fact]
    public void GetStitchLayers_MaleOnlyLegs_FallsBackToMale()
    {
        var appearance = new CharacterAppearance { Head = "Human Female", Legs = "Leggings" };
        var layers = _catalog.GetStitchLayers(appearance);

        var legsLayer = layers.FirstOrDefault(l => l.RootPath.Contains("legs"));
        Assert.NotNull(legsLayer);
        // Leggings are male-only, so even with female head should use male
        Assert.Contains("male", legsLayer.RootPath);
    }

    [Fact]
    public void GetStitchLayers_NoneWeapon_ExcludesWeaponLayer()
    {
        var appearance = new CharacterAppearance { WeaponType = "None" };
        var layers = _catalog.GetStitchLayers(appearance);

        Assert.DoesNotContain(layers, l => l.RootPath.Contains("weapon"));
    }

    [Fact]
    public void GetStitchLayers_SkinColor_AppliesTintHex()
    {
        var appearance = new CharacterAppearance { SkinColor = "Amber" };
        var layers = _catalog.GetStitchLayers(appearance);

        var bodyLayer = layers.First(l => l.RootPath.Contains("body"));
        Assert.Equal("#E0AC69", bodyLayer.TintHex);
    }

    [Fact]
    public void GetDefaultAppearanceForClass_Warrior_ReturnsPlateAndSword()
    {
        var appearance = _catalog.GetDefaultAppearanceForClass("Warrior");
        Assert.Equal("Plate (Steel)", appearance.ArmorType);
        Assert.Equal("Arming Sword (Steel)", appearance.WeaponType);
        Assert.Equal("Crusader", appearance.ShieldType);
    }

    [Fact]
    public void GetDefaultAppearanceForClass_Mage_ReturnsRobesAndWand()
    {
        var appearance = _catalog.GetDefaultAppearanceForClass("Mage");
        Assert.Equal("Mage Robes (Blue)", appearance.ArmorType);
        Assert.Equal("Mage Wand", appearance.WeaponType);
    }

    [Fact]
    public void GetDefaultAppearanceForClass_UnknownClass_ReturnsDefault()
    {
        var appearance = _catalog.GetDefaultAppearanceForClass("UnknownClass");
        Assert.Equal("Leather", appearance.ArmorType);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteLayerCatalogTests"`
Expected: All tests pass

- [ ] **Step 5: Delete legacy model**

Delete `Darkness.Core/Models/SpriteLayerDefinition.cs`. Remove any remaining references.

- [ ] **Step 6: Build to verify no broken references**

Run: `dotnet build Darkness.sln`
Expected: Build succeeded (or fix any remaining references to removed types)

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: rewrite SpriteLayerCatalog to query LiteDB, remove legacy sprite system"
```

---

## Track B: Quest System

### Task 8: New Quest Models

**Files:**
- Create: `Darkness.Core/Models/QuestChain.cs`
- Create: `Darkness.Core/Models/QuestStep.cs`
- Create: `Darkness.Core/Models/BranchData.cs`
- Create: `Darkness.Core/Models/BranchCondition.cs`
- Create: `Darkness.Core/Models/CombatData.cs`
- Create: `Darkness.Core/Models/EnemySpawn.cs`
- Create: `Darkness.Core/Models/RewardData.cs`
- Create: `Darkness.Core/Models/LocationTrigger.cs`
- Create: `Darkness.Core/Models/QuestState.cs`

- [ ] **Step 1: Create all quest models**

```csharp
// Darkness.Core/Models/QuestChain.cs
namespace Darkness.Core.Models;

public class QuestChain
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsMainStory { get; set; }
    public int SortOrder { get; set; }
    public List<string> Prerequisites { get; set; } = new();
    public List<QuestStep> Steps { get; set; } = new();
}
```

```csharp
// Darkness.Core/Models/QuestStep.cs
namespace Darkness.Core.Models;

public class QuestStep
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;  // "dialogue", "combat", "location", "branch"
    public string? NextStepId { get; set; }
    public DialogueData? Dialogue { get; set; }
    public CombatData? Combat { get; set; }
    public LocationTrigger? Location { get; set; }
    public BranchData? Branch { get; set; }
}
```

```csharp
// Darkness.Core/Models/BranchData.cs
namespace Darkness.Core.Models;

public class BranchData
{
    public List<BranchOption> Options { get; set; } = new();
}

public class BranchOption
{
    public string Text { get; set; } = string.Empty;
    public string NextStepId { get; set; } = string.Empty;
    public int MoralityImpact { get; set; }
    public List<BranchCondition>? Conditions { get; set; }
}
```

```csharp
// Darkness.Core/Models/BranchCondition.cs
namespace Darkness.Core.Models;

public class BranchCondition
{
    public string Type { get; set; } = string.Empty;     // "morality", "class", "has_item", "quest_completed"
    public string Operator { get; set; } = string.Empty;  // ">=", "<=", "==", "contains"
    public string Value { get; set; } = string.Empty;
}
```

```csharp
// Darkness.Core/Models/CombatData.cs
namespace Darkness.Core.Models;

public class CombatData
{
    public List<EnemySpawn> Enemies { get; set; } = new();
    public string? BackgroundKey { get; set; }
    public int? SurvivalTurns { get; set; }
    public List<RewardData>? Rewards { get; set; }
}
```

```csharp
// Darkness.Core/Models/EnemySpawn.cs
namespace Darkness.Core.Models;

public class EnemySpawn
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Accuracy { get; set; }
    public int Evasion { get; set; }
    public string SpriteKey { get; set; } = "hound";
    public bool IsInvincible { get; set; }
    public int MoralityImpact { get; set; }
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
}
```

```csharp
// Darkness.Core/Models/RewardData.cs
namespace Darkness.Core.Models;

public class RewardData
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public int ExperiencePoints { get; set; }
}
```

```csharp
// Darkness.Core/Models/LocationTrigger.cs
namespace Darkness.Core.Models;

public class LocationTrigger
{
    public string LocationKey { get; set; } = string.Empty;
    public string? SceneKey { get; set; }
}
```

```csharp
// Darkness.Core/Models/QuestState.cs
namespace Darkness.Core.Models;

public class QuestState
{
    public int Id { get; set; }
    public int CharacterId { get; set; }
    public string ChainId { get; set; } = string.Empty;
    public string CurrentStepId { get; set; } = string.Empty;
    public string Status { get; set; } = "available";  // "available", "in_progress", "completed"
    public Dictionary<string, string> Flags { get; set; } = new();
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build Darkness.Core/Darkness.Core.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Models/QuestChain.cs Darkness.Core/Models/QuestStep.cs Darkness.Core/Models/BranchData.cs Darkness.Core/Models/BranchCondition.cs Darkness.Core/Models/CombatData.cs Darkness.Core/Models/EnemySpawn.cs Darkness.Core/Models/RewardData.cs Darkness.Core/Models/LocationTrigger.cs Darkness.Core/Models/QuestState.cs
git commit -m "feat: add new quest system models (QuestChain, QuestStep, QuestState, BranchData, CombatData)"
```

---

### Task 9: Quest Chain Seed Data Files

**Files:**
- Create: `Darkness.Godot/assets/data/quests/beat_1_the_awakening.json`
- Create: `Darkness.Godot/assets/data/quests/beat_2_dark_warrior.json`
- Create: `Darkness.Godot/assets/data/quests/beat_3_the_sorcerer.json`

- [ ] **Step 1: Create beat_1 chain file**

```json
{
  "Id": "beat_1",
  "Title": "The Awakening",
  "IsMainStory": true,
  "SortOrder": 1,
  "Prerequisites": [],
  "Steps": [
    {
      "Id": "beat_1_intro",
      "Type": "branch",
      "Dialogue": {
        "Speaker": "Old Man",
        "Lines": [
          "Welcome to the Shore of Camelot, Wanderer.",
          "The path to the castle is blocked by shadows.",
          "You must choose your path carefully."
        ]
      },
      "Branch": {
        "Options": [
          {
            "Text": "[Fight] I will slay the hounds.",
            "NextStepId": "beat_1_combat_dialogue",
            "MoralityImpact": 5
          },
          {
            "Text": "[Sneak] I will find another way.",
            "NextStepId": "beat_1_stealth_dialogue",
            "MoralityImpact": -5
          }
        ]
      }
    },
    {
      "Id": "beat_1_combat_dialogue",
      "Type": "dialogue",
      "NextStepId": "beat_1_combat",
      "Dialogue": {
        "Speaker": "Old Man",
        "Lines": [
          "Brave choice. Head to the east, best of luck to you.",
          "The hounds are relentless, but your blade is sharp."
        ]
      }
    },
    {
      "Id": "beat_1_combat",
      "Type": "combat",
      "Location": { "LocationKey": "SandyShore_East" },
      "Combat": {
        "Enemies": [
          { "Name": "Hell Hound 1", "Level": 1, "MaxHP": 50, "CurrentHP": 50, "Attack": 7, "Defense": 5, "Speed": 12, "SpriteKey": "hound", "ExperienceReward": 25 },
          { "Name": "Undead Dog B", "Level": 1, "MaxHP": 50, "CurrentHP": 50, "Attack": 6, "Defense": 5, "Speed": 12, "SpriteKey": "hound", "ExperienceReward": 25 },
          { "Name": "Undead Dog C", "Level": 1, "MaxHP": 50, "CurrentHP": 50, "Attack": 6, "Defense": 5, "Speed": 12, "SpriteKey": "hound", "ExperienceReward": 25 }
        ],
        "BackgroundKey": "forest"
      }
    },
    {
      "Id": "beat_1_stealth_dialogue",
      "Type": "dialogue",
      "NextStepId": "beat_1_stealth",
      "Dialogue": {
        "Speaker": "Old Man",
        "Lines": [
          "Wise choice. The shadows are thickest near the main road.",
          "Follow the riverbed to the east, but move quietly."
        ]
      }
    },
    {
      "Id": "beat_1_stealth",
      "Type": "location",
      "NextStepId": "beat_1_sneak_combat",
      "Location": { "LocationKey": "SandyShore_East", "SceneKey": "stealth" }
    },
    {
      "Id": "beat_1_sneak_combat",
      "Type": "combat",
      "Combat": {
        "Enemies": [
          { "Name": "Creek Monster", "Level": 3, "MaxHP": 300, "CurrentHP": 300, "Attack": 15, "Defense": 8, "Speed": 10, "SpriteKey": "hound", "MoralityImpact": 2, "ExperienceReward": 100 }
        ],
        "BackgroundKey": "riverbed"
      }
    }
  ]
}
```

- [ ] **Step 2: Create beat_2 chain file**

```json
{
  "Id": "beat_2",
  "Title": "Dark Warrior",
  "IsMainStory": true,
  "SortOrder": 2,
  "Prerequisites": ["beat_1"],
  "Steps": [
    {
      "Id": "beat_2_dialogue",
      "Type": "dialogue",
      "NextStepId": "beat_2_combat",
      "Dialogue": {
        "Speaker": "Dark Warrior",
        "Lines": [
          "You dare approach the gates of Camelot?",
          "Your journey ends here, mortal.",
          "The shadow consumes all."
        ]
      }
    },
    {
      "Id": "beat_2_combat",
      "Type": "combat",
      "Combat": {
        "Enemies": [
          { "Name": "Balgathor", "Level": 20, "MaxHP": 9999, "CurrentHP": 9999, "Attack": 50, "Defense": 100, "Speed": 30, "IsInvincible": true, "SpriteKey": "bosses/Balgathor", "ExperienceReward": 500 }
        ],
        "SurvivalTurns": 5,
        "BackgroundKey": "dark_castle"
      }
    }
  ]
}
```

- [ ] **Step 3: Create beat_3 chain file**

```json
{
  "Id": "beat_3",
  "Title": "The Sorcerer",
  "IsMainStory": true,
  "SortOrder": 3,
  "Prerequisites": ["beat_2"],
  "Steps": [
    {
      "Id": "beat_3_dialogue",
      "Type": "dialogue",
      "NextStepId": "beat_3_combat",
      "Dialogue": {
        "Speaker": "The Sorcerer",
        "Lines": [
          "The Dark Warrior was only the beginning.",
          "I have woven the shadows into a cage for you.",
          "Let us see if your spirit is truly unbreakable."
        ]
      }
    },
    {
      "Id": "beat_3_combat",
      "Type": "combat",
      "Combat": {
        "Enemies": [
          { "Name": "Risidian", "Level": 8, "MaxHP": 120, "CurrentHP": 120, "Attack": 35, "Defense": 8, "Speed": 20, "SpriteKey": "bosses/Risidian", "ExperienceReward": 300 }
        ],
        "BackgroundKey": "dark_castle"
      }
    }
  ]
}
```

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/assets/data/quests/
git commit -m "feat: add per-chain quest JSON files (beat_1, beat_2, beat_3)"
```

---

### Task 10: ConditionEvaluator with Tests

**Files:**
- Create: `Darkness.Core/Services/ConditionEvaluator.cs`
- Create: `Darkness.Tests/Services/ConditionEvaluatorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Darkness.Tests/Services/ConditionEvaluatorTests.cs
using Darkness.Core.Models;
using Darkness.Core.Services;

namespace Darkness.Tests.Services;

public class ConditionEvaluatorTests
{
    [Fact]
    public void Evaluate_MoralityGreaterOrEqual_ReturnsTrue_WhenMet()
    {
        var character = new Character { Morality = 10 };
        var condition = new BranchCondition { Type = "morality", Operator = ">=", Value = "5" };

        Assert.True(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_MoralityGreaterOrEqual_ReturnsFalse_WhenNotMet()
    {
        var character = new Character { Morality = 3 };
        var condition = new BranchCondition { Type = "morality", Operator = ">=", Value = "5" };

        Assert.False(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_ClassEquals_ReturnsTrue_WhenMatches()
    {
        var character = new Character { Class = "Mage" };
        var condition = new BranchCondition { Type = "class", Operator = "==", Value = "Mage" };

        Assert.True(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_ClassEquals_ReturnsFalse_WhenDifferent()
    {
        var character = new Character { Class = "Warrior" };
        var condition = new BranchCondition { Type = "class", Operator = "==", Value = "Mage" };

        Assert.False(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_HasItem_ReturnsTrue_WhenInventoryContains()
    {
        var character = new Character { Inventory = new List<Item> { new() { Name = "Iron Key" } } };
        var condition = new BranchCondition { Type = "has_item", Operator = "contains", Value = "Iron Key" };

        Assert.True(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_HasItem_ReturnsFalse_WhenMissing()
    {
        var character = new Character { Inventory = new List<Item>() };
        var condition = new BranchCondition { Type = "has_item", Operator = "contains", Value = "Iron Key" };

        Assert.False(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_QuestCompleted_ReturnsTrue_WhenCompleted()
    {
        var character = new Character();
        var completedChainIds = new List<string> { "beat_1" };
        var condition = new BranchCondition { Type = "quest_completed", Operator = "==", Value = "beat_1" };

        Assert.True(ConditionEvaluator.Evaluate(condition, character, completedChainIds));
    }

    [Fact]
    public void Evaluate_NullConditions_ReturnsTrue()
    {
        var character = new Character();
        Assert.True(ConditionEvaluator.EvaluateAll(null, character, new List<string>()));
    }

    [Fact]
    public void Evaluate_EmptyConditions_ReturnsTrue()
    {
        var character = new Character();
        Assert.True(ConditionEvaluator.EvaluateAll(new List<BranchCondition>(), character, new List<string>()));
    }

    [Fact]
    public void Evaluate_UnknownType_ReturnsFalse()
    {
        var character = new Character();
        var condition = new BranchCondition { Type = "unknown", Operator = "==", Value = "x" };

        Assert.False(ConditionEvaluator.Evaluate(condition, character, new List<string>()));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~ConditionEvaluatorTests" --no-restore`
Expected: Compilation error — `ConditionEvaluator` does not exist

- [ ] **Step 3: Implement ConditionEvaluator**

```csharp
// Darkness.Core/Services/ConditionEvaluator.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public static class ConditionEvaluator
{
    public static bool EvaluateAll(List<BranchCondition>? conditions, Character character, List<string> completedChainIds)
    {
        if (conditions == null || conditions.Count == 0)
            return true;
        return conditions.All(c => Evaluate(c, character, completedChainIds));
    }

    public static bool Evaluate(BranchCondition condition, Character character, List<string> completedChainIds)
    {
        return condition.Type switch
        {
            "morality" => EvaluateNumeric(character.Morality, condition.Operator, condition.Value),
            "class" => condition.Operator == "==" && string.Equals(character.Class, condition.Value, StringComparison.OrdinalIgnoreCase),
            "has_item" => character.Inventory.Any(i => string.Equals(i.Name, condition.Value, StringComparison.OrdinalIgnoreCase)),
            "quest_completed" => completedChainIds.Contains(condition.Value),
            _ => false
        };
    }

    private static bool EvaluateNumeric(int actual, string op, string valueStr)
    {
        if (!int.TryParse(valueStr, out var value))
            return false;

        return op switch
        {
            ">=" => actual >= value,
            "<=" => actual <= value,
            "==" => actual == value,
            ">" => actual > value,
            "<" => actual < value,
            _ => false
        };
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~ConditionEvaluatorTests"`
Expected: All 10 tests pass

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/ConditionEvaluator.cs Darkness.Tests/Services/ConditionEvaluatorTests.cs
git commit -m "feat: add ConditionEvaluator for extensible quest branching conditions"
```

---

### Task 11: Quest Seeder with Tests

**Files:**
- Create: `Darkness.Core/Services/QuestSeeder.cs`
- Create: `Darkness.Tests/Services/QuestSeederTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Darkness.Tests/Services/QuestSeederTests.cs
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using System.Text.Json;

namespace Darkness.Tests.Services;

public class QuestSeederTests : IDisposable
{
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly string _dbPath;
    private readonly LiteDatabase _db;

    public QuestSeederTests()
    {
        _fsMock = new Mock<IFileSystemService>();
        _dbPath = Path.Combine(Path.GetTempPath(), $"QuestSeederTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    private string MakeChainJson(string id, string title, int sortOrder = 1)
    {
        var chain = new QuestChain
        {
            Id = id,
            Title = title,
            IsMainStory = true,
            SortOrder = sortOrder,
            Steps = new List<QuestStep>
            {
                new() { Id = $"{id}_step1", Type = "dialogue", Dialogue = new DialogueData { Speaker = "NPC", Lines = new() { "Hello" } } }
            }
        };
        return JsonSerializer.Serialize(chain);
    }

    [Fact]
    public void Seed_LoadsChainsIntoDatabase()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/quests", "*.json")).Returns(new[] { "beat_1.json" });
        _fsMock.Setup(f => f.ReadAllText("beat_1.json")).Returns(MakeChainJson("beat_1", "The Awakening"));

        var seeder = new QuestSeeder(_fsMock.Object);
        seeder.Seed(_db);

        var col = _db.GetCollection<QuestChain>("quest_chains");
        Assert.Equal(1, col.Count());
        Assert.Equal("beat_1", col.FindAll().First().Id);
    }

    [Fact]
    public void Seed_DuplicateRun_DoesNotDuplicate()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/quests", "*.json")).Returns(new[] { "beat_1.json" });
        _fsMock.Setup(f => f.ReadAllText("beat_1.json")).Returns(MakeChainJson("beat_1", "The Awakening"));

        var seeder = new QuestSeeder(_fsMock.Object);
        seeder.Seed(_db);
        seeder.Seed(_db);

        Assert.Equal(1, _db.GetCollection<QuestChain>("quest_chains").Count());
    }

    [Fact]
    public void Seed_MissingDirectory_LogsAndDoesNotThrow()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(false);

        var seeder = new QuestSeeder(_fsMock.Object);
        var ex = Record.Exception(() => seeder.Seed(_db));

        Assert.Null(ex);
    }

    [Fact]
    public void Seed_MalformedJson_SkipsFileAndContinues()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/quests", "*.json")).Returns(new[] { "bad.json", "good.json" });
        _fsMock.Setup(f => f.ReadAllText("bad.json")).Returns("{ not valid }");
        _fsMock.Setup(f => f.ReadAllText("good.json")).Returns(MakeChainJson("beat_1", "Good Chain"));

        var seeder = new QuestSeeder(_fsMock.Object);
        seeder.Seed(_db);

        Assert.Equal(1, _db.GetCollection<QuestChain>("quest_chains").Count());
    }

    [Fact]
    public void Seed_MultipleFiles_LoadsAll()
    {
        _fsMock.Setup(f => f.DirectoryExists("assets/data/quests")).Returns(true);
        _fsMock.Setup(f => f.GetFiles("assets/data/quests", "*.json")).Returns(new[] { "a.json", "b.json" });
        _fsMock.Setup(f => f.ReadAllText("a.json")).Returns(MakeChainJson("beat_1", "Chain A", 1));
        _fsMock.Setup(f => f.ReadAllText("b.json")).Returns(MakeChainJson("beat_2", "Chain B", 2));

        var seeder = new QuestSeeder(_fsMock.Object);
        seeder.Seed(_db);

        Assert.Equal(2, _db.GetCollection<QuestChain>("quest_chains").Count());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~QuestSeederTests" --no-restore`
Expected: Compilation error

- [ ] **Step 3: Implement QuestSeeder**

```csharp
// Darkness.Core/Services/QuestSeeder.cs
using System;
using System.IO;
using System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class QuestSeeder
{
    private readonly IFileSystemService _fileSystem;

    public QuestSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(LiteDatabase db)
    {
        const string questDir = "assets/data/quests";

        if (!_fileSystem.DirectoryExists(questDir))
        {
            Console.WriteLine($"[QuestSeeder] ERROR: Quest directory not found: {questDir}");
            return;
        }

        string[] files;
        try
        {
            files = _fileSystem.GetFiles(questDir, "*.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuestSeeder] ERROR: Failed to list quest files — {ex.Message}");
            return;
        }

        var chainCol = db.GetCollection<QuestChain>("quest_chains");
        chainCol.DeleteAll();

        int chainCount = 0, stepCount = 0, errorCount = 0;

        foreach (var file in files)
        {
            try
            {
                var json = _fileSystem.ReadAllText(file);
                var chain = JsonSerializer.Deserialize<QuestChain>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (chain == null)
                {
                    Console.WriteLine($"[QuestSeeder] WARN: File deserialized to null: {Path.GetFileName(file)}");
                    errorCount++;
                    continue;
                }

                chainCol.Upsert(chain);
                chainCount++;
                stepCount += chain.Steps.Count;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[QuestSeeder] ERROR: Failed to parse quest file: {Path.GetFileName(file)} — {ex.Message}");
                errorCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QuestSeeder] ERROR: Failed to read quest file: {Path.GetFileName(file)} — {ex.Message}");
                errorCount++;
            }
        }

        chainCol.EnsureIndex(c => c.Id);
        chainCol.EnsureIndex(c => c.IsMainStory);

        Console.WriteLine($"[QuestSeeder] INFO: Loaded {chainCount} quest chains with {stepCount} steps from {files.Length} files ({errorCount} errors)");
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~QuestSeederTests"`
Expected: All 5 tests pass

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Services/QuestSeeder.cs Darkness.Tests/Services/QuestSeederTests.cs
git commit -m "feat: add QuestSeeder with per-chain JSON loading, validation, and error logging"
```

---

### Task 12: Rewrite QuestService with Tests

**Files:**
- Modify: `Darkness.Core/Interfaces/IQuestService.cs`
- Modify: `Darkness.Core/Services/QuestService.cs`
- Modify: `Darkness.Tests/Services/QuestServiceTests.cs`

- [ ] **Step 1: Update IQuestService interface**

```csharp
// Darkness.Core/Interfaces/IQuestService.cs
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IQuestService
{
    List<QuestChain> GetAvailableChains(Character character);
    QuestChain? GetChainById(string chainId);
    QuestStep? GetCurrentStep(Character character, string chainId);
    QuestStep? AdvanceStep(Character character, string chainId, string? choiceStepId = null);
    QuestState? GetQuestState(int characterId, string chainId);
    bool IsMainStoryComplete(Character character);
    List<string> GetCompletedChainIds(int characterId);
}
```

- [ ] **Step 2: Write failing tests**

```csharp
// Darkness.Tests/Services/QuestServiceTests.cs
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;
using System.Text.Json;

namespace Darkness.Tests.Services;

public class QuestServiceTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly QuestService _questService;

    public QuestServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"QuestServiceTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);

        // Seed test chains
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "beat_1",
            Title = "The Awakening",
            IsMainStory = true,
            SortOrder = 1,
            Prerequisites = new(),
            Steps = new List<QuestStep>
            {
                new() { Id = "beat_1_intro", Type = "branch",
                    Branch = new BranchData { Options = new()
                    {
                        new BranchOption { Text = "Fight", NextStepId = "beat_1_combat", MoralityImpact = 5 },
                        new BranchOption { Text = "Sneak", NextStepId = "beat_1_stealth", MoralityImpact = -5 }
                    }}},
                new() { Id = "beat_1_combat", Type = "combat" },
                new() { Id = "beat_1_stealth", Type = "location" }
            }
        });
        chains.Insert(new QuestChain
        {
            Id = "beat_2",
            Title = "Dark Warrior",
            IsMainStory = true,
            SortOrder = 2,
            Prerequisites = new() { "beat_1" },
            Steps = new List<QuestStep>
            {
                new() { Id = "beat_2_combat", Type = "combat" }
            }
        });

        _questService = new QuestService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void GetAvailableChains_ReturnsChainWithNoPrereqs()
    {
        var character = new Character { Id = 1 };
        var chains = _questService.GetAvailableChains(character);
        Assert.Single(chains);
        Assert.Equal("beat_1", chains[0].Id);
    }

    [Fact]
    public void GetAvailableChains_ExcludesChainWithUnmetPrereqs()
    {
        var character = new Character { Id = 1 };
        var chains = _questService.GetAvailableChains(character);
        Assert.DoesNotContain(chains, c => c.Id == "beat_2");
    }

    [Fact]
    public void GetAvailableChains_IncludesChainWhenPrereqsComplete()
    {
        var character = new Character { Id = 1 };
        // Complete beat_1
        var stateCol = _db.GetCollection<QuestState>("quest_states");
        stateCol.Insert(new QuestState { CharacterId = 1, ChainId = "beat_1", Status = "completed" });

        var chains = _questService.GetAvailableChains(character);
        Assert.Contains(chains, c => c.Id == "beat_2");
    }

    [Fact]
    public void GetCurrentStep_ReturnsFirstStep_WhenNoState()
    {
        var character = new Character { Id = 1 };
        var step = _questService.GetCurrentStep(character, "beat_1");
        Assert.NotNull(step);
        Assert.Equal("beat_1_intro", step.Id);
    }

    [Fact]
    public void AdvanceStep_WithBranchChoice_SetsCorrectStep()
    {
        var character = new Character { Id = 1 };
        var step = _questService.AdvanceStep(character, "beat_1", "beat_1_combat");

        Assert.NotNull(step);
        Assert.Equal("beat_1_combat", step.Id);

        var state = _questService.GetQuestState(1, "beat_1");
        Assert.NotNull(state);
        Assert.Equal("beat_1_combat", state.CurrentStepId);
        Assert.Equal("in_progress", state.Status);
    }

    [Fact]
    public void AdvanceStep_OnLastStep_CompletesChain()
    {
        var character = new Character { Id = 1 };
        // Advance to combat (the branch choice)
        _questService.AdvanceStep(character, "beat_1", "beat_1_combat");
        // Advance past combat (final step, no NextStepId)
        _questService.AdvanceStep(character, "beat_1");

        var state = _questService.GetQuestState(1, "beat_1");
        Assert.NotNull(state);
        Assert.Equal("completed", state.Status);
    }

    [Fact]
    public void AdvanceStep_AppliesMoralityImpact()
    {
        var character = new Character { Id = 1, Morality = 0 };
        _questService.AdvanceStep(character, "beat_1", "beat_1_combat");

        // Fight choice has MoralityImpact = 5
        Assert.Equal(5, character.Morality);
    }

    [Fact]
    public void IsMainStoryComplete_ReturnsFalse_WhenChainsIncomplete()
    {
        var character = new Character { Id = 1 };
        Assert.False(_questService.IsMainStoryComplete(character));
    }

    [Fact]
    public void IsMainStoryComplete_ReturnsTrue_WhenAllMainChainsComplete()
    {
        var character = new Character { Id = 1 };
        var stateCol = _db.GetCollection<QuestState>("quest_states");
        stateCol.Insert(new QuestState { CharacterId = 1, ChainId = "beat_1", Status = "completed" });
        stateCol.Insert(new QuestState { CharacterId = 1, ChainId = "beat_2", Status = "completed" });

        Assert.True(_questService.IsMainStoryComplete(character));
    }

    [Fact]
    public void GetChainById_ReturnsCorrectChain()
    {
        var chain = _questService.GetChainById("beat_1");
        Assert.NotNull(chain);
        Assert.Equal("The Awakening", chain.Title);
    }

    [Fact]
    public void GetChainById_ReturnsNull_ForUnknownId()
    {
        Assert.Null(_questService.GetChainById("nonexistent"));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~QuestServiceTests" --no-restore`
Expected: Compilation errors

- [ ] **Step 4: Implement new QuestService**

```csharp
// Darkness.Core/Services/QuestService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class QuestService : IQuestService
{
    private readonly LiteDatabase _db;

    public QuestService(LiteDatabase db)
    {
        _db = db;
    }

    public List<QuestChain> GetAvailableChains(Character character)
    {
        var chainCol = _db.GetCollection<QuestChain>("quest_chains");
        var completedIds = GetCompletedChainIds(character.Id);
        var allChains = chainCol.FindAll().ToList();

        return allChains.Where(c =>
            !completedIds.Contains(c.Id) &&
            c.Prerequisites.All(p => completedIds.Contains(p))
        ).OrderBy(c => c.SortOrder).ToList();
    }

    public QuestChain? GetChainById(string chainId)
    {
        var col = _db.GetCollection<QuestChain>("quest_chains");
        return col.FindOne(c => c.Id == chainId);
    }

    public QuestStep? GetCurrentStep(Character character, string chainId)
    {
        var chain = GetChainById(chainId);
        if (chain == null || chain.Steps.Count == 0) return null;

        var state = GetQuestState(character.Id, chainId);
        if (state == null)
            return chain.Steps[0];

        return chain.Steps.FirstOrDefault(s => s.Id == state.CurrentStepId);
    }

    public QuestStep? AdvanceStep(Character character, string chainId, string? choiceStepId = null)
    {
        var chain = GetChainById(chainId);
        if (chain == null) return null;

        var stateCol = _db.GetCollection<QuestState>("quest_states");
        var state = GetQuestState(character.Id, chainId);
        var currentStep = state != null
            ? chain.Steps.FirstOrDefault(s => s.Id == state.CurrentStepId)
            : chain.Steps.FirstOrDefault();

        if (currentStep == null) return null;

        // Determine next step
        string? nextStepId = null;

        if (choiceStepId != null && currentStep.Branch != null)
        {
            // Branch choice — find the matching option and apply morality
            var option = currentStep.Branch.Options.FirstOrDefault(o => o.NextStepId == choiceStepId);
            if (option != null)
            {
                character.Morality += option.MoralityImpact;
                nextStepId = option.NextStepId;
            }
        }
        else
        {
            // Linear advancement
            nextStepId = currentStep.NextStepId;
        }

        if (nextStepId != null)
        {
            var nextStep = chain.Steps.FirstOrDefault(s => s.Id == nextStepId);
            if (nextStep != null)
            {
                // Update or create state
                if (state == null)
                {
                    state = new QuestState
                    {
                        CharacterId = character.Id,
                        ChainId = chainId,
                        CurrentStepId = nextStepId,
                        Status = "in_progress"
                    };
                    stateCol.Insert(state);
                }
                else
                {
                    state.CurrentStepId = nextStepId;
                    state.Status = "in_progress";
                    stateCol.Update(state);
                }
                return nextStep;
            }
        }

        // No next step — chain is complete
        if (state == null)
        {
            state = new QuestState
            {
                CharacterId = character.Id,
                ChainId = chainId,
                CurrentStepId = currentStep.Id,
                Status = "completed"
            };
            stateCol.Insert(state);
        }
        else
        {
            state.Status = "completed";
            stateCol.Update(state);
        }

        return null;
    }

    public QuestState? GetQuestState(int characterId, string chainId)
    {
        var col = _db.GetCollection<QuestState>("quest_states");
        return col.FindOne(s => s.CharacterId == characterId && s.ChainId == chainId);
    }

    public bool IsMainStoryComplete(Character character)
    {
        var chainCol = _db.GetCollection<QuestChain>("quest_chains");
        var mainChains = chainCol.Find(c => c.IsMainStory).ToList();
        if (mainChains.Count == 0) return false;

        var completedIds = GetCompletedChainIds(character.Id);
        return mainChains.All(c => completedIds.Contains(c.Id));
    }

    public List<string> GetCompletedChainIds(int characterId)
    {
        var col = _db.GetCollection<QuestState>("quest_states");
        return col.Find(s => s.CharacterId == characterId && s.Status == "completed")
                  .Select(s => s.ChainId).ToList();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~QuestServiceTests"`
Expected: All 11 tests pass

- [ ] **Step 6: Commit**

```bash
git add Darkness.Core/Interfaces/IQuestService.cs Darkness.Core/Services/QuestService.cs Darkness.Tests/Services/QuestServiceTests.cs
git commit -m "feat: rewrite QuestService with LiteDB-backed chain/step/state model"
```

---

### Task 13: TriggerService with Tests

**Files:**
- Create: `Darkness.Core/Interfaces/ITriggerService.cs`
- Create: `Darkness.Core/Services/TriggerService.cs`
- Create: `Darkness.Tests/Services/TriggerServiceTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Darkness.Tests/Services/TriggerServiceTests.cs
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;

namespace Darkness.Tests.Services;

public class TriggerServiceTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly TriggerService _triggerService;

    public TriggerServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"TriggerServiceTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);

        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "beat_1", Title = "Test", IsMainStory = true, SortOrder = 1,
            Steps = new()
            {
                new() { Id = "beat_1_combat", Type = "combat",
                    Location = new LocationTrigger { LocationKey = "SandyShore_East" } }
            }
        });

        var questService = new QuestService(_db);
        _triggerService = new TriggerService(questService);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void CheckLocationTrigger_ReturnsStep_WhenLocationMatches()
    {
        var character = new Character { Id = 1 };
        var step = _triggerService.CheckLocationTrigger(character, "SandyShore_East");
        Assert.NotNull(step);
        Assert.Equal("beat_1_combat", step.Id);
    }

    [Fact]
    public void CheckLocationTrigger_ReturnsNull_WhenNoMatch()
    {
        var character = new Character { Id = 1 };
        var step = _triggerService.CheckLocationTrigger(character, "UnknownLocation");
        Assert.Null(step);
    }

    [Fact]
    public void CheckLocationTrigger_ReturnsNull_WhenChainPrereqsNotMet()
    {
        // beat_1 has no prereqs, so add a chain that does
        var chains = _db.GetCollection<QuestChain>("quest_chains");
        chains.Insert(new QuestChain
        {
            Id = "beat_2", Title = "Test 2", IsMainStory = true, SortOrder = 2,
            Prerequisites = new() { "beat_1" },
            Steps = new()
            {
                new() { Id = "beat_2_loc", Type = "location",
                    Location = new LocationTrigger { LocationKey = "Forest_Entrance" } }
            }
        });

        var character = new Character { Id = 1 };
        var step = _triggerService.CheckLocationTrigger(character, "Forest_Entrance");
        Assert.Null(step);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~TriggerServiceTests" --no-restore`
Expected: Compilation error

- [ ] **Step 3: Create ITriggerService and TriggerService**

```csharp
// Darkness.Core/Interfaces/ITriggerService.cs
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface ITriggerService
{
    QuestStep? CheckLocationTrigger(Character character, string locationKey);
}
```

```csharp
// Darkness.Core/Services/TriggerService.cs
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public class TriggerService : ITriggerService
{
    private readonly IQuestService _questService;

    public TriggerService(IQuestService questService)
    {
        _questService = questService;
    }

    public QuestStep? CheckLocationTrigger(Character character, string locationKey)
    {
        var availableChains = _questService.GetAvailableChains(character);

        foreach (var chain in availableChains)
        {
            var currentStep = _questService.GetCurrentStep(character, chain.Id);
            if (currentStep?.Location?.LocationKey == locationKey)
                return currentStep;

            // Also check if any step in the chain has this location and is reachable
            foreach (var step in chain.Steps)
            {
                if (step.Location?.LocationKey == locationKey)
                    return step;
            }
        }

        return null;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~TriggerServiceTests"`
Expected: All 3 tests pass

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Interfaces/ITriggerService.cs Darkness.Core/Services/TriggerService.cs Darkness.Tests/Services/TriggerServiceTests.cs
git commit -m "feat: add TriggerService for data-driven location triggers"
```

---

## Track C: XP & Leveling

### Task 14: Level Table Seed Data

**Files:**
- Create: `Darkness.Godot/assets/data/level-table.json`

- [ ] **Step 1: Create level table seed file**

```json
[
  { "Value": 1, "ExperienceRequired": 0 },
  { "Value": 2, "ExperienceRequired": 100 },
  { "Value": 3, "ExperienceRequired": 250 },
  { "Value": 4, "ExperienceRequired": 500 },
  { "Value": 5, "ExperienceRequired": 900 },
  { "Value": 6, "ExperienceRequired": 1400 },
  { "Value": 7, "ExperienceRequired": 2100 },
  { "Value": 8, "ExperienceRequired": 3000 },
  { "Value": 9, "ExperienceRequired": 4200 },
  { "Value": 10, "ExperienceRequired": 5700 },
  { "Value": 11, "ExperienceRequired": 7500 },
  { "Value": 12, "ExperienceRequired": 9800 },
  { "Value": 13, "ExperienceRequired": 12600 },
  { "Value": 14, "ExperienceRequired": 16000 },
  { "Value": 15, "ExperienceRequired": 20000 },
  { "Value": 16, "ExperienceRequired": 25000 },
  { "Value": 17, "ExperienceRequired": 31000 },
  { "Value": 18, "ExperienceRequired": 38000 },
  { "Value": 19, "ExperienceRequired": 46500 },
  { "Value": 20, "ExperienceRequired": 56500 }
]
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/assets/data/level-table.json
git commit -m "feat: add level table seed data (20 levels)"
```

---

### Task 15: Level Seeder with Tests

**Files:**
- Create: `Darkness.Core/Services/LevelSeeder.cs`
- Create: `Darkness.Tests/Services/LevelSeederTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Darkness.Tests/Services/LevelSeederTests.cs
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using LiteDB;
using Moq;

namespace Darkness.Tests.Services;

public class LevelSeederTests : IDisposable
{
    private readonly Mock<IFileSystemService> _fsMock;
    private readonly string _dbPath;
    private readonly LiteDatabase _db;

    public LevelSeederTests()
    {
        _fsMock = new Mock<IFileSystemService>();
        _dbPath = Path.Combine(Path.GetTempPath(), $"LevelSeederTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void Seed_LoadsLevelsIntoDatabase()
    {
        var json = "[{\"Value\":1,\"ExperienceRequired\":0},{\"Value\":2,\"ExperienceRequired\":100}]";
        _fsMock.Setup(f => f.ReadAllText("assets/data/level-table.json")).Returns(json);

        var seeder = new LevelSeeder(_fsMock.Object);
        seeder.Seed(_db);

        var col = _db.GetCollection<Level>("levels");
        Assert.Equal(2, col.Count());
    }

    [Fact]
    public void Seed_DuplicateRun_DoesNotDuplicate()
    {
        var json = "[{\"Value\":1,\"ExperienceRequired\":0}]";
        _fsMock.Setup(f => f.ReadAllText("assets/data/level-table.json")).Returns(json);

        var seeder = new LevelSeeder(_fsMock.Object);
        seeder.Seed(_db);
        seeder.Seed(_db);

        Assert.Equal(1, _db.GetCollection<Level>("levels").Count());
    }

    [Fact]
    public void Seed_MissingFile_DoesNotThrow()
    {
        _fsMock.Setup(f => f.ReadAllText("assets/data/level-table.json"))
               .Throws(new FileNotFoundException());

        var seeder = new LevelSeeder(_fsMock.Object);
        var ex = Record.Exception(() => seeder.Seed(_db));
        Assert.Null(ex);
    }
}
```

- [ ] **Step 2: Implement LevelSeeder**

```csharp
// Darkness.Core/Services/LevelSeeder.cs
using System;
using System.Collections.Generic;
using System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class LevelSeeder
{
    private readonly IFileSystemService _fileSystem;

    public LevelSeeder(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Seed(LiteDatabase db)
    {
        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/level-table.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LevelSeeder] ERROR: Failed to read level-table.json — {ex.Message}");
            return;
        }

        List<Level>? levels;
        try
        {
            levels = JsonSerializer.Deserialize<List<Level>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[LevelSeeder] ERROR: Failed to parse level-table.json — {ex.Message}");
            return;
        }

        if (levels == null || levels.Count == 0)
        {
            Console.WriteLine("[LevelSeeder] WARN: level-table.json is empty or null");
            return;
        }

        var col = db.GetCollection<Level>("levels");
        col.DeleteAll();
        foreach (var level in levels)
            col.Insert(level);
        col.EnsureIndex(l => l.Value);

        Console.WriteLine($"[LevelSeeder] INFO: Loaded {col.Count()} level thresholds (max level: {levels[^1].Value})");
    }
}
```

- [ ] **Step 3: Run tests to verify they pass**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~LevelSeederTests"`
Expected: All 3 tests pass

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/Services/LevelSeeder.cs Darkness.Tests/Services/LevelSeederTests.cs
git commit -m "feat: add LevelSeeder for XP threshold data"
```

---

### Task 16: LevelUpResult Model

**Files:**
- Create: `Darkness.Core/Models/LevelUpResult.cs`

- [ ] **Step 1: Create LevelUpResult**

```csharp
// Darkness.Core/Models/LevelUpResult.cs
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
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Models/LevelUpResult.cs
git commit -m "feat: add LevelUpResult model"
```

---

### Task 17: LevelingService with Tests

**Files:**
- Create: `Darkness.Core/Interfaces/ILevelingService.cs`
- Create: `Darkness.Core/Services/LevelingService.cs`
- Create: `Darkness.Tests/Services/LevelingServiceTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Darkness.Tests/Services/LevelingServiceTests.cs
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;

namespace Darkness.Tests.Services;

public class LevelingServiceTests : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _dbPath;
    private readonly LevelingService _levelingService;

    public LevelingServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"LevelingServiceTests_{Guid.NewGuid()}.db");
        _db = new LiteDatabase(_dbPath);

        // Seed level table
        var levels = _db.GetCollection<Level>("levels");
        levels.Insert(new Level { Value = 1, ExperienceRequired = 0 });
        levels.Insert(new Level { Value = 2, ExperienceRequired = 100 });
        levels.Insert(new Level { Value = 3, ExperienceRequired = 250 });
        levels.Insert(new Level { Value = 4, ExperienceRequired = 500 });
        levels.Insert(new Level { Value = 5, ExperienceRequired = 900 });

        _levelingService = new LevelingService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public void AwardExperience_IncreasesCharacterXp()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 50 };
        var result = _levelingService.AwardExperience(character, 50);

        Assert.Equal(50, character.Experience);
        Assert.Equal(50, result.XpAwarded);
        Assert.Equal(50, result.TotalXp);
        Assert.False(result.DidLevelUp);
    }

    [Fact]
    public void AwardExperience_TriggersLevelUp_WhenThresholdCrossed()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 30, AttributePoints = 0 };
        var result = _levelingService.AwardExperience(character, 100);

        Assert.True(result.DidLevelUp);
        Assert.Equal(2, character.Level);
        Assert.Equal(2, result.NewLevel);
        Assert.Equal(1, result.PreviousLevel);
        Assert.Equal(2, result.AttributePointsAwarded);
    }

    [Fact]
    public void AwardExperience_RestoresHpOnLevelUp()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 10 };
        _levelingService.AwardExperience(character, 100);

        Assert.Equal(50, character.CurrentHP);
    }

    [Fact]
    public void AwardExperience_MultiLevelUp_AwardsCorrectPoints()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 50, AttributePoints = 0 };
        // 500 XP should get to level 4 (threshold is 500)
        var result = _levelingService.AwardExperience(character, 500);

        Assert.Equal(4, character.Level);
        Assert.Equal(3, result.LevelsGained);
        Assert.Equal(6, result.AttributePointsAwarded); // 3 levels * 2 points
    }

    [Fact]
    public void AwardExperience_NoLevelUp_DoesNotRestoreHp()
    {
        var character = new Character { Level = 1, Experience = 0, MaxHP = 50, CurrentHP = 30 };
        _levelingService.AwardExperience(character, 50);

        Assert.Equal(30, character.CurrentHP);
    }

    [Fact]
    public void GetXpToNextLevel_ReturnsCorrectRemaining()
    {
        var character = new Character { Level = 1, Experience = 60 };
        var remaining = _levelingService.GetXpToNextLevel(character);

        Assert.Equal(40, remaining); // 100 - 60
    }

    [Fact]
    public void GetXpToNextLevel_AtMaxLevel_ReturnsZero()
    {
        var character = new Character { Level = 5, Experience = 9999 };
        var remaining = _levelingService.GetXpToNextLevel(character);

        Assert.Equal(0, remaining);
    }

    [Fact]
    public void GetLevelForXp_ReturnsCorrectLevel()
    {
        Assert.Equal(1, _levelingService.GetLevelForXp(0));
        Assert.Equal(1, _levelingService.GetLevelForXp(99));
        Assert.Equal(2, _levelingService.GetLevelForXp(100));
        Assert.Equal(3, _levelingService.GetLevelForXp(250));
        Assert.Equal(5, _levelingService.GetLevelForXp(10000));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~LevelingServiceTests" --no-restore`
Expected: Compilation error

- [ ] **Step 3: Create ILevelingService**

```csharp
// Darkness.Core/Interfaces/ILevelingService.cs
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface ILevelingService
{
    LevelUpResult AwardExperience(Character character, int xp);
    int GetXpToNextLevel(Character character);
    int GetLevelForXp(int totalXp);
}
```

- [ ] **Step 4: Implement LevelingService**

```csharp
// Darkness.Core/Services/LevelingService.cs
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class LevelingService : ILevelingService
{
    private const int AttributePointsPerLevel = 2;
    private readonly LiteDatabase _db;

    public LevelingService(LiteDatabase db)
    {
        _db = db;
    }

    public LevelUpResult AwardExperience(Character character, int xp)
    {
        int previousLevel = character.Level;
        character.Experience += xp;

        int newLevel = GetLevelForXp(character.Experience);
        int levelsGained = newLevel - previousLevel;
        int pointsAwarded = 0;

        if (levelsGained > 0)
        {
            character.Level = newLevel;
            pointsAwarded = levelsGained * AttributePointsPerLevel;
            character.AttributePoints += pointsAwarded;
            character.CurrentHP = character.MaxHP;
        }

        return new LevelUpResult
        {
            XpAwarded = xp,
            TotalXp = character.Experience,
            PreviousLevel = previousLevel,
            NewLevel = newLevel,
            AttributePointsAwarded = pointsAwarded
        };
    }

    public int GetXpToNextLevel(Character character)
    {
        var levels = GetLevelTable();
        var nextLevel = levels.FirstOrDefault(l => l.Value == character.Level + 1);
        if (nextLevel == null) return 0;
        return Math.Max(0, nextLevel.ExperienceRequired - character.Experience);
    }

    public int GetLevelForXp(int totalXp)
    {
        var levels = GetLevelTable();
        int level = 1;
        foreach (var l in levels.OrderBy(l => l.Value))
        {
            if (totalXp >= l.ExperienceRequired)
                level = l.Value;
            else
                break;
        }
        return level;
    }

    private List<Level> GetLevelTable()
    {
        return _db.GetCollection<Level>("levels").FindAll().ToList();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~LevelingServiceTests"`
Expected: All 8 tests pass

- [ ] **Step 6: Commit**

```bash
git add Darkness.Core/Interfaces/ILevelingService.cs Darkness.Core/Services/LevelingService.cs Darkness.Tests/Services/LevelingServiceTests.cs
git commit -m "feat: add LevelingService with XP award, level-up, and threshold queries"
```

---

## Track D: Wiring & Cleanup

### Task 18: Update NavigationArgs

**Files:**
- Modify: `Darkness.Core/Models/NavigationArgs.cs`

- [ ] **Step 1: Add quest context to BattleArgs**

```csharp
// Darkness.Core/Models/NavigationArgs.cs
using Darkness.Core.Models;

namespace Darkness.Core.Models;

public abstract class NavigationArgs
{
}

public class BattleArgs : NavigationArgs
{
    public CombatData? Combat { get; set; }
    public string? QuestChainId { get; set; }
    public string? QuestStepId { get; set; }
}

public class StealthArgs : NavigationArgs
{
    public string? QuestChainId { get; set; }
    public string? QuestStepId { get; set; }
}

public class PvpArgs : NavigationArgs
{
    public Character Player1 { get; set; } = null!;
    public Character Player2 { get; set; } = null!;
}
```

Note: `BattleArgs.Encounter` changes to `BattleArgs.Combat` (new `CombatData` type). All references to `BattleArgs.Encounter` in BattleScene must be updated.

- [ ] **Step 2: Build to find broken references**

Run: `dotnet build Darkness.sln`
Expected: Errors in BattleScene, WorldScene referencing old `Encounter` property. These will be fixed in the next tasks.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Models/NavigationArgs.cs
git commit -m "feat: add quest context to BattleArgs, add StealthArgs, switch to CombatData"
```

---

### Task 19: Remove Character.CompletedQuestIds

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`

- [ ] **Step 1: Remove CompletedQuestIds property**

Delete this line from `Character.cs`:

```csharp
public List<string> CompletedQuestIds { get; set; } = new();
```

Quest state is now tracked in the `QuestState` LiteDB collection, not on the character.

- [ ] **Step 2: Build to find broken references**

Run: `dotnet build Darkness.Core/Darkness.Core.csproj`
Expected: Errors in any code still referencing `CompletedQuestIds`. Note them for cleanup.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Models/Character.cs
git commit -m "refactor: remove CompletedQuestIds from Character (replaced by QuestState collection)"
```

---

### Task 20: Delete Old Quest Models

**Files:**
- Delete: `Darkness.Core/Models/QuestNode.cs`
- Delete: `Darkness.Core/Models/EncounterData.cs`
- Delete: `Darkness.Core/Models/DialogueChoice.cs`
- Delete: `Darkness.Core/Models/SpriteLayerDefinition.cs`
- Delete: `Darkness.Godot/assets/data/quests.json`

- [ ] **Step 1: Delete the old files**

```bash
rm Darkness.Core/Models/QuestNode.cs
rm Darkness.Core/Models/EncounterData.cs
rm Darkness.Core/Models/DialogueChoice.cs
rm Darkness.Core/Models/SpriteLayerDefinition.cs
rm Darkness.Godot/assets/data/quests.json
```

- [ ] **Step 2: Build to identify all remaining references**

Run: `dotnet build Darkness.sln`
Expected: Compilation errors in files that still reference deleted types. Note all of them — they'll be fixed in the scene update tasks.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "refactor: delete legacy models (QuestNode, EncounterData, DialogueChoice, SpriteLayerDefinition) and monolithic quests.json"
```

---

### Task 21: Update DI Container and Run Seeders

**Files:**
- Modify: `Darkness.Godot/src/Core/Global.cs`

- [ ] **Step 1: Update Global.cs to register new services and run seeders**

Replace the current DI setup with updated registrations. The key changes are:
1. Open a shared LiteDatabase and register it
2. Run all three seeders at startup
3. Register new services (LevelingService, TriggerService)
4. Update SpriteLayerCatalog to receive LiteDatabase

```csharp
// In Global._Ready(), replace the service registrations section:

var services = new ServiceCollection();

// Infrastructure
services.AddSingleton<IDispatcherService, GodotDispatcherService>();
services.AddSingleton<IFileSystemService, GodotFileSystemService>();
services.AddSingleton<IDialogService>(sp => new GodotDialogService(this));

// Database
services.AddSingleton<LocalDatabaseService>();
services.AddSingleton<LiteDatabase>(sp =>
{
    var dbService = sp.GetRequiredService<LocalDatabaseService>();
    return dbService.OpenDatabase();
});

// Core Services
services.AddSingleton<ISessionService, SessionService>();
services.AddSingleton<IUserService, UserService>();
services.AddSingleton<ICharacterService, CharacterService>();
services.AddSingleton<ICraftingService, CraftingService>();
services.AddSingleton<IDeathmatchService, DeathmatchService>();
services.AddSingleton<IAllyService, AllyService>();
services.AddSingleton<IQuestService, QuestService>();
services.AddSingleton<ISettingsService, SettingsService>();
services.AddSingleton<IRewardService, RewardService>();
services.AddSingleton<ICombatService, CombatEngine>();
services.AddSingleton<ISpriteCompositor, GodotSpriteCompositor>();
services.AddSingleton<ISpriteLayerCatalog, SpriteLayerCatalog>();
services.AddSingleton<IWeaponSkillService, WeaponSkillService>();
services.AddSingleton<ILevelingService, LevelingService>();
services.AddSingleton<ITriggerService, TriggerService>();
services.AddSingleton<INavigationService>(sp => new GodotNavigationService(this));

Services = services.BuildServiceProvider();

// Run seeders
try
{
    var db = Services.GetRequiredService<LiteDatabase>();
    var fs = Services.GetRequiredService<IFileSystemService>();

    new SpriteSeeder(fs).Seed(db);
    new QuestSeeder(fs).Seed(db);
    new LevelSeeder(fs).Seed(db);

    GD.Print("[Global] Data seeding complete.");
}
catch (Exception ex)
{
    GD.PrintErr($"[Global] Seeding error: {ex.Message}");
}

GD.Print("[Global] DI Container initialized.");
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build Darkness.Godot/Darkness.Godot.csproj`
Expected: Build succeeded (or fix remaining import issues)

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/Core/Global.cs
git commit -m "feat: register new services and run data seeders at startup"
```

---

### Task 22: Update BattleScene — XP Award + Quest Advance + Fix Victory Nav

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

This task requires careful adaptation to the existing BattleScene code. The key changes:

- [ ] **Step 1: Add new service dependencies to BattleScene**

Add fields for the new services at the top of the class:

```csharp
private ILevelingService _leveling = null!;
private IQuestService _questService = null!;
```

In the `_Ready()` or initialization method where services are resolved:

```csharp
_leveling = global.Services!.GetRequiredService<ILevelingService>();
_questService = global.Services!.GetRequiredService<IQuestService>();
```

- [ ] **Step 2: Store quest context from BattleArgs**

Add fields to store quest context:

```csharp
private string? _questChainId;
private string? _questStepId;
```

In `Initialize()`, read quest context from BattleArgs:

```csharp
if (args is BattleArgs battleArgs)
{
    _questChainId = battleArgs.QuestChainId;
    _questStepId = battleArgs.QuestStepId;
    // Update: use battleArgs.Combat instead of battleArgs.Encounter
    // Map CombatData.Enemies (EnemySpawn) to Enemy objects for battle
}
```

- [ ] **Step 3: Update Victory() to award XP and advance quest**

Replace the Victory method's OK button handler. Instead of navigating to MainMenuPage:

```csharp
private void Victory()
{
    _combatLog.AppendText("\n[color=yellow]VICTORY![/color]");
    _endBattleTitle.Text = "VICTORY ACHIEVED";
    _endBattleTitle.Set("theme_override_colors/font_color", Colors.Gold);

    // Sum XP from defeated enemies
    int totalXp = _enemies.Sum(e => e.ExperienceReward);
    int totalGold = _enemies.Sum(e => e.GoldReward);

    // Award XP
    var character = _session.CurrentCharacter!;
    var levelResult = _leveling.AwardExperience(character, totalXp);

    // Build victory message
    var msg = $"All enemies have been defeated!\nXP gained: {totalXp}";
    if (totalGold > 0) msg += $"\nGold: {totalGold}";
    if (levelResult.DidLevelUp)
        msg += $"\n\nLEVEL UP! {levelResult.PreviousLevel} → {levelResult.NewLevel}\n+{levelResult.AttributePointsAwarded} Attribute Points";

    _endBattleMessage.Text = msg;
    _retryButton.Hide();
    _endBattlePanel.Show();

    // Advance quest on OK
    GetNode<Button>("%OkButton").Pressed += async () =>
    {
        if (_questChainId != null)
            _questService.AdvanceStep(character, _questChainId);

        await _navigation.NavigateToAsync("WorldScene");
    };
}
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build Darkness.Godot/Darkness.Godot.csproj`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "feat: award XP on victory, advance quest, navigate to WorldScene instead of MainMenu"
```

---

### Task 23: Update WorldScene — Remove Hardcoded Logic

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

This is the largest scene change. The key transformations:

- [ ] **Step 1: Add TriggerService and QuestService dependencies**

```csharp
private ITriggerService _triggerService = null!;
private IQuestService _questService = null!;
```

Resolve in initialization.

- [ ] **Step 2: Replace hardcoded stealth quest handling in Initialize()**

Remove the hardcoded `beat_1_stealth` completion and `beat_1_sneak_combat` assignment. Replace with:

```csharp
// Handle stealth outcome
if (parameters?.TryGetValue("StealthOutcome", out var outcome) == true
    && parameters.TryGetValue("QuestChainId", out var chainId) == true)
{
    if (outcome == "Success")
    {
        _questService.AdvanceStep(_session.CurrentCharacter!, chainId);
    }
    // On failure, the quest state remains at the stealth step — player can retry
}
```

- [ ] **Step 3: Replace TriggerEncounter() with TriggerService calls**

Remove the `X > 1200` coordinate check. Replace with location-based trigger zones. When the player enters a defined zone:

```csharp
private async void OnLocationEntered(string locationKey)
{
    var character = _session.CurrentCharacter;
    if (character == null) return;

    var step = _triggerService.CheckLocationTrigger(character, locationKey);
    if (step == null) return;

    // Find which chain this step belongs to
    var chains = _questService.GetAvailableChains(character);
    var chain = chains.FirstOrDefault(c => c.Steps.Any(s => s.Id == step.Id));
    if (chain == null) return;

    switch (step.Type)
    {
        case "combat":
            if (step.Combat != null)
            {
                await _navigation.NavigateToAsync("BattleScene",
                    new BattleArgs { Combat = step.Combat, QuestChainId = chain.Id, QuestStepId = step.Id });
            }
            break;
        case "location" when step.Location?.SceneKey == "stealth":
            await _navigation.NavigateToAsync("StealthScene",
                new StealthArgs { QuestChainId = chain.Id, QuestStepId = step.Id });
            break;
        case "dialogue":
            StartDialogue(step, chain.Id);
            break;
        case "branch":
            StartDialogue(step, chain.Id);
            break;
    }
}
```

- [ ] **Step 4: Update dialogue/choice handling to use QuestService.AdvanceStep**

Replace `CompleteQuest` calls with `AdvanceStep`. Replace `_pendingNextQuestId` pattern with direct `AdvanceStep` calls when a choice is made.

- [ ] **Step 5: Check for end-game**

After any quest advancement, check:

```csharp
if (_questService.IsMainStoryComplete(character))
{
    // Navigate to ending scene or display completion dialogue
    GD.Print("[WorldScene] Main story complete!");
}
```

- [ ] **Step 6: Remove all hardcoded quest ID strings**

Search for any remaining string literals like `"beat_1"`, `"beat_1_stealth"`, `"beat_1_sneak_combat"` and remove them.

- [ ] **Step 7: Build to verify**

Run: `dotnet build Darkness.Godot/Darkness.Godot.csproj`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "refactor: remove all hardcoded quest IDs and coordinate triggers from WorldScene"
```

---

### Task 24: Update StealthScene

**Files:**
- Modify: `Darkness.Godot/src/Game/StealthScene.cs`

- [ ] **Step 1: Pass quest context through StealthArgs**

Read `StealthArgs` in initialization and pass quest context back to WorldScene on completion:

```csharp
private string? _questChainId;
private string? _questStepId;

// In initialization:
if (args is StealthArgs stealthArgs)
{
    _questChainId = stealthArgs.QuestChainId;
    _questStepId = stealthArgs.QuestStepId;
}

// In EndGame():
var parameters = new Dictionary<string, string>
{
    ["StealthOutcome"] = success ? "Success" : "Failure"
};
if (_questChainId != null) parameters["QuestChainId"] = _questChainId;
if (_questStepId != null) parameters["QuestStepId"] = _questStepId;

await _navigation.NavigateToAsync(Routes.World, parameters);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build Darkness.Godot/Darkness.Godot.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/Game/StealthScene.cs
git commit -m "feat: pass quest context through StealthScene via StealthArgs"
```

---

### Task 25: Delete Old Quest Model Tests and Update Remaining Tests

**Files:**
- Modify: `Darkness.Tests/Models/QuestModelTests.cs`

- [ ] **Step 1: Rewrite QuestModelTests for new models**

Replace tests for old `QuestNode` with tests for `QuestChain`, `QuestStep`, `BranchData`, `CombatData`:

```csharp
// Darkness.Tests/Models/QuestModelTests.cs
using Darkness.Core.Models;

namespace Darkness.Tests.Models;

public class QuestModelTests
{
    [Fact]
    public void QuestChain_DefaultValues_AreCorrect()
    {
        var chain = new QuestChain();
        Assert.Equal(string.Empty, chain.Id);
        Assert.False(chain.IsMainStory);
        Assert.Empty(chain.Prerequisites);
        Assert.Empty(chain.Steps);
    }

    [Fact]
    public void QuestStep_CanHoldCombatData()
    {
        var step = new QuestStep
        {
            Id = "test_combat",
            Type = "combat",
            Combat = new CombatData
            {
                Enemies = new() { new EnemySpawn { Name = "Goblin", Level = 1, MaxHP = 30 } },
                BackgroundKey = "forest"
            }
        };

        Assert.NotNull(step.Combat);
        Assert.Single(step.Combat.Enemies);
        Assert.Equal("Goblin", step.Combat.Enemies[0].Name);
    }

    [Fact]
    public void QuestStep_CanHoldBranchData()
    {
        var step = new QuestStep
        {
            Id = "test_branch",
            Type = "branch",
            Branch = new BranchData
            {
                Options = new()
                {
                    new BranchOption { Text = "Fight", NextStepId = "combat", MoralityImpact = 5 },
                    new BranchOption { Text = "Run", NextStepId = "flee", MoralityImpact = -5 }
                }
            }
        };

        Assert.Equal(2, step.Branch.Options.Count);
    }

    [Fact]
    public void QuestState_DefaultStatus_IsAvailable()
    {
        var state = new QuestState();
        Assert.Equal("available", state.Status);
        Assert.Empty(state.Flags);
    }

    [Fact]
    public void BranchCondition_CanExpressVariousTypes()
    {
        var conditions = new List<BranchCondition>
        {
            new() { Type = "morality", Operator = ">=", Value = "10" },
            new() { Type = "class", Operator = "==", Value = "Mage" },
            new() { Type = "has_item", Operator = "contains", Value = "Iron Key" }
        };

        Assert.Equal(3, conditions.Count);
    }
}
```

- [ ] **Step 2: Run all tests**

Run: `dotnet test Darkness.Tests`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add Darkness.Tests/Models/QuestModelTests.cs
git commit -m "test: rewrite quest model tests for new QuestChain/QuestStep models"
```

---

### Task 26: Final Build and Full Test Run

- [ ] **Step 1: Build entire solution**

Run: `dotnet build Darkness.sln`
Expected: Build succeeded with no errors

- [ ] **Step 2: Run all tests**

Run: `dotnet test Darkness.Tests`
Expected: All tests pass

- [ ] **Step 3: Fix any remaining issues**

If there are compilation errors from references to deleted types in Godot scene code or other files, fix them.

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "chore: final cleanup — resolve all remaining references to deleted types"
```
