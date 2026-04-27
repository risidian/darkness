namespace Darkness.Core.Models;

public class WorldFlag
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty; // Usually "true" or "false"
}
