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
                    Result = new Item
                    {
                        Name = "Simple Dagger", Description = "A basic blade.", Type = "Weapon", AttackBonus = 2,
                        Value = 10
                    },
                    Materials = new Dictionary<string, int> { { "Iron Ore", 2 } }
                },
                new Recipe
                {
                    Id = 2,
                    Name = "Iron Sword",
                    Result = new Item
                    {
                        Name = "Iron Sword", Description = "A sturdy iron blade.", Type = "Weapon", AttackBonus = 5,
                        Value = 50
                    },
                    Materials = new Dictionary<string, int> { { "Iron Ore", 5 } }
                }
            };

            return Task.FromResult(recipes);
        }

        public async Task<bool> CraftItemAsync(Character character, Recipe recipe)
        {
            if (character == null || recipe == null) return false;

            // Check materials
            foreach (var material in recipe.Materials)
            {
                var count = character.Inventory.Count(i => i.Name == material.Key);
                if (count < material.Value) return false;
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
            character.Inventory.Add(new Item
            {
                Name = recipe.Result.Name,
                Description = recipe.Result.Description,
                Type = recipe.Result.Type,
                AttackBonus = recipe.Result.AttackBonus,
                DefenseBonus = recipe.Result.DefenseBonus,
                Value = recipe.Result.Value,
                DamageDice = recipe.Result.DamageDice,
                EquipmentSlot = recipe.Result.EquipmentSlot,
                Tier = 0
            });

            return true;
        }

        public async Task<bool> UpgradeItemAsync(Character character, Item item, List<Item> materials, int gold)
        {
            if (character == null || item == null) return false;
            if (character.Gold < gold) return false;

            // TODO: Check materials if any are required for the upgrade

            character.Gold -= gold;
            item.Tier++;

            // Increase stats based on type
            if (item.Type == "Weapon")
            {
                item.AttackBonus += 2 * item.Tier;
            }
            else if (item.Type == "Armor")
            {
                item.DefenseBonus += 2 * item.Tier;
            }

            return true;
        }

        public async Task<bool> InfuseItemAsync(Character character, Item item, Item essence)
        {
            if (character == null || item == null || essence == null) return false;
            if (!character.Inventory.Contains(essence)) return false;

            // Infusion is derived from the essence name
            item.Infusion = essence.Name.Replace(" Essence", "");
            
            // Remove essence from inventory
            character.Inventory.Remove(essence);

            return true;
        }
    }
}