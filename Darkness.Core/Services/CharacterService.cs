using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class CharacterService : ICharacterService
    {
        private SQLiteAsyncConnection? _database;
        private readonly LocalDatabaseService _dbService;
        private readonly string _dbPath;

        public CharacterService(LocalDatabaseService dbService)
        {
            _dbService = dbService;
            _dbPath = _dbService.GetLocalFilePath("Darkness.db3");
        }

        private async Task InitializeAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath);
            await _database.CreateTableAsync<Character>();
        }

        public async Task<bool> SaveCharacterAsync(Character character)
        {
            await InitializeAsync();
            if (character.Id != 0)
            {
                int rowsUpdated = await _database!.UpdateAsync(character);
                return rowsUpdated > 0;
            }
            else
            {
                int rowsAdded = await _database!.InsertAsync(character);
                return rowsAdded > 0;
            }
        }

        public async Task<List<Character>> GetCharactersByUserIdAsync(int userId)
        {
            await InitializeAsync();
            return await _database!.Table<Character>()
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<Character?> GetCharacterByIdAsync(int characterId)
        {
            await InitializeAsync();
            return await _database!.Table<Character>()
                .Where(c => c.Id == characterId)
                .FirstOrDefaultAsync();
        }
    }
}
