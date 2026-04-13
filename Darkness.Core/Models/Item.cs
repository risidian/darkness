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
        public string DamageDice { get; set; } = "1d4"; // Default fallback
        public string? EquipmentSlot { get; set; }
        public int? EquipmentSpriteId { get; set; }

        // Stat requirements
        public int RequiredStrength { get; set; }
        public int RequiredDexterity { get; set; }
        public int RequiredIntelligence { get; set; }
        public int RequiredLevel { get; set; }

        public bool CanEquip(Character character, out List<string> missingRequirements)
        {
            missingRequirements = new List<string>();
            if (character.Strength < RequiredStrength) missingRequirements.Add($"Strength {RequiredStrength}");
            if (character.Dexterity < RequiredDexterity) missingRequirements.Add($"Dexterity {RequiredDexterity}");
            if (character.Intelligence < RequiredIntelligence) missingRequirements.Add($"Intelligence {RequiredIntelligence}");
            if (character.Level < RequiredLevel) missingRequirements.Add($"Level {RequiredLevel}");

            return missingRequirements.Count == 0;
        }

        public int Tier { get; set; } = 0; // 0 = normal, 1 = +1, 2 = +2, etc.
        public string? Infusion { get; set; } // Elemental/Bonus effect (e.g., "Fire", "Life Steal")
        public int Quantity { get; set; } = 1;
    }
}