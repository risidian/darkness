using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface IDeathmatchService
    {
        /// <summary>
        /// Returns all deathmatch encounters, with IsUnlocked set based on the character's cleared waves.
        /// Pass null to get all encounters with only wave 1 unlocked (no character context).
        /// </summary>
        Task<List<DeathmatchEncounter>> GetEncountersAsync(Character? character = null);
    }
}