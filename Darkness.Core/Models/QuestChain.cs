namespace Darkness.Core.Models;

public class QuestChain
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsMainStory { get; set; }
    public int SortOrder { get; set; }
    public string? ArcId { get; set; }   // nullable; null = un-arced (e.g. exploration_demo)
    public List<string> Prerequisites { get; set; } = new();
    public List<QuestStep> Steps { get; set; } = new();
}
