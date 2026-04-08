using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface IRewardService
    {
        Task<Item?> CheckDailyRewardAsync(User user);
        BattleRewardResult ProcessCombatRewards(Character character, List<Enemy> enemies);
    }
}