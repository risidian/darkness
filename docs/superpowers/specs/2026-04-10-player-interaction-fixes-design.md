# Design Spec: Player Interaction & Flow Fixes

**Date:** 2026-04-10
**Status:** Draft
**Topic:** Logic gaps, flow errors, and QoL failures in Darkness: Reforged.

## 1. Objective
Eliminate "Transition Friction" and potential soft-locks while improving the "feel" of player interactions in the World and Battle scenes.

## 2. Architecture Changes

### 2.1 Navigation Continuity (`BattleArgs` & `StealthArgs`)
- **Problem:** Returning from a battle or stealth scene currently teleports the player to a hardcoded position `(200, 300)`, which can cause infinite trigger loops.
- **Solution:** Add `ReturnPositionX` and `ReturnPositionY` (float) to `BattleArgs` and `StealthArgs`.
- **Data Flow:**
  1. `WorldScene` captures `_player.GlobalPosition.X` and `.Y` before navigation.
  2. `BattleScene`/`StealthScene` receives these values in `Initialize()`.
  3. On victory/completion, the positions are passed back to `WorldScene` via parameters.

### 2.2 Atomic Trigger Locking (`WorldScene` State Machine)
- **Problem:** `async` navigation allows players to move into triggers multiple times before the scene actually changes.
- **Solution:** Introduce a `WorldState` enum (`Exploring`, `Transitioning`, `InDialogue`).
- **Logic:**
  - Triggers only fire if `_state == WorldState.Exploring`.
  - Immediately set `_state = WorldState.Transitioning` upon trigger detection.
  - Reset to `Exploring` in `_Ready` or when dialogue/encounters conclude without scene changes.

### 2.3 Dialogue Interaction QoL
- **Problem:** Players cannot speed up dialogue, making it feel sluggish.
- **Solution:** Two-stage tap interaction.
  - **Tap 1:** If text is crawling (VisibleRatio < 1.0), set VisibleRatio to 1.0 immediately.
  - **Tap 2:** Advance to next line.
- **Note:** Requires implementing `VisibleRatio` logic (likely using a Tween) in `WorldScene.UpdateDialogueUI`.

### 2.4 Battle Sprite Selective Refresh
- **Problem:** Re-instantiating all sprites on every health change or death causes frame hitches.
- **Solution:** Check child count in `UpdateSprites()`. If it matches the entity count, update existing `StatusBar` and `LayeredSprite` properties instead of calling `QueueFree()`.

## 3. Implementation Details

### 3.1 BattleArgs & StealthArgs Update
```csharp
public class BattleArgs : NavigationArgs {
    public CombatData Combat { get; set; }
    public string QuestChainId { get; set; }
    public string QuestStepId { get; set; }
    public float ReturnPositionX { get; set; } // New
    public float ReturnPositionY { get; set; } // New
}

public class StealthArgs : NavigationArgs {
    public string QuestChainId { get; set; }
    public string QuestStepId { get; set; }
    public float ReturnPositionX { get; set; } // New
    public float ReturnPositionY { get; set; } // New
}
```

### 3.2 WorldScene Logic Update
- `_Process` will check `_state`.
- `NextDialogue` will handle the "Completion vs. Advancement" logic.

## 4. Verification Plan
- **Test Case 1 (Teleport Loop):** Trigger a battle at `(200, 300)`. Verify player returns to exact trigger position but trigger does not fire again (using a small cooldown or movement offset).
- **Test Case 2 (Race Condition):** Spam tap/movement into an NPC while a transition is starting. Verify only one BattleScene is instantiated.
- **Test Case 3 (Dialogue Skip):** Verify first tap finishes the line, second tap moves to next.
- **Test Case 4 (Performance):** Verify no "stutter" during enemy death in BattleScene.
