using LiteDB;
using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class DeathmatchEncounter
    {
        public int Id { get; set; }

        /// <summary>Legacy display name — maps to Title for backwards compatibility.</summary>
        public string Name { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;

        /// <summary>If set, the character must have cleared this wave Id before this one unlocks.</summary>
        public int? UnlockRequirement { get; set; } = null;

        /// <summary>Computed at runtime by DeathmatchService — not persisted.</summary>
        [BsonIgnore]
        public bool IsUnlocked { get; set; } = false;

        // Legacy field kept for DeathmatchScene compatibility
        public int RequiredLevel { get; set; }

        public List<Enemy> Enemies { get; set; } = new List<Enemy>();
        public List<Item> Rewards { get; set; } = new List<Item>();
    }
}