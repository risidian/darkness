# Combat System: d20 Rules Conversion

## 1. Objective
Transition the existing deterministic combat engine into a d20-based system inspired by tabletop RPG mechanics (D&D 5e). This includes attack rolls against Armor Class (AC), stat modifiers for bonuses, damage dice rolling, and d20-based initiative.

## 2. Models & Data Structure Changes

### 2.1 Item & Skill Updates
Add a `DamageDice` string property to both `Item` and `Skill` models.
- Example values: `"1d8"`, `"2d6"`, `"1d4"`.
- We will retain the existing `AttackBonus` and `BasePower` fields to act as flat modifiers added to the roll.

### 2.2 New Model: CombatResult
Introduce a new model to capture the outcome of an attack.
```csharp
public class CombatResult
{
    public bool IsHit { get; set; }
    public bool IsCriticalHit { get; set; }
    public bool IsCriticalMiss { get; set; }
    public int DamageDealt { get; set; }
}
```

## 3. Core Combat Logic (`CombatEngine.cs`)

### 3.1 D&D Stat Modifiers
Implement a standard D&D modifier calculation helper:
`Modifier = Math.Floor((Stat - 10) / 2.0)`

### 3.2 Armor Class (AC) Calculation
- **Characters:** Base AC (10) + Item ArmorBonus + DEX Modifier.
- **Enemies:** Base AC = `Enemy.Defense`.

### 3.3 Attack Rolls
- Replace integer return type of `CalculateDamage` with `CombatResult`.
- Roll a `1d20`.
- **Critical Hit:** Natural 20 (Automatic hit, `IsCriticalHit = true`).
- **Critical Miss:** Natural 1 (Automatic miss, `IsCriticalMiss = true`).
- **Standard Hit:** `(1d20 Roll + Stat Modifier + Item/Skill Attack Bonus) >= Target AC`.

### 3.4 Damage Rolls
- Parse the `DamageDice` string.
- Roll the specified number of dice.
- **Total Damage:** `Dice Roll Total + Stat Modifier + Item/Skill Base Power`.
- **Critical Damage:** Roll the damage dice twice, then add modifiers once.

### 3.5 Initiative
- Update `CalculateTurnOrder`.
- Initiative Roll = `1d20 + DEX Modifier`.
- Sort participants descending by their Initiative Roll.

## 4. UI Adjustments (`BattleScene.tscn` / `BattleScene.cs`)

### 4.1 Handling CombatResult
- Update the combat loop to read `CombatResult.IsHit`, `IsCriticalHit`, and `IsCriticalMiss`.
- Update the `_combatLog` to display explicit text for misses and critical hits.
- Skip playing the target's "hurt" animation or showing damage numbers if the attack misses.

### 4.2 Initiative / Turn Order Display
- Add a new UI element (e.g., an `ItemList` or `VBoxContainer` with Labels) to the `BattleScene` to display the current turn order explicitly.
- Populate this list at the start of battle (after `CalculateTurnOrder` is called) so the player can see who acts when.
- Highlight the currently active character/enemy in the list.

## 5. Testing
- Update all existing unit tests covering `CombatEngine.CalculateDamage` and `CombatEngine.CalculateTurnOrder` to account for the new return types, dice parsing, and modifier math.
- Create new tests specifically for critical hits, critical misses, and dice string parsing.
