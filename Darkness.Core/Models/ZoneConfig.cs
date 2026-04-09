namespace Darkness.Core.Models;

public class ZoneConfig
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string Type { get; set; } = "Block"; // "Block", "Text", "Trigger"
    public string? ActionId { get; set; }
    public string? Message { get; set; }
}
