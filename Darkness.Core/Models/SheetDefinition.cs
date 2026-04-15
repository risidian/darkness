using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Darkness.Core.Models;

public class SheetDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public Dictionary<string, SheetLayer> Layers { get; set; } = new();
    public List<string> Variants { get; set; } = new();
    public List<string> Animations { get; set; } = new();
    [JsonPropertyName("preview_row")]
    public int PreviewRow { get; set; }
    [JsonPropertyName("preview_column")]
    public int PreviewColumn { get; set; }
    public bool IsFlipped { get; set; } // For OffHand
}
