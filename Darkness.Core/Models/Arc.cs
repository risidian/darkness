namespace Darkness.Core.Models;

public class Arc
{
    public string Id { get; set; } = string.Empty;          // "arc_1"
    public string TitleKey { get; set; } = string.Empty;    // loc key -> "Darkness: Revenge"
    public int Order { get; set; }                           // 1..10, arc sequence
    public List<string> Prerequisites { get; set; } = new(); // arc ids (DAG)
    public List<string> MainChainIds { get; set; } = new();  // critical-path beats, in order
    public List<string> SideChainIds { get; set; } = new();  // optional content
}
