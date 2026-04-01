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

            System.Console.WriteLine($"[UserService] Initializing database at path: {_dbPath}");
            try
            {
                System.Console.WriteLine("[UserService] Checking if database needs to be copied...");
                await _dbService.CopyDatabaseIfNotExistsAsync("Darkness.db3");
                System.Console.WriteLine("[UserService] Database copy/check completed.");

                System.Console.WriteLine("[UserService] Opening SQLite connection...");
                _database = new SQLiteAsyncConnection(_dbPath);
                System.Console.WriteLine("[UserService] SQLiteAsyncConnection created.");

                System.Console.WriteLine("[UserService] Creating tables (Async)...");
                await _database.CreateTableAsync<User>();
                System.Console.WriteLine("[UserService] Table 'User' verified.");
                await _database.CreateTableAsync<Character>();
                System.Console.WriteLine("[UserService] Table 'Character' verified.");
                await _database.CreateTableAsync<Level>();
                await _database.CreateTableAsync<Item>();
                await _database.CreateTableAsync<Skill>();
                await _database.CreateTableAsync<Enemy>();
                await _database.CreateTableAsync<StatusEffect>();
                System.Console.WriteLine("[UserService] All tables verified/created successfully.");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[UserService] ERROR during initialization: {ex.Message}");
                System.Console.WriteLine(ex.StackTrace);
                throw;
            }
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

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            await InitializeAsync();
            return await _database!.Table<User>()
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            await InitializeAsync();
            return await _database!.Table<User>().ToListAsync();
        }
    }
}
