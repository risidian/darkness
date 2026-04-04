namespace Darkness.Core.Models;

public class StitchLayer
{
    public string RootPath { get; set; } = string.Empty;
    public string FileNameTemplate { get; set; } = "{action}.png";
    public string TintHex { get; set; } = "#FFFFFF";

    public StitchLayer(string rootPath, string template = "{action}.png", string tintHex = "#FFFFFF")
    {
        RootPath = rootPath;
        FileNameTemplate = template;
        TintHex = tintHex;
    }
}
