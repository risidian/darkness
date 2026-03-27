using SQLite;

namespace Darkness.Core.Models
{
    public class Character
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [Indexed]
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string HairColor { get; set; } = string.Empty;
        public string HairStyle { get; set; } = string.Empty;
        public string SkinColor { get; set; } = string.Empty;
        
        // Base Stats
        public int STR { get; set; }
        public int DEX { get; set; }
        public int CON { get; set; }
        public int INT { get; set; }
        public int WIS { get; set; }
        public int CHA { get; set; }
        
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

        public int Level { get; set; }
        public int Experience { get; set; }
    }
}
