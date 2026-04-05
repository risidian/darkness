using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System;
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