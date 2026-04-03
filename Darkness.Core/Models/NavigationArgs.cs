using Darkness.Core.Models;

namespace Darkness.Core.Models;

public abstract class NavigationArgs { }

public class BattleArgs : NavigationArgs
{
    public EncounterData? Encounter { get; set; }
}

public class PvpArgs : NavigationArgs
{
    public Character Player1 { get; set; } = null!;
    public Character Player2 { get; set; } = null!;
}
