# Dynamic Quest Visuals and Story Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable dynamic backgrounds and NPC sprites in World/Battle scenes and expand the story arc to 9 beats.

**Architecture:** Update the `QuestStep` model to include a `Visuals` configuration. Refactor `WorldScene` and `BattleScene` to apply these visuals on load. Expand the quest data JSON files to cover the full "Revenge" story arc.

**Tech Stack:** .NET 10, Godot 4.6.1, LiteDB, System.Text.Json.

---

### Task 1: Update Core Models

**Files:**
- Create: `Darkness.Core/Models/VisualConfig.cs`
- Create: `Darkness.Core/Models/NpcConfig.cs`
- Modify: `Darkness.Core/Models/QuestStep.cs`

- [ ] **Step 1: Create NpcConfig model**

```csharp
namespace Darkness.Core.Models;

public class NpcConfig
{
    public string Name { get; set; } = string.Empty;
    public string? SpriteKey { get; set; }     // Path to full sheet (e.g. "bosses/Balgathor")
    public float PositionX { get; set; } = 400; 
    public float PositionY { get; set; } = 200;
    // LPC override if SpriteKey is missing
    public CharacterAppearance? Appearance { get; set; } 
}
```

- [ ] **Step 2: Create VisualConfig model**

```csharp
namespace Darkness.Core.Models;

public class VisualConfig
{
    public string? BackgroundKey { get; set; } // Path to PNG artwork
    public string? GroundColor { get; set; }   // Hex fallback (e.g. "#222222")
    public string? WaterColor { get; set; }    // Hex fallback (null to hide water)
    public NpcConfig? Npc { get; set; }        // The NPC present in this beat
}
```

- [ ] **Step 3: Add Visuals to QuestStep**

Modify `Darkness.Core/Models/QuestStep.cs`:
```csharp
namespace Darkness.Core.Models;

public class QuestStep
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? NextStepId { get; set; }
    public DialogueData? Dialogue { get; set; }
    public CombatData? Combat { get; set; }
    public LocationTrigger? Location { get; set; }
    public BranchData? Branch { get; set; }
    public VisualConfig? Visuals { get; set; } // Add this line
}
```

- [ ] **Step 4: Commit**

```bash
git add Darkness.Core/Models/
git commit -m "feat: add VisualConfig and NpcConfig models to QuestStep"
```

---

### Task 2: Implement WorldScene Dynamic Visuals

**Files:**
- Modify: `Darkness.Godot/scenes/WorldScene.tscn`
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Add TextureRect to WorldScene.tscn**

Add a `TextureRect` named `BackgroundImage` behind the `Background` `ColorRect`. Set its `expand_mode` to `1` (Ignore Size) and `stretch_mode` to `6` (Keep Aspect Centered). Set `anchors_preset` to `15` (Full Rect).

- [ ] **Step 2: Implement UpdateVisuals in WorldScene.cs**

```csharp
    private void UpdateVisuals(QuestStep? step)
    {
        if (step?.Visuals == null) return;

        var visuals = step.Visuals;

        // 1. Background Logic
        var bgRect = GetNode<ColorRect>("Background");
        var waterRect = GetNode<ColorRect>("Water");
        var bgImage = GetNode<TextureRect>("CanvasLayer/BackgroundImage"); // Adjust path if needed

        if (!string.IsNullOrEmpty(visuals.BackgroundKey))
        {
            var texPath = visuals.BackgroundKey.StartsWith("res://") 
                ? visuals.BackgroundKey 
                : $"res://assets/backgrounds/{visuals.BackgroundKey}.png";
            
            if (FileAccess.FileExists(texPath))
            {
                bgImage.Texture = GD.Load<Texture2D>(texPath);
                bgImage.Show();
                bgRect.Hide(); // Hide placeholder color
            }
            else
            {
                GD.PrintErr($"[WorldScene] Background artwork not found: {texPath}. Falling back to colors.");
                bgImage.Hide();
                bgRect.Show();
            }
        }
        else
        {
            bgImage.Hide();
            bgRect.Show();
        }

        // Apply fallback colors
        if (!string.IsNullOrEmpty(visuals.GroundColor))
        {
            bgRect.Color = Color.FromHtml(visuals.GroundColor);
        }

        if (!string.IsNullOrEmpty(visuals.WaterColor))
        {
            waterRect.Color = Color.FromHtml(visuals.WaterColor);
            waterRect.Show();
        }
        else
        {
            waterRect.Hide();
        }

        // 2. NPC Logic
        if (visuals.Npc != null)
        {
            var npcNode = GetNode<Area2D>("NPC");
            npcNode.GlobalPosition = new Vector2(visuals.Npc.PositionX, visuals.Npc.PositionY);
            _speakerName = visuals.Npc.Name;

            var npcSprite = GetNode<LayeredSprite>("NPC/Sprite");
            
            if (!string.IsNullOrEmpty(visuals.Npc.SpriteKey))
            {
                var spritePath = visuals.Npc.SpriteKey.StartsWith("res://") 
                    ? visuals.Npc.SpriteKey 
                    : $"res://assets/sprites/{visuals.Npc.SpriteKey}.png";

                if (FileAccess.FileExists(spritePath))
                {
                    _ = npcSprite.SetupFullSheet(spritePath, _fileSystem);
                }
                else if (visuals.Npc.Appearance != null)
                {
                    GD.Print($"[WorldScene] NPC sprite '{spritePath}' not found. Using generated LPC appearance.");
                    _ = npcSprite.SetupCharacter(new Character { Appearance = visuals.Npc.Appearance }, _catalog, _fileSystem, _compositor);
                }
            }
            else if (visuals.Npc.Appearance != null)
            {
                _ = npcSprite.SetupCharacter(new Character { Appearance = visuals.Npc.Appearance }, _catalog, _fileSystem, _compositor);
            }
            npcNode.Show();
        }
        else
        {
            GetNode<Area2D>("NPC").Hide();
        }
    }
```

- [ ] **Step 3: Call UpdateVisuals in _Ready and after Quest Advancement**

Update `_Ready`:
```csharp
    public override async void _Ready()
    {
        // ... existing DI setup ...
        
        // Find current step to initialize visuals
        if (_session.CurrentCharacter != null)
        {
            var availableChains = _questService.GetAvailableChains(_session.CurrentCharacter);
            var chain = availableChains.FirstOrDefault();
            if (chain != null)
            {
                var step = _questService.GetCurrentStep(_session.CurrentCharacter, chain.Id);
                UpdateVisuals(step);
            }
        }

        await UpdateSprites(); // Update player sprite
    }
```

- [ ] **Step 4: Update OnChoiceSelected to refresh visuals**

In `OnChoiceSelected`, after `_questService.AdvanceStep`, call `UpdateVisuals(nextStep)`.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Godot/scenes/WorldScene.tscn Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: implement dynamic world visuals and NPC spawning"
```

---

### Task 3: Implement BattleScene Dynamic Backgrounds

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

- [ ] **Step 1: Update Initialize to set background**

```csharp
    public void Initialize(IDictionary<string, object> parameters)
    {
        // ... existing initialization ...
        
        if (args.Combat != null && !string.IsNullOrEmpty(args.Combat.BackgroundKey))
        {
            var bgRect = GetNode<ColorRect>("Background");
            var texPath = $"res://assets/backgrounds/{args.Combat.BackgroundKey}.png";
            
            if (FileAccess.FileExists(texPath))
            {
                // Note: BattleScene usually uses a TextureRect for better atmosphere
                // If it's still a ColorRect, we might want to swap it or apply a shader.
                // For now, we'll try to load it if a TextureRect exists.
                if (HasNode("BackgroundImage"))
                {
                    GetNode<TextureRect>("BackgroundImage").Texture = GD.Load<Texture2D>(texPath);
                }
            }
        }
    }
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "feat: support dynamic backgrounds in BattleScene"
```

---

### Task 4: Update Existing Quest Data (Beats 1-3)

**Files:**
- Modify: `Darkness.Godot/assets/data/quests/beat_1_the_awakening.json`
- Modify: `Darkness.Godot/assets/data/quests/beat_2_dark_warrior.json`
- Modify: `Darkness.Godot/assets/data/quests/beat_3_the_sorcerer.json`

- [ ] **Step 1: Update Beat 1 with Visuals**
Add `Visuals` block to `beat_1_intro`. 
Ground: `#C2B280` (Sand), Water: `#006994`. 
NPC: "Old Man", LPC config (generated).

- [ ] **Step 2: Update Beat 2 with Visuals**
Add `Visuals` block to `beat_2_dialogue`.
Ground: `#333333` (Stone), BackgroundKey: `dark_castle`.
NPC: "Dark Warrior", SpriteKey: `bosses/Balgathor`.

- [ ] **Step 3: Update Beat 3 with Visuals**
Add `Visuals` block to `beat_3_dialogue`.
Ground: `#333333` (Stone), BackgroundKey: `dark_castle`.
NPC: "The Sorcerer", SpriteKey: `bosses/Risidian`.

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/assets/data/quests/
git commit -m "data: add visual configurations to beats 1-3"
```

---

### Task 5: Expand Story to Beats 4-9

**Files:**
- Create: `Darkness.Godot/assets/data/quests/beat_4_the_tavern.json`
- Create: `Darkness.Godot/assets/data/quests/beat_5_journey_begins.json`
- Create: `Darkness.Godot/assets/data/quests/beat_6_the_knight.json`
- Create: `Darkness.Godot/assets/data/quests/beat_7_araknos_demon.json`
- Create: `Darkness.Godot/assets/data/quests/beat_8_undead_army.json`
- Create: `Darkness.Godot/assets/data/quests/beat_9_kyarias_final.json`

- [ ] **Step 1: Create Beat 4 (The Tavern)**
Interaction with Tavern Keeper. Hub tutorial beat.

- [ ] **Step 2: Create Beat 5 (Journey Begins)**
Forest exploration visuals. Combat vs 2 Goblins.

- [ ] **Step 3: Create Beat 6 (The Knight)**
Meeting Tywin. Visuals: Bridge/Road. Combat vs Tywin (NPC joins after).

- [ ] **Step 4: Create Beat 7 (Araknos Demon)**
Visuals: Dark Cave. Boss fight vs Araknos.

- [ ] **Step 5: Create Beat 8 (Undead Army)**
Visuals: Battlefield. Multi-target combat.

- [ ] **Step 6: Create Beat 9 (Kyarias Final)**
Visuals: Throne Room. Final boss vs Kyarias.

- [ ] **Step 7: Commit**

```bash
git add Darkness.Godot/assets/data/quests/
git commit -m "data: expand story to full 9 beats"
```

---

### Task 6: Verification

- [ ] **Step 1: Run Seeder Test**
Ensure all 9 JSONs are loaded without errors.

- [ ] **Step 2: Manual Playthrough**
Start a new game, complete Beat 1, and verify `WorldScene` visuals change to the Dark Castle for Beat 2.
Verify "Continue Story" button in `BattleScene` correctly refreshes `WorldScene`.
