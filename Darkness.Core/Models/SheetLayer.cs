using System.Collections.Generic;

namespace Darkness.Core.Models;

public class SheetLayer
{
    public string? CustomAnimation { get; set; }
    public int ZPos { get; set; }
    public Dictionary<string, string> Paths { get; set; } = new(); // "male", "female" keys
    
    // Helper to get path for gender
    public string GetPath(string gender)
    {
        if (Paths.TryGetValue(gender.ToLower(), out var path)) return path;
        if (Paths.TryGetValue("male", out path)) return path;
        return string.Empty;
    }
}
