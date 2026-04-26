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
        private readonly ISessionService _session;

        public RewardService(LiteDatabase db, ISessionService session)
        {
            _db = db;
            _session = session;
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

            // Update character in database
            var charCol = _db.GetCollection<Character>("characters");
            charCol.Update(character);

            return result;
        }

        public Task<List<Item>> CheckDailyRewardAsync(User user)
        {
            if (user == null) return Task.FromResult(new List<Item>());

            return Task.Run(() =>
            {
                var awardedItems = new List<Item>();
                DateTime today = DateTime.Today;

                if (user.LastLogin.Date < today)
                {
                    var character = _session.CurrentCharacter;
                    if (character == null) return awardedItems;

                    var randomCol = _db.GetCollection<RandomReward>("random_rewards");
                    var calendarCol = _db.GetCollection<CalendarReward>("login_calendar");
                    var itemCol = _db.GetCollection<Item>("items");

                    // 1. Weighted Random Selection
                    var randomRewards = randomCol.FindAll().ToList();
                    if (randomRewards.Any())
                    {
                        int totalWeight = randomRewards.Sum(r => r.Weight);
                        if (totalWeight > 0)
                        {
                            var random = new Random();
                            int roll = random.Next(totalWeight);
                            int currentSum = 0;
                            RandomReward? selectedRandom = null;
                            foreach (var r in randomRewards)
                            {
                                currentSum += r.Weight;
                                if (roll < currentSum)
                                {
                                    selectedRandom = r;
                                    break;
                                }
                            }

                            if (selectedRandom != null)
                            {
                                var itemTemplate = itemCol.FindOne(x => x.Name == selectedRandom.ItemName);
                                if (itemTemplate != null)
                                {
                                    if (character.TotalWeight + itemTemplate.Weight <= character.CarryCapacity)
                                    {
                                        character.Inventory.Add(itemTemplate);
                                        awardedItems.Add(itemTemplate);
                                    }
                                }
                            }
                        }
                    }

                    // 2. Calendar Selection
                    var now = DateTime.Now;
                    var calendar = calendarCol.FindOne(c => c.Month == now.Month);
                    if (calendar != null && calendar.Items.Count >= now.Day)
                    {
                        string itemName = calendar.Items[now.Day - 1];
                        var itemTemplate = itemCol.FindOne(x => x.Name == itemName);
                        if (itemTemplate != null)
                        {
                            if (character.TotalWeight + itemTemplate.Weight <= character.CarryCapacity)
                            {
                                character.Inventory.Add(itemTemplate);
                                awardedItems.Add(itemTemplate);
                            }
                        }
                    }

                    // 3. Update User and Character
                    user.LastLogin = DateTime.Now;
                    _db.GetCollection<User>("users").Update(user);

                    if (awardedItems.Any())
                    {
                        character.ConsolidateInventory();
                        _db.GetCollection<Character>("characters").Update(character);
                    }
                }

                return awardedItems;
            });
        }
    }
}