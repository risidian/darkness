# Talent Tree System Design

## 1. Overview
The Talent Tree system provides a data-driven framework for character progression beyond basic attributes. Players gain 1 Talent Point every 2 levels to spend on passive stat boosts and active combat skills. Trees are organized into tiers (Base, Advanced, Hidden) with specific unlocking requirements.

## 2. Requirements
- **Point Generation:** 1 Talent Point awarded every 2 levels.
- **Tree Tiers:**
    - **Tier 1 (Base):** Available from Level 1 (e.g., Knight).
    - **Tier 2 (Advanced):** Requires specific level and points spent in Tier 1 (e.g., Holy Knight: Level 20 + 5 points in Knight).
    - **Tier 3 (Hidden):** Requires specific level, base stats, and points spent in Tier 2 (e.g., Crusader: Level 40 + 20 Strength + 20 points in Holy Knight).
- **Node Costs:** 1 point per talent node.
- **Node Types:**
    - **Passive:** Permanent stat increases (Strength, Dexterity, etc.).
    - **Active:** Unlocks new skills for use in combat.
- **Visibility:** Hidden trees remain invisible until prerequisites (Level/Stats/Points) are met.

## 3. Data Architecture

### 3.1 `talent-trees.json`
Located in `assets/data/talent-trees.json`.
```json
[
  {
    "Id": "knight_tree",
    "Name": "Knight",
    "Tier": 1,
    "Prerequisites": { "Level": 1 },
    "Nodes": [
      {
        "Id": "k1_strength",
        "Name": "Iron Grip",
        "Description": "+5 Strength",
        "PointsRequired": 1,
        "Effects": { "Stat": "Strength", "Value": 5 }
      },
      {
        "Id": "k2_holy_strike",
        "Name": "Holy Strike",
        "Description": "Unlocks Holy Strike skill",
        "PointsRequired": 1,
        "Prerequisites": { "NodeId": "k1_strength" },
        "Effects": { "Skill": "Holy Strike" }
      }
    ]
  }
]
```

### 3.2 Model Updates (`Darkness.Core.Models`)
**Character.cs:**
- `int TalentPoints`: Unspent points.
- `List<string> UnlockedTalentIds`: List of purchased node IDs.

**Talent Models:**
- `TalentTree`: Container for tree metadata and nodes.
- `TalentNode`: Individual talent data.
- `TalentEffect`: Defines what the talent grants (Stat vs Skill).

## 4. Service Layer (`Darkness.Core`)

### 4.1 `ITalentService` / `TalentService`
- `GetAvailableTrees(Character character)`: Filter trees by prerequisites.
- `PurchaseTalent(Character character, string nodeId)`: Validate and unlock.
- `GetTotalPointsSpentInTree(Character character, string treeId)`: For tier unlocking.
- `ApplyPassiveBonuses(Character character)`: Calculate total stat boosts from talents.

### 4.2 Integration
- **`LevelingService`**: Update to award `TalentPoints` every 2 levels.
- **`WeaponSkillService`**: Update to include skills unlocked via `UnlockedTalentIds`.
- **`Character.RecalculateDerivedStats()`**: Ensure talent passives are included in calculations.

## 5. UI Design (`Darkness.Godot`)

### 5.1 `TalentTreeScene`
- **Tabbed Interface:** Switch between unlocked trees.
- **Point Counter:** "Unspent Talents: X" header.
- **Node Grid:** Visual representation of nodes.
    - Locked (Requirements not met).
    - Available (Prereqs met + points available).
    - Purchased (Already active).
- **Detail Panel:** Shows node name, description, and "Learn" button.

## 6. Testing Strategy
- **Unit Tests:**
    - Verify point awarding at levels 2, 4, 6, etc.
    - Verify tree unlocking logic (Level/Stats/Points).
    - Verify stat bonus calculation.
- **Integration Tests:**
    - Verify talent skills appear in combat.
    - Verify hidden trees appear only when conditions are met.
