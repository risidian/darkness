using System;
using System.Collections.Generic;

namespace Darkness.WebAPI.Models
{
    public partial class Characters
    {
        public int CharacterId { get; set; }
        public required string CharacterName { get; set; }
        public int CharacterLevel { get; set; }
        public int CharacterXp { get; set; }
        public required string PlayerGuid { get; set; }
        public int Health { get; set; }
        public required string Class { get; set; }
        public required string PremierClass { get; set; }
        public int Speed { get; set; }
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Shield { get; set; }
        public int Armor { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public DateTime? Creation { get; set; }
    }
}
