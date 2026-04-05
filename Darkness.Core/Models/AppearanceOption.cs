namespace Darkness.Core.Models;

public class AppearanceOption
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AssetPath { get; set; } = string.Empty;
    public string FileNameTemplate { get; set; } = "{action}.png";
    public string TintHex { get; set; } = "#FFFFFF";
    public int ZOrder { get; set; }
    public string Gender { get; set; } = "universal";
    public string? FallbackGender { get; set; }
}
