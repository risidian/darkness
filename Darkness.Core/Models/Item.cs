namespace Darkness.Core.Models
{
    public class Item
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // i18n readiness (Phase B): optional localization keys. Display sites may resolve
        // these; identity/FK matching keeps using Name/Description. Null = English-only.
        public string? NameKey { get; set; }
        public string? DescriptionKey { get; set; }

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

        // Stat requirements
        public int RequiredStrength { get; set; }
        public int RequiredDexterity { get; set; }
        public int RequiredIntelligence { get; set; }
        public int RequiredLevel { get; set; }
        public string? ClassRequirement { get; set; }

        public bool CanEquip(Character character, out List<string> missingRequirements)
        {
            missingRequirements = new List<string>();
            if (character.Strength < RequiredStrength) missingRequirements.Add($"Strength {RequiredStrength}");
            if (character.Dexterity < RequiredDexterity) missingRequirements.Add($"Dexterity {RequiredDexterity}");
            if (character.Intelligence < RequiredIntelligence) missingRequirements.Add($"Intelligence {RequiredIntelligence}");
            if (character.Level < RequiredLevel) missingRequirements.Add($"Level {RequiredLevel}");
            if (!string.IsNullOrEmpty(ClassRequirement) && ClassRequirement != character.Class) missingRequirements.Add($"Class {ClassRequirement}");

            return missingRequirements.Count == 0;
        }

        public int Tier { get; set; } = 0; // 0 = normal, 1 = +1, 2 = +2, etc.
        public string? Infusion { get; set; } // Elemental/Bonus effect (e.g., "Fire", "Life Steal")
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Returns a copy of this item suitable for adding to a character's Inventory list.
        /// The clone's Id is reset to 0 — inventory items are embedded list entries, not LiteDB rows,
        /// so they don't need a stable Id, and reusing the seed Id would falsely suggest a back-pointer.
        /// Quantity is preserved; callers awarding stackable rewards should normalize Quantity = 1
        /// before adding, then rely on ConsolidateInventory to merge stacks.
        /// </summary>
        public Item Clone()
        {
            return new Item
            {
                Name = Name,
                Description = Description,
                NameKey = NameKey,
                DescriptionKey = DescriptionKey,
                Type = Type,
                Weight = Weight,
                Value = Value,
                StrengthBonus = StrengthBonus,
                DexterityBonus = DexterityBonus,
                IntelligenceBonus = IntelligenceBonus,
                DefenseBonus = DefenseBonus,
                AttackBonus = AttackBonus,
                ArmorClass = ArmorClass,
                DamageDice = DamageDice,
                EquipmentSlot = EquipmentSlot,
                RequiredStrength = RequiredStrength,
                RequiredDexterity = RequiredDexterity,
                RequiredIntelligence = RequiredIntelligence,
                RequiredLevel = RequiredLevel,
                ClassRequirement = ClassRequirement,
                Tier = Tier,
                Infusion = Infusion,
                Quantity = Quantity
            };
        }
    }
}