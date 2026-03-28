using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface IAllyService
    {
        Task<List<Ally>> GetAlliesForUserAsync(int userId);
        Task<bool> SendAllyRequestAsync(int userId, int allyUserId, string allyUsername);
        Task<bool> RespondToAllyRequestAsync(int allyId, bool accepted);
    }
}
