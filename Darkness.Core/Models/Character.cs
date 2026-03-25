using SQLite;

namespace Darkness.Core.Models
{
    public class Character
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public int Health { get; set; }
        
        public int Armour { get; set; }
        
        public int Attack { get; set; }
        
        public int Speed { get; set; }
    }
}
