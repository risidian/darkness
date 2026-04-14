using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class CharacterService : ICharacterService
{
    private readonly LiteDatabase _db;

    public CharacterService(LiteDatabase db)
    {
        _db = db;
    }

    public bool SaveCharacter(Character character)
    {
        var col = _db.GetCollection<Character>("characters");
        return col.Upsert(character);
    }

    public Character? GetCharacterById(int characterId)
    {
        var col = _db.GetCollection<Character>("characters");
        return col.FindById(characterId);
    }

    public List<Character> GetCharactersForUser(int userId)
    {
        var col = _db.GetCollection<Character>("characters");
        return col.Find(c => c.UserId == userId).ToList();
    }
}
