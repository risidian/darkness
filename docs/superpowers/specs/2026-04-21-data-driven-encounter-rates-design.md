# Design: Data-Driven Random Encounter Rates

This document outlines the migration of hardcoded encounter rates and distances in `WorldScene.cs` to the data-driven `encounters.json` configuration.

## 1. Problem Statement
Currently, the "Shore of Camelot" and all future areas share the same hardcoded encounter rate (8%) and distance threshold (1000px). To allow for varying difficulty and "safe zones," these parameters must be moved into the area-specific configuration data.

## 2. Goals
- Move `EncounterChance` and `EncounterDistance` to `encounters.json`.
- Encapsulate encounter rolling logic within `EncounterService`.
- Simplify `WorldScene.cs` by delegating encounter decisions to the service.

## 3. Data Model Changes

### 3.1 `EncounterTable.cs`
Add new properties to represent area-wide settings:
- `EncounterChance` (int): Percentage (0-100).
- `EncounterDistance` (float): Pixels required for a roll.

```csharp
public class EncounterTable
{
    public string BackgroundKey { get; set; } = string.Empty;
    public int EncounterChance { get; set; } = 5; // Default 5%
    public float EncounterDistance { get; set; } = 1000f; // Default 1000px
    public List<EncounterEntry> Encounters { get; set; } = new();
}
```

## 4. Service Logic Changes

### 4.1 `IEncounterService.cs`
Update the interface to include the rolling method.

```csharp
public interface IEncounterService
{
    CombatData? GetRandomEncounter(string backgroundKey);
    CombatData? RollForEncounter(string backgroundKey, double distanceMoved);
}
```

### 4.2 `EncounterService.cs`
Implement `RollForEncounter` to centralize the decision logic.

```csharp
public CombatData? RollForEncounter(string backgroundKey, double distanceMoved)
{
    var table = GetTable(backgroundKey); // Internal helper
    if (table == null || distanceMoved < table.EncounterDistance) return null;

    int roll = _random.Next(1, 101);
    if (roll <= table.EncounterChance)
    {
        return GetRandomEncounter(backgroundKey);
    }
    return null;
}
```

## 5. View Logic Changes (`WorldScene.cs`)
Remove the constants and the manual roll. `WorldScene` will simply pass its accumulated distance to the service and react if a `CombatData` is returned.

```csharp
private void CheckRandomEncounter(double delta)
{
    float dist = _player.GlobalPosition.DistanceTo(_lastPlayerPosition);
    _distanceMovedSinceLastEncounter += dist;
    _lastPlayerPosition = _player.GlobalPosition;

    string? bgKey = _currentDialogueStep?.Visuals?.BackgroundKey;
    if (string.IsNullOrEmpty(bgKey)) return;

    var combat = _encounterService.RollForEncounter(bgKey, _distanceMovedSinceLastEncounter);
    if (combat != null)
    {
        _distanceMovedSinceLastEncounter = 0; // Reset only on success
        _ = StartRandomEncounter(combat);
    }
}
```

## 6. Testing Strategy
- **Unit Test**: `EncounterService_RollsCorrectly` to verify distance threshold and percentage roll logic.
- **Integration Test**: Verify `encounters.json` seeding loads the new fields correctly.
- **Manual Verification**: Walk around in `WorldScene` and verify encounters still trigger. Change `EncounterChance` to 100 in JSON to verify immediate triggers.
