# Quest & Skill Balance Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Balance the first quest beat by nerfing the Creek Monster and add resource costs/cooldowns to all skills to make mana and stamina pools meaningful.

**Architecture:** Data-driven balance pass modifying JSON seed files.

**Tech Stack:** JSON, .NET 10 (Build Verification).

---

### Task 1: Nerf Creek Monster

**Files:**
- Modify: `Darkness.Godot/assets/data/quests/beat_1_the_awakening.json`

- [ ] **Step 1: Locate and update Creek Monster stats**

In `Darkness.Godot/assets/data/quests/beat_1_the_awakening.json`, find the `Creek Monster` in the `beat_1_sneak_combat` step and update its stats:
- Level: 3 -> 1
- MaxHP: 300 -> 45
- CurrentHP: 300 -> 45
- Attack: 15 -> 9
- Defense: 8 -> 5
- ExperienceReward: 100 -> 50

### Task 2: Update Skill Costs and Cooldowns

**Files:**
- Modify: `Darkness.Godot/assets/data/skills.json`

- [ ] **Step 1: Add ManaCost, StaminaCost, and Cooldown to skills**

Update `Darkness.Godot/assets/data/skills.json` with the following values:
1.  **Arcane Bolt**: Add `"ManaCost": 5`
2.  **Fireball**: Add `"ManaCost": 10`, `"Cooldown": 2`
3.  **Quick Shot**: Add `"StaminaCost": 5`
4.  **Snipe**: Add `"StaminaCost": 10`, `"Cooldown": 1`
5.  **Quick Stab**: Add `"StaminaCost": 4`
6.  **Vitals**: Add `"StaminaCost": 8`, `"Cooldown": 1`
7.  **Cleave**: Add `"StaminaCost": 12`, `"Cooldown": 2`
8.  **Crush**: Add `"StaminaCost": 10`, `"Cooldown": 1`
9.  **Smash**: Add `"StaminaCost": 8`, `"Cooldown": 1`
10. **Stun**: Add `"StaminaCost": 10`, `"Cooldown": 3`
11. **Slash**: Add `"StaminaCost": 6`
12. **Thrust**: Add `"StaminaCost": 8`, `"Cooldown": 1`
13. **Punch**: Add `"StaminaCost": 2`
14. **Kick**: Add `"StaminaCost": 5`, `"Cooldown": 1`
15. **Mana Shield**: Add `"ManaCost": 15`, `"Cooldown": 3`
16. **Dodge**: Add `"StaminaCost": 10`, `"Cooldown": 3`
17. **Parry**: Add `"StaminaCost": 12`, `"Cooldown": 3`
18. **Shield Block**: Add `"StaminaCost": 15`, `"Cooldown": 2`
19. **Deflect**: Add `"StaminaCost": 10`, `"Cooldown": 2`
20. **Brace**: Add `"StaminaCost": 5`, `"Cooldown": 2`
21. **Holy Strike**: Add `"Cooldown": 2` (already has ManaCost)
22. **Offhand Stab**: Add `"StaminaCost": 5`

### Task 3: Verification

- [ ] **Step 1: Build the solution**

Run: `dotnet build Darkness.sln`
Expected: Success.
