# SQLite to LiteDB Migration

**Date:** 2026-04-02
**Status:** Approved
**Scope:** Darkness.Core, Darkness.Godot

## Problem

`SQLitePCLRaw.lib.e_sqlite3` ships no Android-native binaries. When Godot exports for Android, .NET selects the Linux (glibc) `.so` as the closest match. Android uses Bionic libc, so the library fails at runtime with `dlopen failed: library "libc.so.6" not found`. This is a fundamental incompatibility between SQLitePCLRaw's native packaging and Godot's .NET Android export pipeline.

## Solution

Replace SQLite (`sqlite-net-pcl` + `SQLitePCLRaw.*`) with **LiteDB**, a pure managed C# embedded database. Zero native dependencies means it works on every platform .NET runs on without any platform-specific packaging concerns.

Additionally, remove `CommunityToolkit.Mvvm` from `Darkness.Core` since the MAUI-to-Godot migration makes it unnecessary.

## Changes

### Package Changes

**Darkness.Core.csproj:**
- Remove: `sqlite-net-pcl`
- Add: `LiteDB` (latest stable, currently 5.0.21)
- Keep: `CommunityToolkit.Mvvm` (still used by all ViewModels via ViewModelBase)

**Darkness.Godot.csproj:**
- Remove: `SQLitePCLRaw.bundle_e_sqlite3`, `SQLitePCLRaw.provider.e_sqlite3`
- Remove: `FixAndroidSqliteNative` MSBuild target (no longer needed)

### Models (7 files in Darkness.Core/Models/)

Remove all SQLite attributes (`[PrimaryKey]`, `[AutoIncrement]`, `[Indexed]`, `[Unique]`, `using SQLite;`).

LiteDB auto-detects properties named `Id` as the document identity with auto-increment for `int` types, so most models need zero LiteDB-specific attributes.

**Character.cs** additionally:
- Remove `ObservableObject` base class
- Replace `[ObservableProperty] private int _fieldName;` with plain `public int FieldName { get; set; }`
- Remove `using CommunityToolkit.Mvvm.ComponentModel;`
- Keep the `ToSnapshot()` method

**Affected models:** `User`, `Character`, `Item`, `Level`, `Skill`, `Enemy`, `StatusEffect`

### Services (3 files in Darkness.Core/Services/)

Replace `SQLiteAsyncConnection` with `LiteDatabase`. LiteDB is synchronous; wrap operations in `Task.Run()` to preserve existing async interfaces.

**Pattern for each service:**

```csharp
public class UserService : IUserService
{
    private readonly string _dbPath;

    public UserService(LocalDatabaseService dbService)
    {
        _dbPath = dbService.GetLocalFilePath("Darkness.db");
    }

    private LiteDatabase OpenDb() => new LiteDatabase(_dbPath);

    public Task<User?> GetUserByIdAsync(int userId)
    {
        return Task.Run(() =>
        {
            using var db = OpenDb();
            var col = db.GetCollection<User>("users");
            return col.FindById(userId);
        });
    }
}
```

**Collection names:** `users`, `characters`, `items`, `levels`, `skills`, `enemies`, `statusEffects`

**UserService.InitializeAsync():** LiteDB creates collections on first insert automatically. `InitializeAsync()` becomes a no-op that ensures the database file path is accessible. The interface method is retained for backward compatibility with callers.

**CharacterService.SaveCharacterAsync():** Use `col.Upsert(character)` which handles both insert and update based on the `Id` field.

**RewardService.CheckDailyRewardAsync():** Replace `_database.UpdateAsync(user)` with `col.Update(user)`.

**Unique constraint on User.Username:** Enforce via `col.EnsureIndex(u => u.Username, unique: true)` during initialization.

### LocalDatabaseService

Simplify — LiteDB creates the database file on first access. Remove `CopyDatabaseIfNotExistsAsync()` (no seed database needed). Retain `GetLocalFilePath()` as the path provider.

### Global.cs (Darkness.Godot)

Remove:
- Entire `static Global()` constructor (DLL import resolver, `DlOpen`/`DlError` P/Invoke declarations)
- `SQLitePCL.Batteries_V2.Init()` call and its try/catch block
- `using System.Runtime.InteropServices;`

### Interfaces

No changes. `IUserService`, `ICharacterService`, `IRewardService`, `IFileSystemService` remain identical.

### DI Registration (Global._Ready())

No changes. `LocalDatabaseService`, `UserService`, `CharacterService`, `RewardService` remain registered as singletons with the same types.

## Database File

- **New path:** `{AppDataDirectory}/Darkness.db` (LiteDB convention)
- **Old file:** `Darkness.db3` becomes orphaned. Acceptable in early development.
- **Format:** BSON document store (LiteDB v5 format)

## Data Flow (Unchanged)

```
Godot Scenes -> Services (in Core) -> LiteDB file at {AppDataDirectory}/Darkness.db
```

## What Doesn't Change

- All service interfaces
- DI registration
- Database file location (same directory, different filename)
- All callers of these services
- WebAPI project (uses EF Core / SQL Server, unaffected)
- Tests (reference Core only, will need SQLite mocks updated)

## Risk

- Existing `Darkness.db3` files on test devices become orphaned (early development, acceptable)
- LiteDB's LINQ support is more limited than SQLite-net's, but all current queries are simple enough to translate directly
- `Character` losing `ObservableObject` may affect any remaining MAUI code that binds to it — verify no MAUI pages reference Character properties with data binding

## Files Modified

| File | Action |
|------|--------|
| `Darkness.Core/Darkness.Core.csproj` | Remove sqlite-net-pcl, CommunityToolkit.Mvvm; add LiteDB |
| `Darkness.Core/Models/User.cs` | Remove SQLite attributes |
| `Darkness.Core/Models/Character.cs` | Remove SQLite attrs + ObservableObject, convert to POCO |
| `Darkness.Core/Models/Item.cs` | Remove SQLite attributes |
| `Darkness.Core/Models/Level.cs` | Remove SQLite attributes |
| `Darkness.Core/Models/Skill.cs` | Remove SQLite attributes |
| `Darkness.Core/Models/Enemy.cs` | Remove SQLite attributes |
| `Darkness.Core/Models/StatusEffect.cs` | Remove SQLite attributes |
| `Darkness.Core/Services/UserService.cs` | Rewrite to use LiteDB |
| `Darkness.Core/Services/CharacterService.cs` | Rewrite to use LiteDB |
| `Darkness.Core/Services/RewardService.cs` | Rewrite to use LiteDB |
| `Darkness.Core/Data/LocalDatabaseService.cs` | Simplify (remove seed copy logic) |
| `Darkness.Godot/Darkness.Godot.csproj` | Remove SQLitePCLRaw packages + MSBuild target |
| `Darkness.Godot/src/Core/Global.cs` | Remove native lib resolver + SQLite init |
