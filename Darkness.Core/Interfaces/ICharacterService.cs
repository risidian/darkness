using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface ICharacterService
{
    bool SaveCharacter(Character character);
    Character? GetCharacterById(int characterId);
    List<Character> GetCharactersForUser(int userId);
}
