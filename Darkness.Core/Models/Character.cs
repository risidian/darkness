using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace Darkness.Core.Models
{
    public class Character
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Class { get; set; } = "Knight";
        public string HairColor { get; set; } = "Black";
        public string HairStyle { get; set; } = "Long";
        public string SkinColor { get; set; } = "Light";
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

        // Base Stats
        public int BaseStrength { get; set; }
        public int BaseDexterity { get; set; }
        public int BaseConstitution { get; set; }
        public int BaseIntelligence { get; set; }
        public int BaseWisdom { get; set; }
        public int BaseCharisma { get; set; }

        public Dictionary<string, int> StatBonuses { get; set; } = new();
        public Dictionary<string, int> TalentStatBonuses { get; set; } = new();

        // Effective Stats (Computed)
        [BsonIgnore]
        public int Strength
        {
            get => BaseStrength + 
                   (StatBonuses.TryGetValue("Strength", out var b) ? b : 0) +
                   (TalentStatBonuses.TryGetValue("Strength", out var tb) ? tb : 0);
            set => BaseStrength = value;
        }

        [BsonIgnore]
        public int Dexterity
        {
            get => BaseDexterity + 
                   (StatBonuses.TryGetValue("Dexterity", out var b) ? b : 0) +
                   (TalentStatBonuses.TryGetValue("Dexterity", out var tb) ? tb : 0);
            set => BaseDexterity = value;
        }

        [BsonIgnore]
        public int Constitution
        {
            get => BaseConstitution + 
                   (StatBonuses.TryGetValue("Constitution", out var b) ? b : 0) +
                   (TalentStatBonuses.TryGetValue("Constitution", out var tb) ? tb : 0);
            set => BaseConstitution = value;
        }

        [BsonIgnore]
        public int Intelligence
        {
            get => BaseIntelligence + 
                   (StatBonuses.TryGetValue("Intelligence", out var b) ? b : 0) +
                   (TalentStatBonuses.TryGetValue("Intelligence", out var tb) ? tb : 0);
            set => BaseIntelligence = value;
        }

        [BsonIgnore]
        public int Wisdom
        {
            get => BaseWisdom + 
                   (StatBonuses.TryGetValue("Wisdom", out var b) ? b : 0) +
                   (TalentStatBonuses.TryGetValue("Wisdom", out var tb) ? tb : 0);
            set => BaseWisdom = value;
        }

        [BsonIgnore]
        public int Charisma
        {
            get => BaseCharisma + 
                   (StatBonuses.TryGetValue("Charisma", out var b) ? b : 0) +
                   (TalentStatBonuses.TryGetValue("Charisma", out var tb) ? tb : 0);
            set => BaseCharisma = value;
        }

        // Derived Stats
        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }
        public int Attack { get; set; }
        public int Stamina { get; set; }
        public int Mana { get; set; }
        public int Speed { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int Defense { get; set; }
        public int CriticalChance { get; set; }
        public int CriticalDamage { get; set; }
        public int CriticalDefense { get; set; }
        public int MagicDefense { get; set; }
        public int ArmorClass { get; set; }

        public int Level { get; set; }
        public int Experience { get; set; }
        public int AttributePoints { get; set; } = 5;
        public int TalentPoints { get; set; } = 0;
        public List<string> UnlockedTalentIds { get; set; } = new();
        public byte[]? Thumbnail { get; set; }
        public byte[]? FullSpriteSheet { get; set; }
        public List<Item> Inventory { get; set; } = new();
        public int Morality { get; set; } = 0;
        public int Gold { get; set; } = 0;
        public string?[] Hotbar { get; set; } = new string?[5];
        public bool IsBlocking { get; set; } = false;

        public void ConsolidateInventory()
        {
            if (Inventory == null || Inventory.Count <= 1) return;

            var consolidated = new List<Item>();
            foreach (var item in Inventory)
            {
                // Only stack Consumables and Materials
                if (item.Type == "Consumable" || item.Type == "Material")
                {
                    var existing = consolidated.FirstOrDefault(i => 
                        i.Name == item.Name && 
                        i.Type == item.Type && 
                        i.Tier == item.Tier && 
                        i.Infusion == item.Infusion);

                    if (existing != null)
                    {
                        existing.Quantity += item.Quantity;
                    }
                    else
                    {
                        consolidated.Add(item);
                    }
                }
                else
                {
                    // Equipment (Weapon, Armor, Shield) usually shouldn't stack
                    consolidated.Add(item);
                }
            }

            Inventory = consolidated;
        }

        public void RecalculateDerivedStats()
        {
            int oldMaxHP = MaxHP;
            MaxHP = Constitution * 10 + GetTotalBonus("MaxHP");
            if (MaxHP > oldMaxHP)
            {
                CurrentHP += (MaxHP - oldMaxHP);
            }
            
            Mana = Wisdom * 5 + GetTotalBonus("Mana");
            Stamina = Constitution * 5 + GetTotalBonus("Stamina");
            Speed = Dexterity + GetTotalBonus("Speed");
            Attack = Strength * 2 + GetTotalBonus("Attack");
            Accuracy = 80 + Dexterity / 2 + GetTotalBonus("Accuracy");
            Evasion = Dexterity / 2 + GetTotalBonus("Evasion");
            Defense = Constitution / 2 + GetTotalBonus("Defense");
            MagicDefense = Wisdom / 2 + GetTotalBonus("MagicDefense");
            ArmorClass = Constitution / 2 + GetTotalBonus("ArmorClass");
            CriticalChance = GetTotalBonus("CriticalChance");
            CriticalDamage = GetTotalBonus("CriticalDamage");
            CriticalDefense = GetTotalBonus("CriticalDefense");
        }

        public int GetTotalBonus(string key) =>
            (StatBonuses.TryGetValue(key, out var b) ? b : 0) +
            (TalentStatBonuses.TryGetValue(key, out var tb) ? tb : 0);

        private int GetTalentBonus(string key) => TalentStatBonuses.TryGetValue(key, out var val) ? val : 0;

        public CharacterSnapshot ToSnapshot() => new CharacterSnapshot(
            Name, Class, CurrentHP, MaxHP, Level, Thumbnail, HairColor, HairStyle, SkinColor
        );
    }
}