using SQLite;

namespace Darkness.Core.Models
{
    public class Level
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        public int Value { get; set; }
        
        public int ExperienceRequired { get; set; }
    }
}
