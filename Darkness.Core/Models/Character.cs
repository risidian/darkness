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
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }

        // Derived Stats
        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }
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
            MaxHP = Constitution * 10;
            if (MaxHP > oldMaxHP)
            {
                CurrentHP += (MaxHP - oldMaxHP);
            }
            
            Mana = Wisdom * 5;
            Stamina = Constitution * 5;
            Speed = Dexterity;
            Accuracy = 80 + Dexterity / 2;
            Evasion = Dexterity / 2;
            Defense = Constitution / 2;
            MagicDefense = Wisdom / 2;
        }

        public CharacterSnapshot ToSnapshot() => new CharacterSnapshot(
            Name, Class, CurrentHP, MaxHP, Level, Thumbnail, HairColor, HairStyle, SkinColor
        );
    }
}