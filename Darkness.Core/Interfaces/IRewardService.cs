using Darkness.Core.Models;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface IRewardService
    {
        Task<Item?> CheckDailyRewardAsync(User user);
    }
}