namespace Darkness.Core.Models
{
    public class CharacterAppearance
    {
        public string SkinColor { get; set; } = "Light";
        public string HairStyle { get; set; } = "Long";
        public string HairColor { get; set; } = "Black";
        public string Face { get; set; } = "Default";
        public string Eyes { get; set; } = "Default";
        public string Head { get; set; } = "Human Male";
        public string Feet { get; set; } = "Boots (Basic)";
        public string Arms { get; set; } = "None";
        public string Legs { get; set; } = "Slacks";
        public string ArmorType { get; set; } = "Leather";
        public string WeaponType { get; set; } = "Arming Sword (Steel)";
        public string ShieldType { get; set; } = "None";
        public string? OffHandType { get; set; } = "None";
    }
}