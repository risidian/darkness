namespace Darkness.Core.Models
{
    public class SpriteLayerDefinition
    {
        public string ResourcePath { get; set; } = string.Empty;
        public int ZOrder { get; set; }

        public SpriteLayerDefinition(string resourcePath, int zOrder)
        {
            ResourcePath = resourcePath;
            ZOrder = zOrder;
        }
    }
}
