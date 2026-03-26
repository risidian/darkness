using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services
{
    public class SessionService : ISessionService
    {
        public User? CurrentUser { get; set; }
    }
}
