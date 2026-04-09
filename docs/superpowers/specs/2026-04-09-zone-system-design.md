# Coordinate-Based Zone System Design

## 1. Overview
This feature introduces a data-driven system for defining invisible collision and trigger boundaries in `WorldScene.cs` using JSON. This allows quest authors to prevent players from walking off the screen or define specific areas that trigger text or encounters without needing to create separate Godot scene files for every map.

## 2. Architecture

### 2.1 Data Models (JSON Schema)
A new `ZoneConfig` model will be added to `Darkness.Core/Models`.
```csharp
namespace Darkness.Core.Models;

public class ZoneConfig
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string Type { get; set; } = "Block"; // "Block", "Text", "Trigger"
    public string? ActionId { get; set; } // e.g., "next_step"
    public string? Message { get; set; } // Text to display
}
```

The existing `VisualConfig` model will be updated to hold a list of these zones:
```csharp
public class VisualConfig
{
    // ... existing properties
    public List<ZoneConfig> Zones { get; set; } = new();
}
```

### 2.2 Hard Boundaries (Screen Edges)
By default, `WorldScene.cs` will clamp the player's movement to the viewport boundaries.
- The `_Process` loop will calculate the screen dimensions using `GetViewportRect().Size`.
- The player's `X` and `Y` velocity will be zeroed out if moving in that direction would cause their `GlobalPosition` to exit the screen boundaries (factoring in a small margin for the sprite width/height).

### 2.3 Zone Evaluation
During the movement update in `WorldScene._Process`:
1. Calculate the intended next position based on input/click target.
2. Check if the player's collision rect intersects with any of the `Zones` defined in the current `QuestStep`'s `VisualConfig`.
3. Handle intersection based on the `ZoneConfig.Type`:
    - **"Block"**: Zero out the velocity in the direction of the intersection, acting as an invisible wall.
    - **"Text"**: Stop the player, display the `Message` in the dialogue box. Implement a brief cooldown (e.g., 1-2 seconds) to prevent the text box from instantly reappearing if the player is still standing inside the zone after closing the dialogue.
    - **"Trigger"**: Call `TriggerEncounter()` to advance the quest (similar to the existing `SandyShore_East` hardcoded logic, which will be replaced by this data-driven approach).

## 3. Testing Strategy
- **Unit Tests:** Ensure JSON deserialization correctly maps arrays of zones into `VisualConfig.Zones`.
- **Manual Verification:** 
  1. Verify the player cannot walk off the edge of the default screen.
  2. Author a test JSON with a "Block" zone in the middle of the screen and verify the player cannot walk through it.
  3. Author a test JSON with a "Text" zone and verify the dialogue box appears and is dismissible without an infinite loop.
  4. Update `beat_1.json` to use a "Trigger" zone on the east edge instead of the hardcoded `SandyShore_East` check.