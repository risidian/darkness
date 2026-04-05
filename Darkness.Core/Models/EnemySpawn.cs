namespace Darkness.Core.Models;

public class EnemySpawn
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Accuracy { get; set; }
    public int Evasion { get; set; }
    public string SpriteKey { get; set; } = "hound";
    public bool IsInvincible { get; set; }
    public int MoralityImpact { get; set; }
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
}
