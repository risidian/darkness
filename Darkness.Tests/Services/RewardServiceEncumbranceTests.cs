using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Darkness.Tests.Services
{
    public class RewardServiceEncumbranceTests : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly string _dbPath;
        private readonly RewardService _rewardService;
        private readonly Mock<ISessionService> _sessionMock;

        public RewardServiceEncumbranceTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"RewardEncumbranceTests_{Guid.NewGuid()}.db");
            _db = new LiteDatabase(_dbPath, new BsonMapper());
            _sessionMock = new Mock<ISessionService>();
            _rewardService = new RewardService(_db, _sessionMock.Object);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
        }

        [Fact]
        public async Task CheckDailyRewardAsync_DoesNotAddItems_IfWeightExceedsCapacity()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                LastLogin = DateTime.Today.AddDays(-1)
            };
            _db.GetCollection<User>("users").Insert(user);

            var character = new Character
            {
                Id = 1,
                UserId = 1,
                Name = "OverburdenedHero",
                BaseStrength = 10, // Capacity = 200
                Inventory = new List<Item>
                {
                    new Item { Name = "Heavy Rock", Weight = 195, Quantity = 1, Type = "Material" }
                }
            };
            _db.GetCollection<Character>("characters").Insert(character);
            _sessionMock.Setup(s => s.CurrentCharacter).Returns(character);

            // Seed items
            var itemCol = _db.GetCollection<Item>("items");
            var rewardItem = new Item { Name = "Reward Sword", Weight = 10, Type = "Weapon" };
            itemCol.Insert(rewardItem);

            // Seed random rewards
            var randomCol = _db.GetCollection<RandomReward>("random_rewards");
            randomCol.Insert(new RandomReward { ItemName = "Reward Sword", Weight = 100 });

            // Seed calendar rewards
            var calendarCol = _db.GetCollection<CalendarReward>("login_calendar");
            var now = DateTime.Now;
            var items = new List<string>(new string[31]);
            for (int i = 0; i < 31; i++) items[i] = "Reward Sword";
            calendarCol.Insert(new CalendarReward { Month = now.Month, Items = items });

            // Act
            var awardedItems = await _rewardService.CheckDailyRewardAsync(user);

            // Assert
            Assert.Empty(awardedItems);
            Assert.Single(character.Inventory);
            Assert.Equal("Heavy Rock", character.Inventory[0].Name);

            // LastLogin must NOT be bumped — otherwise an over-capacity player loses today's
            // reward permanently. They should be able to retry tomorrow after dropping weight.
            var updatedUser = _db.GetCollection<User>("users").FindById(1);
            Assert.True(updatedUser.LastLogin < DateTime.Today);
        }

        [Fact]
        public void ProcessCombatRewards_RespectsCarryCapacity()
        {
            // Regression for missing encumbrance check on combat loot.
            var character = new Character
            {
                Id = 99,
                UserId = 99,
                Name = "OverburdenedFighter",
                BaseStrength = 10, // Capacity = 200
                Inventory = new List<Item>
                {
                    new Item { Name = "Heavy Rock", Weight = 195, Quantity = 1, Type = "Material" }
                }
            };
            _db.GetCollection<Character>("characters").Insert(character);

            var itemCol = _db.GetCollection<Item>("items");
            var heavyDrop = new Item { Name = "Greatsword", Weight = 50, Type = "Weapon" };
            itemCol.Insert(heavyDrop);

            var enemy = new Enemy
            {
                Name = "Ogre",
                FixedDrops = new List<string> { "Greatsword" }
            };

            var result = _rewardService.ProcessCombatRewards(character, new List<Enemy> { enemy });

            // The drop is too heavy → silently skipped.
            Assert.Empty(result.ItemsAwarded);
            Assert.DoesNotContain(character.Inventory, i => i.Name == "Greatsword");
        }

        [Fact]
        public void ProcessCombatRewards_AwardsCloneNotSeedReference()
        {
            // Regression: combat drops used to add the LiteDB seed reference directly,
            // causing cross-character item-state corruption.
            var character = new Character
            {
                Id = 100,
                UserId = 100,
                BaseStrength = 30, // capacity 600
                Inventory = new List<Item>()
            };
            _db.GetCollection<Character>("characters").Insert(character);

            var itemCol = _db.GetCollection<Item>("items");
            var template = new Item { Name = "Small Knife", Weight = 1, Type = "Weapon", Quantity = 1 };
            itemCol.Insert(template);
            var seed = itemCol.FindOne(i => i.Name == "Small Knife");

            var enemy = new Enemy
            {
                Name = "Goblin",
                FixedDrops = new List<string> { "Small Knife" }
            };

            var result = _rewardService.ProcessCombatRewards(character, new List<Enemy> { enemy });

            Assert.Single(result.ItemsAwarded);
            Assert.NotSame(seed, result.ItemsAwarded[0]);
            Assert.NotSame(seed, character.Inventory.First(i => i.Name == "Small Knife"));
        }

        [Fact]
        public async Task CheckDailyRewardAsync_AddsItems_IfWeightWithinCapacity()
        {
            // Arrange
            var user = new User
            {
                Id = 2,
                Username = "fituser",
                LastLogin = DateTime.Today.AddDays(-1)
            };
            _db.GetCollection<User>("users").Insert(user);

            var character = new Character
            {
                Id = 2,
                UserId = 2,
                Name = "StrongHero",
                BaseStrength = 10, // Capacity = 200
                Inventory = new List<Item>()
            };
            _db.GetCollection<Character>("characters").Insert(character);
            _sessionMock.Setup(s => s.CurrentCharacter).Returns(character);

            // Seed items
            var itemCol = _db.GetCollection<Item>("items");
            var rewardItem = new Item { Name = "Light Potion", Weight = 1, Type = "Consumable" };
            itemCol.Insert(rewardItem);

            // Seed random rewards
            var randomCol = _db.GetCollection<RandomReward>("random_rewards");
            randomCol.Insert(new RandomReward { ItemName = "Light Potion", Weight = 100 });

            // Seed calendar rewards
            var calendarCol = _db.GetCollection<CalendarReward>("login_calendar");
            var now = DateTime.Now;
            var items = new List<string>(new string[31]);
            for (int i = 0; i < 31; i++) items[i] = "Light Potion";
            calendarCol.Insert(new CalendarReward { Month = now.Month, Items = items });

            // Act
            var awardedItems = await _rewardService.CheckDailyRewardAsync(user);

            // Assert
            Assert.NotEmpty(awardedItems);
            Assert.Contains(awardedItems, i => i.Name == "Light Potion");
            Assert.Contains(character.Inventory, i => i.Name == "Light Potion");
        }
    }
}
