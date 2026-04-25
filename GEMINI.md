# Darkness: Reforged — Project Guide

## 1. Project Overview
A cross-platform RPG built on **Godot 4.6.1 (.NET 10)** with a platform-agnostic core library.

### Core Projects
- **`Darkness.Core`**: Pure .NET 10 library — models (45), interfaces (23), services (23+), combat engine, and LiteDB persistence. All game logic lives here.
- **`Darkness.Godot`**: Main game host. Godot C# project for rendering, UI, and input.
- **`Darkness.WebAPI`**: ASP.NET Core 10 backend with EF Core.
- **`Darkness.Tests`**: XUnit tests with Moq. References Darkness.Core.

### Build & Run
```bash
dotnet restore                                    # Restore all projects
dotnet build Darkness.sln                         # Build entire solution
dotnet build Darkness.Godot/Darkness.Godot.csproj # Build Godot project
dotnet run --project Darkness.WebAPI              # Run WebAPI backend
dotnet test Darkness.Tests                        # Run all tests
dotnet test Darkness.Tests --filter "FullyQualifiedName~TestMethodName"  # Single test
```

## 2. Architecture & Design Patterns

### Dependency Injection (DI)
- Uses `Microsoft.Extensions.DependencyInjection`.
- **Global Autoload (`Global.cs`)**: Initializes `ServiceProvider`, registers all services, and runs data seeders at startup.
- **Service Resolution**: Scenes resolve services via `Global.Services` in `_Ready()`.
- **Key services** (23 interfaces total): `ICharacterService`, `IUserService`, `ISessionService`, `ISettingsService`, `ICombatService`, `IQuestService`, `ITriggerService`, `IEncounterService`, `ILevelingService`, `ITalentService`, `IWeaponSkillService`, `ICraftingService`, `IRewardService`, `IDeathmatchService`, `IAllyService`, `INavigationService`, `ISheetDefinitionCatalog`, `ISpriteCompositor`, `IFileSystemService`, `IDispatcherService`, `IDialogService`, `IEquipmentService`, `LocalDatabaseService`.

### Data Architecture
The project uses a **data-driven architecture** with JSON seed files and LiteDB runtime storage:

- **Seed files** (human-readable JSON in `assets/data/`) define game content — quest chains, level thresholds, skills, talent trees, encounters, items, and recipes.
- **Sheet Definitions** (JSON in `assets/data/sheet_definitions/`) define sprite layers and animations for equipment.
- **Seeders** (10 total): `SheetDefinition`, `Appearance`, `Quest`, `Level`, `Talent`, `Skill`, `Recipe`, `Item`, `Reward`, and `Encounter`.
- **Services** query LiteDB at runtime for all lookups.
- **Adding new content** (equipment, quests, levels) requires only JSON edits — no code changes.

### Navigation System
- **`GodotNavigationService`**: Custom `INavigationService` using `GetTree().ChangeSceneToFile()`.
- **`BattleArgs`**: Carries `CombatData` + `QuestChainId`/`QuestStepId` for quest-aware combat.
- **`StealthArgs`**: Carries quest context for stealth scenes.

## 3. Sprite Composition Pipeline

Character appearance uses an 11+ layer system composited at runtime:

- **`SheetDefinition`** (LiteDB collection): Maps equipment names to z-order, animations, and variant-based asset paths for each layer.
- **`AppearanceOption`** (LiteDB collection): Maps hair/skin/face/eye options to asset paths, z-order, and tint hex values.
- **`SheetDefinitionCatalog`**: Resolves `CharacterAppearance` into a list of `SheetDefinition` objects, handling gender-specific paths and tints (skin, hair).
- **`SkiaSharpSpriteCompositor`**: (Implementing `ISpriteCompositor`) Composites PNG layers into 1536x2112 sprite sheets (6 LPC rows) using SkiaSharp.
- **Layer resolution**: 1. Body, 2. Head, 3. Face, 4. Eyes, 5. Hair, 6. Armor, 7. Feet, 8. Arms, 9. Legs, 10. Weapon, 11. Shield, 12. OffHand.
- **Seed files**: `assets/data/sheet_definitions/` (Armor, Weapons, etc.) and `assets/data/sprite-catalog.json` (AppearanceOptions).

To add new equipment: create a new JSON in the appropriate `sheet_definitions/` subdirectory. To add appearance options: edit `sprite-catalog.json`.

## 4. Quest System

Quest content is authored as per-chain JSON files and managed via LiteDB at runtime:

- **Quest chain files**: One JSON per chain in `assets/data/quests/` (e.g., `beat_1_the_awakening.json`).
- **`QuestChain`**: Container with id, title, prerequisites, sort order, and a list of `QuestStep`s.
- **`QuestStep`**: Typed step — `"dialogue"`, `"combat"`, `"location"`, or `"branch"`. Each type has its own data field (`Dialogue`, `Combat`, `Location`, `Branch`).
- **`BranchData`**: Explicit branching with `BranchOption`s containing text, next step ID, morality impact, and optional `BranchCondition`s.
- **`BranchCondition`**: Extensible conditions — `"morality"`, `"class"`, `"has_item"`, `"quest_completed"` with operators `>=`, `<=`, `==`, `contains`.
- **`QuestState`** (LiteDB): Per-character, per-chain progress tracking (current step, status, flags).
- **`QuestService.AdvanceStep()`**: Central method for all quest progression.
- **`TriggerService`**: Replaces coordinate-based triggers with data-driven location matching.
- **`ConditionEvaluator`**: Evaluates branch conditions against character state.

To add a new quest: create a JSON file in `assets/data/quests/` following the QuestChain schema.

## 5. XP & Leveling System

- **Level thresholds**: Defined in `assets/data/level-table.json` (20 levels, data-driven).
- **`LevelingService.AwardExperience()`**: Awards XP, checks level-up thresholds, grants attribute points (2 per level), talent points (1 per even level), scales MaxHP, restores HP on level-up.
- **`LevelUpResult`**: Returned after XP award — includes `DidLevelUp`, `LevelsGained`, `AttributePointsAwarded`, `TalentPointsAwarded`.
- **Combat flow**: `BattleScene.Victory()` → sums `Enemy.ExperienceReward` → `LevelingService.AwardExperience()` → `QuestService.AdvanceStep()` → navigate to WorldScene.

## 5a. Talent System

Talent trees defined in `assets/data/talent-trees.json`, seeded into LiteDB by `TalentSeeder`.

- **`TalentTree`**: Id, Name, Tier, RequiredClass, ExclusiveGroupId, IsHidden, RequiredMorality, Prerequisites, Nodes.
- **`TalentNode`**: Id, Name, Description, PointsRequired, Row, Column, PrerequisiteNodeIds, IsPassive, AutomaticallyUnlocked, IconPath, Effect.
- **`TalentEffect`**: Stat (bonus stat name), Value, Skill (skill unlock).
- **`TalentService`**: Validates class requirements, morality requirements, exclusivity groups, hidden trees. Returns `TalentPurchaseResult`.
- **`TalentLayoutHelper`**: Static utility for calculating spatial layout (BFS depth for rows, collision resolution).
- **Talent points**: 1 per even level, spent to unlock nodes in class-restricted trees.

## 5b. Skills & Weapons

Skills defined in `assets/data/skills.json`, seeded into LiteDB by `SkillSeeder`.

- **`Skill`**: Name, ManaCost, StaminaCost, BasePower, DamageDice, DamageMultiplier, SkillType (Physical/Magical/Defensive), ActionType, Cooldown, WeaponRequirement, TalentRequirement, IsPassive.
- **`WeaponSkillService`**: Resolves available skills — prioritizes `ActiveSkillSlots` (player-assigned hotbar), falls back to weapon-type matching. Generates inline skills for various weapon types (Wand, Bow, Sword, Shield, Unarmed).
- **`ActiveSkillSlots`** on Character: Player-curated skill hotbar that overrides weapon-based defaults.

## 5c. Random Encounter System

Random world encounters are data-driven and triggered by movement distance.

- **`EncounterTable`**: Maps a `BackgroundKey` to a list of weighted `EncounterEntry`s, along with `EncounterChance` (percentage) and `EncounterDistance` (pixels).
- **`EncounterService.RollForEncounter()`**: Central logic that tracks distance moved, rolls against the area's chance, and selects a weighted mob on success.
- **"Heartbeat" Transition**: When triggered, a ghost enemy spawns near the player, and the camera performs 4 pulses zooming closer to the enemy before transitioning to `BattleScene`.
- **Seed file**: `assets/data/encounters.json` — defines rates and mob pools for each area.

## 6. Game Flow & Scenes

1. **`SplashScene`**: Initialization & database health check.
2. **`LoadUserScene`**: Login and user selection.
3. **`CreateUserScene`**: User creation.
4. **`CharactersScene`**: Character list with thumbnails.
5. **`MainMenuScene` (Hub)**: Central hub — Story, Deathmatch, PVP, Characters, Forge, Study, Allies, Talents, Skills, Settings, Logout.
6. **`WorldScene`**: World exploration with `TriggerService`-driven quest triggers. Handles dialogue, branch choices, and scene transitions.
7. **`BattleScene`**: Turn-based combat with XP awards and quest advancement on victory.
8. **`StealthScene`**: Timing-bar stealth minigame with quest context pass-through (5 successes to win, 3 failures to lose).
9. **`DeathmatchScene`**: Special combat arena for testing and grinding.
10. **`SkillsScene`**: Skill management and hotbar assignment.
11. **`TalentTreeScene`**: Multi-tab talent tree visualization with node purchasing.
12. **`InventoryScene`**: Item/equipment management with sprite preview and tooltips.
13. **`ForgeScene`**: Crafting recipes, equipment upgrades, and essence infusion.
14. **`StudyScene`**: Stat point allocation across 6 attributes.
15. **`AlliesScene`**: Ally request and management.
16. **`SettingsScene`**: Volume controls (Master, Music, SFX).
17. **`CharacterGenScene`**: 3-step character creation with live sprite preview.

## 7. Build & Deployment

### Android (APK)
- **Tool**: `build-android.ps1` in the root.
- Auto-builds C# project, headless Godot export to `bin/Darkness.apk`.
- Auto-increments `version/code` in `export_presets.cfg`.
- **Requires**: JDK 17+ configured in Godot Editor Settings.

## 8. Implementation Notes
- **Touch Input**: `emulate_mouse_from_touch` enabled for Android button compatibility.
- **UI Layering**: Use `ScrollContainer` → `VBoxContainer` hierarchies. Set root `mouse_filter = Ignore` if buttons are unresponsive.
- **Audio Driver**: WASAPI with `enable_input = false` for Windows.
- **Namespaces**: Use `global::Godot.FileAccess` when `System.IO` is in scope. Use `using SystemJson = System.Text.Json` when `LiteDB` is in scope (both have `JsonSerializer`).
- **Data seeding**: All 10 seeders (`SheetDefinition`, `Appearance`, `Quest`, `Level`, `Talent`, `Skill`, `Recipe`, `Item`, `Reward`, `Encounter`) use `DeleteAll()` + re-insert pattern (idempotent). Errors are logged but don't crash startup.
- **Camera Centering**: `WorldScene` uses a static `Camera2D` centered at `(640, 360)` (scene root) to maintain static backgrounds while allowing encounter zoom effects.
- **D20 Combat**: `CombatEngine` uses d20 hit rolls against AC, dice-based damage (XdY parser), initiative from DEX modifier, critical hit on natural 20, critical miss on natural 1. Supports Character vs Enemy, Enemy vs Character, and Character vs Character.

## 9. Workflow Requirements

- **Always use superpowers skills** for all work. Use brainstorming before creative/design work, TDD for implementation, systematic-debugging for bugs, and writing-plans for multi-step tasks. Never skip the skill workflow.
- **Regression Testing**: Every bug fix MUST include a new regression test in `Darkness.Tests` to prevent the issue from recurring.
- **Continuous Validation**: Run `dotnet test Darkness.Tests -p:ParallelizeTestCollections=false` after every code change to ensure no regressions were introduced.
- **All tests must pass before committing.** Verify all tests pass before every commit. Do not commit with failing tests. If a pre-existing test fails, investigate and fix it or explicitly flag it to the user — do not ignore it.

## 10. Project Conventions
- .NET 10 with `ImplicitUsings`, `Nullable`, and `TreatWarningsAsErrors` enabled.
- App identifier: `com.risidian.darkness`
- Rendering: `gl_compatibility` mode. Viewport: 1280x720, stretch mode `canvas_items`. Pixel snapping enabled.
- Sprite assets in `assets/sprites/full/` (LPC-based character sprites).
- Seed data in `Darkness.Godot/assets/data/` (sprite-catalog.json, skills.json, talent-trees.json, level-table.json, encounters.json, quests/*.json, recipes.json, items.json, random-rewards.json, login-calendar.json).
- Key dependencies: CommunityToolkit.Mvvm 8.4.2, LiteDB 5.0.21, Newtonsoft.Json 13.0.3 (Core); Microsoft.Extensions.DependencyInjection 9.0.2 (Godot); SkiaSharp 3.119.2 (Tests).
- 165 tests across 37 test files (5 excluded files depend on removed MonoGame project).
