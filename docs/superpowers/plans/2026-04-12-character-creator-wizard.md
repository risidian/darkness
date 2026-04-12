# Character Creator Wizard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the character generator into a 3-step wizard with automated class gear.

**Architecture:**
- **UI Nesting:** Group existing Godot controls into `Step1`, `Step2`, and `Step3` containers.
- **State Management:** Add `_currentStep` to `CharacterGenScene.cs` to manage visibility.
- **Class-to-Gear Mapping:** Utilize existing `ClassDefaults` logic but hide the equipment dropdowns from the user.

**Tech Stack:** Godot 4.6.1 (.NET 10), LiteDB.

---

### Task 1: UI Scene Reorganization

**Files:**
- Modify: `Darkness.Godot/scenes/CharacterGenScene.tscn`

- [ ] **Step 1: Wrap controls in Step containers**
    - Group `NameEdit` into `Step1Container`.
    - Group `HeadOption` into `Step2Container`.
    - Group `ClassOption`, `SkinOption`, `FaceOption`, `EyesOption`, `HairStyleOption`, `HairColorOption` into `Step3Container`.
    - Hide equipment labels and options (`Armor`, `Legs`, etc.) by setting their `visible` property to `false` permanently in the scene.
- [ ] **Step 2: Add Navigation buttons**
    - Ensure `NextButton` and `BackButton` exist at the bottom of the `ControlsArea`.

---

### Task 2: Wizard Logic Implementation

**Files:**
- Modify: `Darkness.Godot/src/UI/CharacterGenScene.cs`

- [ ] **Step 1: Add step management variables**
    - Add `private int _currentStep = 1;`.
    - Reference the new `StepContainer` nodes.
- [ ] **Step 2: Implement `SetStep(int step)`**
    - Toggle visibility of containers.
    - Toggle visibility of `_spritePreview` (hidden in Step 1).
    - Update `NextButton` text ("NEXT" vs "FINISH").
- [ ] **Step 3: Update Event Handlers**
    - `OnNextPressed`: Validate input (Step 1 Name), then increment step or call `OnCreatePressed`.
    - `OnBackPressed`: Decrement step or go back to character list.
- [ ] **Step 4: Refine `OnClassChanged`**
    - Ensure it updates the "hidden" equipment selections while keeping aesthetics (Hair, etc.) constant.
    - Call `UpdateGenderFiltering()` whenever moving to Step 3 or changing gender in Step 2.

---

### Task 3: Unit & Regression Testing (Exhaustive Sprite Check)

**Files:**
- Create: `Darkness.Tests/Scenes/CharacterGenWizardTests.cs`

- [ ] **Step 1: Test Step Transitions**
    - Verify `SetStep(2)` hides Step 1 UI and shows Sprite Preview.
    - Verify validation prevents moving to Step 2 if Name is empty.
- [ ] **Step 2: Exhaustive Class/Gender Sprite Mapping Tests**
    - Write a theory test that iterates through all classes (Knight, Rogue, Mage, Cleric, Warrior) and genders (Male, Female).
    - For each combination, verify that `SpriteLayerCatalog.GetStitchLayers()` returns a valid set of layers where the `RootPath` for armor/weapons matches the class archetype and respects the chosen gender.
- [ ] **Step 3: Test Class Gear Automation in UI**
    - Verify that selecting "Knight" applies Plate armor to the `Character` object even if the dropdown is hidden.
- [ ] **Step 4: Run all tests**
    - Run: `dotnet test Darkness.Tests`
    - Expected: All tests PASS.

---

### Task 4: Final Validation

- [ ] **Step 1: Manual Wizard Run-through**
    - Test the flow: Name -> Gender -> Class/Aesthetics -> Save.
- [ ] **Step 2: Verify Inventory**
    - Confirm the saved character has the correct starter items in LiteDB.
