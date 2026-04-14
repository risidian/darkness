using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Darkness.Tests.Services
{
    public class RewardServiceTests : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly string _dbPath;
        private readonly RewardService _rewardService;

        public RewardServiceTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"RewardServiceTests_{Guid.NewGuid()}.db");
            _db = new LiteDatabase(_dbPath, new BsonMapper());

            var characters = _db.GetCollection<Character>("characters");
            var items = _db.GetCollection<Item>("items");

            items.Insert(new Item { Id = 1, Name = "Health Potion", Type = "Consumable", Value = 50 });
            items.Insert(new Item { Id = 2, Name = "Gold Coin", Type = "Currency", Value = 1 });

            _rewardService = new RewardService(_db);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_dbPath); } catch { }
        }

        [Fact]
        public void ProcessCombatRewards_DoesNotRestoreHP()
        {
            // Arrange
            var character = new Character
            {
                Id = 1,
                Name = "TestChar",
                MaxHP = 100,
                CurrentHP = 10, // Damaged character
                Gold = 0,
                Inventory = new List<Item>()
            };

            var charCol = _db.GetCollection<Character>("characters");
            charCol.Insert(character);

            var enemies = new List<Enemy>
            {
                new Enemy { Name = "Goblin", GoldReward = 50, ExperienceReward = 20 }
            };

            // Act
            var result = _rewardService.ProcessCombatRewards(character, enemies);

            // Assert
            Assert.Equal(10, character.CurrentHP); // HP should NOT be restored on victory
            Assert.Equal(50, result.GoldAwarded);
            Assert.Equal(50, character.Gold);
        }
    }
}
