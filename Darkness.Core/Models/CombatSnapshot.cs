using System.Collections.Generic;

namespace Darkness.Core.Models;

public class CombatSnapshot
{
    public Dictionary<int, int> PartyHP { get; set; } = new();
    public Dictionary<int, int> EnemyHP { get; set; } = new();
    public Dictionary<int, int> SkillCooldowns { get; set; } = new(); // SkillId -> Turns
    public int CurrentRound { get; set; }
    public int CurrentTurnIndex { get; set; }
}
