namespace Darkness.Core.Models
{
    public class TalentNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PointsRequired { get; set; } = 1;
        public string? PrerequisiteNodeId { get; set; }
        public TalentEffect Effect { get; set; } = new();
    }
}
