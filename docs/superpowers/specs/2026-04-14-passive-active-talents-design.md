# Design Spec: Passive vs. Active Talent Distinction

## Overview
This feature introduces a visual distinction between active and passive talents and updates the `TalentNode` model to support this. Active talents will be equipable, while passive talents will be automatically applied.

## 1. Data Model Changes
*   **Modify:** `Darkness.Core/Models/TalentNode.cs`
    *   Add `bool IsPassive { get; set; }`.

## 2. JSON Migration
*   **Modify:** `Darkness.Godot/assets/data/talent-trees.json`
    *   Add `"IsPassive": true/false` to every node entry.
    *   Passive talents (like `k_toughness`, `k_strength`) will have `true`.
    *   Active skills will have `false`.

## 3. UI Implementation
*   **TalentTreeScene:**
    *   Update `TalentNodeBox` to render a different background color based on `IsPassive` status (e.g., Passive: Blue/Grey, Active: Orange/Purple).
*   **Skills Menu:**
    *   Filter the "Available Skills" list to ONLY show nodes where `IsPassive == false`. Passive talents should not appear here as they are not equipable to the hotbar.

## 4. Logic Updates
*   **TalentService.cs:**
    *   When `ApplyTalentPassives` runs, it will process all unlocked talents.
    *   Ensure that only `IsPassive == true` talents are treated as stat-boosters (though existing logic already uses the `Effect` property, we can keep this for safety).

---

**Next Steps:**
1.  **Write design doc** to `docs/superpowers/specs/2026-04-14-passive-active-talents.md`.
2.  **Commit design doc.**
3.  **Wait for approval.**

Does this design look right to you? If approved, I will proceed to create an implementation plan.
