# Fix BattleScene Black Screen and Layout Issues Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve the `BattleScene` black screen issue caused by an `InvalidCastException` and fix several UI/layout bugs identified during investigation.

**Architecture:**
1.  Fix the `InvalidCastException` in `BattleScene.cs` by using the correct node type (`Control`) for `CombatArea`.
2.  Improve `BattleScene` layout by relocating the `TurnOrderList` to a more appropriate container.
3.  Fix enemy initialization logic in `SetupBattle` to ensure `_originalEnemies` is correctly populated.
4.  Remove unnecessary frame delays in `_Ready` that could be causing UI flickering or delays.

**Tech Stack:** C#, Godot 4.6.1 (.NET 10).

---

### Task 1: Fix InvalidCastException and Relocate TurnOrderList

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

- [ ] **Step 1: Update node access and TurnOrderList placement**

Modify `_Ready()` to use `Control` for `CombatArea` and add `_turnOrderList` to a side container or the main root to avoid HBox issues.

```csharp
// Around line 180
        _partyContainer = GetNode<HBoxContainer>("CombatArea/PartyContainer");
        _enemyContainer = GetNode<VBoxContainer>("CombatArea/EnemyContainer");

        _turnOrderList = new ItemList();
        _turnOrderList.Name = "TurnOrderList";
        _turnOrderList.CustomMinimumSize = new Vector2(200, 200);
        // Add to main scene root or a dedicated panel instead of CombatArea (which is a Control, not HBox)
        AddChild(_turnOrderList);
        _turnOrderList.SetPosition(new Vector2(20, 150));
```

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "fix: resolve InvalidCastException and improve BattleScene layout"
```

### Task 2: Fix Enemy Initialization Logic and Delays

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

- [ ] **Step 1: Ensure _originalEnemies is correctly managed**

```csharp
    private void SetupBattle()
    {
        if (_party.Count == 0 && _session.CurrentCharacter != null)
        {
            var pc = _session.CurrentCharacter;
            if (pc.MaxHP <= 0) pc.MaxHP = 100;
            if (pc.CurrentHP <= 0) pc.CurrentHP = pc.MaxHP;
            pc.IsBlocking = false;
            _party.Add(pc);
        }

        if (_enemies.Count == 0)
        {
            GD.PrintErr("[BattleScene] No enemies provided in parameters, using default Hellhound encounter.");
            // ... (keep defaults)
        }

        // Ensure _originalEnemies is only populated once if empty
        if (_originalEnemies.Count == 0)
        {
            foreach (var enemy in _enemies)
            {
                _originalEnemies.Add(new Enemy
                {
                    Name = enemy.Name,
                    Level = enemy.Level,
                    MaxHP = enemy.MaxHP,
                    CurrentHP = enemy.MaxHP,
                    Attack = enemy.Attack,
                    Defense = enemy.Defense,
                    Accuracy = enemy.Accuracy,
                    Speed = enemy.Speed,
                    Evasion = enemy.Evasion,
                    SpriteKey = enemy.SpriteKey,
                    IsInvincible = enemy.IsInvincible,
                    MoralityImpact = enemy.MoralityImpact,
                    ExperienceReward = enemy.ExperienceReward,
                    GoldReward = enemy.GoldReward
                });
            }
        }
```

- [ ] **Step 2: Reduce _Ready delays**

```csharp
// Around line 238
-        await ToSignal(GetTree(), "process_frame");
-        await ToSignal(GetTree(), "process_frame");
-        await ToSignal(GetTree(), "process_frame");
+        await ToSignal(GetTree(), "process_frame");
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "fix: simplify BattleScene initialization and fix enemy setup logic"
```
