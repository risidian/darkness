namespace Darkness.Core.Models;

public class QuestReward
{
    public QuestRewardType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public int Amount { get; set; } = 1;
}
