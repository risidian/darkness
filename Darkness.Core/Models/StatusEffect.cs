namespace Darkness.Core.Models
{
    public class StatusEffect
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Duration { get; set; } // Turns or seconds

        public string EffectType { get; set; } = string.Empty; // E.g., Poison, Stun, Buff

        public float Magnitude { get; set; }
    }
}