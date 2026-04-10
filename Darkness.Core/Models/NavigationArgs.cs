using Darkness.Core.Models;

namespace Darkness.Core.Models;

public abstract class NavigationArgs
{
}

public class BattleArgs : NavigationArgs
{
    public CombatData? Combat { get; set; }
    public string? QuestChainId { get; set; }
    public string? QuestStepId { get; set; }
    public float ReturnPositionX { get; set; }
    public float ReturnPositionY { get; set; }
}

public class StealthArgs : NavigationArgs
{
    public string? QuestChainId { get; set; }
    public string? QuestStepId { get; set; }
    public float ReturnPositionX { get; set; }
    public float ReturnPositionY { get; set; }
}

public class PvpArgs : NavigationArgs
{
    public Character Player1 { get; set; } = null!;
    public Character Player2 { get; set; } = null!;
}
