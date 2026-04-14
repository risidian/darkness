# Talent Tree Data Migration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `IsPassive` boolean property to all talent nodes in `talent-trees.json` to distinguish between stat bonuses (passive) and skill unlocks (active).

**Architecture:** Data-driven enhancement. No code changes required, only data schema update.

**Tech Stack:** JSON.

---

### Task 1: Update `talent-trees.json`

**Files:**
- Modify: `C:\Users\Mayce\Documents\GitHub\darkness\Darkness.Godot\assets\data\talent-trees.json`

- [ ] **Step 1: Apply `IsPassive` tags to all nodes in `talent-trees.json`**

  - For every node:
    - If node has `Effect` with `Stat` (e.g., Constitution, Strength, Armor, Attack, Block, HolyDamage, Wisdom, FireDamage, FrostDamage, Intelligence, Dexterity, Stealth, Healing):
      - Add `"IsPassive": true`
    - (Wait, looking at data, *all* nodes have `Effect`. Are there any active skill nodes? "Holy Smite" has `Effect`. The description says "Imbues your weapon with holy light (+5 Holy Damage)". This looks like a stat boost.)
    - *Correction*: I will assume nodes with `Effect` are all passive stat boosts based on the current schema, and thus all set to `true`. If there were skills that triggered active abilities (not just stat changes), they might be `false`. Given the current data provided, they all seem to follow the `Effect: { Stat, Value }` pattern.

- [ ] **Step 2: Build `Darkness.Godot` to verify parsing**

Run: `dotnet build C:\Users\Mayce\Documents\GitHub\darkness\Darkness.Godot\Darkness.Godot.csproj`
Expected: Success

- [ ] **Step 3: Commit**

```bash
git add C:\Users\Mayce\Documents\GitHub\darkness\Darkness.Godot\assets\data\talent-trees.json
git commit -m "data: add IsPassive to talent tree nodes"
```
