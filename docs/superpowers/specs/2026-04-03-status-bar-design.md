# Design Spec: Reusable Status Bar Component

## 1. Overview
The **StatusBar** is a reusable Godot UI component designed to display unit vitals (Health, Mana, Stamina, EXP) as floating bars above characters, enemies, and NPCs. It supports both visual progress tracking and detailed text labels.

## 2. Architecture

### 2.1 Scene Structure (`StatusBar.tscn`)
- `VBoxContainer` (Root: `StatusBar`)
    - `Label` (`NameLabel`): Displays the unit's name. Centered.
    - `Control` (`BarWrapper`): Manages the layout for the bar and its text.
        - `TextureProgressBar` (`ProgressBar`): The visual bar.
        - `Label` (`ValueLabel`): Displays "Current / Max" (e.g., "75 / 100"). Centered over the bar.

### 2.2 Component Configuration (`StatusBar.cs`)
The component will have a C# script with the following properties and methods:

**Properties:**
- `StatusType Type`: An enum (`HP`, `Mana`, `Stamina`, `EXP`).
- `bool ShowName`: Toggle for the Name Label.
- `bool ShowValue`: Toggle for the Value Label.
- `Color CustomColor`: Optional override for the bar color.

**Methods:**
- `void Setup(string unitName, int current, int max, StatusType type = StatusType.HP)`: Initializes the bar's state and visuals.
- `void UpdateValue(int current, int max)`: Updates the bar's progress and text.
- `void SetColor(Color color)`: Manages the `TextureProgressBar` tint.

## 3. Visual Design
- **HP (Red):** `linear-gradient(90deg, #8b0000, #ff4d4d)`
- **Mana (Blue):** `linear-gradient(90deg, #00008b, #4d4dff)`
- **Stamina (Green):** `linear-gradient(90deg, #006400, #4dff4d)`
- **Font:** Uses the project's default font (Medieval-style where applicable).

## 4. Integration

### 4.1 BattleScene
- During `UpdateSprites()`, each `LayeredSprite` wrapper will now include one or more `StatusBar` instances.
- **Party Members:** Display both HP and Mana bars.
- **Enemies/Creeps:** Display only HP bars.
- `ExecuteAttack()` will call `UpdateValue()` on the target's `StatusBar`.

### 4.2 WorldScene
- `Player` and `NPC` nodes will have a `StatusBar` added to their hierarchy.
- Bars will stay hidden if `CurrentHP == MaxHP` (unless hovered).
- When a unit takes damage (future feature), the bar will become visible.

## 5. Data Flow
1. `BattleScene` logic (e.g., `ExecuteAttack`) modifies the `Character` or `Enemy` model's `CurrentHP`.
2. The scene controller calls `StatusBar.UpdateValue(model.CurrentHP, model.MaxHP)`.
3. `StatusBar` updates its internal UI nodes.

## 6. Testing Strategy
- **Unit Test:** Verify `StatusBar.UpdateValue()` correctly calculates percentages and updates labels.
- **Integration Test:** Ensure `BattleScene` correctly instantiates and updates bars during a mock combat sequence.
- **Visual Check:** Confirm bars are correctly positioned above scaled sprites.
