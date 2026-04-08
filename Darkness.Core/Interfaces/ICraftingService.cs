using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface ICraftingService
    {
        Task<List<Recipe>> GetAvailableRecipesAsync();
        Task<bool> CraftItemAsync(Character character, Recipe recipe);
        Task<bool> UpgradeItemAsync(Character character, Item item, List<Item> materials, int gold);
        Task<bool> InfuseItemAsync(Character character, Item item, Item essence);
    }
}