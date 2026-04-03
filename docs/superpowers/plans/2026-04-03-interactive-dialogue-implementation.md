# Interactive Dialogue Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a simple branching dialogue system where players can make choices that affect quest progression.

**Architecture:** 
- **Data Models:** Expand `DialogueData` to include a list of `DialogueChoice` objects.
- **UI Logic:** Update `WorldScene` to dynamically spawn buttons for each choice at the end of a conversation.
- **Progression Logic:** Clicking a choice resolves the current dialogue node and unlocks the chosen path by adding its ID to completed quests.

**Tech Stack:** .NET 10, Godot 4.6.1, C#.

---

### Task 1: Core Models

**Files:**
- Create: `Darkness.Core/Models/DialogueChoice.cs`
- Modify: `Darkness.Core/Models/DialogueData.cs`

- [ ] **Step 1: Create DialogueChoice.cs**

```csharp
namespace Darkness.Core.Models
{
    public class DialogueChoice
    {
        public string Text { get; set; } = string.Empty;
        public string NextQuestId { get; set; } = string.Empty;
    }
}
```

- [ ] **Step 2: Update DialogueData.cs**

```csharp
using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class DialogueData
    {
        public string Speaker { get; set; } = string.Empty;
        public List<string> Lines { get; set; } = new();
        public List<DialogueChoice> Choices { get; set; } = new();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Darkness.Core/Models/
git commit -m "feat: add DialogueChoice model to support interactive dialogue"
```

---

### Task 2: Data Update

**Files:**
- Modify: `Darkness.Godot/assets/data/quests.json`

- [ ] **Step 1: Add a branching choice to beat_1 in quests.json**

Replace the existing `beat_1` entry with this branching version:

```json
  {
    "Id": "beat_1",
    "Title": "The Awakening",
    "IsMainStory": true,
    "Prerequisites": [],
    "Dialogue": {
      "Speaker": "Old Man",
      "Lines": [
        "Welcome to the Shore of Camelot, Wanderer.",
        "The path to the castle is blocked by shadows.",
        "You must choose your path carefully."
      ],
      "Choices": [
        {
          "Text": "[Fight] I will slay the hounds.",
          "NextQuestId": "beat_1_combat"
        },
        {
          "Text": "[Sneak] I will find another way.",
          "NextQuestId": "beat_1_sneak"
        }
      ]
    }
  },
  {
    "Id": "beat_1_combat",
    "Title": "The Hound Master",
    "IsMainStory": true,
    "Prerequisites": ["beat_1", "beat_1_combat"],
    "Encounter": {
      "Enemies": [
        { "Name": "Undead Dog A", "MaxHP": 40, "Attack": 12, "Defense": 5, "Speed": 12, "SpriteKey": "hound" },
        { "Name": "Undead Dog B", "MaxHP": 40, "Attack": 12, "Defense": 5, "Speed": 12, "SpriteKey": "hound" },
        { "Name": "Undead Dog C", "MaxHP": 40, "Attack": 12, "Defense": 5, "Speed": 12, "SpriteKey": "hound" }
      ]
    }
  },
  {
    "Id": "beat_1_sneak",
    "Title": "The Hidden Path",
    "IsMainStory": true,
    "Prerequisites": ["beat_1", "beat_1_sneak"],
    "Dialogue": {
      "Speaker": "Old Man",
      "Lines": [
        "Wise choice. The shadows are thickest near the main road.",
        "Follow the riverbed, but beware the Creek Monster."
      ]
    },
    "Encounter": {
      "Enemies": [
        { "Name": "Creek Monster", "MaxHP": 80, "Attack": 15, "Defense": 8, "Speed": 10, "SpriteKey": "hound" }
      ]
    }
  }
```
*(Note: We add the choice ID to its own prerequisites so it can only be started if the choice was made)*

- [ ] **Step 2: Commit**

```bash
git add Darkness.Godot/assets/data/quests.json
git commit -m "feat: add interactive dialogue choices to quest data"
```

---

### Task 3: WorldScene UI Updates

**Files:**
- Modify: `Darkness.Godot/src/Game/WorldScene.cs`
- Modify: `Darkness.Godot/scenes/WorldScene.tscn` (Implicitly, via code adding nodes)

- [ ] **Step 1: Add a Container for Choices in WorldScene.cs**

Add a class-level variable for the choices container and a list to track current choices:
```csharp
// Add to class fields
private VBoxContainer _choicesContainer = null!;
private List<DialogueChoice> _currentChoices = new();
private QuestNode? _currentDialogueQuest = null;
```

- [ ] **Step 2: Initialize the Container in _Ready**

```csharp
// Inside _Ready()
_choicesContainer = new VBoxContainer();
GetNode<VBoxContainer>("CanvasLayer/DialogueBox/VBoxContainer").AddChild(_choicesContainer);
```

- [ ] **Step 3: Update StartDialogue to store choices and quest**

```csharp
// Update StartDialogue()
private void StartDialogue()
{
    // ... existing velocity/idle setup ...

    _speakerName = "Old Man";
    _dialogue = new List<string> {
        "Welcome to the Shore of Camelot, Wanderer.",
        "The path to the castle is blocked by shadows.",
        "You'll find only hounds and darkness to the east."
    };
    _currentChoices.Clear();
    _currentDialogueQuest = null;

    var questService = GetNode<Global>("/root/Global").Services!.GetRequiredService<IQuestService>();
    var session = GetNode<Global>("/root/Global").Services!.GetRequiredService<ISessionService>();
    
    var quest = questService.GetNextAvailableMainStoryQuest(session.CurrentCharacter);
    
    if (quest?.Dialogue != null)
    {
        _currentDialogueQuest = quest;
        _speakerName = quest.Dialogue.Speaker;
        _dialogue = quest.Dialogue.Lines;
        if (quest.Dialogue.Choices != null)
        {
            _currentChoices = quest.Dialogue.Choices;
        }
    }

    if (_dialogue.Count == 0) return;

    _currentDialogueIndex = 0;
    _dialogueBox.Show();
    
    UpdateDialogueUI();
}
```

- [ ] **Step 4: Update UpdateDialogueUI to show buttons**

```csharp
// Update UpdateDialogueUI()
private void UpdateDialogueUI()
{
    _nameLabel.Text = _speakerName;
    _textLabel.Text = _dialogue[_currentDialogueIndex];
    
    var prompt = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/PromptLabel");
    
    // Clear existing buttons
    foreach (Node child in _choicesContainer.GetChildren())
    {
        child.QueueFree();
    }

    // If we are at the last line and have choices, show them
    if (_currentDialogueIndex == _dialogue.Count - 1 && _currentChoices.Count > 0)
    {
        prompt.Hide(); // Hide "TAP TO CONTINUE"
        
        foreach (var choice in _currentChoices)
        {
            var btn = new Button { Text = choice.Text };
            btn.Pressed += () => OnChoiceSelected(choice);
            _choicesContainer.AddChild(btn);
        }
    }
    else
    {
        prompt.Show(); // Show "TAP TO CONTINUE"
        prompt.Text = "[TAP TO CONTINUE]";
    }
}
```

- [ ] **Step 5: Implement OnChoiceSelected logic**

```csharp
// Add new method
private async void OnChoiceSelected(DialogueChoice choice)
{
    // 1. Mark the current dialogue quest as complete
    if (_currentDialogueQuest != null && _session.CurrentCharacter != null)
    {
        var questService = GetNode<Global>("/root/Global").Services!.GetRequiredService<IQuestService>();
        questService.CompleteQuest(_session.CurrentCharacter, _currentDialogueQuest.Id);
        
        // 2. Mark the chosen path as "completed" so it meets its own prerequisite
        // This effectively "unlocks" the chosen path.
        questService.CompleteQuest(_session.CurrentCharacter, choice.NextQuestId);
    }

    // 3. Hide dialogue box and end conversation
    _currentDialogueIndex = -1;
    _dialogueBox.Hide();
    
    // Clear buttons
    foreach (Node child in _choicesContainer.GetChildren())
    {
        child.QueueFree();
    }
    
    // 4. Optionally, immediately check if the chosen path is an encounter and trigger it.
    // For now, the player can just walk to the east boundary to trigger whatever path they chose.
}
```

- [ ] **Step 6: Prevent NextDialogue from closing if choices are visible**

```csharp
// Update NextDialogue()
private void NextDialogue()
{
    // If we are showing choices, tapping should not advance or close the dialogue
    if (_currentDialogueIndex == _dialogue.Count - 1 && _currentChoices.Count > 0)
    {
        return;
    }

    _currentDialogueIndex++;
    if (_currentDialogueIndex >= _dialogue.Count)
    {
        _currentDialogueIndex = -1;
        _dialogueBox.Hide();
        
        // If there were no choices, mark the dialogue quest as complete when finished
        if (_currentChoices.Count == 0 && _currentDialogueQuest != null && _session.CurrentCharacter != null)
        {
            var questService = GetNode<Global>("/root/Global").Services!.GetRequiredService<IQuestService>();
            questService.CompleteQuest(_session.CurrentCharacter, _currentDialogueQuest.Id);
        }
    }
    else
    {
        UpdateDialogueUI();
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add Darkness.Godot/src/Game/WorldScene.cs
git commit -m "feat: implement interactive dialogue UI and choice progression logic"
```
