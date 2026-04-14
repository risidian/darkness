using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Xunit;

namespace Darkness.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly MemoryStream _ms = new();
    private readonly LiteDatabase _db;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _db = new LiteDatabase(_ms);
        _service = new UserService(_db);
    }

    [Fact]
    public async Task CreateUser_AssignsId()
    {
        var user = new User { Username = "testuser", Password = "pass123" };
        var result = await _service.CreateUserAsync(user);
        Assert.True(result);
        Assert.True(user.Id > 0);
    }

    [Fact]
    public async Task GetUser_MatchesCredentials()
    {
        await _service.CreateUserAsync(new User { Username = "user1", Password = "pass1" });
        var found = await _service.GetUserAsync("user1", "pass1");
        Assert.NotNull(found);
        Assert.Equal("user1", found.Username);
    }

    [Fact]
    public async Task GetUser_WrongPassword_ReturnsNull()
    {
        await _service.CreateUserAsync(new User { Username = "user1", Password = "pass1" });
        var found = await _service.GetUserAsync("user1", "wrong");
        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAll()
    {
        await _service.CreateUserAsync(new User { Username = "u1", Password = "p" });
        await _service.CreateUserAsync(new User { Username = "u2", Password = "p" });
        var all = await _service.GetAllUsersAsync();
        Assert.Equal(2, all.Count);
    }

    public void Dispose()
    {
        _db.Dispose();
        _ms.Dispose();
    }
}
