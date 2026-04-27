namespace Darkness.Core.Models;

public class QuestReward
{
    public string Type { get; set; } = string.Empty; // "Item", "Experience", "Gold", "WorldFlag", "AttributePoint"
    public string Value { get; set; } = string.Empty;
    public int Amount { get; set; } = 1;
}
