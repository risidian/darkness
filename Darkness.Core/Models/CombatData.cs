namespace Darkness.Core.Models;

public class CombatData
{
    public List<EnemySpawn> Enemies { get; set; } = new();
    public string? BackgroundKey { get; set; }
    public int? SurvivalTurns { get; set; }
    public List<RewardData>? Rewards { get; set; }
}
