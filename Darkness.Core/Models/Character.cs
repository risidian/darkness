namespace Darkness.Core.Models
{
    public class Character
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string HairColor { get; set; } = string.Empty;
        public string HairStyle { get; set; } = string.Empty;
        public string SkinColor { get; set; } = string.Empty;
        public string Face { get; set; } = "Default";
        public string Eyes { get; set; } = "Default";
        public string Head { get; set; } = "Human Male";
        public string Feet { get; set; } = "Boots (Basic)";
        public string Arms { get; set; } = "None";
        public string Legs { get; set; } = "Slacks";
        public string ArmorType { get; set; } = "Leather";
        public string WeaponType { get; set; } = "Arming Sword (Steel)";
        public string ShieldType { get; set; } = "None";

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
        public int MagicDefense { get; set; }
        public int ArmorClass { get; set; }

        public int Level { get; set; }
        public int Experience { get; set; }
        public int AttributePoints { get; set; } = 5;
        public byte[]? Thumbnail { get; set; }
        public byte[]? FullSpriteSheet { get; set; }
        public List<Item> Inventory { get; set; } = new();
        public List<string> CompletedQuestIds { get; set; } = new();
        public int Morality { get; set; } = 0;
        public bool IsBlocking { get; set; } = false;

        public CharacterSnapshot ToSnapshot() => new CharacterSnapshot(
            Name, Class, CurrentHP, MaxHP, Level, Thumbnail, HairColor, HairStyle, SkinColor
        );
    }
}