using System.Collections.Generic;

namespace Darkness.Core.Models;

public class SheetDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public Dictionary<string, SheetLayer> Layers { get; set; } = new();
    public List<string> Variants { get; set; } = new();
    public List<string> Animations { get; set; } = new();
    public int PreviewRow { get; set; }
    public int PreviewColumn { get; set; }
    public bool IsFlipped { get; set; } // For OffHand
}
