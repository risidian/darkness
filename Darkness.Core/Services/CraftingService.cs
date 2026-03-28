using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class CraftingService : ICraftingService
    {
        public Task<List<Recipe>> GetAvailableRecipesAsync()
        {
            var recipes = new List<Recipe>
            {
                new Recipe
                {
                    Id = 1,
                    Name = "Simple Dagger",
                    Result = new Item { Name = "Simple Dagger", Description = "A basic blade.", Type = "Weapon", AttackBonus = 2, Value = 10 },
                    Materials = new Dictionary<string, int> { { "Iron Ore", 2 } }
                },
                new Recipe
                {
                    Id = 2,
                    Name = "Iron Sword",
                    Result = new Item { Name = "Iron Sword", Description = "A sturdy iron blade.", Type = "Weapon", AttackBonus = 5, Value = 50 },
                    Materials = new Dictionary<string, int> { { "Iron Ore", 5 } }
                }
            };

            return Task.FromResult(recipes);
        }

        public Task<bool> CraftItemAsync(Character character, Recipe recipe)
        {
            // Simple logic for now: return true if both exist
            if (character == null || recipe == null) return Task.FromResult(false);
            
            // In a real implementation, we would check character inventory for materials
            // and remove them, then add the result item to inventory.
            return Task.FromResult(true);
        }
    }
}
