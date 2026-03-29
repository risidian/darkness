using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface IUserService
    {
        Task<bool> CreateUserAsync(User user);
        Task<User?> GetUserAsync(string username, string password);
        Task<User?> GetUserByIdAsync(int userId);
        Task<List<User>> GetAllUsersAsync();
        Task InitializeAsync();
    }
}
