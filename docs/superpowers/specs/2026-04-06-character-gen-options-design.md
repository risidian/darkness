# Character Generation Options Update Design

## Goal
Simplify the Character Creation user interface by hiding equipment-related appearance options that are exclusively determined by the character's chosen class. This will maintain character customization for personal physical features (such as gender, hair, face) while removing unnecessary visual clutter.

## Requirements
The following options (and their corresponding descriptive labels) will be hidden in `CharacterGenScene`:
- Legs (`LegsOption`, `LegsLabel`)
- Feet (`FeetOption`, `FeetLabel`)
- Arms (`ArmsOption`, `ArmsLabel`)
- Armor (`ArmorOption`, `ArmorLabel`)
- Weapon (`WeaponOption`, `WeaponLabel`)
- Shield (`ShieldOption`, `ShieldLabel`)

The user will still be able to see and select:
- Name (`NameEdit`, `Label`)
- Class (`ClassOption`)
- Skin Color (`SkinOption`, `SkinLabel`)
- Gender/Base (`HeadOption`, `HeadLabel`)
- Hair Style (`HairStyleOption`, `HairStyleLabel`)
- Hair Color (`HairColorOption`, `HairColorLabel`)
- Face Type (`FaceOption`, `FaceLabel`)
- Eyes (`EyesOption`, `EyesLabel`)

## Implementation Strategy
To avoid disrupting the underlying sprite generation (`_compositor`) and data population routines (`_catalog` and `GetCurrentAppearance()`), we will set the `Visible` property to `false` for the restricted options and labels during initialization (`_Ready()`) in `CharacterGenScene.cs`.

This allows `OnClassChanged()` to continue silently updating the selected indices for these background options based on class defaults, and ensures that `GetCurrentAppearance()` still resolves all required equipment parameters seamlessly.
