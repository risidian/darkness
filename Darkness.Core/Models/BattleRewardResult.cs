using System.Collections.Generic;

namespace Darkness.Core.Models;

public class BattleRewardResult
{
    public int GoldAwarded { get; set; }
    public List<Item> ItemsAwarded { get; set; } = new();
}
