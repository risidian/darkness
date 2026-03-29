using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISessionService
    {
        User? CurrentUser { get; set; }
        Task InitializeAsync();
    }
}
