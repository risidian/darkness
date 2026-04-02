namespace Darkness.Core.Models
{
    public class Skill
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public int ManaCost { get; set; }
        
        public int StaminaCost { get; set; }
        
        public int BasePower { get; set; }
        
        public string SkillType { get; set; } = string.Empty; // E.g., Physical, Magical, Support
    }
}
