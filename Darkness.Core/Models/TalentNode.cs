using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class TalentNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PointsRequired { get; set; } = 1;
        public int Row { get; set; }
        public int Column { get; set; }
        public List<string> PrerequisiteNodeIds { get; set; } = new();
        public bool IsPassive { get; set; } = false;
        public string? IconPath { get; set; }
        public TalentEffect Effect { get; set; } = new();
    }
}
