namespace Darkness.Core.Models;

public class QuestStep
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? NextStepId { get; set; }
    public DialogueData? Dialogue { get; set; }
    public CombatData? Combat { get; set; }
    public LocationTrigger? Location { get; set; }
    public BranchData? Branch { get; set; }
    public VisualConfig? Visuals { get; set; }
}
