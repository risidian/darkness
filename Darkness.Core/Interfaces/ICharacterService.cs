using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface ICharacterService
    {
        Task<bool> SaveCharacterAsync(Character character);
        Task<List<Character>> GetCharactersByUserIdAsync(int userId);
        Task<Character?> GetCharacterByIdAsync(int characterId);
    }
}
