# Fix Seeders and Playability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Standardize the database seeding architecture to use a stable Upsert/Delete pattern, implement post-seeding cross-validation, and improve game playability by adding error handling to the splash screen and a quest log UI to the world scene.

**Architecture:** 
- Convert `EncounterSeeder` to use the Upsert + Delete Orphans pattern matching the rest of the project to maintain database ID stability.
- Add a new `DataValidator` service that runs after all seeders in `Global.cs` to verify referential integrity (e.g., Recipe items exist, Quest NextStepIds are valid).
- Enhance `SplashScene` to halt and alert the player if database seeding fails, and enhance `WorldScene` with an overlay label to display current quest progress.

**Tech Stack:** C# 11, .NET 10, Godot 4.6.1, LiteDB

---

### Task 1: Standardize EncounterTable and EncounterSeeder

**Files:**
- Modify: `Darkness.Core/Models/EncounterTable.cs`
- Modify: `Darkness.Core/Services/EncounterSeeder.cs`

- [ ] **Step 1: Add `BsonId` to `EncounterTable`**
We need `BackgroundKey` to act as the primary key so `Upsert` works correctly.

Modify `Darkness.Core/Models/EncounterTable.cs`:

```csharp
using System.Collections.Generic;

namespace Darkness.Core.Models;

public class EncounterTable
{
    [LiteDB.BsonId]
    public string BackgroundKey { get; set; } = string.Empty;
    public int EncounterChance { get; set; } = 5; // Default 5%
    public float EncounterDistance { get; set; } = 1000f; // Default 1000px
    public List<EncounterEntry> Encounters { get; set; } = new();
}
```

- [ ] **Step 2: Update `EncounterSeeder` to use Upsert + Delete Orphans**

Modify `Darkness.Core/Services/EncounterSeeder.cs` inside the `try` block of the `Seed` method:

```csharp
            if (tables != null)
            {
                var loadedKeys = new List<string>();
                foreach (var table in tables)
                {
                    col.Upsert(table);
                    loadedKeys.Add(table.BackgroundKey);
                }
                
                // Cleanup orphaned encounter tables
                col.DeleteMany(x => !loadedKeys.Contains(x.BackgroundKey));
                
                Console.Error.WriteLine($"[EncounterSeeder] INFO: Loaded {tables.Count} encounter tables");
            }
```

- [ ] **Step 3: Run the project to verify it builds**

Run: `dotnet build Darkness.sln`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/Models/EncounterTable.cs Darkness.Core/Services/EncounterSeeder.cs
git commit -m "refactor: standardize EncounterSeeder to use upsert pattern"
```

### Task 2: Create DataValidator for Cross-Validation

**Files:**
- Create: `Darkness.Core/Services/DataValidator.cs`

- [ ] **Step 1: Write the validation logic**

Create `Darkness.Core/Services/DataValidator.cs`:

```csharp
using System;
using System.Linq;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class DataValidator
{
    public static void Validate(ILiteDatabase db)
    {
        Console.WriteLine("[DataValidator] Running post-seed cross-validation...");
        int errorCount = 0;

        var items = db.GetCollection<Item>("items").FindAll().Select(i => i.Name).ToHashSet();
        var recipes = db.GetCollection<Recipe>("recipes").FindAll().ToList();

        // Validate Recipes
        foreach (var recipe in recipes)
        {
            foreach (var kvp in recipe.Materials)
            {
                if (!items.Contains(kvp.Key))
                {
                    Console.Error.WriteLine($"[DataValidator] ERROR: Recipe '{recipe.Name}' requires unknown material '{kvp.Key}'");
                    errorCount++;
                }
            }
        }

        // Validate Quests
        var questChains = db.GetCollection<QuestChain>("quest_chains").FindAll().ToList();
        foreach (var chain in questChains)
        {
            var stepIds = chain.Steps.Select(s => s.Id).ToHashSet();
            foreach (var step in chain.Steps)
            {
                if (step.Branch?.Options != null)
                {
                    foreach (var opt in step.Branch.Options)
                    {
                        if (!string.IsNullOrEmpty(opt.NextStepId) && !stepIds.Contains(opt.NextStepId))
                        {
                            Console.Error.WriteLine($"[DataValidator] ERROR: Quest Chain '{chain.Id}' Step '{step.Id}' branches to missing NextStepId '{opt.NextStepId}'");
                            errorCount++;
                        }
                    }
                }
            }
        }

        if (errorCount > 0)
        {
            throw new Exception($"Data validation failed with {errorCount} errors. See logs for details.");
        }
        Console.WriteLine("[DataValidator] Validation complete. All relationships intact.");
    }
}
```

- [ ] **Step 2: Verify it builds**

Run: `dotnet build Darkness.Core/Darkness.Core.csproj`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Services/DataValidator.cs
git commit -m "feat: add DataValidator for cross-validation of seeded data"
```

### Task 3: Integrate DataValidator into Global.cs

**Files:**
- Modify: `Darkness.Godot/src/Core/Global.cs`

- [ ] **Step 1: Invoke validation after seeding**

In `Darkness.Godot/src/Core/Global.cs`, update the `try` block under `// Seed data synchronously` to call `DataValidator.Validate(db);` after the last seeder:

```csharp
                new RewardSeeder(fs).Seed(db);
                new EncounterSeeder(fs).Seed(db);
                GD.Print("[Global] Data seeding complete.");

                // Run Cross-Validation
                DataValidator.Validate(db);

                // Create runtime indexes (once at startup, not per operation)
```

And update the `catch` block to rethink how it bubbles up errors so `SplashScene` can catch them. Change it to rethrow so the caller (`SeedingTask`) is marked as faulted, OR just leave it as is if `SeedingTask` is set up via a proper `Task`. Wait, `SeedingTask` is a `Task.CompletedTask` in `Global.cs`. The seeding is currently completely synchronous in `_Ready()`.

Wait, the prompt says `SplashScene` awaits `global.SeedingTask`. If it's a completed task, it just skips it. We need to wrap the seeding in a real `Task`!

In `Darkness.Godot/src/Core/Global.cs`, change `public Task SeedingTask { get; private set; } = Task.CompletedTask;` to not initialize it yet, and update `_Ready`:

```csharp
    public Task SeedingTask { get; private set; } = null!;

    public override void _Ready()
    {
        // ... [keep DI initialization] ...
        
            // Initialize Transition Overlay
            Transition = new TransitionLayer();
            AddChild(Transition);

            // Wrap seeding in the task so SplashScene can await and catch it
            SeedingTask = Task.Run(() =>
            {
                var db = Services.GetRequiredService<ILiteDatabase>();
                var fs = Services.GetRequiredService<IFileSystemService>();

                GD.Print("[Global] Seeding data...");
                new SheetDefinitionSeeder(fs).Seed(db);
                new AppearanceSeeder(fs).Seed(db);
                new QuestSeeder(fs).Seed(db);
                new LevelSeeder(fs).Seed(db);
                new TalentSeeder(fs).Seed(db);
                new SkillSeeder(fs).Seed(db);
                new RecipeSeeder(fs).Seed(db);
                new ItemSeeder(fs).Seed(db);
                new RewardSeeder(fs).Seed(db);
                new EncounterSeeder(fs).Seed(db);
                GD.Print("[Global] Data seeding complete.");

                // Run Cross-Validation
                DataValidator.Validate(db);

                // Create runtime indexes
                db.GetCollection<Character>("characters").EnsureIndex(c => c.UserId);
                db.GetCollection<QuestState>("quest_states").EnsureIndex(s => s.CharacterId);
                db.GetCollection<QuestState>("quest_states").EnsureIndex(s => s.Status);
            });
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Global] Critical error during DI initialization: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }
```

- [ ] **Step 2: Run build**

Run: `dotnet build Darkness.sln`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/Core/Global.cs
git commit -m "feat: run data validation and wrap seeding in an awaitable Task"
```

### Task 4: Add Seeding Error UI to SplashScene

**Files:**
- Modify: `Darkness.Godot/src/UI/SplashScene.cs`

- [ ] **Step 1: Enhance Catch Block in `SplashScene.cs`**

Modify `StartGame()` in `Darkness.Godot/src/UI/SplashScene.cs` to show a more descriptive error if seeding fails (which now includes validation errors):

```csharp
        try
        {
            // Wait for data seeding to finish before any DB access
            var global = GetNode<Global>("/root/Global");
            await global.SeedingTask; // This will now throw if DataValidator fails

            // Ensure DI and session are ready
            GD.Print("[Splash] Initializing Session/Database...");
            await _session.InitializeAsync();
            GD.Print("[Splash] Initialization Successful.");

            if (_session.CurrentUser == null)
            {
                await _navigation.NavigateToAsync("LoadUserPage");
            }
            else
            {
                await _navigation.NavigateToAsync("MainMenuPage");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[Splash] CRITICAL INITIALIZATION FAILURE: {ex.Message}");
            await _dialog.DisplayAlertAsync("System Error",
                $"Failed to initialize game database or invalid data detected.\n\nError: {ex.Message}", "OK");
            _isInitialized = false;
            GetNode<Label>("VBoxContainer/Subtitle").Show();
            loading.Hide();
            
            // Optionally, we update the subtitle to indicate an error state instead of "Tap to Start"
            GetNode<Label>("VBoxContainer/Subtitle").Text = "Initialization Failed. Check Logs.";
        }
```

- [ ] **Step 2: Verify Build**

Run: `dotnet build Darkness.sln`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/UI/SplashScene.cs
git commit -m "fix: properly handle seeding errors in SplashScene"
```

### Task 5: Add Quest Log UI to WorldScene

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Create the Quest Log UI**

We'll add a simple Label to `CanvasLayer` in `_Ready` programmatically so we don't have to edit the Godot scene `.tscn` file directly. 

In `Darkness.Godot/src/Game/WorldScene.cs`, add a new field:
```csharp
    private Label _questLogLabel = null!;
```

Inside `_Ready()` just before the `_isReady = true;` block, add:
```csharp
        // Create Quest Log Label
        _questLogLabel = new Label
        {
            Text = "Current Objective: ...",
            Position = new Vector2(20, 80), // Below top menu
            AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.8f)),
            AddThemeFontSizeOverride("font_size", 18)
        };
        GetNode<CanvasLayer>("CanvasLayer").AddChild(_questLogLabel);
```
*(Note: Godot 4 C# `AddThemeColorOverride` syntax might require `_questLogLabel.AddThemeColorOverride("font_color", ...)`, make sure it compiles. Actually `_questLogLabel.Set("theme_override_colors/font_color", ...)` or simply assigning it works, but better to set properties if possible, or just default label text).*

Let's use safer initialization:
```csharp
        // Create Quest Log Label
        _questLogLabel = new Label();
        _questLogLabel.Position = new Vector2(20, 80); // Below top menu
        _questLogLabel.Text = "Current Objective: None";
        GetNode<CanvasLayer>("CanvasLayer").AddChild(_questLogLabel);
```

- [ ] **Step 2: Update the Quest Log text during UpdateVisuals**

In `UpdateVisuals()`, find the end of the method and add the update logic:

```csharp
            npcNode.Show();
        }
        else
        {
            GD.Print("[WorldScene] Hiding NPC node (none in this step).");
            GetNode<Area2D>("NPC").Hide();
        }

        // Update Quest Log UI
        if (_questLogLabel != null)
        {
            if (chain != null && step != null)
            {
                _questLogLabel.Text = $"Quest: {chain.Title}\nObjective: {step.Id}";
            }
            else
            {
                _questLogLabel.Text = "Quest: None";
            }
        }
```

- [ ] **Step 3: Verify Build**

Run: `dotnet build Darkness.Godot/Darkness.Godot.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: add quest log overlay to world scene"
```
