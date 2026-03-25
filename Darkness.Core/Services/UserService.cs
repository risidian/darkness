using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class UserService : IUserService
    {
        private SQLiteAsyncConnection? _database;
        private readonly LocalDatabaseService _dbService;
        private readonly string _dbPath;

        public UserService(LocalDatabaseService dbService)
        {
            _dbService = dbService;
            _dbPath = _dbService.GetLocalFilePath("Darkness.db3");
        }

        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            await _dbService.CopyDatabaseIfNotExistsAsync("Darkness.db3");
            _database = new SQLiteAsyncConnection(_dbPath);
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<Character>();
            await _database.CreateTableAsync<Level>();
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            await InitializeAsync();
            int rowsAdded = await _database!.InsertAsync(user);
            return rowsAdded > 0;
        }

        public async Task<User?> GetUserAsync(string username, string password)
        {
            await InitializeAsync();
            return await _database!.Table<User>()
                .Where(u => u.Username == username && u.Password == password)
                .FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            await InitializeAsync();
            return await _database!.Table<User>().ToListAsync();
        }
    }
}
