using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class EncounterData
    {
        public List<Enemy> Enemies { get; set; } = new();
        public int? SurvivalTurns { get; set; }
        public string? BackgroundKey { get; set; }
        public List<Item> Rewards { get; set; } = new();
        public List<Character> AdditionalPartyMembers { get; set; } = new();
    }
}