using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Darkness.Core.Services
{
    public class AllyService : IAllyService
    {
        private readonly List<Ally> _mockAllies;

        public AllyService()
        {
            _mockAllies = new List<Ally>
            {
                new Ally { Id = 1, UserId = 1, AllyUserId = 2, AllyUsername = "Friend1", Status = "Accepted" },
                new Ally { Id = 2, UserId = 1, AllyUserId = 3, AllyUsername = "Friend2", Status = "Pending" }
            };
        }

        public async Task<List<Ally>> GetAlliesForUserAsync(int userId)
        {
            await Task.Delay(100); // Simulate network/DB latency
            return _mockAllies.Where(a => a.UserId == userId).ToList();
        }

        public async Task<bool> SendAllyRequestAsync(int userId, int allyUserId, string allyUsername)
        {
            await Task.Delay(100);
            _mockAllies.Add(new Ally
            {
                Id = _mockAllies.Any() ? _mockAllies.Max(a => a.Id) + 1 : 1,
                UserId = userId,
                AllyUserId = allyUserId,
                AllyUsername = allyUsername,
                Status = "Pending"
            });
            return true;
        }

        public async Task<bool> RespondToAllyRequestAsync(int allyId, bool accepted)
        {
            await Task.Delay(100);
            var ally = _mockAllies.FirstOrDefault(a => a.Id == allyId);
            if (ally != null)
            {
                if (accepted)
                {
                    ally.Status = "Accepted";
                }
                else
                {
                    _mockAllies.Remove(ally);
                }

                return true;
            }

            return false;
        }
    }
}