# SQLite to LiteDB Migration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace SQLite (broken on Godot Android due to glibc native lib) with LiteDB (pure managed C#, zero native dependencies).

**Architecture:** Swap the persistence layer from sqlite-net-pcl (SQLiteAsyncConnection) to LiteDB (LiteDatabase). Models become plain POCOs (no SQLite attributes). Services use LiteDB collections with synchronous API wrapped in Task.Run to preserve async interfaces. Character model loses ObservableObject base class (MVVM not needed for models post-MAUI migration).

**Tech Stack:** LiteDB 5.0.x, .NET 10, Godot 4.6.1

**Spec:** `docs/superpowers/specs/2026-04-02-sqlite-to-litedb-migration-design.md`

---

### Task 1: Update Package References

**Files:**
- Modify: `Darkness.Core/Darkness.Core.csproj`
- Modify: `Darkness.Godot/Darkness.Godot.csproj`

- [ ] **Step 1: Update Darkness.Core.csproj**

Remove `sqlite-net-pcl`, add `LiteDB`. Keep `CommunityToolkit.Mvvm` (still used by ViewModels).

Replace the `<ItemGroup>` containing packages:

```xml
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
    <PackageReference Include="LiteDB" Version="5.0.21" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="SkiaSharp" Version="3.116.1" />
  </ItemGroup>
```

- [ ] **Step 2: Update Darkness.Godot.csproj**

Remove `SQLitePCLRaw.bundle_e_sqlite3`, `SQLitePCLRaw.provider.e_sqlite3` packages and the entire `FixAndroidSqliteNative` MSBuild target. The file should become:

```xml
<Project Sdk="Godot.NET.Sdk/4.6.1">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>
  <ItemGroup>
    <Compile Remove="android/**" />
    <EmbeddedResource Remove="android/**" />
    <None Remove="android/**" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Darkness.Core\Darkness.Core.csproj" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.2" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.2" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Restore packages**

Run: `dotnet restore Darkness.sln`
Expected: Successful restore with no errors.

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/Darkness.Core.csproj Darkness.Godot/Darkness.Godot.csproj
git commit -m "chore: swap sqlite-net-pcl for LiteDB, remove SQLitePCLRaw packages"
```

---

### Task 2: Update Models — Remove SQLite Attributes

**Files:**
- Modify: `Darkness.Core/Models/User.cs`
- Modify: `Darkness.Core/Models/Item.cs`
- Modify: `Darkness.Core/Models/Level.cs`
- Modify: `Darkness.Core/Models/Skill.cs`
- Modify: `Darkness.Core/Models/Enemy.cs`
- Modify: `Darkness.Core/Models/StatusEffect.cs`

For each model: remove `using SQLite;` and all SQLite attributes (`[PrimaryKey]`, `[AutoIncrement]`, `[Indexed]`, `[Unique]`). LiteDB auto-detects `Id` as document identity with auto-increment for `int`.

- [ ] **Step 1: Update User.cs**

```csharp
namespace Darkness.Core.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public int Age { get; set; }
        public string TimeZone { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Experience { get; set; }
        public System.DateTime LastLogin { get; set; }
    }
}
```

- [ ] **Step 2: Update Item.cs**

```csharp
namespace Darkness.Core.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Weight { get; set; }
        public int Value { get; set; }
        public int StrengthBonus { get; set; }
        public int DexterityBonus { get; set; }
        public int IntelligenceBonus { get; set; }
        public int DefenseBonus { get; set; }
        public int AttackBonus { get; set; }
    }
}
```

- [ ] **Step 3: Update Level.cs**

```csharp
namespace Darkness.Core.Models
{
    public class Level
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public int ExperienceRequired { get; set; }
    }
}
```

- [ ] **Step 4: Update Skill.cs**

```csharp
namespace Darkness.Core.Models
{
    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ManaCost { get; set; }
        public int StaminaCost { get; set; }
        public int BasePower { get; set; }
        public string SkillType { get; set; } = string.Empty;
    }
}
```

- [ ] **Step 5: Update Enemy.cs**

```csharp
namespace Darkness.Core.Models
{
    public class Enemy
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int STR { get; set; }
        public int DEX { get; set; }
        public int CON { get; set; }
        public int INT { get; set; }
        public int WIS { get; set; }
        public int CHA { get; set; }
        public int MaxHP { get; set; }
        public int CurrentHP { get; set; }
        public int Stamina { get; set; }
        public int Mana { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int MagicDefense { get; set; }
        public int ExperienceReward { get; set; }
        public int GoldReward { get; set; }
        public bool IsInvincible { get; set; } = false;
    }
}
```

- [ ] **Step 6: Update StatusEffect.cs**

```csharp
namespace Darkness.Core.Models
{
    public class StatusEffect
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string EffectType { get; set; } = string.Empty;
        public float Magnitude { get; set; }
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add Darkness.Core/Models/User.cs Darkness.Core/Models/Item.cs Darkness.Core/Models/Level.cs Darkness.Core/Models/Skill.cs Darkness.Core/Models/Enemy.cs Darkness.Core/Models/StatusEffect.cs
git commit -m "refactor: remove SQLite attributes from models"
```

---

### Task 3: Convert Character Model to POCO

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`

Character uses `ObservableObject` + `[ObservableProperty]` from CommunityToolkit.Mvvm. Convert to a plain POCO with standard auto-properties. Keep `ToSnapshot()`.

- [ ] **Step 1: Rewrite Character.cs**

```csharp
namespace Darkness.Core.Models
{
    public class Character
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string HairColor { get; set; } = string.Empty;
        public string HairStyle { get; set; } = string.Empty;
        public string SkinColor { get; set; } = string.Empty;
        public string Face { get; set; } = "Default";
        public string Eyes { get; set; } = "Default";
        public string Head { get; set; } = "Human Male";
        public string Feet { get; set; } = "Boots (Basic)";
        public string Arms { get; set; } = "None";
        public string Legs { get; set; } = "Slacks";
        public string ArmorType { get; set; } = "Leather";
        public string WeaponType { get; set; } = "Arming Sword (Steel)";

        // Base Stats
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }

        // Derived Stats
        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }
        public int Stamina { get; set; }
        public int Mana { get; set; }
        public int Speed { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int Defense { get; set; }
        public int MagicDefense { get; set; }

        public int Level { get; set; }
        public int Experience { get; set; }
        public int AttributePoints { get; set; } = 5;
        public byte[]? Thumbnail { get; set; }

        public CharacterSnapshot ToSnapshot() => new CharacterSnapshot(
            Name, Class, CurrentHP, MaxHP, Level, Thumbnail, HairColor, HairStyle, SkinColor
        );
    }
}
```

- [ ] **Step 2: Build to check for compile errors from Character change**

Run: `dotnet build Darkness.Core/Darkness.Core.csproj 2>&1 | grep -i error | head -20`

Expected: Build errors in ViewModels or services that reference generated property names. The `[ObservableProperty]` source generator creates properties from `_fieldName` → `FieldName`. Since the new POCO uses the same `FieldName` property names, most references should work. However, any code that referenced the private backing field `_fieldName` directly will break.

If errors appear referencing private fields like `_name`, `_class`, etc., fix them in the affected files by replacing `_fieldName` with `FieldName`.

- [ ] **Step 3: Fix any compile errors**

Check output from step 2 and fix any files that reference old generated names. Common patterns to fix:
- `character._name` → `character.Name`
- `character._class` → `character.Class`
- Setter notification methods like `OnNameChanged()` no longer exist — remove calls if present.

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/Models/Character.cs
git commit -m "refactor: convert Character from ObservableObject to plain POCO"
```

---

### Task 4: Simplify LocalDatabaseService

**Files:**
- Modify: `Darkness.Core/Data/LocalDatabaseService.cs`

LiteDB creates the database file automatically on first access. Remove the seed-database copy logic. Keep `GetLocalFilePath()` — services still need the path.

- [ ] **Step 1: Rewrite LocalDatabaseService.cs**

```csharp
using Darkness.Core.Interfaces;
using System.IO;

namespace Darkness.Core.Data
{
    public class LocalDatabaseService
    {
        private readonly IFileSystemService _fileSystem;

        public LocalDatabaseService(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
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
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Data/LocalDatabaseService.cs
git commit -m "refactor: simplify LocalDatabaseService for LiteDB (no seed copy needed)"
```

---

### Task 5: Rewrite UserService for LiteDB

**Files:**
- Modify: `Darkness.Core/Services/UserService.cs`

Replace SQLiteAsyncConnection with LiteDatabase. Use `Task.Run()` to keep async interface. Enforce unique username via `EnsureIndex`.

- [ ] **Step 1: Rewrite UserService.cs**

```csharp
using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class UserService : IUserService
    {
        private readonly string _dbPath;
        private bool _initialized;

        public UserService(LocalDatabaseService dbService)
        {
            _dbPath = dbService.GetLocalFilePath("Darkness.db");
        }

        private LiteDatabase OpenDb() => new LiteDatabase(_dbPath);

        public Task InitializeAsync()
        {
            if (_initialized)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                col.EnsureIndex(u => u.Username, unique: true);
                _initialized = true;
            });
        }

        public Task<bool> CreateUserAsync(User user)
        {
            return Task.Run(async () =>
            {
                await InitializeAsync();
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                var id = col.Insert(user);
                user.Id = id.AsInt32;
                return true;
            });
        }

        public Task<User?> GetUserAsync(string username, string password)
        {
            return Task.Run(async () =>
            {
                await InitializeAsync();
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                return col.FindOne(u => u.Username == username && u.Password == password);
            });
        }

        public Task<User?> GetUserByIdAsync(int userId)
        {
            return Task.Run(async () =>
            {
                await InitializeAsync();
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                return col.FindById(userId);
            });
        }

        public Task<List<User>> GetAllUsersAsync()
        {
            return Task.Run(async () =>
            {
                await InitializeAsync();
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                return col.FindAll().ToList();
            });
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Services/UserService.cs
git commit -m "feat: rewrite UserService to use LiteDB"
```

---

### Task 6: Rewrite CharacterService for LiteDB

**Files:**
- Modify: `Darkness.Core/Services/CharacterService.cs`

- [ ] **Step 1: Rewrite CharacterService.cs**

```csharp
using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly string _dbPath;

        public CharacterService(LocalDatabaseService dbService)
        {
            _dbPath = dbService.GetLocalFilePath("Darkness.db");
        }

        private LiteDatabase OpenDb() => new LiteDatabase(_dbPath);

        public Task<bool> SaveCharacterAsync(Character character)
        {
            return Task.Run(() =>
            {
                using var db = OpenDb();
                var col = db.GetCollection<Character>("characters");
                col.EnsureIndex(c => c.UserId);
                return col.Upsert(character);
            });
        }

        public Task<Character?> GetCharacterByIdAsync(int characterId)
        {
            return Task.Run(() =>
            {
                using var db = OpenDb();
                var col = db.GetCollection<Character>("characters");
                return col.FindById(characterId);
            });
        }

        public Task<List<Character>> GetCharactersForUserAsync(int userId)
        {
            return Task.Run(() =>
            {
                using var db = OpenDb();
                var col = db.GetCollection<Character>("characters");
                col.EnsureIndex(c => c.UserId);
                return col.Find(c => c.UserId == userId).ToList();
            });
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Services/CharacterService.cs
git commit -m "feat: rewrite CharacterService to use LiteDB"
```

---

### Task 7: Rewrite RewardService for LiteDB

**Files:**
- Modify: `Darkness.Core/Services/RewardService.cs`

- [ ] **Step 1: Rewrite RewardService.cs**

```csharp
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Data;
using LiteDB;
using System;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class RewardService : IRewardService
    {
        private readonly string _dbPath;

        public RewardService(LocalDatabaseService dbService)
        {
            _dbPath = dbService.GetLocalFilePath("Darkness.db");
        }

        private LiteDatabase OpenDb() => new LiteDatabase(_dbPath);

        public Task<Item?> CheckDailyRewardAsync(User user)
        {
            if (user == null) return Task.FromResult<Item?>(null);

            return Task.Run(() =>
            {
                DateTime today = DateTime.Today;

                if (user.LastLogin.Date < today)
                {
                    Item reward = GenerateRandomReward();

                    user.LastLogin = DateTime.Now;
                    using var db = OpenDb();
                    var col = db.GetCollection<User>("users");
                    col.Update(user);

                    return (Item?)reward;
                }

                return null;
            });
        }

        private Item GenerateRandomReward()
        {
            var random = new Random();
            int choice = random.Next(3);

            return choice switch
            {
                0 => new Item
                {
                    Name = "Health Potion",
                    Description = "A crimson elixir that mends flesh and bone.",
                    Type = "Consumable",
                    Value = 50
                },
                1 => new Item
                {
                    Name = "Mana Potion",
                    Description = "A swirling blue liquid that restores magical energy.",
                    Type = "Consumable",
                    Value = 50
                },
                _ => new Item
                {
                    Name = "Iron Ore",
                    Description = "Raw iron extracted from the deep earth. Used in smithing.",
                    Type = "Material",
                    Value = 25
                }
            };
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Core/Services/RewardService.cs
git commit -m "feat: rewrite RewardService to use LiteDB"
```

---

### Task 8: Clean Up Global.cs (Godot)

**Files:**
- Modify: `Darkness.Godot/src/Core/Global.cs`

Remove the entire static constructor (DLL import resolver, DlOpen/DlError P/Invoke), the SQLite initialization block, and unused imports.

- [ ] **Step 1: Rewrite Global.cs**

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Godot;
using Darkness.Core.Data;
using Darkness.Core.Services;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Godot.Services;

namespace Darkness.Godot.Core;

public partial class Global : Node
{
    public IServiceProvider? Services { get; private set; }

    public override void _Ready()
    {
        GD.Print("[Global] _Ready started.");
        try
        {
            var services = new ServiceCollection();

            // Infrastructure
            services.AddSingleton<IDispatcherService, GodotDispatcherService>();
            services.AddSingleton<IFileSystemService, GodotFileSystemService>();
            services.AddSingleton<IDialogService>(sp => new GodotDialogService(this));

            // Core Services
            services.AddSingleton<LocalDatabaseService>();
            services.AddSingleton<ISessionService, SessionService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<ICharacterService, CharacterService>();
            services.AddSingleton<ICraftingService, CraftingService>();
            services.AddSingleton<IDeathmatchService, DeathmatchService>();
            services.AddSingleton<IAllyService, AllyService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IRewardService, RewardService>();
            services.AddSingleton<ICombatService, CombatEngine>();
            services.AddSingleton<ISpriteCompositor, SpriteCompositor>();
            services.AddSingleton<ISpriteLayerCatalog, SpriteLayerCatalog>();
            services.AddSingleton<INavigationService>(sp => new GodotNavigationService(this));
            services.AddSingleton<StoryController>();

            Services = services.BuildServiceProvider();
            GD.Print("[Global] DI Container initialized.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Global] Critical error during DI initialization: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/src/Core/Global.cs
git commit -m "fix: remove SQLite native lib resolver from Global.cs (LiteDB needs no native libs)"
```

---

### Task 9: Update Tests

**Files:**
- Modify: `Darkness.Tests/LocalDatabaseServiceTests.cs`
- Modify: `Darkness.Tests/Services/SessionPersistenceTests.cs`

`LocalDatabaseServiceTests` tests the removed `CopyDatabaseIfNotExistsAsync` method. Replace with tests for the simplified service. `SessionPersistenceTests` uses real `UserService` — it should work with LiteDB since we kept the same interface, but verify.

- [ ] **Step 1: Rewrite LocalDatabaseServiceTests.cs**

```csharp
using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Moq;
using System;
using System.IO;
using Xunit;

namespace Darkness.Tests
{
    public class LocalDatabaseServiceTests : IDisposable
    {
        private readonly Mock<IFileSystemService> _fileSystemMock;
        private readonly LocalDatabaseService _service;
        private readonly string _testAppDataDir;

        public LocalDatabaseServiceTests()
        {
            _fileSystemMock = new Mock<IFileSystemService>();
            _testAppDataDir = Path.Combine(Path.GetTempPath(), "DarknessTests_" + Guid.NewGuid().ToString());
            _fileSystemMock.Setup(f => f.AppDataDirectory).Returns(_testAppDataDir);
            _service = new LocalDatabaseService(_fileSystemMock.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testAppDataDir))
            {
                Directory.Delete(_testAppDataDir, true);
            }
        }

        [Fact]
        public void GetLocalFilePath_ReturnsCorrectPath()
        {
            string result = _service.GetLocalFilePath("test.db");
            Assert.Equal(Path.Combine(_testAppDataDir, "test.db"), result);
        }

        [Fact]
        public void GetLocalFilePath_CreatesDirectoryIfMissing()
        {
            // The directory shouldn't exist yet (we didn't create it in constructor)
            Assert.False(Directory.Exists(_testAppDataDir));

            _service.GetLocalFilePath("test.db");

            Assert.True(Directory.Exists(_testAppDataDir));
        }
    }
}
```

- [ ] **Step 2: Verify SessionPersistenceTests still compile**

The tests use `UserService` through its `IUserService` interface. Since the interface is unchanged, the tests should compile and run. The `_fileSystemMock.Setup(f => f.OpenAppPackageFileAsync(...)).ThrowsAsync(...)` setup is no longer needed but harmless (the service no longer calls that method).

Run: `dotnet build Darkness.Tests/Darkness.Tests.csproj 2>&1 | grep -i error | head -20`
Expected: 0 errors.

- [ ] **Step 3: Run all tests**

Run: `dotnet test Darkness.Tests`
Expected: All tests pass. SessionPersistenceTests now uses LiteDB under the hood via UserService.

- [ ] **Step 4: Commit**

```bash
git add Darkness.Tests/LocalDatabaseServiceTests.cs
git commit -m "test: update LocalDatabaseServiceTests for simplified service"
```

---

### Task 10: Full Build and Verify

- [ ] **Step 1: Build entire solution**

Run: `dotnet build Darkness.sln 2>&1 | grep -i error | head -30`
Expected: 0 errors. Warnings about nullable fields in Godot UI files are pre-existing and acceptable.

- [ ] **Step 2: Run all tests**

Run: `dotnet test Darkness.Tests`
Expected: All tests pass.

- [ ] **Step 3: Verify no SQLite references remain**

Run: `grep -ri "sqlite\|SQLitePCLRaw" --include="*.cs" --include="*.csproj" Darkness.Core/ Darkness.Godot/src/ | grep -v "obj/" | grep -v "bin/"`
Expected: Zero matches.

- [ ] **Step 4: Final commit if any fixups needed**

Only commit if steps 1-3 revealed issues that needed fixing.
