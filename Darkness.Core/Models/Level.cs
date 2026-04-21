using LiteDB;

namespace Darkness.Core.Models
{
    public class Level
    {
        [BsonId]
        public int Value { get; set; }

        public int ExperienceRequired { get; set; }
    }
}