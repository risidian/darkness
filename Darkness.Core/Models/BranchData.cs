namespace Darkness.Core.Models;

public class BranchData
{
    public List<BranchOption> Options { get; set; } = new();
}

public class BranchOption
{
    public string Text { get; set; } = string.Empty;
    public string NextStepId { get; set; } = string.Empty;
    public int MoralityImpact { get; set; }
    public List<BranchCondition>? Conditions { get; set; }
}
