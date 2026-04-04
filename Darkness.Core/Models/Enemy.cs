namespace Darkness.Core.Models
{
    public class Enemy
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public int Level { get; set; }
        
        // Base Stats
        public int STR { get; set; }
        public int DEX { get; set; }
        public int CON { get; set; }
        public int INT { get; set; }
        public int WIS { get; set; }
        public int CHA { get; set; }
        
        // Derived Stats
        public int MaxHP { get; set; }
        public int CurrentHP { get; set; }
        public int Stamina { get; set; }
        public int Mana { get; set; }
        
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int MagicDefense { get; set; }
        
        public int ExperienceReward { get; set; }
        public int GoldReward { get; set; }

        public bool IsInvincible { get; set; } = false;
        public string SpriteKey { get; set; } = "knight";
        public int MoralityImpact { get; set; } = 0;
        public bool IsBlocking { get; set; } = false;
    }
}
