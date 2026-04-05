namespace Darkness.Core.Models;

public class QuestChain
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsMainStory { get; set; }
    public int SortOrder { get; set; }
    public List<string> Prerequisites { get; set; } = new();
    public List<QuestStep> Steps { get; set; } = new();
}
