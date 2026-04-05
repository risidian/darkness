using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly LiteDatabase _db;

        public CharacterService(LiteDatabase db)
        {
            _db = db;
        }

        public Task<bool> SaveCharacterAsync(Character character)
        {
            return Task.Run(() =>
            {
                var col = _db.GetCollection<Character>("characters");
                col.EnsureIndex(c => c.UserId);
                return col.Upsert(character);
            });
        }

        public Task<Character?> GetCharacterByIdAsync(int characterId)
        {
            return Task.Run<Character?>(() =>
            {
                var col = _db.GetCollection<Character>("characters");
                return col.FindById(characterId);
            });
        }

        public Task<List<Character>> GetCharactersForUserAsync(int userId)
        {
            return Task.Run(() =>
            {
                var col = _db.GetCollection<Character>("characters");
                col.EnsureIndex(c => c.UserId);
                return col.Find(c => c.UserId == userId).ToList();
            });
        }
    }
}