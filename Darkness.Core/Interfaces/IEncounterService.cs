using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IEncounterService
{
    CombatData? GetRandomEncounter(string backgroundKey);
    CombatData? RollForEncounter(string backgroundKey, double distanceMoved);
}
