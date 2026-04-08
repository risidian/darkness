using Xunit;
using Darkness.Core.Models;
using Darkness.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Darkness.Tests.Services
{
    public class CraftingServiceTests
    {
        [Fact]
        public async Task CraftItem_Success_With_Materials()
        {
            var service = new CraftingService();
            var character = new Character 
            { 
                Inventory = new List<Item> 
                { 
                    new Item { Name = "Iron Ore" }, 
                    new Item { Name = "Iron Ore" } 
                } 
            };
            var recipe = new Recipe 
            { 
                Name = "Simple Dagger",
                Materials = new Dictionary<string, int> { { "Iron Ore", 2 } },
                Result = new Item { Name = "Simple Dagger", Type = "Weapon" }
            };

            var result = await service.CraftItemAsync(character, recipe);
            
            Assert.True(result);
            Assert.Contains(character.Inventory, i => i.Name == "Simple Dagger");
            Assert.DoesNotContain(character.Inventory, i => i.Name == "Iron Ore");
        }

        [Fact]
        public async Task CraftItem_Fails_If_Insufficient_Materials()
        {
            var service = new CraftingService();
            var character = new Character 
            { 
                Inventory = new List<Item> { new Item { Name = "Iron Ore" } } 
            };
            var recipe = new Recipe 
            { 
                Name = "Simple Dagger",
                Materials = new Dictionary<string, int> { { "Iron Ore", 2 } },
                Result = new Item { Name = "Simple Dagger" }
            };

            var result = await service.CraftItemAsync(character, recipe);
            Assert.False(result);
            Assert.DoesNotContain(character.Inventory, i => i.Name == "Simple Dagger");
        }

        [Fact]
        public async Task UpgradeItem_Increases_Tier_And_Stats()
        {
            var service = new CraftingService();
            var character = new Character { Gold = 1000 };
            var item = new Item { Name = "Steel Sword", AttackBonus = 10, Tier = 0, Type = "Weapon" };
            
            var result = await service.UpgradeItemAsync(character, item, new List<Item>(), 500);
            
            Assert.True(result);
            Assert.Equal(1, item.Tier);
            Assert.Equal("Steel Sword +1", item.Name);
            Assert.True(item.AttackBonus > 10);
            Assert.Equal(500, character.Gold);
        }

        [Fact]
        public async Task UpgradeItem_Handles_Multiple_Upgrades_Correctly()
        {
            var service = new CraftingService();
            var character = new Character { Gold = 2000 };
            var item = new Item { Name = "Steel Sword", AttackBonus = 10, Tier = 0, Type = "Weapon" };
            
            await service.UpgradeItemAsync(character, item, new List<Item>(), 500);
            Assert.Equal("Steel Sword +1", item.Name);
            
            await service.UpgradeItemAsync(character, item, new List<Item>(), 1000);
            Assert.Equal(2, item.Tier);
            Assert.Equal("Steel Sword +2", item.Name);
        }

        [Fact]
        public async Task UpgradeItem_Fails_Insufficient_Gold()
        {
            var service = new CraftingService();
            var character = new Character { Gold = 100 };
            var item = new Item { Name = "Steel Sword", AttackBonus = 10, Tier = 0, Type = "Weapon" };
            
            var result = await service.UpgradeItemAsync(character, item, new List<Item>(), 500);
            
            Assert.False(result);
            Assert.Equal(0, item.Tier);
            Assert.Equal(100, character.Gold);
        }

        [Fact]
        public async Task InfuseItem_Sets_Infusion_And_Consumes_Essence()
        {
            var service = new CraftingService();
            var character = new Character 
            { 
                Inventory = new List<Item> { new Item { Name = "Fire Essence" } } 
            };
            var item = new Item { Name = "Steel Sword" };
            var essence = character.Inventory[0];

            var result = await service.InfuseItemAsync(character, item, essence);
            
            Assert.True(result);
            Assert.Equal("Fire", item.Infusion);
            Assert.Empty(character.Inventory);
        }
    }
}
