using Darkness.Core.Models;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface ISessionService
    {
        User? CurrentUser { get; set; }
        Character? CurrentCharacter { get; set; }
        Task InitializeAsync();
    }
}
