# Coordinate-Based Zone System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a data-driven spatial boundary and trigger system using JSON configuration instead of hardcoded coordinates or Godot scene files.

**Architecture:** Adds a `ZoneConfig` list to `VisualConfig`. The `WorldScene` `_Process` loop checks the player's intended movement against screen boundaries and these configured zones to block movement, show text, or trigger encounters.

**Tech Stack:** C# 10, Godot 4.x, System.Text.Json

---

### Task 1: Define ZoneConfig Data Model

**Files:**
- Create: `Darkness.Core/Models/ZoneConfig.cs`

- [ ] **Step 1: Write the failing test**
Create `Darkness.Tests/Models/ZoneConfigTests.cs`
```csharp
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class ZoneConfigTests
{
    [Fact]
    public void ZoneConfig_Initialization_SetsDefaults()
    {
        var zone = new ZoneConfig();
        Assert.Equal("Block", zone.Type);
        Assert.Equal(0f, zone.X);
        Assert.Equal(0f, zone.Y);
        Assert.Equal(0f, zone.Width);
        Assert.Equal(0f, zone.Height);
        Assert.Null(zone.ActionId);
        Assert.Null(zone.Message);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**
Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~ZoneConfigTests"`
Expected: FAIL (Build error, type not found)

- [ ] **Step 3: Write minimal implementation**
Create `Darkness.Core/Models/ZoneConfig.cs`
```csharp
namespace Darkness.Core.Models;

public class ZoneConfig
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string Type { get; set; } = "Block"; // "Block", "Text", "Trigger"
    public string? ActionId { get; set; }
    public string? Message { get; set; }
}
```

- [ ] **Step 4: Run test to verify it passes**
Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~ZoneConfigTests"`
Expected: PASS

- [ ] **Step 5: Commit**
Run:
```bash
git add Darkness.Core/Models/ZoneConfig.cs Darkness.Tests/Models/ZoneConfigTests.cs
git commit -m "feat: add ZoneConfig data model"
```

---

### Task 2: Update VisualConfig to include Zones

**Files:**
- Modify: `Darkness.Core/Models/VisualConfig.cs`

- [ ] **Step 1: Write the failing test**
Create `Darkness.Tests/Models/VisualConfigTests.cs`
```csharp
using Darkness.Core.Models;
using System.Collections.Generic;
using Xunit;

namespace Darkness.Tests.Models;

public class VisualConfigTests
{
    [Fact]
    public void VisualConfig_Initialization_InitializesZonesList()
    {
        var config = new VisualConfig();
        Assert.NotNull(config.Zones);
        Assert.Empty(config.Zones);
        Assert.IsType<List<ZoneConfig>>(config.Zones);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**
Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~VisualConfigTests"`
Expected: FAIL (Build error, Zones property not found)

- [ ] **Step 3: Write minimal implementation**
Modify `Darkness.Core/Models/VisualConfig.cs` by adding the property inside the class:
```csharp
namespace Darkness.Core.Models;

public class VisualConfig
{
    public string? BackgroundKey { get; set; } // Path to PNG artwork
    public string? GroundColor { get; set; }   // Hex fallback (e.g. "#222222")
    public string? WaterColor { get; set; }    // Hex fallback (null to hide water)
    public NpcConfig? Npc { get; set; }        // The NPC present in this beat
    public float? PlayerPositionX { get; set; } // Starting X position for the player
    public float? PlayerPositionY { get; set; } // Starting Y position for the player
    public System.Collections.Generic.List<ZoneConfig> Zones { get; set; } = new();
}
```

- [ ] **Step 4: Run test to verify it passes**
Run: `dotnet test Darkness.Tests --filter "FullyQualifiedName~VisualConfigTests"`
Expected: PASS

- [ ] **Step 5: Commit**
Run:
```bash
git add Darkness.Core/Models/VisualConfig.cs Darkness.Tests/Models/VisualConfigTests.cs
git commit -m "feat: add Zones list to VisualConfig"
```

---

### Task 3: Implement Hard Screen Boundaries in WorldScene

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Update WorldScene to clamp to viewport**
Modify `_Process` in `Darkness.Godot/src/Game/WorldScene.cs`. Replace the velocity application logic around line 170. Note: Godot nodes don't have easily mockable Viewport rectangles in pure Xunit without standing up the engine, so we implement carefully and skip TDD for the engine integration layer.

Find:
```csharp
        if (velocity != Vector2.Zero)
        {
            _player.Velocity = velocity.Normalized() * _moveSpeed;
            _player.MoveAndSlide();
            UpdateAnimation(velocity);
        }
        else
        {
            _playerSprite.Play("idle_" + _lastDirection);
        }
```

Replace with:
```csharp
        if (velocity != Vector2.Zero)
        {
            Vector2 intendedVelocity = velocity.Normalized() * _moveSpeed;
            Vector2 nextPos = _player.GlobalPosition + (intendedVelocity * (float)delta);
            
            // Hard boundaries (Screen Edges)
            // Assuming default Godot viewport size 1920x1080 and character sprite ~100px wide/tall
            float minX = 50f;
            float maxX = GetViewportRect().Size.X - 50f;
            float minY = 50f;
            float maxY = GetViewportRect().Size.Y - 50f;

            if (nextPos.X < minX || nextPos.X > maxX) intendedVelocity.X = 0;
            if (nextPos.Y < minY || nextPos.Y > maxY) intendedVelocity.Y = 0;

            _player.Velocity = intendedVelocity;
            
            if (intendedVelocity != Vector2.Zero)
            {
                _player.MoveAndSlide();
                UpdateAnimation(intendedVelocity);
            }
            else
            {
                _playerSprite.Play("idle_" + _lastDirection);
            }
        }
        else
        {
            _playerSprite.Play("idle_" + _lastDirection);
        }
```

- [ ] **Step 2: Build to verify compilation**
Run: `dotnet build Darkness.sln`
Expected: Build succeeds.

- [ ] **Step 3: Commit**
Run:
```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: implement hard screen boundaries in WorldScene"
```

---

### Task 4: Implement Zone Evaluation in WorldScene

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Implement Zone intersection logic**
We need to check the JSON configured zones during movement. Add state for the text cooldown.

At the top of `WorldScene.cs`, add a new field:
```csharp
    private double _textZoneCooldown = 0;
```

Modify the `_Process` method again. Right after the hard boundary check (around line 185) and before `_player.Velocity = intendedVelocity;`, add the zone check logic. We also need to decrease the cooldown.

Find:
```csharp
    public override void _Process(double delta)
    {
        if (!IsInsideTree() || !_isReady) return;
        
        // ... (input handling)
```

Replace with:
```csharp
    public override void _Process(double delta)
    {
        if (!IsInsideTree() || !_isReady) return;
        
        if (_textZoneCooldown > 0)
        {
            _textZoneCooldown -= delta;
        }
        // ... (input handling)
```

Find the boundary logic we just added:
```csharp
            if (nextPos.X < minX || nextPos.X > maxX) intendedVelocity.X = 0;
            if (nextPos.Y < minY || nextPos.Y > maxY) intendedVelocity.Y = 0;

            _player.Velocity = intendedVelocity;
```

Replace with:
```csharp
            if (nextPos.X < minX || nextPos.X > maxX) intendedVelocity.X = 0;
            if (nextPos.Y < minY || nextPos.Y > maxY) intendedVelocity.Y = 0;

            // Zone Evaluation
            if (_currentDialogueStep?.Visuals?.Zones != null)
            {
                Rect2 playerRect = new Rect2(nextPos.X - 25, nextPos.Y - 25, 50, 50); // Approximate player size

                foreach (var zone in _currentDialogueStep.Visuals.Zones)
                {
                    Rect2 zoneRect = new Rect2(zone.X, zone.Y, zone.Width, zone.Height);
                    
                    if (playerRect.Intersects(zoneRect))
                    {
                        if (zone.Type.Equals("Block", System.StringComparison.OrdinalIgnoreCase))
                        {
                            // Simple block: zero velocity entirely for now to prevent getting stuck
                            intendedVelocity = Vector2.Zero;
                            _targetPosition = null; // Cancel pathfinding
                        }
                        else if (zone.Type.Equals("Text", System.StringComparison.OrdinalIgnoreCase))
                        {
                            intendedVelocity = Vector2.Zero;
                            _targetPosition = null;
                            if (_textZoneCooldown <= 0 && _currentDialogueIndex < 0 && !string.IsNullOrEmpty(zone.Message))
                            {
                                ShowZoneText(zone.Message);
                                _textZoneCooldown = 2.0; // 2 second cooldown after showing text
                            }
                        }
                        else if (zone.Type.Equals("Trigger", System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (!_isEncounterTriggered && (zone.ActionId == "next_step" || string.IsNullOrEmpty(zone.ActionId)))
                            {
                                _targetPosition = null;
                                intendedVelocity = Vector2.Zero;
                                _ = TriggerEncounter(true);
                            }
                        }
                    }
                }
            }

            _player.Velocity = intendedVelocity;
```

- [ ] **Step 2: Add ShowZoneText method**
Add this method to `WorldScene.cs` below `NextDialogue()`:
```csharp
    private void ShowZoneText(string message)
    {
        _targetPosition = null;
        _player.Velocity = Vector2.Zero;
        _playerSprite.Play("idle_" + _lastDirection);

        _speakerName = "System";
        _dialogue = new List<string> { message };
        _currentChoices.Clear();
        
        _currentDialogueIndex = 0;
        _dialogueBox.Show();

        var prompt = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/PromptLabel");
        prompt.Text = "[TAP TO CONTINUE]";

        UpdateDialogueUI();
    }
```

- [ ] **Step 3: Remove old hardcoded location trigger**
Find and delete this block at the end of `_Process`:
```csharp
        // Check location trigger for quest encounters at the eastern edge
        if (_session.CurrentCharacter != null && _player.GlobalPosition.X > 1200)
        {
            var triggerStep = _triggerService.CheckLocationTrigger(_session.CurrentCharacter, "SandyShore_East");
            if (triggerStep != null)
            {
                _ = TriggerEncounter(true);
            }
        }
```

- [ ] **Step 4: Build to verify compilation**
Run: `dotnet build Darkness.sln`
Expected: Build succeeds.

- [ ] **Step 5: Commit**
Run:
```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: evaluate JSON zones in WorldScene movement loop"
```

---

### Task 5: Update Seed Data to utilize new zones

**Files:**
- Modify: `assets/data/quests/beat_1_the_awakening.json` (or whichever file represents beat 1)

- [ ] **Step 1: Replace hardcoded trigger with Zone definition**
Find the first combat step in `beat_1` (e.g. `beat_1_combat`). Add the `visuals` block with the trigger zone on the east, and the text zone on the south.

Edit `assets/data/quests/beat_1_the_awakening.json`:
Find the step:
```json
    {
      "id": "beat_1_combat",
      "type": "combat",
      "autoTransition": false,
      ...
```
Add the `visuals` object if it doesn't exist, or merge the `zones` array into it:
```json
      "visuals": {
        "backgroundKey": "sandy_shore",
        "zones": [
          { "x": 1800, "y": 0, "width": 120, "height": 1080, "type": "Trigger", "actionId": "next_step" },
          { "x": 0, "y": 950, "width": 1920, "height": 130, "type": "Text", "message": "The water is too deep. Don't go any further or you will drown." }
        ]
      },
```

- [ ] **Step 2: Commit**
Run:
```bash
git add assets/data/quests/beat_1_the_awakening.json
git commit -m "data: convert beat 1 location triggers to data-driven zones"
```