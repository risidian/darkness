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
            await CheckForCharacterAsync();
            await CheckDailyRewardAsync();
        }

        private async Task CheckForCharacterAsync()
        {
            if (_sessionService.CurrentUser == null)
            {
                await _navigationService.NavigateToAsync("///LoadUserPage");
                return;
            }

            var characters = await _characterService.GetCharactersByUserIdAsync(_sessionService.CurrentUser.Id);
            if (characters == null || characters.Count == 0)
            {
                await _navigationService.NavigateToAsync("CharacterGenPage");
            }
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
        [RelayCommand] public Task StorylineAsync() => _dialogService.DisplayAlertAsync("Mode", "Storyline coming soon!", "OK");
        [RelayCommand] public Task CharactersAsync() => _dialogService.DisplayAlertAsync("Mode", "Characters coming soon!", "OK");
        [RelayCommand] public Task DeathmatchAsync() => _dialogService.DisplayAlertAsync("Mode", "Deathmatch coming soon!", "OK");
        [RelayCommand] public Task TrainingModeAsync() => _dialogService.DisplayAlertAsync("Mode", "Training Mode coming soon!", "OK");
        [RelayCommand] public Task PvpAsync() => _dialogService.DisplayAlertAsync("Mode", "Pvp coming soon!", "OK");
        [RelayCommand] public Task SiegeAsync() => _dialogService.DisplayAlertAsync("Mode", "Siege coming soon!", "OK");
        [RelayCommand] public Task AlliesAsync() => _dialogService.DisplayAlertAsync("Mode", "Allies coming soon!", "OK");
        [RelayCommand] public Task ForgeAsync() => _dialogService.DisplayAlertAsync("Mode", "Forge coming soon!", "OK");
        [RelayCommand] public Task StudyAsync() => _dialogService.DisplayAlertAsync("Mode", "Study coming soon!", "OK");
        [RelayCommand] public Task SettingsAsync() => _dialogService.DisplayAlertAsync("Mode", "Settings coming soon!", "OK");
    }
}
