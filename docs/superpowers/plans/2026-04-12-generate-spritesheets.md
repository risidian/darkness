# Sprite Sheet Generation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Write a script or unit test to programmatically generate and save sprite sheets for each class (Knight, Rogue, Mage, Cleric, Warrior) for both male and female genders, and save them to `GeneratedSpriteSheets`.

**Architecture:** 
- We can leverage the existing `SpriteLayerCatalog` and `SpriteCompositor` within a temporary XUnit test or a standalone console app.
- A test method is fastest since we already have mocking setup for `IFileSystemService` and a LiteDB seeder in `CharacterGenWizardTests.cs`.
- We will read the real seed file, get default appearances, generate the `StitchLayer` lists, use the real `GodotSpriteCompositor` (or a mock of it that actually runs ImageSharp/SkiaSharp if we don't want to run Godot headless).
- Wait, the `Darkness.Godot` project has `GodotSpriteCompositor` which uses Godot's `Image` class. Running Godot classes in an XUnit test outside the engine is difficult without a headless runner. 
- Alternative: We have a C# console script, or we use a Python script. But we have C# code that does the compositing. 
- Let's look at `Darkness.Tests.Services.SpriteCompositorTests`. It uses `Darkness.Core.Services.SpriteCompositor` which relies on `SkiaSharp`!
- Excellent! `Darkness.Core` has a `SpriteCompositor` using `SkiaSharp`. We can just write a quick test/script using it.

**Tech Stack:** C# (XUnit), LiteDB, SkiaSharp.

---

### Task 1: Create the Generation Script/Test

**Files:**
- Create: `Darkness.Tests/Generation/SpriteSheetGenerator.cs`

- [ ] **Step 1: Write the generation logic**
    - Setup LiteDB with `SpriteSeeder` using the real `sprite-catalog.json`.
    - Instantiate `SpriteLayerCatalog`.
    - Instantiate `Darkness.Core.Services.SpriteCompositor`.
    - For each class ("Knight", "Rogue", "Mage", "Cleric", "Warrior"):
        - For each gender ("Human Male", "Human Female"):
            - Get `CharacterAppearance`.
            - `GetStitchLayers()`.
            - Open `FileStream`s for each layer's actual image file.
            - Call `_compositor.CompositeLayers()`.
            - Save the resulting byte array to `GeneratedSpriteSheets/{className}_{gender}.png`.
- [ ] **Step 2: Run the test**
    - `dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteSheetGenerator"`
- [ ] **Step 3: Verify the output**
    - Check the `GeneratedSpriteSheets` folder for 10 PNG files.
