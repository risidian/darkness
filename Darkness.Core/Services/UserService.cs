using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class UserService : IUserService
    {
        private readonly string _dbPath;
        private bool _initialized;

        public UserService(LocalDatabaseService dbService)
        {
            _dbPath = dbService.GetLocalFilePath("Darkness.db");
        }

        private LiteDatabase OpenDb() => new LiteDatabase(_dbPath);

        public Task InitializeAsync()
        {
            if (_initialized)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                col.EnsureIndex(u => u.Username, unique: true);
                _initialized = true;
            });
        }

        public Task<bool> CreateUserAsync(User user)
        {
            return Task.Run(async () =>
            {
                await InitializeAsync();
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                var id = col.Insert(user);
                user.Id = id.AsInt32;
                return true;
            });
        }

        public Task<User?> GetUserAsync(string username, string password)
        {
            return Task.Run(async () =>
            {
                await InitializeAsync();
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                return col.FindOne(u => u.Username == username && u.Password == password);
            });
        }

        public Task<User?> GetUserByIdAsync(int userId)
        {
            return Task.Run(async () =>
            {
                await InitializeAsync();
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                return col.FindById(userId);
            });
        }

        public Task<List<User>> GetAllUsersAsync()
        {
            return Task.Run(async () =>
            {
                await InitializeAsync();
                using var db = OpenDb();
                var col = db.GetCollection<User>("users");
                return col.FindAll().ToList();
            });
        }
    }
}
