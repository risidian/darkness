using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IEncounterService
{
    /// <summary>
    /// Roll for encounter based on distance traveled. Returns combat data if triggered,
    /// null otherwise. Sets didRoll to true if the distance threshold was met (regardless
    /// of whether the encounter actually triggered), so callers can reset their distance counter.
    /// </summary>
    CombatData? RollForEncounter(string backgroundKey, double distanceMoved, out bool didRoll);
}
