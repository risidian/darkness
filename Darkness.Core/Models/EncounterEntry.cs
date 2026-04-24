namespace Darkness.Core.Models;

public class EncounterEntry
{
    public int Weight { get; set; } = 100;
    public CombatData Combat { get; set; } = new();
}
