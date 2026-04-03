# Morality System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a numerical morality system tracked on the player character, influenced by dialogue choices and enemies killed, and capable of unlocking/locking quests.

**Architecture:** 
- **Core Models:** Add `Morality` to `Character`, `MoralityImpact` to `Enemy` and `DialogueChoice`, and Morality bounds to `QuestNode`.
- **Combat Integration:** Update `BattleScene` to apply enemy `MoralityImpact` upon enemy defeat.
- **Dialogue Integration:** Update `WorldScene` to apply choice `MoralityImpact` upon selection.
- **Quest Filtering:** Update `QuestService` to filter quests based on the new `RequiredMoralityMin` and `RequiredMoralityMax` bounds.

**Tech Stack:** .NET 10, Godot 4.6.1, C#.

---

### Task 1: Core Model Updates

**Files:**
- Modify: `Darkness.Core/Models/Character.cs`
- Modify: `Darkness.Core/Models/Enemy.cs`
- Modify: `Darkness.Core/Models/DialogueChoice.cs`
- Modify: `Darkness.Core/Models/QuestNode.cs`

- [ ] **Step 1: Update Character.cs**

```csharp
// Add to Darkness.Core/Models/Character.cs
public int Morality { get; set; } = 0;
```

- [ ] **Step 2: Update Enemy.cs**

```csharp
// Add to Darkness.Core/Models/Enemy.cs
public int MoralityImpact { get; set; } = 0;
```

- [ ] **Step 3: Update DialogueChoice.cs**

```csharp
// Add to Darkness.Core/Models/DialogueChoice.cs
public int MoralityImpact { get; set; } = 0;
```

- [ ] **Step 4: Update QuestNode.cs**

```csharp
// Add to Darkness.Core/Models/QuestNode.cs
public int? RequiredMoralityMin { get; set; }
public int? RequiredMoralityMax { get; set; }
```

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Models/
git commit -m "feat: add morality tracking to core models"
```

---

### Task 2: Service & Data Updates

**Files:**
- Modify: `Darkness.Core/Services/QuestService.cs`
- Modify: `Darkness.Godot/assets/data/quests.json`

- [ ] **Step 1: Update QuestService filtering**

```csharp
// Update GetAvailableQuests in Darkness.Core/Services/QuestService.cs
public List<QuestNode> GetAvailableQuests(Character character)
{
    return _quests.Where(q => 
        !character.CompletedQuestIds.Contains(q.Id) && 
        q.Prerequisites.All(p => character.CompletedQuestIds.Contains(p)) &&
        (!q.RequiredMoralityMin.HasValue || character.Morality >= q.RequiredMoralityMin.Value) &&
        (!q.RequiredMoralityMax.HasValue || character.Morality <= q.RequiredMoralityMax.Value)).ToList();
}
```

- [ ] **Step 2: Update quests.json with morality impacts**

Modify the `beat_1` choices to have impact:
```json
  "Choices": [
    {
      "Text": "[Fight] I will slay the hounds.",
      "NextQuestId": "beat_1_combat",
      "MoralityImpact": -5
    },
    {
      "Text": "[Sneak] I will find another way.",
      "NextQuestId": "beat_1_sneak",
      "MoralityImpact": 5
    }
  ]
```

Modify the `beat_1_sneak` encounter to make the Creek Monster impact morality:
```json
  "Encounter": {
    "Enemies": [
      {
        "Name": "Creek Monster",
        "Level": 1,
        "MaxHP": 800,
        "CurrentHP": 800,
        "Attack": 15,
        "Defense": 8,
        "Speed": 10,
        "SpriteKey": "hound",
        "MoralityImpact": -2
      }
    ]
  }
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Services/QuestService.cs Darkness.Godot/assets/data/quests.json
git commit -m "feat: implement morality filtering in QuestService and update data"
```

---

### Task 3: Scene Integration

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`

- [ ] **Step 1: Apply morality in BattleScene**

```csharp
// Update ExecuteAttack in Darkness.Godot/src/Game/BattleScene.cs
// Around line 301, inside the block: if (target.CurrentHP <= 0)
if (target.CurrentHP <= 0)
{
    _combatLog.AppendText($"\n[color=gold]{target.Name} is defeated![/color]");
    
    // Apply morality impact
    if (target.MoralityImpact != 0 && _party.Count > 0)
    {
        _party[0].Morality += target.MoralityImpact;
        string color = target.MoralityImpact > 0 ? "cyan" : "purple";
        string sign = target.MoralityImpact > 0 ? "+" : "";
        _combatLog.AppendText($"\n[color={color}]Morality changed {sign}{target.MoralityImpact} (Total: {_party[0].Morality})[/color]");
    }
    
    _enemies.Remove(target);
    await UpdateSprites();
}
```

- [ ] **Step 2: Apply morality in WorldScene dialogue choices**

```csharp
// Update OnChoiceSelected in Darkness.Godot/src/Game/WorldScene.cs
private void OnChoiceSelected(DialogueChoice choice)
{
    if (_currentDialogueQuest != null && _session.CurrentCharacter != null)
    {
        var questService = GetNode<Global>("/root/Global").Services!.GetRequiredService<IQuestService>();
        questService.CompleteQuest(_session.CurrentCharacter, _currentDialogueQuest.Id);
        questService.CompleteQuest(_session.CurrentCharacter, choice.NextQuestId);
        
        // Apply morality
        if (choice.MoralityImpact != 0)
        {
            _session.CurrentCharacter.Morality += choice.MoralityImpact;
            GD.Print($"[Morality] Changed by {choice.MoralityImpact}. New Total: {_session.CurrentCharacter.Morality}");
        }
    }

    _currentDialogueIndex = -1;
    _dialogueBox.Hide();
    
    foreach (Node child in _choicesContainer.GetChildren())
    {
        child.QueueFree();
    }
}
```

- [ ] **Step 3: Verify the build**

Run: `dotnet build Darkness.sln`
Ensure no errors occur.

- [ ] **Step 4: Commit**

```bash
git add Darkness.Godot/src/Game/
git commit -m "feat: integrate morality impact into battle and dialogue scenes"
```
