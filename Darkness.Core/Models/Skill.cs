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
        public string SkillType { get; set; } = "Physical"; // Physical, Magical, Defensive
        
        // Multipliers and Modifiers
        public float DamageMultiplier { get; set; } = 1.0f;
        public float ArmorPenetration { get; set; } = 0.0f; // 0.0 to 1.0
        public int AccuracyModifier { get; set; } = 0;
        public ActionType AssociatedAction { get; set; } = ActionType.Standard;
        public float BlockReduction { get; set; } = 0.0f; // Only for Defensive types
    }
}
