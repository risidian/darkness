namespace Darkness.Core.Models;

public class CombatData
{
    public List<EnemySpawn> Enemies { get; set; } = new();

    /// <summary>
    /// Player-side named allies (e.g. Darius, Tywin) fighting alongside the PC.
    /// Reuses the <see cref="EnemySpawn"/> shape (Name, SpriteKey, HP, Attack, Defense, Speed, ...)
    /// so quest JSON can declare party allies. Empty by default (behavior-neutral).
    /// </summary>
    public List<EnemySpawn> Allies { get; set; } = new();

    public string? BackgroundKey { get; set; }
    public int? SurvivalTurns { get; set; }
    public List<RewardData>? Rewards { get; set; }
}
