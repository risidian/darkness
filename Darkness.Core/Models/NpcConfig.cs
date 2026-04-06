namespace Darkness.Core.Models;

public class NpcConfig
{
    public string Name { get; set; } = string.Empty;
    public string? SpriteKey { get; set; }     // Path to full sheet (e.g. "bosses/Balgathor")
    public float PositionX { get; set; } = 400; 
    public float PositionY { get; set; } = 200;
    // LPC override if SpriteKey is missing
    public CharacterAppearance? Appearance { get; set; } 
}
