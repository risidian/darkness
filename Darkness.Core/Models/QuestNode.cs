using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class QuestNode
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsMainStory { get; set; }
        public List<string> Prerequisites { get; set; } = new();
        public EncounterData? Encounter { get; set; }
        public string? DialogueKey { get; set; }
        public string? Location { get; set; }
        public DialogueData? Dialogue { get; set; }
        public int? RequiredMoralityMin { get; set; }
        public int? RequiredMoralityMax { get; set; }
    }
}
