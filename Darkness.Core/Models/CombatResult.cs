namespace Darkness.Core.Models
{
    public class CombatResult
    {
        public bool IsHit { get; set; }
        public bool IsCriticalHit { get; set; }
        public bool IsCriticalMiss { get; set; }
        public int DamageDealt { get; set; }
        //these fields are for debugging the game engine
        public int D20Roll { get; set; }
        public int TargetAC { get; set; }
        public int AttackModifier { get; set; }
        public float? DamageMultiplier { get; set; }
        public string? DamageDice { get; set; }
        public int DamageRoll { get; set; }
    }
}