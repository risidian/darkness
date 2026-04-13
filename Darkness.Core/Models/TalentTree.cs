using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class TalentTree
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Tier { get; set; }
        public bool IsHidden { get; set; } = false;
        public Dictionary<string, int> Prerequisites { get; set; } = new();
        public List<TalentNode> Nodes { get; set; } = new();
    }
}