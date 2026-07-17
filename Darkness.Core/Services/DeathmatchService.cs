using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class DeathmatchService : IDeathmatchService
    {
        private readonly ILiteDatabase _db;

        public DeathmatchService(ILiteDatabase db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns all deathmatch encounters ordered by Id, with IsUnlocked computed
        /// from the character's ClearedDeathmatchWaves. Encounters with no
        /// UnlockRequirement are always unlocked. Null character = wave 1 only unlocked.
        /// </summary>
        public Task<List<DeathmatchEncounter>> GetEncountersAsync(Character? character = null)
        {
            var col = _db.GetCollection<DeathmatchEncounter>("deathmatch_encounters");
            var encounters = col.FindAll().OrderBy(e => e.Id).ToList();

            var cleared = character?.ClearedDeathmatchWaves ?? new List<int>();

            foreach (var encounter in encounters)
            {
                encounter.IsUnlocked = encounter.UnlockRequirement == null
                    || cleared.Contains(encounter.UnlockRequirement.Value);
            }

            return Task.FromResult(encounters);
        }
    }
}