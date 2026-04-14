using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class UserService : IUserService
{
    private readonly LiteDatabase _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public UserService(LiteDatabase db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            var col = _db.GetCollection<User>("users");
            col.EnsureIndex(u => u.Username, unique: true);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<bool> CreateUserAsync(User user)
    {
        await InitializeAsync();
        var col = _db.GetCollection<User>("users");
        var id = col.Insert(user);
        user.Id = id.AsInt32;
        return true;
    }

    public async Task<User?> GetUserAsync(string username, string password)
    {
        await InitializeAsync();
        var col = _db.GetCollection<User>("users");
        return col.FindOne(u => u.Username == username && u.Password == password);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        await InitializeAsync();
        var col = _db.GetCollection<User>("users");
        return col.FindById(userId);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await InitializeAsync();
        var col = _db.GetCollection<User>("users");
        return col.FindAll().ToList();
    }
}
