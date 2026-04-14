using Darkness.Core.Models;
using Darkness.Core.Services;
using LiteDB;
using Xunit;

namespace Darkness.Tests.Services;

public class CharacterServiceTests : IDisposable
{
    private readonly MemoryStream _ms = new();
    private readonly LiteDatabase _db;
    private readonly CharacterService _service;

    public CharacterServiceTests()
    {
        _db = new LiteDatabase(_ms);
        _service = new CharacterService(_db);
    }

    [Fact]
    public void SaveCharacter_InsertsNew()
    {
        var character = new Character { Name = "TestHero", UserId = 1 };
        var result = _service.SaveCharacter(character);
        Assert.True(result);
        Assert.True(character.Id > 0);
    }

    [Fact]
    public void GetCharacterById_ReturnsCorrect()
    {
        var character = new Character { Name = "TestHero", UserId = 1 };
        _service.SaveCharacter(character);
        var loaded = _service.GetCharacterById(character.Id);
        Assert.NotNull(loaded);
        Assert.Equal("TestHero", loaded.Name);
    }

    [Fact]
    public void GetCharacterById_ReturnsNullForMissing()
    {
        var loaded = _service.GetCharacterById(999);
        Assert.Null(loaded);
    }

    [Fact]
    public void GetCharactersForUser_FiltersCorrectly()
    {
        _service.SaveCharacter(new Character { Name = "A", UserId = 1 });
        _service.SaveCharacter(new Character { Name = "B", UserId = 1 });
        _service.SaveCharacter(new Character { Name = "C", UserId = 2 });
        var chars = _service.GetCharactersForUser(1);
        Assert.Equal(2, chars.Count);
        Assert.All(chars, c => Assert.Equal(1, c.UserId));
    }

    public void Dispose()
    {
        _db.Dispose();
        _ms.Dispose();
    }
}
