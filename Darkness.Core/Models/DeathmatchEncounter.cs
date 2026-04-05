using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class DeathmatchEncounter
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int RequiredLevel { get; set; }
        public List<Enemy> Enemies { get; set; } = new List<Enemy>();
        public List<Item> Rewards { get; set; } = new List<Item>();
    }
}
