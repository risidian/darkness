namespace Darkness.Core.Models;

public class ClassDefault
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ArmorType { get; set; } = "None";
    public string WeaponType { get; set; } = "None";
    public string ShieldType { get; set; } = "None";
    public string? OffHandType { get; set; } = "None";
    public string Head { get; set; } = "Human Male";
    public string Face { get; set; } = "Default";
    public string Feet { get; set; } = "None";
    public string Arms { get; set; } = "None";
    public string Legs { get; set; } = "None";
}
