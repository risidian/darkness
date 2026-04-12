namespace Darkness.Core.Models;

public class StitchLayer
{
    public string RootPath { get; set; } = string.Empty;
    public string FileNameTemplate { get; set; } = "{action}.png";
    public string TintHex { get; set; } = "#FFFFFF";
    public bool IsFlipped { get; set; } = false;

    public StitchLayer(string rootPath, string template = "{action}.png", string tintHex = "#FFFFFF", bool isFlipped = false)
    {
        RootPath = rootPath;
        FileNameTemplate = template;
        TintHex = tintHex;
        IsFlipped = isFlipped;
    }
}