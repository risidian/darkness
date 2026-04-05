using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class DialogueChoice
    {
        public string Text { get; set; } = string.Empty;
        public string NextQuestId { get; set; } = string.Empty;
        public int MoralityImpact { get; set; } = 0;
    }

    public class DialogueData
    {
        public string Speaker { get; set; } = string.Empty;
        public List<string> Lines { get; set; } = new();
        public List<DialogueChoice> Choices { get; set; } = new();
    }
}
