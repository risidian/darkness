namespace Darkness.Core.Models
{
    public class Item
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string Type { get; set; } = string.Empty; // E.g., Weapon, Armor, Consumable
        
        public int Weight { get; set; }
        
        public int Value { get; set; }
        
        // Potential stat bonuses
        public int StrengthBonus { get; set; }
        public int DexterityBonus { get; set; }
        public int IntelligenceBonus { get; set; }
        public int DefenseBonus { get; set; }
        public int AttackBonus { get; set; }
        public int ArmorClass { get; set; }
    }
}
