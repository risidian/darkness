namespace Darkness.Core.Models;

public class EquipmentSprite
{
    public int Id { get; set; }
    public string Slot { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AssetPath { get; set; } = string.Empty;
    public string FileNameTemplate { get; set; } = "{action}.png";
    public int ZOrder { get; set; }
    public string Gender { get; set; } = "universal";
    public string? FallbackGender { get; set; }
    public string TintHex { get; set; } = "#FFFFFF";

    // Stat requirements
    public int RequiredStrength { get; set; }
    public int RequiredDexterity { get; set; }
    public int RequiredIntelligence { get; set; }
    public int RequiredLevel { get; set; }
}
