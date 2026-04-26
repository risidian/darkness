# Study Scene Mobile UX Design

## Purpose
The Study scene currently has usability issues on mobile devices. Elements spill off the screen, buttons are too small for touch interaction, and the layout doesn't allow players to see the impact of their attribute choices before committing them. This redesign focuses on a mobile-first, side-by-side landscape layout that provides immediate feedback on attribute changes with clear touch targets and a safety net for unsaved changes.

## UI Architecture
- **Layout Structure:** The main container will be an `HBoxContainer` splitting the screen into left and right panels.
- **Left Panel (`ScrollContainer`):**
  - Header showing the Character Name and Available Attribute Points.
  - A vertical list of the 6 core attributes (Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma).
  - Each attribute row will have an `HBoxContainer` containing:
    - A `-` button (to remove unsaved points).
    - The Attribute Name and Value.
    - A `+` button (to add points).
- **Right Panel (`ScrollContainer`):**
  - A vertical list of derived stats: Max HP, Max Mana, Armor Class, Evasion, Accuracy, Melee Damage, Magic Damage.
  - This panel updates in real-time as attributes are adjusted on the left.
- **Bottom Bar:**
  - Placed at the bottom of the screen (or bottom of the left panel) with large touch targets for "Save" and "Back".

## Mobile Affordances
- **Touch Targets:** Buttons will have an increased `custom_minimum_size`.
  - `+` and `-` buttons: `100x100`.
  - Main action buttons (Save/Back): `200x80`.
- **Scrolling:** `ScrollContainer`s are used for both the attribute list and the stats list to prevent content from spilling off the screen on devices with smaller aspect ratios.
- **Spacing:** `theme_override_constants/separation` will be increased between rows to prevent accidental mis-taps.

## Data Flow & State Management
- **Initialization:** On `_Ready()`, the scene snapshots the initial state of the character's attributes (STR, DEX, CON, INT, WIS, CHA) and the initial available Attribute Points.
- **Modification:** Pressing `+` or `-` instantly updates the `Character` object's properties and calls `RecalculateDerivedStats()`. Both the left (attributes) and right (derived stats) UI panels are updated to reflect the new state. The `-` button is only enabled if the attribute is strictly greater than its initial snapshot value.
- **Saving:** Pressing "Save" commits the changes to the database using `ICharacterService.SaveCharacter()` and updates the snapshot values to the current values.
- **Navigation Safety:** Pressing "Back" checks if there are unsaved changes (i.e., `_initialPoints != Character.AttributePoints`).
  - If changes exist, a `ConfirmationDialog` or custom popup is shown with the message: "You have unsaved changes."
  - Options: **Save** (commits and navigates back), **Discard** (restores snapshot attributes, recalculates, and navigates back), **Cancel** (closes the popup and stays on the Study scene).
  - If no changes exist, it navigates back immediately.