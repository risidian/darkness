using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class RewardService : IRewardService
    {
        private readonly LiteDatabase _db;

        public RewardService(LiteDatabase db)
        {
            _db = db;
        }

        public BattleRewardResult ProcessCombatRewards(Character character, List<Enemy> enemies)
        {
            var result = new BattleRewardResult();
            var random = new Random();
            var itemCol = _db.GetCollection<Item>("items");

            foreach (var enemy in enemies)
            {
                // Gold
                result.GoldAwarded += enemy.GoldReward;

                // Fixed Drops
                foreach (var itemName in enemy.FixedDrops)
                {
                    var item = itemCol.FindOne(x => x.Name == itemName);
                    if (item != null)
                    {
                        character.Inventory.Add(item);
                        result.ItemsAwarded.Add(item);
                    }
                }

                // Random Drops
                foreach (var loot in enemy.RandomDrops)
                {
                    if (random.NextDouble() <= loot.Chance)
                    {
                        var item = itemCol.FindOne(x => x.Name == loot.ItemName);
                        if (item != null)
                        {
                            character.Inventory.Add(item);
                            result.ItemsAwarded.Add(item);
                        }
                    }
                }
            }

            character.Gold += result.GoldAwarded;
            character.ConsolidateInventory();
            
            // Restore HP to full on victory as requested
            character.CurrentHP = character.MaxHP;

            // Update character in database
            var charCol = _db.GetCollection<Character>("characters");
            charCol.Update(character);

            return result;
        }

        public Task<Item?> CheckDailyRewardAsync(User user)
        {
            if (user == null) return Task.FromResult<Item?>(null);

            return Task.Run(() =>
            {
                DateTime today = DateTime.Today;

                if (user.LastLogin.Date < today)
                {
                    Item reward = GenerateRandomReward();

                    user.LastLogin = DateTime.Now;
                    var col = _db.GetCollection<User>("users");
                    col.Update(user);

                    return (Item?)reward;
                }

                return null;
            });
        }

        private Item GenerateRandomReward()
        {
            var random = new Random();
            int choice = random.Next(3);

            return choice switch
            {
                0 => new Item
                {
                    Name = "Health Potion",
                    Description = "A crimson elixir that mends flesh and bone.",
                    Type = "Consumable",
                    Value = 50
                },
                1 => new Item
                {
                    Name = "Mana Potion",
                    Description = "A swirling blue liquid that restores magical energy.",
                    Type = "Consumable",
                    Value = 50
                },
                _ => new Item
                {
                    Name = "Iron Ore",
                    Description = "Raw iron extracted from the deep earth. Used in smithing.",
                    Type = "Material",
                    Value = 25
                }
            };
        }
    }
}