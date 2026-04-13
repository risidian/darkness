using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Darkness.Tests.Logic;

public class BattleRewardTests
{
    [Fact]
    public void ProcessCombatRewards_SumsGoldAndProcessesLoot()
    {
        // Arrange
        // We need a dummy LiteDatabase for RewardService constructor
        var db = new LiteDB.LiteDatabase(new System.IO.MemoryStream());
        
        // Seed items
        var itemCol = db.GetCollection<Item>("items");
        itemCol.Insert(new Item { Name = "Common Herb", Value = 5 });
        itemCol.Insert(new Item { Name = "Iron Scrap", Value = 10 });
        itemCol.Insert(new Item { Name = "Silver Ring", Value = 100 });

        var rewardService = new RewardService(db);
        var character = new Character { Name = "Hero", Gold = 100 };
        
        // Seed character
        var charCol = db.GetCollection<Character>("characters");
        charCol.Insert(character);

        var enemies = new List<Enemy>
        {
            new Enemy 
            { 
                GoldReward = 50, 
                FixedDrops = new List<string> { "Common Herb" },
                RandomDrops = new List<LootEntry> 
                { 
                    new LootEntry { ItemName = "Iron Scrap", Chance = 1.0f } 
                }
            },
            new Enemy 
            { 
                GoldReward = 75,
                RandomDrops = new List<LootEntry> 
                { 
                    new LootEntry { ItemName = "Silver Ring", Chance = 0.0f } 
                }
            }
        };

        // Act
        var result = rewardService.ProcessCombatRewards(character, enemies);

        // Assert
        Assert.Equal(125, result.GoldAwarded);
        Assert.Equal(225, character.Gold);
        Assert.Contains(character.Inventory, i => i.Name == "Common Herb");
        Assert.Contains(character.Inventory, i => i.Name == "Iron Scrap");
        Assert.DoesNotContain(character.Inventory, i => i.Name == "Silver Ring");
        
        // Verify DB update
        var updatedChar = charCol.FindById(character.Id);
        Assert.Equal(225, updatedChar.Gold);
    }
}
