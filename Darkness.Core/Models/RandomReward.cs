using LiteDB;

namespace Darkness.Core.Models;

public class RandomReward
{
    [BsonId]
    public string ItemName { get; set; } = string.Empty;
    public int Weight { get; set; }
}
