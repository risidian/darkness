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
        public string DamageDice { get; set; } = "1d6";
        public string SkillType { get; set; } = "Physical"; // Physical, Magical, Defensive

        // Multipliers and Modifiers
        public float DamageMultiplier { get; set; } = 1.0f;
        public float ArmorPenetration { get; set; } = 0.0f; // 0.0 to 1.0
        public int AccuracyModifier { get; set; } = 0;
        public ActionType AssociatedAction { get; set; } = ActionType.Standard;
        public float BlockReduction { get; set; } = 0.0f; // Only for Defensive types

        // Task 1: New properties
        public int Cooldown { get; set; } = 0;
        public int CurrentCooldown { get; set; } = 0;
        public string WeaponRequirement { get; set; } = "None";
        public string? TalentRequirement { get; set; }
        public bool IsPassive { get; set; } = false;
        public bool IsOffHand { get; set; } = false;
        public bool IsAOE { get; set; } = false;
    }
}