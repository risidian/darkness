using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Data;
using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class RewardService : IRewardService
    {
        private SQLiteAsyncConnection? _database;
        private readonly LocalDatabaseService _dbService;
        private readonly string _dbPath;

        public RewardService(LocalDatabaseService dbService)
        {
            _dbService = dbService;
            _dbPath = _dbService.GetLocalFilePath("Darkness.db3");
        }

        private async Task InitializeAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath);
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<Item>();
        }

        public async Task<Item?> CheckDailyRewardAsync(User user)
        {
            if (user == null) return null;

            await InitializeAsync();

            DateTime today = DateTime.Today;

            // If user has never logged in or hasn't logged in today
            if (user.LastLogin.Date < today)
            {
                // Generate a random reward
                Item reward = GenerateRandomReward();
                
                // Update user's last login
                user.LastLogin = DateTime.Now;
                await _database!.UpdateAsync(user);
                
                return reward;
            }

            return null;
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
