using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface ICraftingService
    {
        Task<List<Recipe>> GetAvailableRecipesAsync();
        Task<bool> CraftItemAsync(Character character, Recipe recipe);
    }
}
