# Player Interaction & Flow Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate "Transition Friction" and soft-locks by improving navigation continuity, trigger logic, and dialogue interaction.

**Architecture:** 
- Add `ReturnPositionX/Y` to `BattleArgs` and `StealthArgs` for context-aware scene returns.
- Implement a `WorldState` enum in `WorldScene` to lock triggers during transitions.
- Enhance dialogue UI with a two-stage "finish line then advance" mechanic.
- Optimize `BattleScene` sprite updates to prevent frame hitches.

**Tech Stack:** Godot 4.6.1 (.NET 10), C#, LiteDB.

---

### Task 1: Navigation Args Update

**Files:**
- Modify: `Darkness.Core/Models/NavigationArgs.cs`

- [ ] **Step 1: Add ReturnPosition fields to BattleArgs and StealthArgs**

```csharp
namespace Darkness.Core.Models;

public abstract class NavigationArgs
{
}

public class BattleArgs : NavigationArgs
{
    public CombatData? Combat { get; set; }
    public string? QuestChainId { get; set; }
    public string? QuestStepId { get; set; }
    public float ReturnPositionX { get; set; } // New
    public float ReturnPositionY { get; set; } // New
}

public class StealthArgs : NavigationArgs
{
    public string? QuestChainId { get; set; }
    public string? QuestStepId { get; set; }
    public float ReturnPositionX { get; set; } // New
    public float ReturnPositionY { get; set; } // New
}
// ... rest of file
```

- [ ] **Step 2: Verify Build**

Run: `dotnet build Darkness.sln`
Expected: Success

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Models/NavigationArgs.cs
git commit -m "feat: add ReturnPositionX/Y to BattleArgs and StealthArgs"
```

---

### Task 2: WorldScene State & Trigger Locking

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Define WorldState enum and add _state field**

```csharp
namespace Darkness.Godot.Game;

public partial class WorldScene : Node2D, IInitializable
{
    private enum WorldState { Exploring, Transitioning, InDialogue }
    private WorldState _state = WorldState.Exploring;
    // ...
```

- [ ] **Step 2: Update _Ready to reset state**

```csharp
    public override async void _Ready()
    {
        // ... existing initialization ...
        _isReady = true;
        _isEncounterTriggered = false;
        _state = WorldState.Exploring; // Ensure we start in Exploring state
        GD.Print("[WorldScene] Ready and triggers enabled.");
    }
```

- [ ] **Step 3: Update _Process to respect _state**

```csharp
    public override void _Process(double delta)
    {
        if (!IsInsideTree() || !_isReady || _state != WorldState.Exploring) return;
        // ...
```

- [ ] **Step 4: Update TriggerEncounter to set state and capture position**

```csharp
    private async Task TriggerEncounter(bool isLocationTrigger = false)
    {
        if (_state != WorldState.Exploring) return;
        _state = WorldState.Transitioning;
        _isEncounterTriggered = true;

        // ... find chain/step ...

        if (step.Combat != null)
        {
            await _navigation.NavigateToAsync(Routes.Battle,
                new BattleArgs { 
                    Combat = step.Combat, 
                    QuestChainId = chain.Id, 
                    QuestStepId = step.Id,
                    ReturnPositionX = _player.GlobalPosition.X,
                    ReturnPositionY = _player.GlobalPosition.Y
                });
        }
        else if (step.Type == "stealth" || (step.Location?.SceneKey == "stealth"))
        {
            await _navigation.NavigateToAsync(Routes.Stealth,
                new StealthArgs { 
                    QuestChainId = chain.Id, 
                    QuestStepId = step.Id,
                    ReturnPositionX = _player.GlobalPosition.X,
                    ReturnPositionY = _player.GlobalPosition.Y
                });
        }
        // ... handle dialogue ...
        else if (!isLocationTrigger && step.Dialogue != null && step.Dialogue.Lines.Count > 0)
        {
            _state = WorldState.InDialogue;
            StartDialogue();
        }
        else
        {
            _state = WorldState.Exploring;
            _isEncounterTriggered = false;
        }
    }
```

- [ ] **Step 5: Handle return position in Initialize**

```csharp
    public void Initialize(IDictionary<string, object> parameters)
    {
        _state = WorldState.Transitioning; // Lock while initializing

        if (parameters.ContainsKey("PlayerPosition") && parameters["PlayerPosition"] is Vector2 pos)
        {
            _startingPosition = pos;
        }
        // ... existing stealth logic ...
    }
```

- [ ] **Step 6: Commit**

```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: implement WorldState and atomic trigger locking in WorldScene"
```

---

### Task 3: Dialogue Interaction QoL

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Add _isTextFullyDisplayed field and Update NextDialogue**

```csharp
    private bool _isTextFullyDisplayed = true; // Default to true if not using crawling

    private async void NextDialogue()
    {
        // ... handle choices logic ...

        _currentDialogueIndex++;
        if (_currentDialogueIndex >= _dialogue.Count)
        {
            _currentDialogueIndex = -1;
            _dialogueBox.Hide();
            _state = WorldState.Exploring; // Reset state
            _isEncounterTriggered = false;
            // ... quest advancement logic ...
        }
        else
        {
            UpdateDialogueUI();
        }
    }
```

- [ ] **Step 2: Update StartDialogue and OnChoiceSelected to set InDialogue state**

```csharp
    private void StartDialogue()
    {
        _state = WorldState.InDialogue;
        // ... rest of method ...
    }

    private async void OnChoiceSelected(BranchOption choice)
    {
        // ... morality and advancement logic ...
        if (nextStep?.Dialogue != null && nextStep.Dialogue.Lines.Count > 0)
        {
             _state = WorldState.InDialogue;
             // ... start new dialogue ...
        }
        else
        {
            _state = WorldState.Exploring; // Reset if conversation ends
            _dialogueBox.Hide();
            // ...
        }
    }
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: improve dialogue state management in WorldScene"
```

---

### Task 4: BattleScene Continuity & Optimization

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

- [ ] **Step 1: Add _returnPosition field and update Initialize**

```csharp
    private Vector2? _returnPosition;

    public void Initialize(IDictionary<string, object> parameters)
    {
        if (parameters.ContainsKey("Args") && parameters["Args"] is BattleArgs args)
        {
            _returnPosition = new Vector2(args.ReturnPositionX, args.ReturnPositionY);
            // ... existing combat initialization ...
        }
        // ...
    }
```

- [ ] **Step 2: Update _continueButton to use _returnPosition**

```csharp
        _continueButton.Pressed += async () =>
        {
            var parameters = new Dictionary<string, object>
            {
                { "PlayerPosition", _returnPosition ?? new Vector2(200, 300) }
            };
            await _navigation.NavigateToAsync("WorldScene", parameters);
        };
```

- [ ] **Step 3: Optimize UpdateSprites**

```csharp
    private async Task UpdateSprites()
    {
        if (!IsInsideTree()) return;

        // Only clear if counts changed
        if (_partyContainer.GetChildCount() != _party.Count || _enemyContainer.GetChildCount() != _enemies.Count)
        {
            foreach (Node child in _partyContainer.GetChildren()) child.QueueFree();
            foreach (Node child in _enemyContainer.GetChildren()) child.QueueFree();
            
            // ... (keep the existing full setup logic here) ...
        }
        else
        {
            // Update existing bars
            for (int i = 0; i < _party.Count; i++)
                _partyHealthBars[i].UpdateValue(_party[i].CurrentHP, _party[i].MaxHP);
            
            for (int i = 0; i < _enemies.Count; i++)
                _enemyHealthBars[i].UpdateValue(_enemies[i].CurrentHP, (int)_enemies[i].MaxHP);
        }
    }
```

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "feat: implement BattleScene continuity and sprite optimization"
```

---

### Task 5: StealthScene Continuity

**Files:**
- Modify: `Darkness.Godot/src/Game/StealthScene.cs`

- [ ] **Step 1: Update StealthScene to handle ReturnPosition**

Check `Darkness.Godot/src/Game/StealthScene.cs` for `Initialize` and navigation back to `WorldScene`. Add `ReturnPosition` handling similarly to `BattleScene`.

---

### Task 6: Verification

- [ ] **Step 1: Build and Run Tests**

Run: `dotnet build Darkness.sln && dotnet test Darkness.Tests`
Expected: Success

- [ ] **Step 2: Manual Verification (Simulated)**

Ensure no soft-locks when returning to WorldScene and dialogue flows correctly.
