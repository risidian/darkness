# Talent Tree Redesign Spec

## 1. Overview
The Talent Tree system provides a data-driven framework for character progression. This redesign aims to implement a "World of Warcraft: WotLK" style 3-column grid layout with class-specific filtering, automatic node positioning, and mutually exclusive hidden trees.

## 2. Core Mechanics

### 2.1 Class Filtering
- `TalentTree` model will include a `string? RequiredClass` property.
- `TalentService.GetAvailableTrees` will filter trees based on `character.Class == tree.RequiredClass`.

### 2.2 Hidden & Exclusive Trees
- **Hidden Trees:** Trees with `IsHidden: true` will remain invisible until all `Prerequisites` (Level, Stats, or Points Spent) are met.
- **Exclusive Groups:** `TalentTree` will include a `string? ExclusiveGroupId`. If a player spends points in a tree within an exclusivity group, all other trees in that same group become permanently unavailable to that character.

### 2.3 Automatic Grid Layout
The UI will dynamically calculate node positions using a 3-column grid (Columns 0, 1, 2).
- **Row:** Calculated by prerequisite depth (Root = Row 0, Child = Row 1, etc.).
- **Column:**
    - Root nodes are centered in Column 1.
    - Child nodes inherit their parent's column to create straight vertical lines.
    - **Branching:** If a node has multiple children, they occupy available columns (0, 2) around the parent's column (1).
    - **Convergence:** If a node has multiple prerequisites, it is forced to Column 1.
- **Lines:** `Line2D` nodes will connect parent nodes to their children.

## 3. Data Model Updates

### 3.1 `TalentTree.cs`
- `string? RequiredClass`: Class restriction for the tree.
- `string? ExclusiveGroupId`: Groups multiple trees that are mutually exclusive.
- `bool IsHidden`: If true, tree is hidden until prerequisites are met.

### 3.2 `TalentNode.cs`
- `int Row { get; set; }` (Runtime only): Calculated position.
- `int Column { get; set; }` (Runtime only): Calculated position.

## 4. UI Components

### 4.1 `TalentTreeScene.tscn`
- **TabContainer:** Switches between available trees (e.g., "Knight", "Paladin").
- **Grid Container (Custom):** A `Control` node that acts as a canvas for the 3-column layout.
- **TalentNodeBox:** A custom UI component for each talent, showing the icon/name and point status.
- **Lines:** `Line2D` nodes drawn between centers of `TalentNodeBox` components.

## 5. Implementation Strategy

### 5.1 TalentService Enhancements
- Update `IsTreeAvailable` to handle `RequiredClass` and `ExclusiveGroupId`.
- Update `GetAvailableTrees` to include hidden trees once unlocked.

### 5.2 Layout Engine
- Create a `TalentLayoutService` (or helper) to resolve `Row` and `Column` for a list of `TalentNode`s before rendering.

### 5.3 UI Refresh
- Replace the `GridContainer` in `TalentTreeScene` with a custom `Control` that uses the calculated coordinates to position `TalentNodeBox` instances.
- Implement line drawing for prerequisites.
