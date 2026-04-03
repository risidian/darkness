using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class DialogueData
    {
        public string Speaker { get; set; } = string.Empty;
        public List<string> Lines { get; set; } = new();
    }
}
