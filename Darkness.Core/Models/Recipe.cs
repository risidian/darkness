using System.Collections.Generic;

namespace Darkness.Core.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Item Result { get; set; } = new();
        public Dictionary<string, int> Materials { get; set; } = new(); // ItemName -> Quantity
    }
}