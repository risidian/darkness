using Darkness.Core.Models;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface ISessionService
    {
        User? CurrentUser { get; set; }
        Task InitializeAsync();
    }
}
