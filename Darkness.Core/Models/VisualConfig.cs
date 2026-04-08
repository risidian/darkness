namespace Darkness.Core.Models;

public class VisualConfig
{
    public string? BackgroundKey { get; set; } // Path to PNG artwork
    public string? GroundColor { get; set; }   // Hex fallback (e.g. "#222222")
    public string? WaterColor { get; set; }    // Hex fallback (null to hide water)
    public NpcConfig? Npc { get; set; }        // The NPC present in this beat
    public float? PlayerPositionX { get; set; } // Starting X position for the player
    public float? PlayerPositionY { get; set; } // Starting Y position for the player
}
