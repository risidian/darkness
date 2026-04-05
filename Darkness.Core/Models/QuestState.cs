namespace Darkness.Core.Models;

public class QuestState
{
    public int Id { get; set; }
    public int CharacterId { get; set; }
    public string ChainId { get; set; } = string.Empty;
    public string CurrentStepId { get; set; } = string.Empty;
    public string Status { get; set; } = "available";
    public Dictionary<string, string> Flags { get; set; } = new();
}
