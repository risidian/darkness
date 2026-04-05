using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface IDeathmatchService
    {
        Task<List<DeathmatchEncounter>> GetEncountersAsync();
    }
}