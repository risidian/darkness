using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISettingsService _settingsService;
        private readonly IUserService _userService;
        private bool _isInitialized;

        public User? CurrentUser { get; set; }
        public Character? CurrentCharacter { get; set; }

        public SessionService(ISettingsService settingsService, IUserService userService)
        {
            _settingsService = settingsService;
            _userService = userService;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _settingsService.LoadSettingsAsync();
            if (_settingsService.LastUserId > 0)
            {
                CurrentUser = await _userService.GetUserByIdAsync(_settingsService.LastUserId);
            }
            _isInitialized = true;
        }
    }
}
