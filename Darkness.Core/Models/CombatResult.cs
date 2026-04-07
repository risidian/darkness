namespace Darkness.Core.Models
{
    public class CombatResult
    {
        public bool IsHit { get; set; }
        public bool IsCriticalHit { get; set; }
        public bool IsCriticalMiss { get; set; }
        public int DamageDealt { get; set; }
    }
}