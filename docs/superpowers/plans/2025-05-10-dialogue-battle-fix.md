# Dialogue & Battle Sprite Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Task 2 of the Dialogue & Battle Fix plan: Update quests.json with dialogue and refactor BattleScene.cs to use SpriteKey for enemy sprites.

**Architecture:** 
1. Update `quests.json` to include inline dialogue for the first 3 beats.
2. Refactor `BattleScene.cs` to remove hardcoded string checks (e.g., "hound") and instead use the `SpriteKey` property from the `Enemy` model.
3. Fix the deep copy logic in `BattleScene.Initialize` to ensure `SpriteKey` and other relevant properties are carried over to the battle-local `_enemies` list.

**Tech Stack:** JSON, C# (Godot)

---

### Task 1: Update quests.json Dialogue

**Files:**
- Modify: `Darkness.Godot/assets/data/quests.json`

- [ ] **Step 1: Add dialogue to beat_1, beat_2, and beat_3**

Update `quests.json` with the following dialogue objects:

```json
// beat_1
"Dialogue": {
  "Speaker": "Old Man",
  "Lines": [
    "Welcome to the Shore of Camelot, Wanderer.",
    "The path to the castle is blocked by shadows.",
    "You'll find only hounds and darkness to the east."
  ]
}

// beat_2
"Dialogue": {
  "Speaker": "Dark Warrior",
  "Lines": [
    "You dare approach the gates of Camelot?",
    "Your journey ends here, mortal.",
    "The shadow consumes all."
  ]
}

// beat_3
"Dialogue": {
  "Speaker": "The Sorcerer",
  "Lines": [
    "The Dark Warrior was only the beginning.",
    "I have woven the shadows into a cage for you.",
    "Let us see if your spirit is truly unbreakable."
  ]
}
```

- [ ] **Step 2: Commit JSON changes**

```bash
git add Darkness.Godot/assets/data/quests.json
git commit -m "feat: add quest dialogue to JSON"
```

---

### Task 2: Refactor BattleScene.cs Sprite Logic

**Files:**
- Modify: `Darkness.Godot/src/Game/BattleScene.cs`

- [ ] **Step 1: Fix deep copy in Initialize**

In `Initialize(IDictionary<string, object> parameters)`, ensure `SpriteKey` is copied when creating the battle-local `_enemies` list.

- [ ] **Step 2: Update UpdateSprites to use SpriteKey**

Refactor the enemy sprite setup loop in `UpdateSprites()` to use `enemy.SpriteKey` instead of checking the name for "hound".

- [ ] **Step 3: Update animation logic in ExecuteAttack**

In `ExecuteAttack`, replace name-based animation triggers with `SpriteKey`-based triggers.

- [ ] **Step 4: Commit C# changes**

```bash
git add Darkness.Godot/src/Game/BattleScene.cs
git commit -m "fix: refactor battle sprite switching to use SpriteKey"
```
