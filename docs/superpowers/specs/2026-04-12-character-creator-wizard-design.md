# Character Creator Wizard Design

**Date:** 2026-04-12
**Status:** Approved

## 1. Overview
The character creation process will be transformed from a single-page list into a 3-step wizard to improve user experience and focus. Granular equipment selection will be automated based on class, while visual aesthetics remain customizable.

## 2. Wizard Steps

### Step 1: Name Your Character
- **Controls**: `NameEdit`.
- **Visibility**: Sprite Preview is hidden.
- **Navigation**: NEXT button (validates that name is not empty).

### Step 2: Choose Your Origin
- **Controls**: `HeadOption` (renamed visually to Gender/Base).
- **Visibility**: Sprite Preview is shown (base body only).
- **Navigation**: BACK, NEXT.

### Step 3: Class & Aesthetics
- **Controls**: 
    - `ClassOption` (Knight, Mage, Rogue, Cleric, Warrior).
    - Aesthetic Customization: `SkinOption`, `FaceOption`, `EyesOption`, `HairStyleOption`, `HairColorOption`.
- **Hidden Logic**: Equipment dropdowns (`Armor`, `Weapon`, `Shield`, `OffHand`, `Legs`, `Feet`, `Arms`) are hidden from the user but updated automatically via `OnClassChanged`.
- **Gender Consistency**: The gender selected in Step 2 is strictly respected. Switching classes in Step 3 will automatically apply the gender-appropriate variant of that class's starter gear.
- **Navigation**: BACK, FINISH.

## 3. Technical Implementation

### UI Structure (`CharacterGenScene.tscn`)
Existing controls will be grouped into three `VBoxContainer` nodes within the `ControlsArea`:
1. `Step1Container` (Name)
2. `Step2Container` (Gender)
3. `Step3Container` (Class + Visuals)

### Logic (`CharacterGenScene.cs`)
- **Step Management**: A `_currentStep` variable will track progress. `SetStep(int step)` will toggle visibility of containers and the Sprite Preview.
- **Filtered Customization**: `UpdateGenderFiltering()` will be called when transitioning to Step 3 to ensure all aesthetic dropdowns (Face, Hair) are already filtered for the chosen gender.
- **Class Defaults**: `OnClassChanged` will update the hidden equipment variables and refresh the preview.

## 4. Success Criteria
- [ ] User cannot proceed past Step 1 without a name.
- [ ] Sprite preview accurately reflects gender choice in Step 2 when customized in Step 3.
- [ ] Changing class in Step 3 swaps armor but retains selected gender and aesthetic choices (Skin, Hair, etc.).
- [ ] Final character is saved with the correct starter gear for their class.
