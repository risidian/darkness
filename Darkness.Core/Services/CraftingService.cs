using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class CraftingService : ICraftingService
    {
        private readonly ILiteDatabase _db;

        public CraftingService(ILiteDatabase db) { _db = db; }

        public Task<List<Recipe>> GetAvailableRecipesAsync()
        {
            var col = _db.GetCollection<Recipe>("recipes");
            return Task.FromResult(col.FindAll().ToList());
        }

        public Task<bool> CraftItemAsync(Character character, Recipe recipe)
        {
            if (character == null || recipe == null) return Task.FromResult(false);

            // Check materials
            foreach (var material in recipe.Materials)
            {
                var count = character.Inventory.Count(i => i.Name == material.Key);
                if (count < material.Value) return Task.FromResult(false);
            }

            // Consume materials
            foreach (var material in recipe.Materials)
            {
                for (int i = 0; i < material.Value; i++)
                {
                    var itemToRemove = character.Inventory.First(item => item.Name == material.Key);
                    character.Inventory.Remove(itemToRemove);
                }
            }

            // Add result
            var resultJson = System.Text.Json.JsonSerializer.Serialize(recipe.Result);
            var newItem = System.Text.Json.JsonSerializer.Deserialize<Item>(resultJson);
            
            if (newItem != null)
            {
                newItem.Tier = 0;
                newItem.Quantity = 1;
                character.Inventory.Add(newItem);
            }

            return Task.FromResult(true);
        }

        public Task<bool> UpgradeItemAsync(Character character, Item item, List<Item> materials, int gold)
        {
            if (character == null || item == null) return Task.FromResult(false);
            if (item.Type != "Weapon" && item.Type != "Armor") return Task.FromResult(false);
            if (character.Gold < gold) return Task.FromResult(false);

            // TODO: Check materials if any are required for the upgrade

            character.Gold -= gold;
            item.Tier++;

            // Update Name with suffix (e.g., "Steel Sword +1")
            // First remove any existing suffix
            var baseName = item.Name;
            if (baseName.Contains(" +"))
            {
                baseName = baseName.Substring(0, baseName.LastIndexOf(" +"));
            }
            item.Name = $"{baseName} +{item.Tier}";

            // Increase stats based on type
            if (item.Type == "Weapon")
            {
                item.AttackBonus += 2 * item.Tier;
            }
            else if (item.Type == "Armor")
            {
                item.DefenseBonus += 2 * item.Tier;
            }

            return Task.FromResult(true);
        }

        public Task<bool> InfuseItemAsync(Character character, Item item, Item essence)
        {
            if (character == null || item == null || essence == null) return Task.FromResult(false);
            if (!character.Inventory.Contains(essence)) return Task.FromResult(false);

            // Infusion is derived from the essence name
            item.Infusion = essence.Name.Replace(" Essence", "");
            
            // Remove essence from inventory
            character.Inventory.Remove(essence);

            return Task.FromResult(true);
        }
    }
}