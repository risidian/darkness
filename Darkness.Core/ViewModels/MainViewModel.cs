using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;

namespace Darkness.Core.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IRewardService _rewardService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly ICharacterService _characterService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private bool _isDailyRewardVisible;

        [ObservableProperty]
        private string _rewardText = string.Empty;

        public MainViewModel(
            IRewardService rewardService,
            ISessionService sessionService,
            INavigationService navigationService,
            ICharacterService characterService,
            IDialogService dialogService)
        {
            _rewardService = rewardService;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _characterService = characterService;
            _dialogService = dialogService;
        }

        public async Task OnAppearingAsync()
        {
            if (await CheckForCharacterAsync())
            {
                await CheckDailyRewardAsync();
            }
        }

        private async Task<bool> CheckForCharacterAsync()
        {
            if (_sessionService.CurrentUser == null)
            {
                await _navigationService.NavigateToAsync("///LoadUserPage");
                return false;
            }

            var characters = await _characterService.GetCharactersForUserAsync(_sessionService.CurrentUser.Id);
            if (characters == null || characters.Count == 0)
            {
                await _navigationService.NavigateToAsync("///CharacterGenPage");
                return false;
            }

            return true;
        }

        private async Task CheckDailyRewardAsync()
        {
            try
            {
                if (_sessionService.CurrentUser != null)
                {
                    var reward = await _rewardService.CheckDailyRewardAsync(_sessionService.CurrentUser);

                    if (reward != null)
                    {
                        IsDailyRewardVisible = true;
                        RewardText = $"You received: {reward.Name} - {reward.Description}";
                        
                        await _dialogService.DisplayAlertAsync("Daily Bonus!", $"You received a {reward.Name}!", "Excellent");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking daily reward: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LogoutAsync()
        {
            _sessionService.CurrentUser = null;
            await _navigationService.NavigateToAsync("///LoadUserPage");
        }

        // Placeholder commands for menu buttons
        [RelayCommand] 
        public async Task StorylineAsync() 
        {
            await _navigationService.NavigateToAsync("///GamePage");
        }

        [RelayCommand] public Task CharactersAsync() => _navigationService.NavigateToAsync("CharactersPage");
        [RelayCommand] public Task DeathmatchAsync() => _navigationService.NavigateToAsync("DeathmatchPage");
        [RelayCommand] public Task TrainingModeAsync() => _dialogService.DisplayAlertAsync("Mode", "Training Mode coming soon!", "OK");
        
        [RelayCommand] 
        public async Task PvpAsync() 
        {
            if (_sessionService.CurrentUser == null) return;
            var characters = await _characterService.GetCharactersForUserAsync(_sessionService.CurrentUser.Id);
            if (characters == null || characters.Count < 2)
            {
                await _dialogService.DisplayAlertAsync("PVP", "You need at least two characters to play PVP.", "OK");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "Mode", "PVP" },
                { "Player1", characters[0] },
                { "Player2", characters[1] }
            };

            await _navigationService.NavigateToAsync("GamePage", parameters);
        }

        [RelayCommand] public Task SiegeAsync() => _dialogService.DisplayAlertAsync("Mode", "Siege coming soon!", "OK");
        [RelayCommand] public Task AlliesAsync() => _navigationService.NavigateToAsync("AlliesPage");
        [RelayCommand] public Task ForgeAsync() => _navigationService.NavigateToAsync("ForgePage");
        
        [RelayCommand] 
        public async Task StudyAsync() 
        {
            if (_sessionService.CurrentUser == null) return;
            var characters = await _characterService.GetCharactersForUserAsync(_sessionService.CurrentUser.Id);
            if (characters == null || characters.Count == 0)
            {
                await _dialogService.DisplayAlertAsync("Study", "You need a character to study.", "OK");
                return;
            }

            await _navigationService.NavigateToAsync("StudyPage", new Dictionary<string, object>
            {
                { "Character", characters[0] }
            });
        }

        [RelayCommand] public Task SettingsAsync() => _navigationService.NavigateToAsync("SettingsPage");
    }
}
